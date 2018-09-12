using Duplicati.Library.Backend.AzureBlob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.Library.ENotariado
{
    public class Utils
    {
        public static bool ContainerAlreadyExists(string targetURL)
        {
            var uri = new Utility.Uri(targetURL);
            uri.RequireHost();
            var containerName = uri.Host.ToLowerInvariant();
            var sasToken = ENotariadoConnection.GetSASToken().GetAwaiter().GetResult();
            var azureBlob = new AzureBlobWrapper(sasToken, containerName);
            return azureBlob.HasContainer();
        }

        public static bool IsENotariadoBackend(string targetURL)
        {
            if (!String.IsNullOrWhiteSpace(targetURL))
            {
                int charLocation = targetURL.IndexOf(":", StringComparison.Ordinal);

                if (charLocation > 0)
                {
                    return targetURL.Substring(0, charLocation) == "enotariado";
                }
            }

            return false;
        }

        public static void RenameAzureContainer(string oldTargetURL, string newTargetURL)
        {
            var uri = new Utility.Uri(oldTargetURL);
            var oldContainerName = uri.Host.ToLowerInvariant();
            uri = new Utility.Uri(newTargetURL);
            var newContainerName = uri.Host.ToLowerInvariant();


            var sasToken = ENotariadoConnection.GetSASToken().GetAwaiter().GetResult();
            AzureBlobWrapper.RenameContainer(sasToken, "duplicatitest", oldContainerName, newContainerName);

        }
    }
}
