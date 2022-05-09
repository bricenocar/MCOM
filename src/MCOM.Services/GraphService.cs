using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using MCOM.Models;
using MCOM.Utilities;

namespace MCOM.Services
{
    public interface IGraphService
    {
        Task<GraphServiceClient> GetGraphServiceClientAsync();

        Task<DriveItem> UploadDriveItemAsync(string driveId, string fileName, Stream stream);

        Task<IDriveItemSearchCollectionPage> SearchDriveAsync(string driveId, string fileName);

        Task<Microsoft.Graph.ListItem> GetListItemAsync(string driveId, string driveItemId);
        Task<Microsoft.Graph.ListItem> GetListItemAsync(string domain, string siteId, string webId, string listId, string itemId);

        Task<List<MCOMSearchResult>> SearchItemAsync(string documentId);

        Task<ISiteDrivesCollectionPage> GetDriveCollectionPageAsync(Uri uri);

        Task<UploadResult<DriveItem>> UploadFileAsync(string driveId, string fileName, Stream stream, int maxSliceSize, string blobFilePath);

        Task<Stream> GetFileContentAsync(string driveId, string driveItemId);

        Task<Drive> GetDriveAsync(string driveId, string select);
        Task<Drive> GetDriveAsync(string domain, string siteId, string webId, string listId, string select);

        Task SetMetadataAsync(ArchiveFileData<string, object> filedata, DriveItem uploadedItem);

        Task SetMetadataByGraphAsync(Dictionary<string, object> fileMetadata, DriveItem extendedItem);

        Task SetMetadataByCSOMAsync(Dictionary<string, object> fileMetadata, DriveItem extendedItem);
    }

    public class GraphService : IGraphService
    {
        public GraphServiceClient GraphServiceClient { get; set; }
        public AccessToken GraphToken { get; set; }


        public async Task<GraphServiceClient> GetGraphServiceClientAsync()
        {
            if (GraphServiceClient == null || GraphToken.ExpiresOn >= DateTime.Now)
            {
                GraphToken = await AzureUtilities.GetAzureServiceTokenAsync("https://graph.microsoft.com");
                GraphServiceClient = new GraphServiceClient(
                new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", GraphToken.Token);
                    return Task.CompletedTask;
                }));
            }

