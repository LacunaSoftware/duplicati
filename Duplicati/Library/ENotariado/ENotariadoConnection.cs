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
        private static string SessionToken;
        private static DateTime SessionTokenExpiration;
        private static string SASToken;
        private static DateTime SASTokenExpiration;
        private static string BackupPassword;

        private static X509Certificate2 Certificate;
        public static Guid ApplicationId;
        public static Guid SubscriptionId;
        public static bool IsVerified;

        private static HttpClient client = new HttpClient();

        // Variables related to periodically sending logs to e-Notariado
        private static ConcurrentQueue<DuplicatiLogModel> LogQueue = new ConcurrentQueue<DuplicatiLogModel>();
        private static readonly long MIN_TIMER_PERIOD = 10000;
        private static readonly long MAX_TIMER_PERIOD = 600000; // 10 minutes
        private static Timer Timer;
        private static long TimerPeriod;

        private static bool HasValidAuthToken
        {
            get { return !(string.IsNullOrWhiteSpace(SessionToken) || SessionTokenExpiration < DateTime.Now); }
        }

        private static bool HasValidSASToken
        {
            get { return !(string.IsNullOrWhiteSpace(SASToken) || SASTokenExpiration < DateTime.Now); }
        }

        public static string AzureAccountName
        {
            get { return SubscriptionId.ToString().Replace("-", "").Substring(0, 24); }
        }

        private static readonly string LOGTAG = "eNotariado Connection";
        private static readonly string PublicKeyAuthenticationSessionState = "X-Public-Key-Auth-Session-State";
        private static readonly string BaseURI = $"https://backup.e-notariado.org.br/api";

        private static void ResetData()
        {
            Logging.Log.WriteVerboseMessage(LOGTAG, "ResetData", $"Resetting all e-Notariado related data");

            SessionToken = null;
            SessionTokenExpiration = DateTime.MinValue;
            SASToken = null;
            SASTokenExpiration = DateTime.MinValue;
            ApplicationId = Guid.Empty;
            SubscriptionId = Guid.Empty;
            IsVerified = false;
        }

        /// <summary>
        /// Simple method to init data regarding the application
        /// To be used when making requests to eNotariado
        /// </summary>
        public static void Init(Guid applicationId, X509Certificate2 cert)
        {
            Logging.Log.WriteVerboseMessage(LOGTAG, "Init", $"Initializing e-Notariado configuration. Certificate Thumbprint: {cert.Thumbprint}. ApplicationId: {applicationId}");

            Certificate = cert;
            ApplicationId = applicationId;

            // Send logs to e-Notariado each 10 seconds
            TimerPeriod = MIN_TIMER_PERIOD;
            Timer = new Timer(_ => _ = SendLogs(), null, 10000, TimerPeriod);
        }

        /// <summary>
        /// Enrolls in the eNotariado server with a predefined certificate
        /// </summary>
        public static async Task<Guid> Enroll(X509Certificate2 cert)
        {
            ResetData();
            var enrollment = new ApplicationEnrollRequest
            {
                Certificate = cert.RawData,
                Description = Environment.MachineName
            };

            Logging.Log.WriteVerboseMessage(LOGTAG, "Enroll", $"Enrolling in e-Notariado with certificate {cert.Thumbprint}");

            var uri = $"{BaseURI}/app-enrollments";
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
        /// Asks the eNotariado server if this application's enrollment has already been verified
        /// </summary>
        public static async Task<Guid> CheckVerifiedStatus()
        {
            if (Certificate == null || ApplicationId == Guid.Empty)
                throw new ENotariadoNotInitializedException();

            Logging.Log.WriteVerboseMessage(LOGTAG, "CheckVerifiedStatus", $"Verifying application in e-Notariado");

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
                    Logging.Log.WriteVerboseMessage(LOGTAG, "Verified", $"Application is verified in e-Notariado");
                    SubscriptionId = (Guid) content.SubscriptionId;
                    return SubscriptionId;
                }

                Logging.Log.WriteVerboseMessage(LOGTAG, "NotVerified", $"Application is not verified in e-Notariado");
                return Guid.Empty;
            }
            else
            {
                throw new FailedRequestException(string.Format(LC.L("Response code: '{0}'. Content: '{1}'."), response.StatusCode, contentString));
            }
        }

        /// <summary>
        /// Asks the eNotariado server for a SAS token to access Azure
        /// </summary>
        public static async Task<string> GetSASToken()
        {
            if (HasValidSASToken)
                return SASToken;

            if (!HasValidAuthToken)
                await GetApplicationAuthToken();

            Logging.Log.WriteVerboseMessage(LOGTAG, "GetSASToken", $"Requesting SAS Token to e-Notariado");

            await GetCredentials();
            return SASToken;
        }

        /// <summary>
        /// Asks the eNotariado server for the password to encrypt the backups
        /// </summary>
        public static async Task<string> GetBackupPassword()
        {
            return "abcdef";

            if (!string.IsNullOrWhiteSpace(BackupPassword))
                return BackupPassword;

            if (!HasValidAuthToken)
                await GetApplicationAuthToken();

            Logging.Log.WriteVerboseMessage(LOGTAG, "GetBackupPassword", $"Requesting backup password to e-Notariado");

            await GetCredentials();
            return BackupPassword;
        }

        public static async Task GetCredentials()
        {
            if (!HasValidAuthToken)
                await GetApplicationAuthToken();

            var sasRequest = new SASRequestModel { AppKeyId = ApplicationId };

            var uri = $"{BaseURI}/sas";
            var jsonInString = JsonConvert.SerializeObject(sasRequest);

            Logging.Log.WriteVerboseMessage(LOGTAG, "GetCredentials", $"Requesting credentials from e-Notariado");

            var stringContent = new StringContent(jsonInString, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(uri, stringContent);
            var contentString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var content = JsonConvert.DeserializeObject<SASResponseModel>(contentString);

                SASToken = content.Token;
                // BackupPassword = content.BackupPassword;

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
        /// Add log to queue in order to later send them to e-Notariado servers
        /// </summary>
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
        /// Sends logs to the e-Notariado servers
        /// </summary>
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
                Library.Logging.Log.WriteWarningMessage(LOGTAG, "SendLogsRequest", null, $"Failed to send {logs.Count} logs to e-Notariado, retrying in {TimerPeriod / 1000} seconds..." +
                    $"Error: {response.StatusCode} - Content: {contentString}");
                await Task.Delay(System.TimeSpan.FromMilliseconds(TimerPeriod));
                response = await client.PostAsync(uri, new StringContent(jsonInString, Encoding.UTF8, "application/json"));
            }

            // Response is successful, reset timer
            TimerPeriod = MIN_TIMER_PERIOD;
            Timer.Change(0, TimerPeriod);
        }

        /// <summary>
        /// Authentication flow in eNotariado servers
        /// </summary>
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

            Logging.Log.WriteVerboseMessage(LOGTAG, "Authentication", $"Authenticating in e-Notariado");

            /* Performing the firts token request, in order to receive a challenge. */
            var uri = $"{BaseURI}/public-key-auth";
            var jsonInString = JsonConvert.SerializeObject(start);

            var startResponse = await client.PostAsync(uri, new StringContent(jsonInString, Encoding.UTF8, "application/json"));
            var contentString = await startResponse.Content.ReadAsStringAsync();

            if (!startResponse.IsSuccessStatusCode)
                throw new FailedRequestException(string.Format(LC.L("Response code: '{0}'. Content: '{1}'."), startResponse.StatusCode, contentString));

            /* Request successful, challenge received and session header stored. */
            var startContent = JsonConvert.DeserializeObject<StartPublicKeyAuthenticationResponse>(contentString);
            var session = startResponse.Headers.GetValues(PublicKeyAuthenticationSessionState).FirstOrDefault();

            var signature = CryptoUtils.SignDataWithCertificate(startContent.ToSignData, Certificate);
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
            Logging.Log.WriteVerboseMessage(LOGTAG, "Authenticated", $"Authenticated in e-Notariado");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("AppToken", completeContent.AppToken);
        }        

        public static List<Backend.AzureBlob.BackupData> GetStoredBackupNames()
        {
            if (!HasValidSASToken)
                GetSASToken().GetAwaiter().GetResult();

            Logging.Log.WriteVerboseMessage(LOGTAG, "GetStoredBackups", $"Retrieving all backups stored remotely");

            return Backend.AzureBlob.AzureBlobWrapper.GetStoredBackups(AzureAccountName, SASToken);
        }
    }
}
