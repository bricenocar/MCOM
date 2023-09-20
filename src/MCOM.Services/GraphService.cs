using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Identity;
using Microsoft.Graph.IdentityProviders;
using Microsoft.Graph.Models;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using MCOM.Models;
using MCOM.Utilities;
using MCOM.Models.Archiving;
using DriveUpload = Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using Azure.Identity;
using Microsoft.Graph.Authentication;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Threading;
using Newtonsoft.Json.Linq;
using Microsoft.VisualBasic;
using Microsoft.Graph.Drives.Item.Items.Item.SearchWithQ;
using Microsoft.Graph.Drives.Item.SearchWithQ;

namespace MCOM.Services
{
    public interface IGraphService
    {
        Task<GraphServiceClient> GetGraphServiceClientAsync();

        Task<Microsoft.Graph.Models.Site> CreateSiteAsync(Microsoft.Graph.Models.Site site, string sensitivityLabel);

        Task<DriveItem> CopySharePointFileAsync(string domain, string siteId, string webId, string listId, string itemId, string name, ItemReference parentReference);

        Task<DriveItem> UploadSharePointFileAsync(string domain, string siteId, string webId, string listId, string fileName, Stream stream);
        Task<DriveItem> ReplaceSharePointFileContentAsync(string domain, string siteId, string webId, string listId, string itemId, Stream stream);
        Task<DriveItem> UploadDriveItemAsync(string driveId, string fileName, Stream stream);
        
        Task<UploadResult<DriveItem>> UploadLargeSharePointFileAsync(string domain, string siteId, string webId, string listId, Stream stream, int maxSliceSize, string fileName, string conflictBehaviour = "fail");
        Task<UploadResult<DriveItem>> ReplaceLargeSharePointFileAsync(string domain, string siteId, string webId, string listId, string itemId, Stream stream, int maxSliceSize, string fileName, string conflictBehaviour = "fail");
        Task<UploadResult<DriveItem>> UploadLargeDriveItemAsync(string driveId, string fileName, Stream stream, int maxSliceSize, string blobFilePath, string conflictBehaviour = "fail");

        Task<IListItemsCollectionPage> SearchItemAsync(string domain, string siteId, string webId, string listId, string filter);
        Task<Microsoft.Graph.Drives.Item.SearchWithQ.SearchWithQResponse> SearchDriveAsync(string driveId, string queryString);
        Task<List<Models.Search.SearchResult>> SearchItemAsync(string documentId);

        Task<Microsoft.Graph.Models.ListItem> GetListItemAsync(string driveId, string driveItemId);
        Task<Microsoft.Graph.Models.ListItem> GetListItemAsync(string domain, string siteId, string webId, string listId, string itemId);
        Task<ISiteDrivesCollectionPage> GetDriveCollectionPageAsync(Uri uri);
        Task<Stream> GetFileContentAsync(string driveId, string driveItemId);
        Task<Drive> GetDriveAsync(string driveId, string select);
        Task<Drive> GetDriveAsync(string domain, string siteId, string webId, string listId, string select);

        Task SetMetadataAsync(ArchiveFileData<string, object> filedata, DriveItem uploadedItem);
        Task SetMetadataByGraphAsync(Dictionary<string, object> fileMetadata, DriveItem extendedItem);
        Task SetMetadataByGraphAsync(Dictionary<string, object> fileMetadata, string siteId, string listId, string itemId);
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
                TokenProvider provider = new TokenProvider(GraphToken);
                var authenticationProvider = new BaseBearerTokenAuthenticationProvider(provider);
                GraphServiceClient = new GraphServiceClient(authenticationProvider);
            }

