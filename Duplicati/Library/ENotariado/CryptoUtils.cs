using Duplicati.Library.Localization.Short;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Duplicati.Library.ENotariado
{
    public static class CryptoUtils
    {
        private const string ProviderName = "Microsoft Enhanced RSA and AES Cryptographic Provider";
        private const int ProviderType = 24; // PROV_RSA_AES
        private const string SignatureAlgorithmOid = "1.2.840.113549.1.1.11"; // SHA-256 with RSA
        private const string SignatureAlgorithmName = "SHA256";
        private const int KeySize = 4096;
        private static readonly bool isMono = Type.GetType("Mono.Runtime") != null;

        /// <summary>
        /// Creates a self-signed X509 certificate and stores it in the specified StoreLocation
        /// </summary>
        public static X509Certificate2 CreateSelfSignedCertificate(StoreLocation keyStoreLocation, string commonName = "localhost", bool allowExport = false)
        {
            if (isMono)
                allowExport = true;

            var keyName = Guid.NewGuid().ToString();
            var key = CreateKey(keyStoreLocation, keyName, allowExport);
            var cert = IssueSelfSignedCertificate(key, commonName);
            var certWithKey = ImportCertificate(cert, key, keyStoreLocation);
            return certWithKey;
        }
        
        /// <summary>
        /// Gets certificate with specified certThumbprint from the specified StoreLocation
        /// </summary>
        public static X509Certificate2 GetCertificate(StoreLocation keyStoreLocation, string certThumbprint)
        {
            X509Certificate2 cert;
            X509Store store = new X509Store(StoreName.My, keyStoreLocation);
            store.Open(OpenFlags.ReadOnly);
            try
            {
                var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, certThumbprint, false);
                if (certCollection.Count == 0)
                {
                    throw new CertificateNotFoundException(certThumbprint);
                }
                cert = certCollection[0];
            }
            finally
            {
                store.Close();
            }
            return cert;
        }

        /// <summary>
        /// Signs byte array using the provided certificate's private key.
        /// </summary>
        public static byte[] SignDataWithCertificate(byte[] data, X509Certificate2 cert)
        {
            return ((RSACryptoServiceProvider) cert.PrivateKey).SignData(data, SignatureAlgorithmName);
        }
        
        private static RSACryptoServiceProvider CreateKey(StoreLocation keyStoreLocation, string keyName, bool allowExport)
        {

            var cspParameters = new CspParameters()
            {
                ProviderName = ProviderName,
                ProviderType = ProviderType,
                KeyContainerName = keyName,
                KeyNumber = (int)KeyNumber.Signature,
                Flags = CspProviderFlags.NoFlags,
            };
            if (keyStoreLocation == StoreLocation.LocalMachine)
            {
                cspParameters.Flags |= CspProviderFlags.UseMachineKeyStore;
            }
            if (!allowExport)
            {
                cspParameters.Flags |= CspProviderFlags.UseNonExportableKey;
            }

            var rsa = new RSACryptoServiceProvider(KeySize, cspParameters);

            // The implementation below uses CNG instead of CAPI, which may be preferable since it is the modern way. However,
            // the method importCertificate() below will fail for .NET Frameworks 4.6-4.7.1 if this implementation is used.
            // Since .NET Framework 4.7.2 is fairly recent as of this date (Apr/2018), we're choosing the CAPI implementation.
            /*
			CngKeyCreationParameters keyCreationParams = new CngKeyCreationParameters() {
				Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
				KeyUsage = CngKeyUsages.Signing,
				ExportPolicy = allowExport ? CngExportPolicies.AllowExport : CngExportPolicies.None,
				KeyCreationOptions = keyStoreLocation == StoreLocation.LocalMachine ? CngKeyCreationOptions.MachineKey : CngKeyCreationOptions.None,
			};
			keyCreationParams.Parameters.Add(new CngProperty(CngLengthProperty, BitConverter.GetBytes(KeySize), CngPropertyOptions.None));
			CngKey key = CngKey.Create(CngAlgorithm.Rsa, keyName, keyCreationParams);
			var rsa = new RSACng(key);
			*/

            return rsa;
        }

        private static X509Certificate2 IssueSelfSignedCertificate(RSACryptoServiceProvider rsa, string commonName)
        {

            var publicParams = rsa.ExportParameters(false);
            var signatureAlgIdentifier = new AlgorithmIdentifier(new DerObjectIdentifier(SignatureAlgorithmOid), DerNull.Instance);
            var subjectName = new X509Name($"CN={commonName}", new X509DefaultEntryConverter());

            var certGen = new V3TbsCertificateGenerator();
            certGen.SetIssuer(subjectName);
            certGen.SetSubject(subjectName);
            certGen.SetSerialNumber(new DerInteger(new Org.BouncyCastle.Math.BigInteger(1, Guid.NewGuid().ToByteArray())));
            certGen.SetStartDate(new Time(DateTime.UtcNow));
            certGen.SetEndDate(new Time(DateTime.UtcNow.AddYears(10)));
            certGen.SetSignature(signatureAlgIdentifier);
            certGen.SetSubjectPublicKeyInfo(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(new RsaKeyParameters(
                false,
                new Org.BouncyCastle.Math.BigInteger(1, publicParams.Modulus),
                new Org.BouncyCastle.Math.BigInteger(1, publicParams.Exponent)
            )));

            var tbsCert = certGen.GenerateTbsCertificate();
            var signature = rsa.SignData(tbsCert.GetDerEncoded(), SignatureAlgorithmName);
            var certEncoded = new X509CertificateStructure(tbsCert, signatureAlgIdentifier, new DerBitString(signature)).GetDerEncoded();
            var cert = new X509Certificate2(certEncoded);

            return cert;
        }

        private static X509Certificate2 ImportCertificate(X509Certificate2 cert, RSA rsa, StoreLocation keyStoreLocation)
        {

            // Associate the key with the certificate. On .NET Frameworks starting on 4.7.2, as well as on .NET Standard/Core,
            // this should be done with the CopyWithPrivateKey() extension method. On .NET Frameworks 4.6-4.7.1, however,
            // that method is not available, so we must perform the operation with the old syntax. Note that the old syntax
            // itself cannot be used indistinctly because on .NET Core it throws a PlatformNotSupportedException.

            X509Certificate2 certWithKey;

            certWithKey = new X509Certificate2(cert) { PrivateKey = rsa };

            // Add the certificate with associated key to the operating system key store

            X509Store store = new X509Store(StoreName.My, keyStoreLocation);
            store.Open(OpenFlags.ReadWrite);
            try
            {
                store.Add(certWithKey);
            }
            finally
            {
                store.Close();
            }

            return certWithKey;
        }
    }
}