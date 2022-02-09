using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using MCOM.Models;
using MCOM.Services;
using MCOM.Utilities;

namespace MCOM.Functions
{
    public class StagingAreaTracker
    {
        private IBlobService _blobService;
        private IGraphService _graphService;
        private ISharePointService _sharePointService;
        private IAzureService _azureService;

        public readonly List<FakeLog> _logList = new();

        public StagingAreaTracker(IGraphService graphService, IBlobService blobService, ISharePointService sharePointService, IAzureService azureService)
        {
            _graphService = graphService;
            _blobService = blobService;
            _sharePointService = sharePointService;
            _azureService = azureService;
        }

        [Function("StagingAreaTracker")]
        public async Task RunAsync([TimerTrigger("0 */45 * * * *")] TimerInfo myTimer, FunctionContext context)
        {
            var logger = context.GetLogger("StagingAreaTracker");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                Global.Log.LogError(e, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", e.Message);
                throw;
            }

            Activity.Current?.AddTag("MCOMOperation", "StagingAreaTracker");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "StagingAreaTracker", "Archiving"))
            {
                try
                {                    
                    // Init graph and blob clients
                    await _graphService.GetGraphServiceClientAsync();
                    _blobService.GetBlobServiceClient();

                    // Get blob service client, get container and blobItems
                    var containerClient = _blobService.GetBlobContainerClient("output");
                    var blobItems = _blobService.GetBlobs(containerClient);

                    await foreach (var blobItem in blobItems)
                    {
                        var outputBlobName = blobItem.Name;

                        try
                        {
                            var outputBlobClient = _blobService.GetBlobClient(containerClient, outputBlobName);

                            if (await _blobService.BlobClientExistsAsync(outputBlobClient))
                            {
                                // Get blob data
                                var blobData = await _blobService.GetBlobDataAsync(outputBlobClient);

                                // Convert data
                                var fileData = JsonConvert.DeserializeObject<ArchiveFileData<string, object>>(blobData);

                                // Validate input
                                fileData.ValidateInput();

                                // Validate
                                if (string.IsNullOrEmpty(fileData.DriveID) || string.IsNullOrEmpty(fileData.DocumentId) || string.IsNullOrEmpty(fileData.BlobFilePath) || string.IsNullOrEmpty(fileData.Source))
                                {
                                    _logList.Add(new FakeLog()
                                    {
                                        Message = $"The blob '{outputBlobName}' is missing driveId '{fileData.DriveID}' or DocumentId '{fileData.DocumentId}' or source '{fileData.Source}'",
                                        LogLevel = LogLevel.Critical
                                    });

                                    // ERROR, the file does exist in SharePoint but is missing Metadata
                                    Global.Log.LogCritical("The blob {BlobName} is missing driveId {DriveId} or DocumentId {DocumentId} or Source {Source}", outputBlobName, fileData.DriveID, fileData.DocumentId, fileData.Source);
                                    continue;
                                }

                                // Get drive
                                var drive = await _graphService.GetDriveAsync(fileData.DriveID, "webUrl,sharepointIds");
                                if (drive == null)
                                {
                                    _logList.Add(new FakeLog()
                                    {
                                        Message = $"Could not get drive with specified ID. DocumentId: {fileData.DocumentId}",
                                        LogLevel = LogLevel.Error
                                    });

                                    Global.Log.LogError(new NullReferenceException(), "Could not get drive with specified ID. DocumentId: {DocumentId}", fileData.DocumentId);
                                    continue;
                                }

                                // Get URL and list id from drive object
                                var fileUri = new Uri(drive.WebUrl);
                                var webUrl = fileUri.AbsoluteUri.Substring(0, fileUri.AbsoluteUri.LastIndexOf("/"));
                                var listUrl = HttpUtility.UrlDecode(fileUri.AbsoluteUri.Substring(fileUri.AbsoluteUri.LastIndexOf("/") + 1));
                                var listId = new Guid(drive.SharePointIds.ListId);

                                // Get token using managed identity                                
                                var accessToken = await _azureService.GetAzureServiceTokenAsync(fileUri);

                                // Get SharePoint context with access token
                                using var clientContext = _sharePointService.GetClientContext(webUrl, accessToken.Token);

                                // Get list from SharePoint site
                                var list = _sharePointService.GetListById(clientContext, listId);
                                _sharePointService.Load(clientContext, list);
                                _sharePointService.Load(clientContext, list.Fields);
                                _sharePointService.ExecuteQuery(clientContext);

                                // Get Item by CAML query
                                var camlQuery = new CamlQuery
                                {
                                    ViewXml = $"<View><Query><Where><Eq>" +
                                    $"<FieldRef Name='{fileData.DocumentIdField}'/>" +
                                    $"<Value Type='Text'>{fileData.DocumentId}</Value>" +
                                    $"</Eq></Where></Query><RowLimit>100</RowLimit></View>"
                                };

                                // Get item based on id field provided
                                var collListItem = _sharePointService.GetListItems(clientContext, list, camlQuery);
                                _sharePointService.Load(clientContext, collListItem);
                                _sharePointService.ExecuteQuery(clientContext);

                                // Get SharePoint ListItem
                                var listItems = _sharePointService.GetListAsGenericList(collListItem);
                                var listItem = listItems.FirstOrDefault();

                                // Get container and blobCLient                          
                                var filesContainerClient = _blobService.GetBlobContainerClient(fileData.Source);
                                var filesBlobClient = _blobService.GetBlobClient(filesContainerClient, $"files/{fileData.DocumentId}");

                                if (listItem == null)
                                {
                                    // ERROR, the file does not exist in SharePoint
                                    Global.Log.LogWarning("The ListItem for blob {BlobName} does not exist in SharePoint.", fileData.DocumentId);
                                    _logList.Add(new FakeLog()
                                    {
                                        Message = $"The ListItem for blob {fileData.DocumentId} does not exist in SharePoint.",
                                        LogLevel = LogLevel.Warning
                                    });

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
                                            var uploadResult = await _graphService.UploadFileAsync(fileData.DriveID, fileData.FileName, filesBlobStream, maxSliceSize, fileData.BlobFilePath);
                                            if (uploadResult.UploadSucceeded)
                                            {
                                                var uploadedItem = uploadResult.ItemResponse;
                                                Global.Log.LogInformation("Completed uploading {BlobFilePath} with id:{DocumentId} to location {SPPath} in drive: {DriveId}", fileData.BlobFilePath, fileData.DocumentId, uploadedItem.WebUrl, fileData.DriveID);

                                                await _graphService.SetMetadataAsync(fileData, uploadedItem);
                                            }
                                        }
                                        catch (Exception uploadLargeFileEx)
                                        {
                                            Global.Log.LogCritical("UploadLargeFileException: An error occured when uploading {BlobFilePath} with id:{DocumentId} to drive {DriveId}. {ErrorMessage}", fileData.BlobFilePath, fileData.DocumentId, fileData.DriveID, uploadLargeFileEx.Message);
                                            continue;
                                        }

                                        Global.Log.LogInformation($"The file {fileData.DocumentId} has been uploaded successfully (UploadLargeFile). Deleting from staging area output and files");
                                        _logList.Add(new FakeLog()
                                        {
                                            Message = $"The file {fileData.DocumentId} has been uploaded successfully (UploadLargeFile). Deleting from staging area output and files",
                                            LogLevel = LogLevel.Information
                                        });
                                    }
                                    else
                                    {
                                        try
                                        {
                                            var uploadedItem = await _graphService.UploadDriveItemAsync(fileData.DriveID, fileData.FileName, filesBlobStream);
                                            Global.Log.LogInformation("Completed uploading {BlobFilePath} with id:{DocumentId} to location {SPPath} in drive: {DriveId}", fileData.BlobFilePath, fileData.DocumentId, uploadedItem.WebUrl, fileData.DriveID);

                                            await _graphService.SetMetadataAsync(fileData, uploadedItem);
                                        }
                                        catch (Exception uploadSmallFileEx)
                                        {
                                            Global.Log.LogCritical("UploadSmallFileException: An error occured when uploading {BlobFilePath} with id:{DocumentId} to drive {DriveId}. {ErrorMessage}", fileData.BlobFilePath, fileData.DocumentId, fileData.DriveID, uploadSmallFileEx.Message);
                                            continue;
                                        }

                                        Global.Log.LogInformation($"The file {fileData.DocumentId} has been uploaded successfully (UploadSmallFile). Deleting from staging area output and files");
                                        _logList.Add(new FakeLog()
                                        {
                                            Message = $"The file {fileData.DocumentId} has been uploaded successfully (UploadSmallFile). Deleting from staging area output and files",
                                            LogLevel = LogLevel.Information
                                        });
                                    }
                                }
                                else
                                {
                                    // Delete blob from staging area (output)
                                    var outputDeleted = await _blobService.DeleteBlobClientIfExistsAsync(outputBlobClient);

                                    if (outputDeleted)
                                    {
                                        Global.Log.LogInformation($"The file {fileData.DocumentId} has been deleted successfully from staging area (output)");
                                        _logList.Add(new FakeLog()
                                        {
                                            Message = $"The file {fileData.DocumentId} has been deleted successfully from staging area (output)",
                                            LogLevel = LogLevel.Information
                                        });
                                    }
                                    else
                                    {
                                        Global.Log.LogError(new NullReferenceException(), $"The file {fileData.DocumentId} could not be deleted from staging area (output)");
                                        _logList.Add(new FakeLog()
                                        {
                                            Message = $"The file {fileData.DocumentId} could not be deleted from staging area (output)",
                                            LogLevel = LogLevel.Error
                                        });
                                    }


                                    // Delete blob from files container
                                    var filesBlobName = filesBlobClient.Name;
                                    var filesConteinerName = filesContainerClient.Name;
                                    var filesDeleted = await _blobService.DeleteBlobClientIfExistsAsync(filesBlobClient);

                                    if (filesDeleted)
                                    {
                                        Global.Log.LogInformation($"The file {filesBlobName} has been deleted successfully from container (files)");
                                        _logList.Add(new FakeLog()
                                        {
                                            Message = $"The file {filesBlobName} has been deleted successfully from container (files)",
                                            LogLevel = LogLevel.Information
                                        });
                                    }
                                    else
                                    {
                                        Global.Log.LogError(new NullReferenceException(), $"The file {filesBlobName} could not be deleted from container (files)");
                                        _logList.Add(new FakeLog()
                                        {
                                            Message = $"The file {filesBlobName} could not be deleted from container (files)",
                                            LogLevel = LogLevel.Error
                                        });
                                    }
                                }                                
                            }
                        }
                        catch (Exception ex)
                        {
                            // ERROR, the file does not exist in SharePoint
                            Global.Log.LogCritical("Exception with blob {BlobName}. Exception: {Exception}", outputBlobName, ex);
                        }
                    }
                }
                catch (Exception e)
                {
                    // Log to analytics
                    Global.Log.LogError(e, "Exception: {ErrorMessage}", e.Message);
                    throw;
                }
            }
        }
    }
}
