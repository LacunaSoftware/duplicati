using Duplicati.Library.ENotariado;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.Library.ENotariado
{
    public class ENotariadoConnection
    {
#if DEBUG
        private const StoreLocation KeyStoreLocation = StoreLocation.CurrentUser;
#else
		private const StoreLocation KeyStoreLocation = StoreLocation.LocalMachine;
#endif

        private static string SessionToken;
        private static X509Certificate2 Certificate;
        private static Guid ApplicationId;
        

        public static void Start(Guid applicationId, X509Certificate2 certificate)
        {
            Certificate = certificate;
            ApplicationId = applicationId;

        }

        /*
        public static string GetSASToken(Guid applicationId, string certThumbprint)
        {
        }
        */
    }
}
