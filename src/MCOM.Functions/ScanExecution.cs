using System;
using System.IO;
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
using Azure.Core;
using Microsoft.SharePoint.Client.Search.Query;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;

namespace MCOM.Functions
{
    public class ScanExecution
    {
        private IBlobService _blobService;
        private IGraphService _graphService;
        private ISharePointService _sharePointService;
        private IAzureService _azureService;

        public ScanExecution(IGraphService graphService, IBlobService blobService, ISharePointService sharePointService, IAzureService azureService)
        {
            _graphService = graphService;
            _blobService = blobService;
            _sharePointService = sharePointService;
            _azureService = azureService;
        }

        [Function("ScanExecution")]
        public async Task RunAsync([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, FunctionContext context)
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
                    // Get OrderNumber field internal name from environment variables
                    var orderNumberField = Environment.GetEnvironmentVariable("OrderNumberField");

                    // Loop through all pages and find blobs
                    await foreach (var blobPage in blobPages)
                    {
                        foreach (var blobItem in blobPage.Values)
                        {
                            var blobName = blobItem.Name;
                            var blobFileName = blobName.Replace("files/", "");
                            var originalBlobMetadata = blobItem.Metadata;
                            var blobFileExtension = Path.GetExtension(blobFileName);
                            var blobFileNameWithoutExtension = Path.GetFileNameWithoutExtension(blobFileName);

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

                                    // Check if the file has been already uploaded to SharePoint
                                    var originalFile = await _graphService.GetListItemAsync(Global.SharePointDomain, siteId, webId, listId, itemId);
                                    var originalFileExtension = Path.GetExtension(originalFile.WebUrl);
                                    var originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFile.WebUrl);
                                    Uri webUrl = null;

                                    // Get access token
                                    var sharePointUrl = new Uri(Global.SharePointUrl);
                                    var accessToken = await _azureService.GetAzureServiceTokenAsync(sharePointUrl);

                                    // Check whether the file has been processed and got OrderNumber
                                    var isFileProcessed = originalFile.Fields.AdditionalData.TryGetValue(orderNumberField, out var orderNumber);
                                    if (!isFileProcessed)
                                    {
                                        // Try finding the file in SharePoint based on Order Number                                          
                                        using var clientContext = _sharePointService.GetClientContext(Global.SharePointUrl, accessToken.Token);

                                        // Search
                                        var resultTable = _sharePointService.SearchItems(clientContext, $"{orderNumberField}:{blobFileNameWithoutExtension}");

                                        // Check if file has been processed
                                        isFileProcessed = resultTable.RowCount > 0;

                                        // Get webUrl
                                        var fileUri = new Uri(originalFile.WebUrl);
                                        webUrl = Web.WebUrlFromPageUrlDirect(clientContext, fileUri);
                                    }

                                    if (isFileProcessed)
                                    {
                                        await DeleteDataFromAzureContainer(filesBlobClient, blobName);
                                    }
                                    else
                                    {
                                        // Init file metadata
                                        var fileMetaData = new Dictionary<string, object>();

                                        // Add all blob metadata into fileMetadata because addRange won't work on different dictionary types string/object
                                        blobMetadata.ForEach(bm =>
                                        {
                                            fileMetaData.Add(bm.Key, bm.Value);
                                        });

                                        // Get updating flag to know whether the file is going to be created or updated in SharePoint
                                        var updatingFile = (blobFileExtension == originalFileExtension);

                                        await CreateOrUpdateSharePointFileAsync(filesBlobClient, drive, fileMetaData, $"{originalFileNameWithoutExtension}{blobFileExtension}", webUrl.ToString(), accessToken.Token, siteId, webId, listId, itemId, updatingFile);
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

        private async Task CreateOrUpdateSharePointFileAsync(BlobClient filesBlobClient, Drive drive, Dictionary<string, object> fileData, string blobName, string siteUrl, string accessToken, string siteId, string webId, string listId, string itemId, bool updatingFile)
        {
            // Max slice size must be a multiple of 320 KiB
            var maxSliceSize = 320 * 1024;

            // Get blob stream
            var filesBlobStream = await _blobService.GetBlobStreamAsync(filesBlobClient);

            // Upload/Update large and small files
            if (filesBlobStream.Length > maxSliceSize)
            {
                try
                {
                    UploadResult<DriveItem> uploadResult = null;

                    // Upload or update the file
                    if (updatingFile)
                    {
                        // Check if the file has a retention label applied
                        Global.Log.LogInformation($"Validating retention label. Site url: {siteUrl}");
                        bool retentionLabelValidated = false;

                        // Open client context and validate retention label
                        var clientContext = _sharePointService.GetClientContext(siteUrl, accessToken);
                        retentionLabelValidated = _sharePointService.ValidateItemRetentionLabel(clientContext, listId, itemId);

                        if (retentionLabelValidated)
                        {
                            // Get drive item after updating
                            uploadResult = await _graphService.ReplaceLargeSharePointFileAsync(Global.SharePointDomain, siteId, webId, listId, itemId, filesBlobStream, maxSliceSize, blobName, "replace");

                            // Remove file name from updating properties because we will reuse the same
                            fileData.Remove("FileLeafRef");
                        }
                    }
                    else
                    {
                        // Upload the new file and get the result
                        uploadResult = await _graphService.UploadLargeSharePointFileAsync(Global.SharePointDomain, siteId, webId, listId, filesBlobStream, maxSliceSize, blobName);
                    }

                    if (uploadResult != null && uploadResult.UploadSucceeded)
                    {
                        // Get drive item
                        var currentDriveItem = uploadResult.ItemResponse;

                        // Get SharePoint list item based on the drive item id
                        var uploadedListItem = await _graphService.GetListItemAsync(drive.Id, currentDriveItem.Id);

                        // Set metdata on the newly created list item
                        // await _graphService.SetMetadataByGraphAsync(fileData, siteId, listId, uploadedListItem.Id);
                        using var clientContext = _sharePointService.GetClientContext(siteUrl, accessToken);

                        // Get list and item
                        var list = _sharePointService.GetListById(clientContext, new Guid(listId));
                        var listItem = _sharePointService.GetListItemById(clientContext, list, Convert.ToInt32(uploadedListItem.Id));
                        var fields = _sharePointService.GetListFields(list);

                        // Load item in context
                        _sharePointService.Load(clientContext, listItem);
                        _sharePointService.Load(clientContext, fields);
                        _sharePointService.ExecuteQuery(clientContext);

                        // Update item
                        _sharePointService.SetListItemMetadata(clientContext, listItem, fields, fileData);
                        _sharePointService.UpdateListItem(listItem, true);

                        // Excecute query
                        _sharePointService.ExecuteQuery(clientContext);
                    }
                    else
                    {
                        Global.Log.LogError(new FileLoadException(), "Error when Uploading/Replacing blobName: {BlobName} in drive: {DriveId}", blobName, drive.Id);
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

                Global.Log.LogInformation($"The blob {blobName} has been uploaded successfully (UploadLargeFile).");
            }
            else
            {
                try
                {
                    DriveItem currentDriveItem = null;

                    if (updatingFile)
                    {
                        // Check if the file has a retention label applied
                        Global.Log.LogInformation($"Validating retention label. Site url: {siteUrl}");
                        bool retentionLabelValidated = false;
                        var clientContext = _sharePointService.GetClientContext(siteUrl, accessToken);
                        retentionLabelValidated = _sharePointService.ValidateItemRetentionLabel(clientContext, listId, itemId);

                        if (retentionLabelValidated)
                        {
                            currentDriveItem = await _graphService.ReplaceSharePointFileContentAsync(Global.SharePointDomain, siteId, webId, listId, itemId, filesBlobStream);

                            // Remove file name from updating properties because we will reuse the same
                            fileData.Remove("FileLeafRef");
                        }
                    }
                    else
                    {
                        currentDriveItem = await _graphService.UploadSharePointFileAsync(Global.SharePointDomain, siteId, webId, listId, blobName, filesBlobStream);
                    }

                    if (currentDriveItem != null)
                    {
                        // Get SharePoint list item based on the drive item id
                        var uploadedListItem = await _graphService.GetListItemAsync(drive.Id, currentDriveItem.Id);

                        // Set metdata on the newly created list item
                        await _graphService.SetMetadataByGraphAsync(fileData, siteId, listId, uploadedListItem.Id);
                    }
                    else
                    {
                        Global.Log.LogError(new FileLoadException(), "Error when Uploading/Replacing blobName: {BlobName} in drive: {DriveId}", blobName, drive.Id);
                    }
                }
                catch (Exception uploadSmallFileEx)
                {
                    Global.Log.LogCritical(uploadSmallFileEx, "UploadSmallFileException: An error occured when uploading blob {BlobName} to drive {DriveId}. {ErrorMessage}", blobName, drive.Id, uploadSmallFileEx.Message);
                }

                Global.Log.LogInformation($"The blob {blobName} has been uploaded successfully (UploadSmallFile).");
            }
        }

        private async Task DeleteDataFromAzureContainer(BlobClient filesBlobClient, string blobName)
        {
            var fileDeleted = await _blobService.DeleteBlobClientIfExistsAsync(filesBlobClient);
            if (fileDeleted)
            {
                Global.Log.LogInformation($"The file {blobName} has been deleted successfully from files/scanexecutions");
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

            if (!blobMetadata.TryGetValue("fileleafref", out var metadataFileLeafRef))
            {
                Global.Log.LogError(new NullReferenceException(), "Missing itemId in metadata.: {blobName}", blobName);
                return (blobMetadata, false, "", "", "", "", "");
            }

            return (blobMetadata, true, siteId, webId, listId, itemId, metadataFileLeafRef);
        }
    }
}