            return GraphServiceClient;
        }



        #region Sites
        public virtual async Task<Microsoft.Graph.Models.Site> CreateSiteAsync(Microsoft.Graph.Models.Site site, string sensitivityLabel)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            // Create the SharePoint site
            var createdSite = await GraphServiceClient.Sites.Add.PostAsync(site);

            var sensitivityLabelAssignment = new SensitivityLabelAssignment
            {
                 SensitivityLabelId = sensitivityLabel
            };

            // Apply the sensitivity label
            await GraphServiceClient.Sites[createdSite.Id].SiteCollection.SensitivityLabel
                .SetAsync(sensitivityLabelAssignment);
            
            return createdSite;
        }
        #endregion

        public virtual async Task<DriveItem> ReplaceSharePointFileContentAsync(string domain, string siteId, string webId, string listId, string itemId, Stream stream)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            return await GraphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Items[itemId].DriveItem.Content.Request().PutAsync<DriveItem>(stream);
        }

        

        public virtual async Task<DriveItem> UploadSharePointFileAsync(string domain, string siteId, string webId, string listId, string fileName, Stream stream)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            return await GraphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Drive.Root.ItemWithPath(fileName).Content.Request().PutAsync<DriveItem>(stream);
        }

        public virtual async Task<DriveItem> CopySharePointFileAsync(string domain, string siteId, string webId, string listId, string itemId, string name, ItemReference parentReference)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            return await GraphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Items[itemId].DriveItem.Copy(name, parentReference).Request().PostAsync();
        }

        public virtual async Task<DriveItem> UploadDriveItemAsync(string driveId, string fileName, Stream stream)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            return await GraphServiceClient.Drives[driveId].Root.ItemWithPath(fileName).Content.Request().PutAsync<DriveItem>(stream);
        }

        public virtual async Task<UploadResult<DriveItem>> UploadLargeSharePointFileAsync(string domain, string siteId, string webId, string listId, Stream stream, int maxSliceSize, string fileName, string conflictBehaviour = "fail")
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

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
            var drive= await GraphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Drive.GetAsync();
            var uploadSession = await GraphServiceClient.Drives[drive?.Id].Root                
                .ItemWithPath(fileName)
                .CreateUploadSession
                .PostAsync(uploadSessionRequestBody);

            var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, stream, maxSliceSize, GraphServiceClient.RequestAdapter);

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
            GraphServiceClient = await GetGraphServiceClientAsync();

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

            var drive = await GraphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Drive.GetAsync();
            //var driveItem = await GraphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Items[itemId].DriveItem.GetAsync();
            var uploadSession = await GraphServiceClient.Drives[drive?.Id]
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

        public virtual async Task<UploadResult<DriveItem>> UploadLargeDriveItemAsync(string driveId, string fileName, Stream stream, int maxSliceSize, string blobFilePath, string conflictBehaviour = "fail")
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            // Use properties to specify the conflict behavior
            var uploadProps = new DriveItemUploadableProperties
            {
                OdataType = null,
                AdditionalData = new Dictionary<string, object>
                {
                    { "@microsoft.graph.conflictBehavior", conflictBehaviour},
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


        public virtual async Task<ListItemCollectionResponse> SearchItemAsync(string domain, string siteId, string webId, string listId, string filter)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            return await GraphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Items.GetAsync(
                requestConfig =>
                {
                    requestConfig.QueryParameters.Filter = filter;
                }
            );
        }

        public virtual async Task<Microsoft.Graph.Drives.Item.SearchWithQ.SearchWithQResponse> SearchDriveAsync(string driveId, string queryString)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            //return await GraphServiceClient.Drives[driveId].Root.Search(queryString).Request().GetAsync();
            return await GraphServiceClient.Drives[driveId].SearchWithQ(queryString).GetAsync();
        }

        public virtual async Task<List<Models.Search.SearchResult>> SearchItemAsync(string queryString)
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
                        QueryString = $"{queryString}"
                    },
                    From = 0,
                    Size = 25
                }
            };
            var response = await GraphServiceClient.Search.Query(requests)
                .Request()
                .PostAsync();

            var results = new List<Models.Search.SearchResult>();
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
                            results.Add(new Models.Search.SearchResult() { Name = spItem.Name });
                        }
                    }
                }
            }

            return results;
        }


        public virtual async Task<Microsoft.Graph.Models.ListItem> GetListItemAsync(string driveId, string driveItemId)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            return await GraphServiceClient.Drives[driveId].Items[driveItemId].ListItem.Request().GetAsync();
        }

        public virtual async Task<Microsoft.Graph.Models.ListItem> GetListItemAsync(string domain, string siteId, string webId, string listId, string itemId)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            return await GraphServiceClient.Sites[$"{domain},{siteId},{webId}"].Lists[listId].Items[itemId].Request().GetAsync();
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

        public virtual async Task SetMetadataByGraphAsync(Dictionary<string, object> fileMetadata, string siteId, string listId, string itemId)
        {
            GraphServiceClient = await GetGraphServiceClientAsync();

            Global.Log.LogInformation("Using Graph for updating list item");

            try
            {
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
                Global.Log.LogError(itemUpdateException, "Update of item metadata failed for item with id {ListItemId}. {ErrorMessage}", itemId, itemUpdateException.Message);
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
