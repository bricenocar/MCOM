using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Search.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        public async Task RunAsync([TimerTrigger("0 0 */1 * * *")] TimerInfo myTimer, FunctionContext context)
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

                    // Run output tracker
                    await OutputTrackerAsync();

                    // Run metadata process tracker
                    await MetadataProcessTrackerAsync();
                }
                catch (Exception e)
                {
                    // Log to analytics
                    Global.Log.LogError(e, "Exception: {ErrorMessage}", e.Message);
                    throw;
                }
            }
        }

        /* LOGIC:
            This logic will check whether the files in output container were copied into SharePoint without any issues.
            After this check, if the file has been copied successfully it will be deleted from: metadata, metadataprocess and output
            If the file does not exist in SharePoint then it will be provisioned again into SharePoint, so in the next run it should be found
            in SharePoint so they will be deleted from all previously mentioned containers.
        */
        /// <summary>
        /// Logic to process output, metadata and metadataprocessed by tracking SharePoint
        /// </summary>
        /// <returns></returns>
        private async Task OutputTrackerAsync()
        {
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
                            Global.Log.LogCritical(new NullReferenceException(), "The blob {BlobName} is missing driveId {DriveId} or DocumentId {DocumentId} or Source {Source}", outputBlobName, fileData.DriveID, fileData.DocumentId, fileData.Source);
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

                        // Get token using managed identity                                
                        var accessToken = await _azureService.GetAzureServiceTokenAsync(fileUri);

                        // Get SharePoint context with access token
                        using var clientContext = _sharePointService.GetClientContext(webUrl, accessToken.Token);

                        // Get files container and blobCLient                          
                        var filesContainerClient = _blobService.GetBlobContainerClient(fileData.Source);
                        var filesBlobClient = _blobService.GetBlobClient(filesContainerClient, $"files/{fileData.DocumentId}");

                        // Get metadataprocessed container and blobCLient                          
                        var metadataprocessedBlobClient = _blobService.GetBlobClient(filesContainerClient, $"metadataprocessed/{fileData.DocumentId}.json");

                        bool found = SearchArchivedFile(clientContext, fileData.DocumentId);

                        if (!found)
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
                                    Global.Log.LogCritical(uploadLargeFileEx, "UploadLargeFileException: An error occured when uploading {BlobFilePath} with id:{DocumentId} to drive {DriveId}. {ErrorMessage}", fileData.BlobFilePath, fileData.DocumentId, fileData.DriveID, uploadLargeFileEx.Message);
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
                                    Global.Log.LogCritical(uploadSmallFileEx, "UploadSmallFileException: An error occured when uploading {BlobFilePath} with id:{DocumentId} to drive {DriveId}. {ErrorMessage}", fileData.BlobFilePath, fileData.DocumentId, fileData.DriveID, uploadSmallFileEx.Message);
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
                                Global.Log.LogError(new InvalidOperationException(), $"The file {fileData.DocumentId} could not be deleted from staging area (output)");
                                _logList.Add(new FakeLog()
                                {
                                    Message = $"The file {fileData.DocumentId} could not be deleted from staging area (output)",
                                    LogLevel = LogLevel.Error
                                });
                            }

                            // Check whether the file is older than 24 hours so it will be copied to metadata and then deleted
                            var metadataprocessedBlobName = metadataprocessedBlobClient.Name;
                            var metadataprocessedDeleted = await _blobService.DeleteBlobClientIfExistsAsync(metadataprocessedBlobClient);

                            if (metadataprocessedDeleted)
                            {
                                Global.Log.LogInformation($"The file {metadataprocessedBlobName} has been deleted successfully from container (metadataprocessed)");
                                _logList.Add(new FakeLog()
                                {
                                    Message = $"The file {metadataprocessedBlobName} has been deleted successfully from container (metadataprocessed)",
                                    LogLevel = LogLevel.Information
                                });
                            }
                            else
                            {
                                Global.Log.LogError(new InvalidOperationException(), $"The file {metadataprocessedBlobName} could not be deleted from container (metadataprocessed)");
                                _logList.Add(new FakeLog()
                                {
                                    Message = $"The file {metadataprocessedBlobName} could not be deleted from container (metadataprocessed)",
                                    LogLevel = LogLevel.Error
                                });
                            }

                            // Delete blob from files container
                            var filesBlobName = filesBlobClient.Name;
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
                                Global.Log.LogError(new InvalidOperationException(), $"The file {filesBlobName} could not be deleted from container (files)");
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
                    Global.Log.LogCritical(ex, "Exception with blob {BlobName}. Exception: {ErrorMessage}", outputBlobName, ex.Message);
                }
            }
        }

        /* LOGIC:
            This tracker will check all the metadata files from the metadataprocess container. In case the files are older than 1 day then
            these will be copied to metadata again (Into the right Source) and then they will be deleted from the metadataprocess container.
            This is done in order to double check in case the metadata files were not copied to the OUTPUT container by the pipeline.
            In some cases the pipeline does not copy the files to output and it just move the files to metadataprocess.
            By copying the metadata files again to metadata container we re-run the process again.
        */
        /// <summary>
        /// Logic to process metadata by tracking files that has not been processed
        /// </summary>
        /// <returns></returns>
        private async Task MetadataProcessTrackerAsync()
        {

            // Get all containers
            var containerPages = _blobService.GetBlobContainers();

            await foreach (var containerPage in containerPages)
            {
                var continerName = containerPage.Name;
                var containerClient = _blobService.GetBlobContainerClient(continerName);

                // Get all blobs based on the given prefix (virtual folder)
                var blobPages = _blobService.GetBlobs(containerClient,
                                                      Azure.Storage.Blobs.Models.BlobTraits.None,
                                                      Azure.Storage.Blobs.Models.BlobStates.None,
                                                      "metadataprocessed/").AsPages();
                // Loop through all pages and find blobs
                await foreach(var blobPage in blobPages)
                {
                    foreach (var blobItem in blobPage.Values)
                    {
                        var blobName = blobItem.Name;
                        var blobCreatedOn = blobItem.Properties.CreatedOn;

                        try
                        {
                            if (blobCreatedOn < DateTime.Now.AddDays(-1))
                            {
                                var blobClient = _blobService.GetBlobClient(containerClient, blobName);

                                if (await _blobService.BlobClientExistsAsync(blobClient))
                                {
                                    // Get blob data
                                    var metadataProcessedData = await _blobService.GetBlobDataAsync(blobClient);

                                    // Convert data
                                    var metadataProcessedFileData = JObject.Parse(metadataProcessedData);

                                    // Get Source;

                                    if (!metadataProcessedFileData.TryGetValue("Source", out var jSource))
                                    {
                                        Global.Log.LogWarning($"The file {blobName} is missing the Source parameter");
                                        _logList.Add(new FakeLog()
                                        {
                                            Message = $"The file {blobName} is missing the Source parameter",
                                            LogLevel = LogLevel.Error
                                        });
                                    }
                                    else
                                    {
                                        // Get source from file properties
                                        var source = jSource?.ToString();

                                        // Remove sub folders from blob name
                                        var strippedBlobName = blobName.Split('/')[1];

                                        // Url to copy data to
                                        var metadataUri = new Uri($"https://{Global.BlobStorageAccountName}.blob.core.windows.net/{source}/metadata/{strippedBlobName}");

                                        Global.Log.LogInformation("Uri if blob to be uploaded ${metadataUri}. BlobName: {BlobName}", metadataUri, strippedBlobName);

                                        // Get blob client and copy file
                                        var blobMetadataClient = _blobService.GetBlobClient(metadataUri);
                                        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(metadataProcessedData)))
                                        {
                                            await blobMetadataClient.UploadAsync(stream, Global.BlobOverwriteExistingFile);
                                        }

                                        Global.Log.LogInformation("Successfully uploaded metadata file without errors. DocumentId: {BlobName}", strippedBlobName);

                                        // Delete blob from files container
                                        var filesDeleted = await _blobService.DeleteBlobClientIfExistsAsync(blobClient);

                                        if (filesDeleted)
                                        {
                                            Global.Log.LogInformation($"The file {strippedBlobName} has been deleted successfully from container (files)");
                                            _logList.Add(new FakeLog()
                                            {
                                                Message = $"The file {strippedBlobName} has been deleted successfully from container (files)",
                                                LogLevel = LogLevel.Information
                                            });
                                        }
                                        else
                                        {
                                            Global.Log.LogError(new NullReferenceException(), $"The file {strippedBlobName} could not be deleted from container (files)");
                                            _logList.Add(new FakeLog()
                                            {
                                                Message = $"The file {strippedBlobName} could not be deleted from container (files)",
                                                LogLevel = LogLevel.Error
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Global.Log.LogError(ex, "Exception when processing blob {BlobItem} - MetadataProcess. {ErrorMessage}", blobName, ex.Message);
                            throw;
                        }
                    }
                }
            }
        }

        private bool SearchArchivedFile(ClientContext clientContext, string documentId)
        {
            bool response = false;
            try
            {
                // Get events
                ResultTable table = _sharePointService.SearchItems(clientContext, documentId);
                if (table.RowCount == 0)
                {
                    var msg = "Could not find file in SharePoint indexed. Trying again in next round";
                    Global.Log.LogWarning(msg + ". File unique id: {DocumentId}.", documentId);
                }
                else
                {
                    response = true;
                }
            }
            catch (Exception e)
            {
                var msg = "Error trying to get items from Archive location";
                Global.Log.LogError(e, msg + ". File unique id: {DocumentId}. Error: {ErrorMessage}. StackTrace: {ErrorStackTrace}", documentId, e.Message, e.StackTrace);
            }

            return response;
        }
    }
}
