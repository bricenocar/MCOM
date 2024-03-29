﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using MCOM.Models;
using MCOM.Utilities;

namespace MCOM.Services
{
    public interface IBlobService
    {
        void GetBlobServiceClient();
        BlobClient GetBlobClient(Uri fileUri);
        BlobContainerClient GetBlobContainerClient(string container);
        AsyncPageable<BlobItem> GetBlobsAsync(BlobContainerClient containerClient, BlobTraits blobTraits = BlobTraits.None, BlobStates blobStates = BlobStates.None, string prefix = null);
        BlobClient GetBlobClient(BlobContainerClient containerClient, string blobName);
        AsyncPageable<BlobContainerItem> GetBlobContainers();
        Task<string> GetBlobDataAsync(BlobClient blobClient);
        Task<Stream> GetBlobStreamAsync(BlobClient blobClient);
        Task<Stream> OpenReadAsync(BlobClient blobClient);
        Task<bool> BlobClientExistsAsync(BlobClient blobClient);
        Task<bool> DeleteBlobClientIfExistsAsync(BlobClient blobClient);
        Task<Response<BlobContainerProperties>> GetBlobContainerPropertiesAsync(BlobContainerClient blobContainerClient);
        void InitializeBlobServiceClient(string storageAccountName);
    }

    public class BlobService : IBlobService
    {
        public BlobServiceClient BlobServiceClient { get; set; }


        public virtual void GetBlobServiceClient()
        {
            if (BlobServiceClient == null)
            {
                var options = new BlobClientOptions();
                options.Diagnostics.IsLoggingEnabled = Global.BlobIsLoggingEnabled;
                options.Diagnostics.IsTelemetryEnabled = Global.BlobIsTelemetryEnabled;
                options.Diagnostics.IsDistributedTracingEnabled = Global.BlobIsDistributedTracingEnabled;
                options.Retry.MaxRetries = Global.BlobMaxRetries;

                var credential = AzureUtilities.GetDefaultCredential();
                if (credential == null)
                {
                    Global.Log.LogError(new NullReferenceException(), "Failed to get chained token credential");
                    throw new AuthenticationFailedException("Failed to get chained token credential");
                }

                BlobServiceClient = new BlobServiceClient(new Uri($"https://{Global.BlobStorageAccountName}.blob.core.windows.net/"), credential, options);
            }
        }

        public virtual void InitializeBlobServiceClient(string storageAccountName)
        {
            if (BlobServiceClient == null)
            {
                var options = new BlobClientOptions();
                options.Diagnostics.IsLoggingEnabled = Global.BlobIsLoggingEnabled;
                options.Diagnostics.IsTelemetryEnabled = Global.BlobIsTelemetryEnabled;
                options.Diagnostics.IsDistributedTracingEnabled = Global.BlobIsDistributedTracingEnabled;
                options.Retry.MaxRetries = Global.BlobMaxRetries;

                var credential = AzureUtilities.GetDefaultCredential();
                if (credential == null)
                {
                    Global.Log.LogError(new NullReferenceException(), "Failed to get chained token credential");
                    throw new AuthenticationFailedException("Failed to get chained token credential");
                }

                BlobServiceClient = new BlobServiceClient(new Uri($"https://{storageAccountName}.blob.core.windows.net/"), credential, options);
            }
        }

        public virtual BlobClient GetBlobClient(Uri fileUri)
        {
            var options = new BlobClientOptions();
            options.Diagnostics.IsLoggingEnabled = Global.BlobIsLoggingEnabled;
            options.Diagnostics.IsTelemetryEnabled = Global.BlobIsTelemetryEnabled;
            options.Diagnostics.IsDistributedTracingEnabled = Global.BlobIsDistributedTracingEnabled;
            options.Retry.MaxRetries = Global.BlobMaxRetries;

            var credential = AzureUtilities.GetDefaultCredential();
            if (credential == null)
            {
                Global.Log.LogError(new NullReferenceException(), "Failed to get chained token credential");
                throw new AuthenticationFailedException("Failed to get chained token credential");
            }

            return new BlobClient(fileUri, credential, options);
        }

        public virtual BlobContainerClient GetBlobContainerClient(string container)
        {
            if (BlobServiceClient == null)
                throw new Exception("BlobServiceClient has to be called first");

            return BlobServiceClient.GetBlobContainerClient(container);
        }

        public virtual AsyncPageable<BlobContainerItem> GetBlobContainers()
        {
            if (BlobServiceClient == null)
                throw new Exception("BlobServiceClient has to be called first");

            return BlobServiceClient.GetBlobContainersAsync();
        }

        public virtual async Task<Response<BlobContainerProperties>> GetBlobContainerPropertiesAsync(BlobContainerClient blobContainerClient)
        {
            if (BlobServiceClient == null)
                throw new Exception("BlobServiceClient has to be called first");

            return await blobContainerClient.GetPropertiesAsync();
        }

        public virtual AsyncPageable<BlobItem> GetBlobsAsync(BlobContainerClient containerClient, BlobTraits blobTraits = BlobTraits.None, BlobStates blobStates = BlobStates.None, string prefix = null)
        {
            if (BlobServiceClient == null)
                throw new Exception("BlobServiceClient has to be called first");

            return containerClient.GetBlobsAsync(blobTraits, blobStates, prefix);
        }

        public virtual BlobClient GetBlobClient(BlobContainerClient containerClient, string blobName)
        {
            return containerClient.GetBlobClient(blobName);
        }

        public virtual async Task<string> GetBlobDataAsync(BlobClient blobClient)
        {
            var response = await blobClient.DownloadContentAsync();
            return response.Value.Content.ToString();
        }

        public virtual async Task<Stream> GetBlobStreamAsync(BlobClient blobClient)
        {
            return await blobClient.OpenReadAsync();
        }

        public virtual async Task<Stream> OpenReadAsync(BlobClient blobClient)
        {
            return await blobClient.OpenReadAsync(new BlobOpenReadOptions(false), new System.Threading.CancellationToken());
        }

        public virtual async Task<bool> BlobClientExistsAsync(BlobClient blobClient)
        {
            return await blobClient.ExistsAsync();
        }

        public virtual async Task<bool> DeleteBlobClientIfExistsAsync(BlobClient blobClient)
        {
            return await blobClient.DeleteIfExistsAsync();
        }
    }
}
