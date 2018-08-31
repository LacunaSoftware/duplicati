﻿using Duplicati.Library.ENotariado;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
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
        private static readonly string PublicKeyAuthenticationSessionState = "X-Public-Key-Auth-Session-State";
        private static readonly string SubscriptionHeader = "X-Subscription";
        private static readonly string BaseURI = $"https://localhost:44392/api";

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
                var uri = $"{BaseURI}/app-enrollments";
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

        public static async Task<string> GetApplicationAuthToken()
        {
            var start = new StartPublicKeyAuthenticationRequest
            {
                ApplicationId = ApplicationId,
                CertificateThumbprint = Certificate.Thumbprint,
            };

            using (var client = new HttpClient())
            {

                /* Performing the firts token request, in order to receive a challenge. */
                var uri = $"{BaseURI}/public-key-auth";
                var jsonInString = JsonConvert.SerializeObject(start);

                var startResponse = await client.PostAsync(uri, new StringContent(jsonInString, Encoding.UTF8, "application/json"));
                var contentString = await startResponse.Content.ReadAsStringAsync();

                if (!startResponse.IsSuccessStatusCode)
                {
                    throw new FailedRequestException($"Error! Response code: '{startResponse.StatusCode}'. Content: '{contentString}'.");
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
                    throw new FailedRequestException($"Error! Response code: '{startResponse.StatusCode}'. Content: '{contentString}'.");
                }

                /*
                 Token received.
                 */
                var completeContent = JsonConvert.DeserializeObject<CompletePublicKeyAuthenticationResponse>(completeContentString);
                return completeContent.AppToken;
            }
        }

        /*
        public static string GetSASToken(Guid applicationId, string certThumbprint)
        {
        }
        */
    }
}
