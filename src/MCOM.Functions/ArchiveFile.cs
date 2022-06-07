using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MCOM.Models;
using MCOM.Models.Archiving;
using MCOM.Models.Azure;
using MCOM.Services;
using MCOM.Utilities;

namespace MCOM.Functions
{
    public class ArchiveFile
    {
        private IGraphService _graphService;
        private IBlobService _blobService;

        public ArchiveFile(IGraphService graphService, IBlobService blobService)
        {
            _graphService = graphService;
            _blobService = blobService;
        }

        [Function("ArchiveFile")]
        [QueueOutput("feedbackqueue", Connection = "QueueConnectionString")]
        public async Task<QueueItem> Run([BlobTrigger("output/{name}", Connection = "BlobStorageConnectionString")] string data, string name, FunctionContext context)
        {
            var logger = context.GetLogger("ArchiveFile");

            // If it is not a block blob then ignore
            if (!name.EndsWith(".json") || name.Contains("DestFilename", StringComparison.InvariantCultureIgnoreCase) || name.Contains("~tmp", StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                Global.Log.LogError(e, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", e.Message);
                throw;
            }

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "ArchiveFile");

            // Convert data
            var fileData = JsonConvert.DeserializeObject<ArchiveFileData<string, object>>(data);

            // Validate input
            fileData.ValidateInput();

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "ArchiveFile", "Archiving"))
            {
                try
                {
                    // Init blob service client
                    _blobService.GetBlobServiceClient();

                    var fileUri = new Uri($"https://{Global.BlobStorageAccountName}.blob.core.windows.net/{fileData.BlobFilePath}");

                    // Replace special characters
                    var fileName = StringUtilities.RemoveSpecialChars(fileData.FileName);

                    // Get file from staging area
                    var blobCLient = _blobService.GetBlobClient(fileUri);
                    var blobContainerClient = _blobService.GetBlobContainerClient(blobCLient.BlobContainerName);
                    var stream = await _blobService.OpenReadAsync(blobCLient);

                    // Get container metadata from properties
                    var blobContainerProperties = await _blobService.GetBlobContainerPropertiesAsync(blobContainerClient);
                    var blobContainerMetadata = blobContainerProperties?.Value?.Metadata;

                    // Max slice size must be a multiple of 320 KiB
                    var maxSliceSize = 320 * 1024;

                    //Upload as large or small file
                    if (stream.Length > maxSliceSize)
                    {
                        try
                        {
                            // Upload the file
                            var uploadResult = await _graphService.UploadLargeDriveItemAsync(fileData.DriveID, fileName, stream, maxSliceSize, fileData.BlobFilePath);
                            if (uploadResult.UploadSucceeded)
                            {
                                var uploadedItem = uploadResult.ItemResponse;
                                Global.Log.LogInformation("Completed uploading {BlobFilePath} with id:{DocumentId} to location {SPPath} in drive: {DriveId}", fileData.BlobFilePath, fileData.DocumentId, uploadedItem.WebUrl, fileData.DriveID);

                                await _graphService.SetMetadataAsync(fileData, uploadedItem);

                                // Return QueueItem in case it is configured for the current source
                                if (blobContainerProperties != null && blobContainerMetadata != null && blobContainerMetadata.Count > 0)
                                {
                                    if (blobContainerMetadata.TryGetValue("PostFeedBackClientUrl", out var cientUrl))
                                    {
                                        var headers = new Dictionary<string, string>();

                                        if (blobContainerMetadata.TryGetValue("PostFeedBackHeaders", out var strHeaders))
                                        {
                                            headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(strHeaders);
                                        }

                                        return new QueueItem()
                                        {
                                            Content = new FeedbackItem()
                                            {
                                                DriveId = fileData.DriveID,
                                                DocumentId = fileData.DocumentId
                                            },
                                            ClientUrl = cientUrl,
                                            Source = fileData.Source,
                                            Headers = headers
                                        };
                                    }
                                }
                            }
                        }
                        catch (Exception uploadLargeFileEx)
                        {
                            Global.Log.LogError(uploadLargeFileEx, "UploadLargeFileException: An error occured when uploading {BlobFilePath} with id:{DocumentId} to drive {DriveId}. {ErrorMessage}", fileData.BlobFilePath, fileData.DocumentId, fileData.DriveID, uploadLargeFileEx.Message);
                            throw;
                        }
                    }
                    else
                    {
                        try
                        {
                            var uploadedItem = await _graphService.UploadDriveItemAsync(fileData.DriveID, fileName, stream);
                            Global.Log.LogInformation("Completed uploading {BlobFilePath} with id:{DocumentId} to location {SPPath} in drive: {DriveId}", fileData.BlobFilePath, fileData.DocumentId, uploadedItem.WebUrl, fileData.DriveID);

                            await _graphService.SetMetadataAsync(fileData, uploadedItem);
                            Global.Log.LogInformation("Checking feedbackurl: {FeedBackUrl}", fileData.FeedBackUrl);

                            // Return QueueItem in case it is configured for the current source
                            if (blobContainerProperties != null && blobContainerMetadata != null && blobContainerMetadata.Count > 0)
                            {
                                if (blobContainerMetadata.TryGetValue("PostFeedBackClientUrl", out var cientUrl))
                                {
                                    var headers = new Dictionary<string, string>();

                                    if (blobContainerMetadata.TryGetValue("PostFeedBackHeaders", out var strHeaders))
                                    {
                                        headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(strHeaders);
                                    }

                                    return new QueueItem()
                                    {
                                        Content = new FeedbackItem()
                                        {
                                            DriveId = fileData.DriveID,
                                            DocumentId = fileData.DocumentId
                                        },
                                        ClientUrl = cientUrl,
                                        Source = fileData.Source,
                                        Headers = headers
                                    };
                                }
                            }
                        }
                        catch (Exception uploadSmallFileEx)
                        {
                            Global.Log.LogError(uploadSmallFileEx, "UploadSmallFileException: An error occured when uploading {BlobFilePath} with id:{DocumentId} to drive {DriveId}. {ErrorMessage}", fileData.BlobFilePath, fileData.DocumentId, fileData.DriveID, uploadSmallFileEx.Message);
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Global.Log.LogError(ex, "An error occured when running ArchiveFile code. {ErrorMessage}", ex.Message);
                    throw;
                }
            }

            return null;
        }
    }
}
