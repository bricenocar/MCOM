using Azure.Core;
using Google.Protobuf.WellKnownTypes;
using MCOM.Models;
using MCOM.Models.Archiving;
using MCOM.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using DriveUpload = Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;

namespace MCOM.Services
{
    public interface IGraphService
    {
        Task<GraphServiceClient> GetGraphServiceClientAsync();

        //Task<DriveItem> CopySharePointFileAsync(string domain, string siteId, string webId, string listId, string itemId, string name, ItemReference parentReference);
        //Task<DriveItem> UploadSharePointFileAsync(string domain, string siteId, string webId, string listId, string fileName, Stream stream);
        Task<DriveItem> ReplaceSharePointFileContentAsync(string domain, string siteId, string webId, string listId, string itemId, Stream stream);
        Task<DriveItem> UploadDriveItemAsync(string driveId, string fileName, Stream stream);
        
        Task<UploadResult<DriveItem>> UploadLargeSharePointFileAsync(string domain, string siteId, string webId, string listId, Stream stream, int maxSliceSize, string fileName, string conflictBehaviour = "fail");
        Task<UploadResult<DriveItem>> ReplaceLargeSharePointFileAsync(string domain, string siteId, string webId, string listId, string itemId, Stream stream, int maxSliceSize, string fileName, string conflictBehaviour = "fail");
        //Task<UploadResult<DriveItem>> UploadLargeDriveItemAsync(string driveId, string fileName, Stream stream, int maxSliceSize, string blobFilePath, string conflictBehaviour = "fail");

        //Task<IListItemsCollectionPage> SearchItemAsync(string domain, string siteId, string webId, string listId, string filter);
        Task<Microsoft.Graph.Drives.Item.SearchWithQ.SearchWithQResponse?> SearchDriveAsync(string driveId, string queryString);
        //Task<List<Models.Search.SearchResult>> SearchItemAsync(string documentId);

        Task<Microsoft.Graph.Models.ListItem> GetListItemAsync(string driveId, string driveItemId);
        Task<Microsoft.Graph.Models.ListItem> GetListItemAsync(string domain, string siteId, string webId, string listId, string itemId);
        //Task<ISiteDrivesCollectionPage> GetDriveCollectionPageAsync(Uri uri);
        Task<Stream?> GetFileContentAsync(string driveId, string driveItemId);
        Task<Drive?> GetDriveAsync(string driveId, string select);
        Task<Drive?> GetDriveAsync(string domain, string siteId, string webId, string listId, string select);

        //Task SetMetadataAsync(ArchiveFileData<string, object> filedata, DriveItem uploadedItem);
        Task SetMetadataByGraphAsync(Dictionary<string, object> fileMetadata, DriveItem extendedItem);
        Task SetMetadataByGraphAsync(Dictionary<string, object> fileMetadata, string siteId, string listId, string itemId);
        Task SetMetadataByCSOMAsync(Dictionary<string, object> fileMetadata, DriveItem extendedItem);
    }

    public class GraphService : IGraphService
    {
        public GraphServiceClient _graphServiceClient { get; set; }
        public AccessToken _graphToken { get; set; }


        public async Task<GraphServiceClient> GetGraphServiceClientAsync()
        {
            if (_graphServiceClient == null || _graphToken.ExpiresOn >= DateTime.Now)
            {
                _graphToken = await AzureUtilities.GetAzureServiceTokenAsync("https://graph.microsoft.com");
                TokenProvider provider = new TokenProvider(_graphToken);
                var authenticationProvider = new BaseBearerTokenAuthenticationProvider(provider);
                _graphServiceClient = new GraphServiceClient(authenticationProvider);
            }

            return _graphServiceClient;
        }

        //public virtual async Task<DriveItem> CopySharePointFileAsync(string domain, string siteId, string webId, string listId, string itemId, string name, ItemReference parentReference)
        //{
        //    _graphServiceClient = await GetGraphServiceClientAsync();

        //    return await _graphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Items[itemId].DriveItem.Copy(name, parentReference).Request().PostAsync();
        //}

        //public virtual async Task<DriveItem> UploadSharePointFileAsync(string domain, string siteId, string webId, string listId, string fileName, Stream stream)
        //{
        //    _graphServiceClient = await GetGraphServiceClientAsync();
        //    var drive = await _graphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Items["root"]
        //    return await _graphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Drive.Root.ItemWithPath(fileName).Content.Request().PutAsync<DriveItem>(stream);
        //}

