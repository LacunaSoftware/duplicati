using System;
using System.Collections.Generic;
using System.IO;
using Duplicati.Library.Backend.AzureBlob;
using Duplicati.Library.Interface;
using Duplicati.Library.Utility;

namespace Duplicati.Library.Backend.ENotariado
{
    public class ENotariadoBackend : IStreamingBackend
    {
        private readonly AzureBlobWrapper _azureBlob;
        private static Guid ApplicationId;
        private static string CertificateThumbprint;

        public ENotariadoBackend()
        {
        }

        public ENotariadoBackend(string url, Dictionary<string, string> options)
        {
            var uri = new Utility.Uri(url);
            uri.RequireHost();
            string containerName = uri.Host.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(CertificateThumbprint))
            {
                throw new UserInformationException(Strings.ENotariadoBackend.NoCertificateThumbprint, "ENotariadoNoCertificate");
            }
            if (ApplicationId == Guid.Empty)
            {
                throw new UserInformationException(Strings.ENotariadoBackend.NoApplicationId, "ENotariadoNoApplicationId");
            }

            string sasToken;
            // sasToken = getSASToken();

            // _azureBlob = new AzureBlobWrapper(sasToken, ontainerName);
        }

        public static void InitENotariadoData(Guid applicationId, string certThumbprint)
        {
            ApplicationId = applicationId;
            CertificateThumbprint = certThumbprint;
        }

        public string DisplayName
        {
            get { return Strings.ENotariadoBackend.DisplayName; }
        }

        public string ProtocolKey
        {
            get { return "enotariado"; }
        }

        public bool SupportsStreaming
        {
            get { return true; }
        }

        public IEnumerable<IFileEntry> List()
        {
            return _azureBlob.ListContainerEntries();
        }

        public void Put(string remotename, string localname)
        {
            using (var fs = File.Open(localname,
                FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Put(remotename, fs);
            }
        }

        public void Put(string remotename, Stream input)
        {
            _azureBlob.AddFileStream(remotename, input);
        }

        public void Get(string remotename, string localname)
        {
            using (var fs = File.Open(localname,
                FileMode.Create, FileAccess.Write,
                FileShare.None))
            {
                Get(remotename, fs);
            }
        }

        public void Get(string remotename, Stream output)
        {
            _azureBlob.GetFileStream(remotename, output);
        }

        public void Delete(string remotename)
        {
            _azureBlob.DeleteObject(remotename);
        }

        public IList<ICommandLineArgument> SupportedCommands
        {
            get
            {
                return new List<ICommandLineArgument>(new ICommandLineArgument[] {
                });

            }
        }

        public string Description
        {
            get
            {
                return Strings.ENotariadoBackend.Description_v2;
            }
        }

        public string[] DNSName
        {
            get { return _azureBlob.DnsNames; }
        }

        public void Test()
        {
            this.TestList();
        }

        public void CreateFolder()
        {
            _azureBlob.AddContainer();
        }

        public void Dispose()
        {

        }
    }
}
