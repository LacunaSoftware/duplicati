using Duplicati.Library.ENotariado;
using Duplicati.Library.Localization.Short;
using Duplicati.Library.Utility;
using ENotariado.Backup.Api.ApplicationEnrollment;
using ENotariado.Backup.Api.DuplicatiLog;
using ENotariado.Backup.Api.PublicKeyAuthentication;
using ENotariado.Backup.Api.SAS;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Duplicati.Library.ENotariado
{
    public static class ENotariadoConnection
    {
        /// <summary>
        /// Session token used to authenticate any operations with e-notariado
        /// </summary>
        private static string SessionToken;

        /// <summary>
        /// Expiration date of the session token, if the current time is higher, a new token is necessary
        /// </summary>
        private static DateTime SessionTokenExpiration;

        /// <summary>
        /// Shared Access SIgnature Token used to authenticate with Azure and execute any remote operations
        /// regarding backups
        /// </summary>
        private static string SASToken;

        /// <summary>
        /// Expiration date of the SAS token, if the current time is higher, a new SAS is necessary
        /// </summary>
        private static DateTime SASTokenExpiration;

        /// <summary>
        /// Password used to encrypt backups, retrieved from e-notariado
        /// </summary>
        private static string BackupPassword;

        /// <summary>
        /// Certificate used to sign nonce, needed to get a SessionToken
        /// </summary>
        private static X509Certificate2 Certificate;

        /// <summary>
        /// Id of this application instance in e-notariado
        /// </summary>
        public static Guid ApplicationId;

        /// <summary>
        /// Id of the subscription in which this application is enrolled under
        /// Equivalent to the respective notary's office
        /// </summary>
        public static Guid SubscriptionId;

        /// <summary>
        /// True if this application instance is verified as a valid enrollment in e-notariado
        /// </summary>
        public static bool IsVerified;

        /// <summary>
        /// HttpClient used to communicate with e-notariado's API
        /// </summary>
        private static HttpClient client = new HttpClient();

        // Variables related to periodically sending logs to e-notariado

        /// <summary>
        /// Concurrent Queue of all incoming logs that are to be sent to e-notariado
        /// </summary>
        private static ConcurrentQueue<DuplicatiLogModel> LogQueue = new ConcurrentQueue<DuplicatiLogModel>();

        /// <summary>
        /// Minimal waiting time before sending more logs to e-notariado
        /// </summary>
        private static readonly long MIN_TIMER_PERIOD = 10000; // 10 seconds

        /// <summary>
        /// Maximum waiting time before sending more logs to e-notariado
        /// Reached after multiple failures in previous sends
        /// </summary>
        private static readonly long MAX_TIMER_PERIOD = 600000; // 10 minutes

        /// <summary>
        /// Timer used to constantly call SendLogs()
        /// </summary>
        private static Timer Timer;
        private static long TimerPeriod;

        /// <summary>
        /// Checks whether the value of SessionToken is set and not expired
        /// </summary>
        private static bool HasValidAuthToken
        {
            get { return !(string.IsNullOrWhiteSpace(SessionToken) || SessionTokenExpiration < DateTime.Now); }
        }

        /// <summary>
        /// Checks whether the value of SASToken is set and not expired
        /// </summary>
        private static bool HasValidSASToken
        {
            get { return !(string.IsNullOrWhiteSpace(SASToken) || SASTokenExpiration < DateTime.Now); }
        }

        /// <summary>
        /// Azure Account Name to connect to Azure
        /// </summary>
        public static string AzureAccountName
        {
            get { return SubscriptionId.ToString().Replace("-", "").Substring(0, 24); }
        }

        /// <summary>
        /// Tag used to log e-notariado related operations
        /// </summary>
        private static readonly string LOGTAG = "e-notariado Connection";

        /// <summary>
        /// Header used when authenticating in e-notariado and retrieving a session token
        /// </summary>
        private static readonly string PublicKeyAuthenticationSessionState = "X-Public-Key-Auth-Session-State";

        /// <summary>
        /// e-notariado API's base URI
        /// </summary>
        private static readonly string BaseURI = $"https://backup.e-notariado.org.br/api";

        /// <summary>
        /// Resets all properties to their default values
        /// </summary>
        public static void ResetData()
        {
            Logging.Log.WriteVerboseMessage(LOGTAG, "ResetData", $"Resetting all e-notariado related data");

            SessionToken = null;
            SessionTokenExpiration = DateTime.MinValue;
            SASToken = null;
            SASTokenExpiration = DateTime.MinValue;
            ApplicationId = Guid.Empty;
            SubscriptionId = Guid.Empty;
            IsVerified = false;
            BackupPassword = null;
        }

        /// <summary>
        /// Simple method to init data regarding the application
        /// To be used when making requests to e-notariado
        /// </summary>
        /// <param name="applicationId">Enrollment Id of the application</param>
        /// <param name="cert">Certificate used in the enrollment</param>
        /// <param name="isVerified">Whether the application is already enrolled and verified</param>
        /// <param name="subscriptionId">Subscription (Notary`s office) ID in which the application is enrolled</param>
        public static void Init(Guid applicationId, X509Certificate2 cert, bool isVerified = false, Guid subscriptionId = new Guid() /* new Guid() creates Guid.Empty */)
        {
            ResetData();
            Logging.Log.WriteVerboseMessage(LOGTAG, "Init", $"Initializing e-notariado configuration. Certificate Thumbprint: {cert.Thumbprint}. ApplicationId: {applicationId}");

            Certificate = cert;
            ApplicationId = applicationId;
            SubscriptionId = subscriptionId;
            IsVerified = isVerified;

            // Send logs to e-notariado each 10 seconds
            TimerPeriod = MIN_TIMER_PERIOD;
            Timer = new Timer(_ => _ = SendLogs(), null, 10000, TimerPeriod);
        }

        /// <summary>
        /// Enrolls in the e-notariado server with predefined certificate. If application id and accessTicket are provided,
        /// it just confirms a pre-approved enrollment
        /// </summary>
        /// <param name="cert">Certificate used in enrollment and future authentications</param>
        /// <param name="applicationId">Application Id received from e-notariado in the pre-approve phase</param>
        /// <param name="accessTicket">Access ticket received from e-notariado in the pre-approve phase</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the Application Id after enrollment
        /// </returns>
        public static async Task<Guid> Enrollment(X509Certificate2 cert, string applicationId = null, string accessTicket = null)
        {
            ResetData();
            var enrollment = new ApplicationEnrollRequest
            {
                Certificate = cert.RawData,
                Description = Environment.MachineName
            };

            Logging.Log.WriteVerboseMessage(LOGTAG, "Enrollment", $"Enrolling in e-notariado with certificate {cert.Thumbprint}");

            string uri;
            if (applicationId == null && accessTicket == null)
            {
                uri = $"{BaseURI}/app-enrollments";
            }
            else
            {
                /// At this point, the application is already pre-enrolled in e-notariado after responding to an API
                /// request with a small image. WHen the e-notariado receives the image, it pre-enrolls the application
                /// and calls the API again with defined applicationId and accessTicket, used then here to confirm the
                /// enrollment
                uri = $"{BaseURI}/app-enrollments/pre-approved/{applicationId}?access_ticket={accessTicket}";
            }

            var jsonInString = JsonConvert.SerializeObject(enrollment);

            var response = await client.PostAsync(uri, new StringContent(jsonInString, Encoding.UTF8, "application/json"));
            var contentString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var content = JsonConvert.DeserializeObject<ApplicationEnrollResponse>(contentString);
                Library.Logging.Log.WriteVerboseMessage(LOGTAG, "EnrollSuccess", $"Enrollment was made with success. Application Id: {content.Id.ToString()}");
                return content.Id;
            }
            else
            {
                throw new FailedRequestException(string.Format(LC.L("Response code: '{0}'. Content: '{1}'."), response.StatusCode, contentString));
            }
        }

        /// <summary>
        /// Asks the e-notariado server if this application's enrollment has already been verified
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the GUID of the Subscription (Notary's office)
        /// </returns>
        public static async Task<Guid> CheckVerifiedStatus()
        {
            // Checks whether the current configuration is valid
            if (Certificate == null || ApplicationId == Guid.Empty)
                throw new ENotariadoNotInitializedException();

            Logging.Log.WriteVerboseMessage(LOGTAG, "CheckVerifiedStatus", $"Verifying application in e-notariado");

            var uri = $"{BaseURI}/app-enrollments";
            var id = ApplicationId.ToString();

            var response = await client.GetAsync($"{uri}/{id}/status");
            var contentString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var content = JsonConvert.DeserializeObject<ApplicationEnrollmentStatusQueryResponse>(contentString);
                IsVerified = content.Approved && content.SubscriptionId != null;

                if (IsVerified)
                {
                    Logging.Log.WriteVerboseMessage(LOGTAG, "Verified", $"Application is verified in e-notariado");
                    SubscriptionId = (Guid) content.SubscriptionId;
                    return SubscriptionId;
                }

                Logging.Log.WriteVerboseMessage(LOGTAG, "NotVerified", $"Application is not verified in e-notariado");
                return Guid.Empty;
            }
            else
            {
                throw new FailedRequestException(string.Format(LC.L("Response code: '{0}'. Content: '{1}'."), response.StatusCode, contentString));
            }
        }

        /// <summary>
        /// Asks the e-notariado server for a SAS token to access Azure
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the SAS token
        /// </returns>
        public static async Task<string> GetSASToken()
        {
            if (HasValidSASToken)
                return SASToken;

            // Current SAS Token is not valid, verifying whether we have a valid session token to proceed
            if (!HasValidAuthToken)
                await GetApplicationAuthToken();

            Logging.Log.WriteVerboseMessage(LOGTAG, "GetSASToken", $"Requesting SAS Token to e-notariado");

            // Calling GetCredentials to retrieve a new SAS Token
            await GetCredentials();
            return SASToken;
        }

        /// <summary>
        /// Asks the e-notariado server for the password to encrypt the backups
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the backup password
        /// </returns>
        public static async Task<string> GetBackupPassword()
        {
            if (!string.IsNullOrWhiteSpace(BackupPassword))
                return BackupPassword;

            // Current BackupPassword is not valid, verifying whether we have a valid session token to proceed
            if (!HasValidAuthToken)
                await GetApplicationAuthToken();

            Logging.Log.WriteVerboseMessage(LOGTAG, "GetBackupPassword", $"Requesting backup password to e-notariado");

            // Calling GetCredentials to retrieve the Backup Password
            await GetCredentials();
            return BackupPassword;
        }

        /// <summary>
        /// Asks the e-notariado server for the credentials of this application
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public static async Task GetCredentials()
        {
            if (!HasValidAuthToken)
                await GetApplicationAuthToken();

            var sasRequest = new SASRequestModel { AppKeyId = ApplicationId };

            var uri = $"{BaseURI}/credentials";
            var jsonInString = JsonConvert.SerializeObject(sasRequest);

            Logging.Log.WriteVerboseMessage(LOGTAG, "GetCredentials", $"Requesting credentials from e-notariado");

            var stringContent = new StringContent(jsonInString, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(uri, stringContent);
            var contentString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var content = JsonConvert.DeserializeObject<SASResponseModel>(contentString);

                SASToken = content.Token;
                BackupPassword = content.BackupPassword;

                var parsed = HttpUtility.ParseQueryString(SASToken);
                var sasExpiration = parsed["se"];
                SASTokenExpiration = DateTime.Parse(sasExpiration);
            }
            else
            {
                throw new FailedRequestException(string.Format(LC.L("Response code: '{0}'. Content: '{1}'."), response.StatusCode, contentString));
            }
        }

        /// <summary>
        /// Add log to queue in order to later send them to e-notariado servers
        /// </summary>
        /// <param name="logId">The ID of the log message</param>
        /// <param name="ts">Timestamp of the log message</param>
        /// <param name="message">The message of the log</param>
        /// <param name="exception">The exception the caused the log message, if it exists</param>
        /// <param name="logType">The level of the log message</param>
        /// <param name="backupTargetURL">The target URL of the backup that is related to this log message, if it exists</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task QueueLog(long logId, DateTime ts, string message, string exception, string logType, string backupTargetURL)
        {
            // Avoids caller waiting for queue to be unlocked
            await Task.Run(() =>
            {
                var logRequest = new DuplicatiLogModel
                {
                    ApplicationLogId = logId,
                    DuplicatiTimestamp = ts,
                    Message = message,
                    Exception = exception,
                    LogType = logType,
                    ApplicationId = ApplicationId
                };

                if (!string.IsNullOrWhiteSpace(backupTargetURL))
                {
                    var backupUri = new Utility.Uri(backupTargetURL);
                    logRequest.BackupName = backupUri.QueryParameters["name"];

                    if (!string.IsNullOrWhiteSpace(backupUri.Host))
                        logRequest.BackupId = Guid.Parse(backupUri.Host.ToLowerInvariant());
                }

                LogQueue.Enqueue(logRequest);
            });
        }

        /// <summary>
        /// Sends logs to the e-notariado servers
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task SendLogs()
        {
            if (!HasValidAuthToken)
                await GetApplicationAuthToken();

            // Empties current queue
            var logs = new List<DuplicatiLogModel>();
            var result = LogQueue.TryDequeue(out DuplicatiLogModel log); ;
            while (result)
            {
                logs.Add(log);
                result = LogQueue.TryDequeue(out log);
            }

            // If there are no logs, just return
            if (logs.Count == 0)
                return;
            
            var uri = $"{BaseURI}/duplicatilog";
            var jsonInString = JsonConvert.SerializeObject(logs);

            var response = await client.PostAsync(uri, new StringContent(jsonInString, Encoding.UTF8, "application/json"));
            var contentString = await response.Content.ReadAsStringAsync();

            // If the request fails, stop the timer to cancel any new calls to SendLogs
            // Halts the sending of logs until the current ones are successfully sent
            if (!response.IsSuccessStatusCode)
                Timer.Change(Timeout.Infinite, TimerPeriod);
            
            // Doubles waiting time up to MAX_TIMER_PERIOD milliseconds
            while (!response.IsSuccessStatusCode)
            {
                TimerPeriod = Math.Min(MAX_TIMER_PERIOD, TimerPeriod * 2);
                Library.Logging.Log.WriteRetryMessage(LOGTAG, "SendLogsRequest", null, $"Failed to send {logs.Count} logs to e-notariado, retrying in {TimerPeriod / 1000} seconds..." +
                    $"Error: {response.StatusCode} - Content: {contentString}");
                await Task.Delay(System.TimeSpan.FromMilliseconds(TimerPeriod));
                response = await client.PostAsync(uri, new StringContent(jsonInString, Encoding.UTF8, "application/json"));
            }

            // Response is successful, reset timer
            TimerPeriod = MIN_TIMER_PERIOD;
            Timer.Change(0, TimerPeriod);
        }

        /// <summary>
        /// Request a new session token from e-notariado
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private static async Task GetApplicationAuthToken()
        {
            if (Certificate == null || Guid.Empty == ApplicationId)
                throw new ENotariadoNotInitializedException();
        
            if (!IsVerified)
                throw new ENotariadoNotVerifiedException();

            var start = new StartPublicKeyAuthenticationRequest
            {
                ApplicationId = ApplicationId,
                CertificateThumbprint = Certificate.Thumbprint,
            };

            Logging.Log.WriteVerboseMessage(LOGTAG, "Authentication", $"Authenticating in e-notariado");

            /* Performing the first token request, in order to receive a challenge. */
            var uri = $"{BaseURI}/public-key-auth";
            var jsonInString = JsonConvert.SerializeObject(start);

            var startResponse = await client.PostAsync(uri, new StringContent(jsonInString, Encoding.UTF8, "application/json"));
            var contentString = await startResponse.Content.ReadAsStringAsync();

            if (!startResponse.IsSuccessStatusCode)
                throw new FailedRequestException(string.Format(LC.L("Response code: '{0}'. Content: '{1}'."), startResponse.StatusCode, contentString));

            /* Request successful, challenge received and session header stored. */
            var startContent = JsonConvert.DeserializeObject<StartPublicKeyAuthenticationResponse>(contentString);
            var session = startResponse.Headers.GetValues(PublicKeyAuthenticationSessionState).FirstOrDefault();

            var signature = CryptoUtils.SignDataWithCertificate(Certificate, startContent.ToSignData);
            var complete = new CompletePublicKeyAuthenticationRequest { Signature = signature };

            var jsonCompleteString = JsonConvert.SerializeObject(complete);

            var stringContent = new StringContent(jsonCompleteString, Encoding.UTF8, "application/json");
            stringContent.Headers.Add(PublicKeyAuthenticationSessionState, session);

            var completeResponse = await client.PostAsync($"{uri}/complete", stringContent);
            var completeContentString = await completeResponse.Content.ReadAsStringAsync();

            if (!completeResponse.IsSuccessStatusCode)
                throw new FailedRequestException(string.Format(LC.L("Response code: '{0}'. Content: '{1}'."), completeResponse.StatusCode, contentString));

            /*
            Token received.
            */
            var completeContent = JsonConvert.DeserializeObject<CompletePublicKeyAuthenticationResponse>(completeContentString);
            SessionToken = completeContent.AppToken;
            SessionTokenExpiration = DateTime.Now.AddMinutes(5);
            Logging.Log.WriteVerboseMessage(LOGTAG, "Authenticated", $"Authenticated in e-notariado");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("AppToken", completeContent.AppToken);
        }        

        /// <summary>
        /// Queries Azure to retrieve all containers and their metadata, the name of the backups stored in each one of them
        /// </summary>
        /// <returns>List of BackupData, a pair that contains the container name and the backup name</returns>
        public static List<Backend.AzureBlob.BackupData> GetStoredBackupNames()
        {
            if (!HasValidSASToken)
                GetSASToken().GetAwaiter().GetResult();

            Logging.Log.WriteVerboseMessage(LOGTAG, "GetStoredBackups", $"Retrieving all backups stored remotely");

            return Backend.AzureBlob.AzureBlobWrapper.GetStoredBackups(AzureAccountName, SASToken);
        }
    }
}
