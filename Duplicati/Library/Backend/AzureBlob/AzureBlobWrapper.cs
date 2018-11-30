#region Disclaimer / License
// Copyright (C) 2015, The Duplicati Team
// http://www.duplicati.com, info@duplicati.com
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// 
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Duplicati.Library.Common.IO;
using Duplicati.Library.Interface;
using Duplicati.Library.Utility;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Duplicati.Library.Backend.AzureBlob
{
    public class BackupData
    {
        public string ContainerName;
        public string BackupName;
    }

    /// <summary>
    /// Azure blob storage facade.
    /// </summary>
    public class AzureBlobWrapper
    {
        private readonly string _containerName;
        private readonly CloudBlobContainer _container;
        private static readonly string BACKUP_NAME = "backupName";

        public string[] DnsNames
        {
            get
            {
                var lst = new List<string>();
                if (_container != null)
                {
                    if (_container.Uri != null)
                        lst.Add(_container.Uri.Host);

                    if (_container.StorageUri != null)
                    {
                        if (_container.StorageUri.PrimaryUri != null)
                            lst.Add(_container.StorageUri.PrimaryUri.Host);
                        if (_container.StorageUri.SecondaryUri != null)
                            lst.Add(_container.StorageUri.SecondaryUri.Host);
                    }
                }

                return lst.ToArray();
            }
        }

        public AzureBlobWrapper(string accountName, string accessKey, string containerName)
        {
            _containerName = containerName;
            var connectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                accountName, accessKey);
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            _container = blobClient.GetContainerReference(containerName);
        }

        public AzureBlobWrapper(string accountName, string sasToken, string containerName, string backupName)
        {
            _containerName = containerName;
            var accountSAS = new StorageCredentials(sasToken);
            var storageAccount = new CloudStorageAccount(accountSAS, accountName, null, true);
            var blobClient = storageAccount.CreateCloudBlobClient();
            _container = blobClient.GetContainerReference(_containerName);
            AddContainerIfNotExists();
            var base64BackupName = Convert.ToBase64String(Encoding.UTF8.GetBytes(backupName));
            _container.Metadata[BACKUP_NAME] = base64BackupName;
            _container.SetMetadata();
        }

        public static List<BackupData> GetStoredBackups(string accountName, string sasToken)
        {
            var accountSAS = new StorageCredentials(sasToken);
            var storageAccount = new CloudStorageAccount(accountSAS, accountName, null, true);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var containers = blobClient.ListContainers();

            var backups = new List<BackupData>();
            
            foreach(var container in containers)
            {
                container.FetchAttributes();
                if (!container.Metadata.ContainsKey(BACKUP_NAME) || container.ListBlobs().Count() == 0)
                    continue;

                var backupName = container.Metadata[BACKUP_NAME];
                backupName = Encoding.UTF8.GetString(Convert.FromBase64String(backupName));
                backups.Add(new BackupData()
                {
                    ContainerName = container.Name,
                    BackupName = backupName
                });
            }
            return backups;
        }

        public void AddContainer()
        {
            _container.Create(BlobContainerPublicAccessType.Off);
        }

        public void AddContainerIfNotExists()
        {
            _container.CreateIfNotExists(BlobContainerPublicAccessType.Off);
        }

        public virtual void GetFileStream(string keyName, Stream target)
        {
            _container.GetBlockBlobReference(keyName).DownloadToStream(target);
        }

        public void GetFileObject(string keyName, string localfile)
        {
            _container.GetBlockBlobReference(keyName).DownloadToFile(localfile, FileMode.Create);
        }

        public void AddFileObject(string keyName, string localfile)
        {
            using (var fs = File.Open(localfile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                AddFileStream(keyName, fs);
            }
        }

        public virtual void AddFileStream(string keyName, Stream source)
        {
            _container.GetBlockBlobReference(keyName).UploadFromStream(source);
        }

        public void DeleteObject(string keyName)
        {
            _container.GetBlockBlobReference(keyName).DeleteIfExists();
        }

        public virtual List<IFileEntry> ListContainerEntries()
        {
            var listBlobItems = _container.ListBlobs(blobListingDetails: BlobListingDetails.Metadata);
            try
            {
                return listBlobItems.Select(x =>
                {
                    var absolutePath = x.StorageUri.PrimaryUri.AbsolutePath;
                    var containerSegment = string.Concat("/", _containerName, "/");
                    var blobName = absolutePath.Substring(absolutePath.IndexOf(
                        containerSegment, System.StringComparison.Ordinal) + containerSegment.Length);

                    try
                    {
                        if (x is CloudBlockBlob)
                        {
                            var cb = (CloudBlockBlob)x;
                            var lastModified = new System.DateTime();
                            if (cb.Properties.LastModified != null)
                                lastModified = new System.DateTime(cb.Properties.LastModified.Value.Ticks, System.DateTimeKind.Utc);
                            return new FileEntry(Utility.Uri.UrlDecode(blobName.Replace("+", "%2B")), cb.Properties.Length, lastModified, lastModified);
                        }
                    }
                    catch
                    { 
                        // If the metadata fails to parse, return the basic entry
                    }

                    return new FileEntry(Utility.Uri.UrlDecode(blobName.Replace("+", "%2B")));
                })
                .Cast<IFileEntry>()
                .ToList();
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404)
                {
                    throw new FolderMissingException(ex);
                }
                throw;
            }
        }
    }
}
