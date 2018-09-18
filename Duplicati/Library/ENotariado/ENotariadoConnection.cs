using Duplicati.Library.ENotariado;
using Duplicati.Library.Localization.Short;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
        private static X509Certificate2 Certificate;
        private static HttpClient client = new HttpClient();
        public static Guid ApplicationId;
        public static Guid SubscriptionId;
        public static bool IsVerified;

        private static bool HasValidAuthToken
        {
            get { return !(string.IsNullOrWhiteSpace(SessionToken) || SessionTokenExpiration == null || SessionTokenExpiration < DateTime.Now); }
        }

        private static bool HasValidSASToken
        {
            get { return !(string.IsNullOrWhiteSpace(SASToken) || SASTokenExpiration == null || SASTokenExpiration < DateTime.Now); }
        }

        private static readonly string LOGTAG = "eNotariado Connection";
        private static readonly string PublicKeyAuthenticationSessionState = "X-Public-Key-Auth-Session-State";
        private static readonly string SubscriptionHeader = "X-Subscription";
        private static readonly string BaseURI = $"https://backup.e-notariado.org.br/api";

        /// <summary>
        /// Simple method to init data regarding the application
        /// To be used when making requests to eNotariado
        /// </summary>
        public static void Init(Guid applicationId, X509Certificate2 cert)
        {
            Certificate = cert;
            ApplicationId = applicationId;
        }

        /// <summary>
        /// Enrolls in the eNotariado server with a predefined certificate
        /// </summary>
        public static async Task<Guid> Enroll(X509Certificate2 cert)
        {
            var enrollment = new ApplicationEnrollRequest
            {
                Certificate = cert.RawData,
                Description = Environment.MachineName
            };

            Library.Logging.Log.WriteVerboseMessage(LOGTAG, "CertThumbprint", $"Thumbprint of certificate is {cert.Thumbprint}");

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
            if (Certificate == null || Guid.Empty == ApplicationId)
            {
                throw new ENotariadoNotInitializedException();
            }

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
                    SubscriptionId = (Guid) content.SubscriptionId;
                    return SubscriptionId;
                }
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
            {
                return SASToken;
            }

            if (!HasValidAuthToken)
            {
                await GetApplicationAuthToken();
            }

            var sasRequest = new SASRequestModel
            {
                AppKeyId = ApplicationId
            };

            var uri = $"{BaseURI}/sas";
            var jsonInString = JsonConvert.SerializeObject(sasRequest);

            var stringContent = new StringContent(jsonInString, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(uri, stringContent);
            var contentString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var content = JsonConvert.DeserializeObject<SASResponseModel>(contentString);
                SASToken = content.Token;
                var parsed = HttpUtility.ParseQueryString(SASToken);
                var sasExpiration = parsed["se"];
                SASTokenExpiration = DateTime.Parse(sasExpiration);
                return SASToken;
            }
            else
            {
                throw new FailedRequestException(string.Format(LC.L("Response code: '{0}'. Content: '{1}'."), response.StatusCode, contentString));
            }
        }

        /// <summary>
        /// Authentication flow in eNotariado servers
        /// </summary>
        private static async Task GetApplicationAuthToken()
        {
            if (Certificate == null || Guid.Empty == ApplicationId)
            {
                throw new ENotariadoNotInitializedException();
            }
        
            if (!IsVerified)
            {
                throw new ENotariadoNotVerifiedException();
            }

            var start = new StartPublicKeyAuthenticationRequest
            {
                ApplicationId = ApplicationId,
                CertificateThumbprint = Certificate.Thumbprint,
            };

            /* Performing the firts token request, in order to receive a challenge. */
            var uri = $"{BaseURI}/public-key-auth";
            var jsonInString = JsonConvert.SerializeObject(start);

            var startResponse = await client.PostAsync(uri, new StringContent(jsonInString, Encoding.UTF8, "application/json"));
            var contentString = await startResponse.Content.ReadAsStringAsync();

            if (!startResponse.IsSuccessStatusCode)
            {
                throw new FailedRequestException(string.Format(LC.L("Response code: '{0}'. Content: '{1}'."), startResponse.StatusCode, contentString));
            }

            /* Request successful, challenge received and session header stored. */
            var startContent = JsonConvert.DeserializeObject<StartPublicKeyAuthenticationResponse>(contentString);
            var session = startResponse.Headers.GetValues(PublicKeyAuthenticationSessionState).FirstOrDefault();

            var signature = CryptoUtils.SignDataWithCertificate(startContent.ToSignData, Certificate);
            var complete = new CompletePublicKeyAuthenticationRequest
            {
                Signature = signature
            };

            var jsonCompleteString = JsonConvert.SerializeObject(complete);

            var stringContent = new StringContent(jsonCompleteString, Encoding.UTF8, "application/json");
            stringContent.Headers.Add(PublicKeyAuthenticationSessionState, session);

            var completeResponse = await client.PostAsync($"{uri}/complete", stringContent);
            var completeContentString = await completeResponse.Content.ReadAsStringAsync();

            if (!completeResponse.IsSuccessStatusCode)
            {
                throw new FailedRequestException(string.Format(LC.L("Response code: '{0}'. Content: '{1}'."), completeResponse.StatusCode, contentString));
            }

            /*
            Token received.
            */
            var completeContent = JsonConvert.DeserializeObject<CompletePublicKeyAuthenticationResponse>(completeContentString);
            SessionToken = completeContent.AppToken;
            SessionTokenExpiration = DateTime.Now.AddMinutes(5);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("AppToken", completeContent.AppToken);
        }        

        public static List<Backend.AzureBlob.BackupData> GetStoredBackupNames()
        {
            if (!HasValidSASToken)
            {
                GetSASToken().GetAwaiter().GetResult();
            }

            var accountName = SubscriptionId.ToString().Replace("-", "").Substring(0, 24);
            return Backend.AzureBlob.AzureBlobWrapper.GetStoredBackups(accountName, SASToken);
        }
    }
}