            return GraphServiceClient;
        }

        public virtual async Task<DriveItem> UploadDriveItemAsync(string driveId, string fileName, Stream stream)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            return await GraphServiceClient.Drives[driveId].Root.ItemWithPath(fileName).Content.Request().PutAsync<DriveItem>(stream);
        }

        public virtual async Task<Drive> GetDriveAsync(string driveId, string select)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            return await GraphServiceClient.Drives[driveId].Request().Select(select).GetAsync();
        }

        public virtual async Task<Drive> GetDriveAsync(string domain, string siteId, string webId, string listId, string select)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            return await GraphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Drive.Request().Select(select).GetAsync();
        }

        public virtual async Task<IDriveItemSearchCollectionPage> SearchDriveAsync(string driveId, string fileName)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            return await GraphServiceClient.Drives[driveId].Root.Search(fileName).Request().GetAsync();
        }

        public virtual async Task<Microsoft.Graph.ListItem> GetListItemAsync(string driveId, string driveItemId)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            return await GraphServiceClient.Drives[driveId].Items[driveItemId].ListItem.Request().GetAsync();
        }

        public virtual async Task<Microsoft.Graph.ListItem> GetListItemAsync(string domain, string siteId, string webId, string listId, string itemId)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            return await GraphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Items[itemId].Request().GetAsync();
        }

        public virtual async Task<List<MCOMSearchResult>> SearchItemAsync(string documentId)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();
            var requests = new List<SearchRequestObject>()
            {
                new SearchRequestObject
                {
                    EntityTypes = new List<EntityType>()
                    {
                        EntityType.DriveItem
                    },
                    Query = new SearchQuery
                    {
                        QueryString = $"{documentId}"
                    },
                    From = 0,
                    Size = 25
                }
            };
            var response = await GraphServiceClient.Search.Query(requests)
                .Request()
                .PostAsync();

            List<MCOMSearchResult> results = new List<MCOMSearchResult>();
            Global.Log.LogInformation($"Count: {response.CurrentPage.Count}");
            if (response.CurrentPage.Count > 0)
            {
                var hitContainers = response.CurrentPage[0].HitsContainers;                
                foreach (var hitContainer in hitContainers)
                {                    
                    if (hitContainer.Hits != null)
                    {
                        foreach (var hit in hitContainer.Hits)
                        {
                            Global.Log.LogInformation($"Entered hit");
                            var spItem = hit.Resource as DriveItem;
                            Global.Log.LogInformation($"{spItem.Name} : {spItem.WebUrl}");
                            results.Add(new MCOMSearchResult() { Name = spItem.Name });
                        }
                    }
                }
            }

            return results;
        }

        public virtual async Task<ISiteDrivesCollectionPage> GetDriveCollectionPageAsync(Uri uri)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            return await GraphServiceClient.Sites.GetByPath(uri.AbsolutePath + ":", uri.Host).Drives.Request().GetAsync();
        }

        public virtual async Task<Stream> GetFileContentAsync(string driveId, string driveItemId)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            return await GraphServiceClient.Drives[driveId].Items[driveItemId].Content.Request().GetAsync();
        }

        public virtual async Task<UploadResult<DriveItem>> UploadFileAsync(string driveId, string fileName, Stream stream, int maxSliceSize, string blobFilePath)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            // Use properties to specify the conflict behavior
            var uploadProps = new DriveItemUploadableProperties
            {
                ODataType = null,
                AdditionalData = new Dictionary<string, object>
                {
                    { "@microsoft.graph.conflictBehavior", "fail"},
                }
            };

            var uploadSession = await GraphServiceClient.Drives[driveId].Root.ItemWithPath(fileName).CreateUploadSession(uploadProps).Request().PostAsync();
            var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, stream, maxSliceSize);

            // Create a callback that is invoked after each slice is uploaded
            var progress = new Progress<long>(prog =>
            {
                Global.Log.LogInformation("Uploaded {UploadProgress} bytes of {StreamLength} bytes for {BlobFilePath}", prog, stream.Length, blobFilePath);
            });

            // Upload the file
            return await fileUploadTask.UploadAsync(progress);
        }

        public virtual async Task SetMetadataAsync(ArchiveFileData<string, object> filedata, DriveItem uploadedItem)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            if (filedata.SetMetadata)
            {
                var extendedItem = await GraphServiceClient.Drives[filedata.DriveID].Items[uploadedItem.Id].Request().Select("id,sharepointids,weburl").GetAsync();
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
                Global.Log.LogInformation("File contained no metadata to update");
            }
        }

        public virtual async Task SetMetadataByGraphAsync(Dictionary<string, object> fileMetadata, DriveItem extendedItem)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            Global.Log.LogInformation("Using Graph for updating list item");

            try
            {
                string siteId = extendedItem.SharepointIds.SiteId;
                string listId = extendedItem.SharepointIds.ListId;
                string itemId = extendedItem.SharepointIds.ListItemId;

                Global.Log.LogInformation("Updating siteId {SiteId} listId {ListId} itemId {ListItemId} with {BlobFileMetadataCount} metadata changes.", siteId, listId, itemId, fileMetadata.Count);

                var fieldValueSet = new FieldValueSet
                {
                    AdditionalData = fileMetadata
                };

                Global.Log.LogInformation("Metadata to send: {FileMetadata}", JsonConvert.SerializeObject(fieldValueSet));

                var itemUpdateResult = await GraphServiceClient.Sites[siteId].Lists[listId].Items[itemId]
                    .Request()
                    .UpdateAsync(new Microsoft.Graph.ListItem() { Fields = fieldValueSet });
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
                var listId = new Guid(extendedItem.SharepointIds.ListId);
                var itemId = extendedItem.SharepointIds.ListItemId;
                var fileUri = new Uri(extendedItem.WebUrl);
                var webUrl = extendedItem.WebUrl.Substring(0, extendedItem.WebUrl.LastIndexOf("/"));

                webUrl = webUrl.Substring(0, webUrl.LastIndexOf("/"));

                Global.Log.LogInformation("Updating web url: {webUrl} listId: {ListId} itemId: {ListItemId} with {BlobFileMetadataCount} metadata changes.", webUrl, listId, itemId, fileMetadata.Count);

                var accessToken = await AzureUtilities.GetAzureServiceTokenAsync(fileUri.Scheme + Uri.SchemeDelimiter + fileUri.Host);
                var context = new ClientContext(webUrl);
                context.ExecutingWebRequest += (sender, e) =>
                {
                    e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken.Token;
                };

                var list = context.Web.Lists.GetById(listId);
                var item = list.GetItemById(itemId);

                var itemMetadata = new List<ListItemFormUpdateValue>();
                foreach (var metadata in fileMetadata)
                {
                    itemMetadata.Add(new ListItemFormUpdateValue() { FieldName = metadata.Key, FieldValue = metadata.Value.ToString() });
                }

                var returnValues = item.ValidateUpdateListItem(itemMetadata, false, string.Empty, true, true);
                await context.ExecuteQueryAsync();

                Global.Log.LogInformation("Completed updating metadata. Checking for individual errors.");

                var hasMetadataError = false;
                foreach (var returnValue in returnValues)
                {
                    if (returnValue.HasException)
                    {
                        hasMetadataError = true;
                        Global.Log.LogError(new NullReferenceException(), "An error occured when updating metadata field {MetadataField} with value {MetadataValue}. {ErrorCode} - {ErrorMessage}", returnValue.FieldName, returnValue.FieldValue, returnValue.ErrorCode, returnValue.ErrorMessage);
                    }
                }
                if (!hasMetadataError)
                {
                    Global.Log.LogInformation("No errors occured");
                }
            }
            catch (Exception itemUpdateException)
            {
                Global.Log.LogError(itemUpdateException, "Update of item metadata failed for {SPPath}. {ErrorMessage}", extendedItem.WebUrl, itemUpdateException.Message);
                throw;
            }
        }
    }
}
