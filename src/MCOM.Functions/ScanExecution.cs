using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Azure.Storage.Blobs;
using MCOM.Models;
using MCOM.Services;
using MCOM.Utilities;
using MCOM.Extensions;

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
                    var blobPages = _blobService.GetBlobsAsync(containerClient,
                                                          Azure.Storage.Blobs.Models.BlobTraits.Metadata,
                                                          Azure.Storage.Blobs.Models.BlobStates.None,
                                                          "files/").AsPages();
                    // Loop through all pages and find blobs
                    await foreach (var blobPage in blobPages)
                    {
                        foreach (var blobItem in blobPage.Values)
                        {
                            var blobName = blobItem.Name;
                            var blobFileName = blobItem.Name.Replace("files/", "");
                            var originalBlobMetadata = blobItem.Metadata;

                            try
                            {
                                var filesBlobClient = _blobService.GetBlobClient(containerClient, blobName);
                                if (await _blobService.BlobClientExistsAsync(filesBlobClient))
                                {
                                    // Validate internal properties
                                    var (blobMetadata, isValid, siteId, webId, listId, itemId, metadataFileLeafRef) = ValidateFileData(originalBlobMetadata, blobFileName);

                                    // Check if valid
                                    if (!isValid)
                                    {
                                        Global.Log.LogError(new MissingFieldException(), "Could not get all required internal properties (siteid, webid, listid, itemid, ordernumber, internalfield) from the request: {BlobName}", blobName);
                                        continue;
                                    }

                                    // Get drive
                                    var drive = await _graphService.GetDriveAsync(Global.SharePointDomain, siteId, webId, listId, "Id");
                                    if (drive == null)
                                    {
                                        Global.Log.LogError(new NullReferenceException(), "Could not get drive with specified blobName: {BlobName}", blobName);
                                        continue;
                                    }

                                    // Init file metadata
                                    var fileMetaData = new Dictionary<string, object>();

                                    // Add all blob metadata into fileMetadata because addRange won't work on different dictionary types string/object
                                    blobMetadata.ForEach(bm => fileMetaData.Add(bm.Key, bm.Value));

                                    // Check whether the item exists in SharePoint
                                    var listItem = await _graphService.GetListItemAsync(Global.SharePointDomain, siteId, webId, listId, itemId);
                                    if (listItem.Fields.AdditionalData.TryGetValue("FileLeafRef", out var listItemFileLeafRef))
                                    {   
                                        // if the internal field is equals to the one coming from the pipeline delete the blob from the container, otherwise update the SharePoint Item
                                        if (listItemFileLeafRef.ToString().Equals(metadataFileLeafRef, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            await DeleteDataFromAzureContainer(filesBlobClient, blobName);
                                        }
                                        else // Update SharePoint file and metadata
                                        {
                                            await UpdateSharePointFileAsync(filesBlobClient, drive, fileMetaData, blobName, siteId, webId, listId, itemId);
                                        }
                                    }
                                    else // Update SharePoint file and metadata
                                    {
                                        await UpdateSharePointFileAsync(filesBlobClient, drive, fileMetaData, blobName, siteId, webId, listId, itemId);
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

        private async Task UpdateSharePointFileAsync(BlobClient filesBlobClient, Drive drive, Dictionary<string, object> fileData, string blobName, string siteId, string webId, string listId, string itemId)
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
                    var uploadResult = await _graphService.UploadLargeSharePointFileAsync(Global.SharePointDomain, siteId, webId, listId, itemId, filesBlobStream, maxSliceSize, blobName, "replace");
                    if (uploadResult.UploadSucceeded)
                    {
                        var uploadedItem = uploadResult.ItemResponse;
                        Global.Log.LogInformation("Completed uploading blobName: {BlobName} to location {SPPath} in drive: {DriveId}", blobName, uploadedItem.WebUrl, drive.Id);

                        await _graphService.SetMetadataByGraphAsync(fileData, uploadedItem, siteId, listId, itemId);
                    }
                }
                catch (ServiceException ex)
                {
                    Global.Log.LogWarning(ex.Message);
                }
                catch (Exception ex)
                {
                    Global.Log.LogCritical(ex, "UploadLargeFileException: An error occured when uploading blobName: {BlobName} to drive {DriveId}. {ErrorMessage}", blobName, drive.Id, ex.Message);
                }

                Global.Log.LogInformation($"The blob {blobName} has been uploaded successfully (UploadLargeFile). Deleting from staging area output and files");
            }
            else
            {
                try
                {
                    var uploadedItem = await _graphService.UploadSharePointFileAsync(Global.SharePointDomain, siteId, webId, listId, itemId, filesBlobStream);
                    Global.Log.LogInformation("Completed uploading blob {BlobName} to location {SPPath} in drive: {DriveId}", blobName, uploadedItem.WebUrl, drive.Id);

                    await _graphService.SetMetadataByGraphAsync(fileData, uploadedItem, siteId, listId, itemId);
                }
                catch (Exception uploadSmallFileEx)
                {
                    Global.Log.LogCritical(uploadSmallFileEx, "UploadSmallFileException: An error occured when uploading blob {BlobName} to drive {DriveId}. {ErrorMessage}", blobName, drive.Id, uploadSmallFileEx.Message);
                }

                Global.Log.LogInformation($"The blob {blobName} has been uploaded successfully (UploadSmallFile). Deleting from staging area output and files");
            }
        }

        private async Task DeleteDataFromAzureContainer(BlobClient filesBlobClient, string blobName)
        {
            var fileDeleted = await _blobService.DeleteBlobClientIfExistsAsync(filesBlobClient);
            if (fileDeleted)
            {
                Global.Log.LogInformation($"The file {blobName} has been deleted successfully from files scanrequests");
            }
            else
            {
                Global.Log.LogError(new InvalidOperationException(), $"The file blob {blobName} could not be deleted from files scanrequests");
            }
        }

        private (IDictionary<string, string>, bool, string, string, string, string, string) ValidateFileData(IDictionary<string, string> blobMetadata, string blobName)
        {
            // Validate all
            if (!blobMetadata.TryGetValue("siteid", out var siteId))
            {
                Global.Log.LogError(new NullReferenceException(), "Missing siteId in metadata.: {blobName}", blobName);
                return (blobMetadata, false, "", "", "", "", "");
            }
            blobMetadata.Remove("siteid");

            if (!blobMetadata.TryGetValue("webid", out var webId))
            {
                Global.Log.LogError(new NullReferenceException(), "Missing webId in metadata.: {blobName}", blobName);
                return (blobMetadata, false, "", "", "", "", "");
            }
            blobMetadata.Remove("webid");

            if (!blobMetadata.TryGetValue("listid", out var listId))
            {
                Global.Log.LogError(new NullReferenceException(), "Missing listId in metadata.: {blobName}", blobName);
                return (blobMetadata, false, "", "", "", "", "");
            }
            blobMetadata.Remove("listid");

            if (!blobMetadata.TryGetValue("itemid", out var itemId))
            {
                Global.Log.LogError(new NullReferenceException(), "Missing itemId in metadata.: {blobName}", blobName);
                return (blobMetadata, false, "", "", "", "", "");
            }
            blobMetadata.Remove("itemid");

            if (!blobMetadata.TryGetValue("FileLeafRef", out var metadataFileLeafRef))
            {
                Global.Log.LogError(new NullReferenceException(), "Missing itemId in metadata.: {blobName}", blobName);
                return (blobMetadata, false, "", "", "", "", "");
            }

            return (blobMetadata, true, siteId, webId, listId, itemId, metadataFileLeafRef);
        }
    }
}