        public virtual async Task<DriveItem> ReplaceSharePointFileContentAsync(string domain, string siteId, string webId, string listId, string itemId, Stream stream)
        {
            _graphServiceClient = await GetGraphServiceClientAsync();

            var requestInformation = _graphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Items[itemId].DriveItem.Content.ToPutRequestInformation(stream);
            requestInformation.URI = new Uri(requestInformation.URI.OriginalString + "?@microsoft.graph.conflictBehavior=replace");
            var result = await _graphServiceClient.RequestAdapter.SendAsync<DriveItem>(requestInformation, DriveItem.CreateFromDiscriminatorValue);

            return result;
        }

        public virtual async Task<DriveItem> UploadDriveItemAsync(string driveId, string fileName, Stream stream)
        {
            _graphServiceClient = await GetGraphServiceClientAsync();
            var requestInformation = _graphServiceClient.Drives[driveId].Root.ItemWithPath(fileName).Content.ToPutRequestInformation(stream);
            var result = await _graphServiceClient.RequestAdapter.SendAsync<DriveItem>(requestInformation, DriveItem.CreateFromDiscriminatorValue);

            return result;
        }

        public virtual async Task<UploadResult<DriveItem>> UploadLargeSharePointFileAsync(string domain, string siteId, string webId, string listId, Stream stream, int maxSliceSize, string fileName, string conflictBehaviour = "fail")
        {
            _graphServiceClient = await GetGraphServiceClientAsync();

            // Use properties to specify the conflict behavior
            var uploadSessionRequestBody = new DriveUpload.CreateUploadSessionPostRequestBody
            {
                Item = new DriveItemUploadableProperties
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "@microsoft.graph.conflictBehavior", conflictBehaviour},
                    }
                }
            };

            // var uploadSession = await GraphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Drive.Root.ItemWithPath(fileName).CreateUploadSession(uploadProps).Request().PostAsync();
            var drive= await _graphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Drive.GetAsync();
            var uploadSession = await _graphServiceClient.Drives[drive?.Id].Root                
                .ItemWithPath(fileName)
                .CreateUploadSession
                .PostAsync(uploadSessionRequestBody);

            var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, stream, maxSliceSize, _graphServiceClient.RequestAdapter);

            // Create a callback that is invoked after each slice is uploaded
            var progress = new Progress<long>(prog =>
            {
                Global.Log.LogInformation("Uploaded {UploadProgress} bytes of {StreamLength} bytes for {BlobName}", prog, stream.Length, fileName);
            });

            // Upload the file
            return await fileUploadTask.UploadAsync(progress);
        }

        public virtual async Task<UploadResult<DriveItem>> ReplaceLargeSharePointFileAsync(string domain, string siteId, string webId, string listId, string itemId, Stream stream, int maxSliceSize, string fileName, string conflictBehaviour = "fail")
        {
            _graphServiceClient = await GetGraphServiceClientAsync();

            // Use properties to specify the conflict behavior
            var uploadSessionRequestBody = new DriveUpload.CreateUploadSessionPostRequestBody
            {
                Item = new DriveItemUploadableProperties
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "@microsoft.graph.conflictBehavior", conflictBehaviour},
                    }
                }
            };          

            var drive = await _graphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Drive.GetAsync();
            //var driveItem = await _graphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Items[itemId].DriveItem.GetAsync();
            var uploadSession = await _graphServiceClient.Drives[drive?.Id]
                .Items[itemId]
                .CreateUploadSession
                .PostAsync(uploadSessionRequestBody);

            var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, stream, maxSliceSize);

            // Create a callback that is invoked after each slice is uploaded
            var progress = new Progress<long>(prog =>
            {
                Global.Log.LogInformation("Uploaded {UploadProgress} bytes of {StreamLength} bytes for {BlobName}", prog, stream.Length, fileName);
            });

            // Upload the file
            return await fileUploadTask.UploadAsync(progress);
        }

        //public virtual async Task<UploadResult<DriveItem>> UploadLargeDriveItemAsync(string driveId, string fileName, Stream stream, int maxSliceSize, string blobFilePath, string conflictBehaviour = "fail")
        //{
        //    _graphServiceClient = await GetGraphServiceClientAsync();

        //    // Use properties to specify the conflict behavior
        //    var uploadProps = new DriveItemUploadableProperties
        //    {
        //        OdataType = null,
        //        AdditionalData = new Dictionary<string, object>
        //        {
        //            { "@microsoft.graph.conflictBehavior", conflictBehaviour},
        //        }
        //    };

        //    var uploadSession = await _graphServiceClient.Drives[driveId].Root.ItemWithPath(fileName).CreateUploadSession(uploadProps).Request().PostAsync();
        //    var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, stream, maxSliceSize);

        //    // Create a callback that is invoked after each slice is uploaded
        //    var progress = new Progress<long>(prog =>
        //    {
        //        Global.Log.LogInformation("Uploaded {UploadProgress} bytes of {StreamLength} bytes for {BlobFilePath}", prog, stream.Length, blobFilePath);
        //    });

        //    // Upload the file
        //    return await fileUploadTask.UploadAsync(progress);
        //}


        //public virtual async Task<ListItemCollectionResponse> SearchItemAsync(string domain, string siteId, string webId, string listId, string filter)
        //{
        //    _graphServiceClient = await GetGraphServiceClientAsync();

        //    return await _graphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Items.GetAsync(
        //        requestConfig =>
        //        {
        //            requestConfig.QueryParameters.Filter = filter;
        //        }
        //    );
        //}

        public virtual async Task<Microsoft.Graph.Drives.Item.SearchWithQ.SearchWithQResponse?> SearchDriveAsync(string driveId, string queryString)
        {
            try
            {
                _graphServiceClient = await GetGraphServiceClientAsync();
                var result = await _graphServiceClient.Drives[driveId].SearchWithQ(queryString).GetAsync();
                return result;
            }
            catch (ODataError odataError)
            {
                Global.Log.LogError($"Error searching drive: {odataError.Error?.Message}. Errorcode: {odataError.Error?.Code}");
                return null;
            }
            catch (ApiException apiex)
            {
                Global.Log.LogError($"Error searching drive: {apiex.Message}");
                return null;
            }
            catch (Exception e)
            {
                Global.Log.LogError($"Error searching drive (Unhandled): {e.Message}");
                return null;
            }
        }

        //public virtual async Task<List<Models.Search.SearchResult>> SearchItemAsync(string queryString)
        //{
        //    _graphServiceClient = await GetGraphServiceClientAsync();
        //    var requests = new List<SearchRequestObject>()
        //    {
        //        new SearchRequestObject
        //        {
        //            EntityTypes = new List<EntityType>()
        //            {
        //                EntityType.DriveItem
        //            },
        //            Query = new SearchQuery
        //            {
        //                QueryString = $"{queryString}"
        //            },
        //            From = 0,
        //            Size = 25
        //        }
        //    };
        //    var response = await _graphServiceClient.Search.Query(requests)
        //        .Request()
        //        .PostAsync();

        //    var results = new List<Models.Search.SearchResult>();
        //    Global.Log.LogInformation($"Count: {response.CurrentPage.Count}");
        //    if (response.CurrentPage.Count > 0)
        //    {
        //        var hitContainers = response.CurrentPage[0].HitsContainers;
        //        foreach (var hitContainer in hitContainers)
        //        {
        //            if (hitContainer.Hits != null)
        //            {
        //                foreach (var hit in hitContainer.Hits)
        //                {
        //                    Global.Log.LogInformation($"Entered hit");
        //                    var spItem = hit.Resource as DriveItem;
        //                    Global.Log.LogInformation($"{spItem.Name} : {spItem.WebUrl}");
        //                    results.Add(new Models.Search.SearchResult() { Name = spItem.Name });
        //                }
        //            }
        //        }
        //    }

        //    return results;
        //}


        public virtual async Task<ListItem> GetListItemAsync(string driveId, string driveItemId)
        {
            _graphServiceClient = await GetGraphServiceClientAsync();

            return await _graphServiceClient.Drives[driveId].Items[driveItemId].ListItem.GetAsync();
        }

        public virtual async Task<ListItem> GetListItemAsync(string domain, string siteId, string webId, string listId, string itemId)
        {
            _graphServiceClient = await GetGraphServiceClientAsync();

            return await _graphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Items[itemId].GetAsync();
        }

        //public virtual async Task<ISiteDrivesCollectionPage> GetDriveCollectionPageAsync(Uri uri)
        //{
        //    _graphServiceClient = await GetGraphServiceClientAsync();

        //    return await _graphServiceClient.Sites.GetByPath(uri.AbsolutePath + ":", uri.Host).Drives.Request().GetAsync();
        //}

        public virtual async Task<Stream?> GetFileContentAsync(string driveId, string driveItemId)
        {
            try
            {
                _graphServiceClient = await GetGraphServiceClientAsync();
                var file = await _graphServiceClient.Drives[driveId].Items[driveItemId].Content.GetAsync();
                return file;
            }
            catch (ODataError odataError)
            {
                Global.Log.LogError($"Error getting file content: {odataError.Error?.Message}. Errorcode: {odataError.Error?.Code}");
                return null;
            }
            catch (ApiException apiex)
            {
                Global.Log.LogError($"Error getting file content: {apiex.Message}");
                return null;
            }
            catch (Exception e)
            {
                Global.Log.LogError($"Error getting file content (Unhandled): {e.Message}");
                return null;
            }
        }

        public virtual async Task<Drive?> GetDriveAsync(string driveId, string select)
        {
            try
            {
                _graphServiceClient = await GetGraphServiceClientAsync();
                var drive = await _graphServiceClient.Drives[driveId].GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = select.Split(",");
                });
                return drive;
            }
            catch (ODataError odataError)
            {
                Global.Log.LogError($"Error getting drive: {odataError.Error?.Message}. Errorcode: {odataError.Error?.Code}");
                return null;
            }
            catch (ApiException apiex)
            {
                Global.Log.LogError($"Error getting drive: {apiex.Message}");
                return null;
            }
            catch (Exception e)
            {
                Global.Log.LogError($"Error getting drive (Unhandled): {e.Message}");
                return null;
            }
        }

        public virtual async Task<Drive?> GetDriveAsync(string domain, string siteId, string webId, string listId, string select)
        {
            try
            {
                _graphServiceClient = await GetGraphServiceClientAsync();
                var drive = await _graphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Drive.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = select.Split(",");
                });
                return drive;
            }
            catch (ODataError odataError)
            {
                string message = $"Error getting drive: {odataError.Error?.Message}. Errorcode: {odataError.Error?.Code}";
                Global.Log.LogError(message);
                return null;
            }
            catch (ApiException apiex)
            {
                string message = $"Error getting drive: {apiex.Message}";
                Global.Log.LogError(message);
                return null;
            }
            catch (Exception e)
            {
                Global.Log.LogError($"Error getting drive (Unhandled): {e.Message}");
                return null;
            }
        }


        public virtual async Task SetMetadataAsync(ArchiveFileData<string, object> filedata, DriveItem uploadedItem)
        {
            try
            {
                _graphServiceClient = await GetGraphServiceClientAsync();
                if (filedata.SetMetadata)
                {
                    var extendedItem = await _graphServiceClient.Drives[filedata.DriveID].Items[uploadedItem.Id].GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = new string[] { "id", "sharepointids", "weburl" }; ;
                    });
                    if (extendedItem != null)
                    {
                        if (filedata.UseCSOM)
                        {
                            await SetMetadataByCSOMAsync(filedata.FileMetadata, extendedItem);
                        }
                        else
                        {
                            await SetMetadataByGraphAsync(filedata.FileMetadata, extendedItem);
                        }
                        Global.Log.LogInformation("Completed updating {BlobFileMetadataCount} metadata values for {SPPath}", filedata.FileMetadata.Count, extendedItem.WebUrl);
                    }
                    else
                    {
                        Global.Log.LogInformation("DriveItem not found in function SetMetadataAsync");
                    }
                }
                else
                {
                    Global.Log.LogInformation("File contained no metadata to update");
                }
            }
            catch (ODataError odataError)
            {
                string message = $"Error setting metadata: {odataError.Error?.Message}. Errorcode: {odataError.Error?.Code}";
                Global.Log.LogError(message);
                throw;
            }
            catch (ApiException apiex)
            {
                string message = $"Error setting metadata: {apiex.Message}";
                Global.Log.LogError(message);
                throw;
            }
            catch (Exception e)
            {
                Global.Log.LogError($"Error setting metadata (Unhandled): {e.Message}");
                throw;
            }
        }

        public virtual async Task SetMetadataByGraphAsync(Dictionary<string, object> fileMetadata, DriveItem extendedItem)
        {
            Global.Log.LogInformation("Using Graph for updating list item");
            try
            {
                _graphServiceClient = await GetGraphServiceClientAsync();
                string siteId = extendedItem.SharepointIds.SiteId;
                string listId = extendedItem.SharepointIds.ListId;
                string itemId = extendedItem.SharepointIds.ListItemId;

                Global.Log.LogInformation("Updating siteId {SiteId} listId {ListId} itemId {ListItemId} with {BlobFileMetadataCount} metadata changes.", siteId, listId, itemId, fileMetadata.Count);

                var fieldValueSet = new FieldValueSet
                {
                    AdditionalData = fileMetadata
                };

                Global.Log.LogInformation("Metadata to send: {FileMetadata}", JsonConvert.SerializeObject(fieldValueSet));

                var itemUpdateResult = await _graphServiceClient.Sites[siteId].Lists[listId].Items[itemId].PatchAsync(new Microsoft.Graph.Models.ListItem() { Fields = fieldValueSet });
            }
            catch (ODataError odataError)
            {
                string message = $"Error setting metadata drive: {odataError.Error?.Message}. Errorcode: {odataError.Error?.Code}";
                Global.Log.LogError(message);
                throw;
            }
            catch (ApiException apiex)
            {
                string message = $"Error setting metadata : {apiex.Message}";
                Global.Log.LogError(message);
                throw;
            }
            catch (Exception itemUpdateException)
            {
                Global.Log.LogError(itemUpdateException, "Update of item metadata failed for {SPPath}. {ErrorMessage}", extendedItem.WebUrl, itemUpdateException.Message);
                throw;
            }
        }

        public virtual async Task SetMetadataByGraphAsync(Dictionary<string, object> fileMetadata, string siteId, string listId, string itemId)
        {
            Global.Log.LogInformation("Using Graph for updating list item");
            try
            {
                _graphServiceClient = await GetGraphServiceClientAsync();
                Global.Log.LogInformation("Updating siteId {SiteId} listId {ListId} itemId {ListItemId} with {BlobFileMetadataCount} metadata changes.", siteId, listId, itemId, fileMetadata.Count);

                var fieldValueSet = new FieldValueSet
                {
                    AdditionalData = fileMetadata
                };

                Global.Log.LogInformation("Metadata to send: {FileMetadata}", JsonConvert.SerializeObject(fieldValueSet));

                var itemUpdateResult = await _graphServiceClient.Sites[siteId].Lists[listId].Items[itemId].PatchAsync(new Microsoft.Graph.Models.ListItem() { Fields = fieldValueSet });
            }
            catch (ODataError odataError)
            {
                string message = $"Error setting metadata drive: {odataError.Error?.Message}. Errorcode: {odataError.Error?.Code}";
                Global.Log.LogError(message);
                throw;
            }
            catch (ApiException apiex)
            {
                string message = $"Error setting metadata : {apiex.Message}";
                Global.Log.LogError(message);
                throw;
            }
            catch (Exception itemUpdateException)
            {
                Global.Log.LogError(itemUpdateException, "Update of item metadata failed for {SPPath}. {ErrorMessage}", extendedItem.WebUrl, itemUpdateException.Message);
                throw;
            }
        }

        public virtual async Task SetMetadataByCSOMAsync(Dictionary<string, object> fileMetadata, DriveItem extendedItem)
        {
            Global.Log.LogInformation("Using CSOM for updating list item");

            try
            {
                // get paraeters from driveitem
                var listId = new Guid(extendedItem.SharepointIds.ListId);
                var itemId = extendedItem.SharepointIds.ListItemId;
                var fileUri = new Uri(extendedItem.WebUrl);
                var webUrl = extendedItem.WebUrl.Substring(0, extendedItem.WebUrl.LastIndexOf("/"));
                webUrl = webUrl.Substring(0, webUrl.LastIndexOf("/"));

                Global.Log.LogInformation("Updating web url: {webUrl} listId: {ListId} itemId: {ListItemId} with {BlobFileMetadataCount} metadata changes.", webUrl, listId, itemId, fileMetadata.Count);
                 
                // Get metadata from file
                var itemMetadata = new List<ListItemFormUpdateValue>();
                foreach (var metadata in fileMetadata)
                {
                    itemMetadata.Add(new ListItemFormUpdateValue() { FieldName = metadata.Key, FieldValue = metadata.Value.ToString() });
                }

                var sharePointService = new SharePointService();
                await sharePointService.SetListItemMetadata(fileUri, webUrl, listId, itemId, itemMetadata);
            }
            catch (Exception itemUpdateException)
            {
                Global.Log.LogError(itemUpdateException, "Update of item metadata failed for {SPPath}. {ErrorMessage}", extendedItem.WebUrl, itemUpdateException.Message);
                throw;
            }
        }
    }

    public class TokenProvider : IAccessTokenProvider
    {
        public AccessToken GraphToken { get; set; }

        public TokenProvider(AccessToken token) {
            GraphToken = token;
        }

        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> additionalAuthenticationContext = default,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GraphToken.Token);
        }

        public AllowedHostsValidator AllowedHostsValidator { get; }
    }
}
