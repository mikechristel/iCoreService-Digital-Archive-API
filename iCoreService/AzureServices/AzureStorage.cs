using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;


namespace iCoreService.AzureServices
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// This class based heavily upon the code example provided in the following article:
    /// http://arcware.net/upload-and-download-files-with-web-api-and-azure-blob-storage/
    /// </remarks>
    public class AzureStorage
    {
        /// <summary>
        /// Helper function to instantiate and retrieve a blob container reference.
        /// </summary>
        /// <param name="containerName">The name of the biography or story container.</param>
        /// <returns>A CloudBlobContainer instance connected to the given container name.</returns>
        private static CloudBlobContainer GetBlobContainer(string containerName)
        {
            // Pull these from config
            var blobStorageConnectionString = ConfigurationManager.AppSettings["AzureStorageConnectionString"];

            // Create blob client and return reference to the container
            var blobStorageAccount = CloudStorageAccount.Parse(blobStorageConnectionString);
            var blobClient = blobStorageAccount.CreateCloudBlobClient();
            return blobClient.GetContainerReference(containerName);
        }

        /// <summary>
        /// Retrieve the JSON document containing the details for the given biography.
        /// </summary>
        /// <param name="accession">A unique identifier for the biography.</param>
        /// <returns>An object containing the BiographyDetails and its metadata.</returns>
        public async Task<BlobDownloadModel> GetBiographyDetails(string accession)
        {
            return await RetrieveBlob("data", string.Format("biography/details/{0}", accession));
        }

        /// <summary>
        /// Retrieve the JSON document containing the details for the given story.
        /// </summary>
        /// <param name="storyID">The id of the story details document.</param>
        /// <returns></returns>
        public async Task<BlobDownloadModel> GetStoryDetails(string storyID)
        {
            return await RetrieveBlob("data", string.Format("story/details/{0}", storyID));
        }


        private async Task<BlobDownloadModel> RetrieveBlob(string containerName, string blobName)
        {
            if (!String.IsNullOrEmpty(blobName))
            {
                var container = GetBlobContainer(containerName);

                var blob = container.GetBlockBlobReference(blobName);

                // Download the blob into a memory stream. Notice that we're not putting the memory
                // stream in a using statement. This is because we need the stream to be open for the
                // API controller in order for the file to actually be downloadable. The closing and
                // disposing of the stream is handled by the Web API framework.
                var ms = new MemoryStream();
                await blob.DownloadToStreamAsync(ms);

                // NOTE:  the content type {application/json; charset=utf-8} is incompatible with the Azure pipeline.
                // Correct for the improper behavior by reducing *; charset=utf-8 in blob.Properties.ContentType to 
                // just * (e.g., application/json; charset=utf-8 to just application/json) and all should be well.
                var blobContentTypeToUse = blob.Properties.ContentType;
                const string CONTENT_TYPE_TO_IGNORE = "; charset=utf-8";
                if (blobContentTypeToUse != null && blobContentTypeToUse.EndsWith(CONTENT_TYPE_TO_IGNORE))
                    blobContentTypeToUse = blobContentTypeToUse.Substring(0, blobContentTypeToUse.Length - CONTENT_TYPE_TO_IGNORE.Length);

                // Build and return the download model with the blob stream and its relevant info
                var download = new BlobDownloadModel
                {
                    BlobStream = ms,
                    BlobLength = blob.Properties.Length,
                    BlobContentType = blobContentTypeToUse
                };

                return download;
            }

            // Otherwise
            return null;
        }
    }

    /// <summary>
    /// Represents a downloaded blob.
    /// </summary>
    public class BlobDownloadModel
    {
        /// <summary>
        /// The stream containing the blob data.
        /// </summary>
        public MemoryStream BlobStream { get; set; }

        /// <summary>
        /// The mime-type of the blob.
        /// </summary>
        public string BlobContentType { get; set; }

        /// <summary>
        /// The byte-length of the blob.
        /// </summary>
        public long BlobLength { get; set; }
    }
}