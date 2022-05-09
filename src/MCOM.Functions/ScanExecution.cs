using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using MCOM.Models;
using MCOM.Services;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace MCOM.Functions
{
    public class ScanExecution
    {
        private IBlobService _blobService;
        private IGraphService _graphService;

        public ScanExecution(IGraphService graphService, IBlobService blobService, ISharePointService sharePointService, IAzureService azureService)
        {
            _graphService = graphService;
            _blobService = blobService;
        }

        [Function("ScanExecution")]
        public async Task RunAsync([TimerTrigger("0 0 * * * *")] TimerInfo myTimer, FunctionContext context)
        {
            var logger = context.GetLogger("ScanExecution");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", ex.Message);
                throw;
            }

            Activity.Current?.AddTag("MCOMOperation", "ScanExecution");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "ScanExecution", "ScanOnDemand"))
            {
                // Init service
                _blobService.GetBlobServiceClient();

                try
                {
                    // Get blob service client, get container and blobItems
                    var containerClient = _blobService.GetBlobContainerClient("scanexecutions");
                    // Get all blobs based on the given prefix (virtual folder)
                    var blobPages = _blobService.GetBlobs(containerClient,
                                                          Azure.Storage.Blobs.Models.BlobTraits.None,
                                                          Azure.Storage.Blobs.Models.BlobStates.None,
                                                          "metadata/").AsPages();
                    // Loop through all pages and find blobs
                    await foreach (var blobPage in blobPages)
                    {
                        foreach (var blobItem in blobPage.Values)
                        {
                            var blobName = blobItem.Name;

                            try
                            {
                                var metadataBlobClient = _blobService.GetBlobClient(containerClient, blobName);
                                if (await _blobService.BlobClientExistsAsync(metadataBlobClient))
                                {
                                    // Get blob data
                                    var blobData = await _blobService.GetBlobDataAsync(metadataBlobClient);

                                    // Convert data
                                    var fileData = JsonConvert.DeserializeObject<Dictionary<string, object>>(blobData);

                                    // Validate all                               
                                    if (!fileData.TryGetValue("domain", out var domain))
                                    {
                                        Global.Log.LogError(new NullReferenceException(), "Missing domain in metadata.: {blobName}", blobName);
                                        continue;
                                    }
                                    if (!fileData.TryGetValue("siteId", out var siteId))
                                    {
                                        Global.Log.LogError(new NullReferenceException(), "Missing siteId in metadata.: {blobName}", blobName);
                                        continue;
                                    }
                                    if (!fileData.TryGetValue("webId", out var webId))
                                    {
                                        Global.Log.LogError(new NullReferenceException(), "Missing webId in metadata.: {blobName}", blobName);
                                        continue;
                                    }
                                    if (!fileData.TryGetValue("listId", out var listId))
                                    {
                                        Global.Log.LogError(new NullReferenceException(), "Missing listId in metadata.: {blobName}", blobName);
                                        continue;
                                    }
                                    if (!fileData.TryGetValue("itemId", out var itemId))
                                    {
                                        Global.Log.LogError(new NullReferenceException(), "Missing itemId in metadata.: {blobName}", blobName);
                                        continue;
                                    }
                                    if (!fileData.TryGetValue("documentId", out var documentId))
                                    {
                                        Global.Log.LogError(new NullReferenceException(), "Missing documentId in metadata.: {blobName}", blobName);
                                        continue;
                                    }
                                    if (!fileData.TryGetValue("fileName", out var fileName))
                                    {
                                        Global.Log.LogError(new NullReferenceException(), "Missing fileName in metadata.: {blobName}", blobName);
                                        continue;
                                    }
                                    if (!fileData.TryGetValue("documentIdField", out var documentIdField))
                                    {
                                        Global.Log.LogError(new NullReferenceException(), "Missing documentIdField in metadata.: {blobName}", blobName);
                                        continue;
                                    }

                                    // Remove all internal properties
                                    fileData = CleanFileData(fileData);

                                    // Get drive
                                    var drive = await _graphService.GetDriveAsync(domain.ToString(), siteId.ToString(), webId.ToString(), listId.ToString(), "Id");
                                    if (drive == null)
                                    {
                                        Global.Log.LogError(new NullReferenceException(), "Could not get drive with specified blobName: {blobName}", blobName);
                                        continue;
                                    }

                                    // Get blob clients files and metadata
                                    var filesBlobClient = _blobService.GetBlobClient(containerClient, $"files/{documentId}");
                                    if (!await _blobService.BlobClientExistsAsync(filesBlobClient))
                                    {
                                        Global.Log.LogError(new NullReferenceException(), "Could not get files blob client with document id: {documentId}", documentId);
                                        continue;
                                    }

                                    // Check whether the item exists in SharePoint
                                    var listItem = await _graphService.GetListItemAsync(domain.ToString(), siteId.ToString(), webId.ToString(), listId.ToString(), itemId.ToString());
                                    if (listItem.Fields.AdditionalData.TryGetValue(documentIdField.ToString(), out var spCustomField))
                                    {
                                        if (spCustomField.ToString().Equals(documentIdField.ToString()))
                                        {
                                            await DeleteDataFromAzureContainers(metadataBlobClient, filesBlobClient, documentId.ToString());
                                        }
                                        else // Update SHarePoint file and metadata
                                        {
                                            await UpdateSharePointFileAsync(filesBlobClient, drive, fileData, fileName.ToString(), documentId.ToString());
                                        }
                                    }
                                    else // Update SharePoint file and metadata
                                    {
                                        await UpdateSharePointFileAsync(filesBlobClient, drive, fileData, fileName.ToString(), documentId.ToString());
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Global.Log.LogError(ex, "The scan excecution could not be completed on the document with id {BlobName}", blobName);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Global.Log.LogError(ex, "The scan excecution could not be completed because of issues opening connection against the azure container");
                    throw;
                }
            }

            logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        }

        private async Task UpdateSharePointFileAsync(BlobClient filesBlobClient, Drive drive, Dictionary<string, object> fileData, string fileName, string documentId)
        {
            // Max slice size must be a multiple of 320 KiB
            var maxSliceSize = 320 * 1024;

            // Get blob stream
            var filesBlobStream = await _blobService.GetBlobStreamAsync(filesBlobClient);

            // Upload as large or small file
            if (filesBlobStream.Length > maxSliceSize)
            {
                try
                {
                    // Upload the file
                    var uploadResult = await _graphService.UploadFileAsync(drive.Id, fileName, filesBlobStream, maxSliceSize, $"files/{documentId}.json");
                    if (uploadResult.UploadSucceeded)
                    {
                        var uploadedItem = uploadResult.ItemResponse;
                        Global.Log.LogInformation("Completed uploading {BlobFilePath} with id:{DocumentId} to location {SPPath} in drive: {DriveId}", $"files/{documentId}.json", documentId, uploadedItem.WebUrl, drive.Id);

                        await _graphService.SetMetadataByGraphAsync(fileData, uploadedItem);
                    }
                }
                catch (Microsoft.Graph.ServiceException ex)
                {
                    Global.Log.LogWarning(ex.Message);
                }
                catch (Exception ex)
                {
                    Global.Log.LogCritical(ex, "UploadLargeFileException: An error occured when uploading {BlobFilePath} with id:{DocumentId} to drive {DriveId}. {ErrorMessage}", $"files/{documentId}.json", documentId, drive.Id, ex.Message);                   
                }

                Global.Log.LogInformation($"The file {documentId} has been uploaded successfully (UploadLargeFile). Deleting from staging area output and files");
            }
            else
            {
                try
                {
                    var uploadedItem = await _graphService.UploadDriveItemAsync(drive.Id, fileName, filesBlobStream);
                    Global.Log.LogInformation("Completed uploading {BlobFilePath} with id:{DocumentId} to location {SPPath} in drive: {DriveId}", $"files/{documentId}.json", documentId, uploadedItem.WebUrl, drive.Id);

                    await _graphService.SetMetadataByGraphAsync(fileData, uploadedItem);
                }
                catch (Exception uploadSmallFileEx)
                {
                    Global.Log.LogCritical(uploadSmallFileEx, "UploadSmallFileException: An error occured when uploading {BlobFilePath} with id:{DocumentId} to drive {DriveId}. {ErrorMessage}", $"files/{documentId}.json", documentId, drive.Id, uploadSmallFileEx.Message);                   
                }

                Global.Log.LogInformation($"The file {documentId} has been uploaded successfully (UploadSmallFile). Deleting from staging area output and files");
            }
        }

        private async Task DeleteDataFromAzureContainers(BlobClient metadataBlobClient, BlobClient filesBlobClient, string documentId)
        {
            // Delete from metadata and files
            var metadataDeleted = await _blobService.DeleteBlobClientIfExistsAsync(metadataBlobClient);
            if (metadataDeleted)
            {
                Global.Log.LogInformation($"The metadata {documentId} has been deleted successfully from metadata scanrequests");
            }
            else
            {
                Global.Log.LogError(new InvalidOperationException(), $"The blob {documentId} could not be deleted from metadata scanrequests");
            }

            var fileDeleted = await _blobService.DeleteBlobClientIfExistsAsync(filesBlobClient);
            if (fileDeleted)
            {
                Global.Log.LogInformation($"The file {documentId} has been deleted successfully from files scanrequests");
            }
            else
            {
                Global.Log.LogError(new InvalidOperationException(), $"The blob {documentId} could not be deleted from files scanrequests");
            }
        }

        private Dictionary<string, object> CleanFileData(Dictionary<string, object> fileData)
        {
            fileData.Remove("domain");
            fileData.Remove("siteId");
            fileData.Remove("webId");
            fileData.Remove("listId");
            fileData.Remove("itemId");
            fileData.Remove("documentId");
            fileData.Remove("documentIdField");
            fileData.Remove("fileName");

            return fileData;
        }
    }
}
