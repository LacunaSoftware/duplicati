using Duplicati.Library.ENotariado;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.Library.ENotariado
{
    public class ENotariadoConnection
    {
        private static string SessionToken;
        private static X509Certificate2 Certificate;
        private static Guid ApplicationId;
        private static bool IsVerified;
        private static readonly string LOGTAG = Library.Logging.Log.LogTagFromType<ENotariadoConnection>();
        
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

            using (var client = new HttpClient())
            {
                var uri = $"https://localhost:44392/api/app-enrollments";
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
                    throw new FailedEnrollmentException($"Error!Response code: '{response.StatusCode}'.Content: '{contentString}'.");
                }
            }
        }

        /// <summary>
        /// Asks the eNotariado server if this application's enrollment has already been verified
        /// </summary>
        public static async Task<bool> CheckVerifiedStatus()
        {
            if (Certificate == null || Guid.Empty == ApplicationId)
            {
                throw new ENotariadoNotInitialized("ENotariado não foi inicializado corretamente");
            }

            using (var client = new HttpClient())
            {
                var uri = $"https://localhost:44392/api/app-enrollments";
                var id = ApplicationId.ToString();

                var response = await client.GetAsync($"{uri}/{id}/status");
                var contentString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var content = JsonConvert.DeserializeObject<ApplicationEnrollmentStatusQueryResponse>(contentString);
                    IsVerified = content.Approved;
                    return content.Approved;
                }
                else
                {
                    throw new FailedEnrollmentException("Não foi possível verificar a resposta com o servidor");
                }
            }
        }

        /*
        public static string GetSASToken(Guid applicationId, string certThumbprint)
        {
        }
        */
    }
}
