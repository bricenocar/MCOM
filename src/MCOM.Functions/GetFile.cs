using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Search.Query;
using Newtonsoft.Json;
using MCOM.Models;
using MCOM.Models.AppInsights;
using MCOM.Services;
using MCOM.Utilities;


namespace MCOM.Functions
{
    public class GetFile
    {
        private IGraphService _graphService;
        private ISharePointService _sharePointService;
        private IAppInsightsService _appInsightsService;
        private IAzureService _azureService;

        public GetFile(IAzureService azureService, ISharePointService sharePointService, IGraphService graphService, IAppInsightsService appInsightsService)
        {
            _azureService = azureService;
            _graphService = graphService;
            _sharePointService = sharePointService;
            _appInsightsService = appInsightsService;
        }

        [Function("GetFile")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("GetFile");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                Global.Log.LogError(e, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", e.Message);
                throw;
            }

            Activity.Current?.AddTag("MCOMOperation", "GetFile");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "GetFile", "Archiving"))
            {
                HttpResponseData response = null;

                // Parse query parameters
                var query = QueryHelpers.ParseQuery(req.Url.Query);

                // Get parameters from body in case og POST
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);

                // Get query params
                var msg = "";
                string documentId = query.Keys.Contains("documentId") ? query["documentId"] : data?.documentId;
                string driveId = query.Keys.Contains("driveId") ? query["driveId"] : data?.driveId;
                string documentIdField = query.Keys.Contains("documentIdField") ? query["documentIdField"] : data?.documentIdField;
                documentIdField = string.IsNullOrEmpty(documentIdField) ? "LRMHPECMRecordID" : documentIdField;

                // Validate mandatory values in body
                if (string.IsNullOrEmpty(documentId) || string.IsNullOrEmpty(documentIdField))
                {
                    msg = $"Mandatory parameter not provided 'documentId' or 'documentIdField'";
                    Global.Log.LogError(new NullReferenceException(), msg + ". DocumentId: {DocumentId}", documentId);
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    response.WriteString(JsonConvert.SerializeObject(new
                    {
                        Message = msg,
                        Status = "Error"
                    }));
                    return response;
                }

                // Initialize graph service and app insights service
                await _graphService.GetGraphServiceClientAsync();
                await _appInsightsService.GetApplicationInsightsDataClientAsync();

                // Validate SharePointUrl
                if (string.IsNullOrEmpty(Global.SharePointUrl))
                {
                    response = req.CreateResponse(HttpStatusCode.InternalServerError);
                    response.WriteString(JsonConvert.SerializeObject(new
                    {
                        Message = "Failed to get SharePointUrl from App settings.",
                        Status = "Error"
                    }));
                    return response;
                }

                // Get SharePoint token using managed identity
                var sharepointUri = new Uri(Global.SharePointUrl);
                var accessToken = await _azureService.GetAzureServiceTokenAsync(sharepointUri);

                // Get SharePoint context using access token            
                

                // Check presence of drive Id in the request
                if (string.IsNullOrEmpty(driveId) || driveId.Equals("null"))
                {
                    // Check if the document id is a guid (it means that the document came through the archiving pipeline and not from HPECM migration)
                    if(StringUtilities.IsGuid(documentId))
                    {
                        // Return trace logs from app insights with status of the document
                        return await GetEventsFromAppInsights(req, documentId);
                    } else
                    {
                        using ClientContext clientContext = _sharePointService.GetClientContext(Global.SharePointUrl, accessToken.Token);
                        // If the file is coming from HPCEM migration then return it via graph search
                        return SearchArchivedFile(req, clientContext, documentId, documentIdField, accessToken.Token);
                    }                    
                }                

                // If drive Id is not empty proceed to check 
                Drive driveObject = null;
                try
                {
                    // Get drive from graph
                    driveObject = await _graphService.GetDriveAsync(driveId, "webUrl,sharepointIds");
                    if (driveObject == null)
                    {
                        Global.Log.LogError(new NullReferenceException(), "Could not find drive with specified ID. driveId: {DocumentId}", driveId);
                        response = req.CreateResponse(HttpStatusCode.NotFound);
                        response.WriteString(JsonConvert.SerializeObject(new
                        {
                            Message = $"Could not find any drive with specified ID. driveId: {driveId}",
                            Status = "Error"
                        }));
                        return response;
                    }
                }
                catch (Exception e)
                {
                    Global.Log.LogError(e, "Failed to get drive from SharePoint. DocumentId: {DocumentId}. DriveId: {DriveId}. Error: {ErrorMessage}", documentId, driveId, e.Message);
                    response = req.CreateResponse(HttpStatusCode.InternalServerError);
                    response.WriteString(JsonConvert.SerializeObject(new
                    {
                        Message = $"Failed to get drive from SharePoint. Error: {e.Message}",
                        Status = "Error"
                    }));
                    return response;
                }

                try
                {
                    // Validate null values
                    if (driveObject.WebUrl == null || driveObject.SharePointIds == null)
                    {
                        msg = "Could not retrieve web url or SharePoint list id from drive object";
                        Global.Log.LogError(new NullReferenceException(), msg + ". DriveId: {DriveId.} DocumentId: {DocumentId}.", driveId, documentId);
                        response = req.CreateResponse(HttpStatusCode.NotFound);
                        response.WriteString(JsonConvert.SerializeObject(new
                        {
                            Message = msg,
                            Status = "Error"
                        }));
                        return response;
                    }
                    // Get SharePoint site URL and context
                    var fileUri = new Uri(driveObject.WebUrl);
                    var webUrl = fileUri.AbsoluteUri.Substring(0, fileUri.AbsoluteUri.LastIndexOf("/"));
                    using ClientContext clientContext = _sharePointService.GetClientContext(webUrl, accessToken.Token);

                    // Get list from drive object
                    var listId = new Guid(driveObject.SharePointIds.ListId);
                    var list = _sharePointService.GetListById(clientContext, listId);
                    
                    response = await GetArchivedFile(req, clientContext, list, documentId, documentIdField);
                }
                catch (Exception e)
                {
                    Global.Log.LogError(e, "Failed to get item from SharePoint. DocumentId: {DocumentId}. Error: {ErrorMessage}", documentId, e.Message);
                    var exResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                    exResponse.WriteString(JsonConvert.SerializeObject(new
                    {
                        Message = $"Failed to get item from SharePoint. Error: {e.Message}",
                        Status = "Error"
                    }));
                    return exResponse;
                }

                return response;
            }
        }

        private HttpResponseData SearchArchivedFile(HttpRequestData req, ClientContext clientContext, string documentId, string documentIdField, string token)
        {            
            HttpResponseData response;
            try
            {
                // Get events
                ResultTable table = _sharePointService.SearchItems(clientContext, documentId);
                if (table.RowCount == 0)
                {
                    response = req.CreateResponse(HttpStatusCode.NotFound);
                    response.WriteString("No result found in Archive");
                }
                else
                {
                    var resultRow = table.ResultRows.FirstOrDefault();
                    var searchResult = new Models.Search.SearchResult()
                    {
                        Name = resultRow["Title"].ToString(),
                        SiteId = resultRow["SPSiteURL"].ToString(),
                        ListId = resultRow["ListID"].ToString(),
                        ListItemId = resultRow["UniqueID"].ToString()
                    };

                    // Get site client context
                    var siteContext = _sharePointService.GetClientContext(resultRow["SPSiteURL"].ToString(), token);

                    // Get list
                    var list = _sharePointService.GetListById(siteContext, new Guid(resultRow["ListID"].ToString()));
                    _sharePointService.Load(siteContext, list);
                    _sharePointService.ExecuteQuery(siteContext);

                    // Get listitem
                    var listItem = _sharePointService.GetListItemByUniqueId(siteContext, list, new Guid(resultRow["UniqueID"].ToString()));
                    _sharePointService.Load(siteContext, listItem);
                    _sharePointService.ExecuteQuery(siteContext);

                    // return file
                    response = GetFileFromListItem(siteContext, req, listItem, documentId);
                   
                }
            }
            catch (Exception e)
            {
                var msg = "Error trying to get items from Archive location";
                Global.Log.LogError(e, msg + ". File unique id: {DocumentId}. Error: {ErrorMessage}. StackTrace: {ErrorStackTrace}", documentId, e.Message, e.StackTrace);
                var exResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                exResponse.WriteString(JsonConvert.SerializeObject(new
                {
                    Message = msg,
                    Status = "Error"
                }));
                return exResponse;
            }

            return response;
        }

        private async Task<HttpResponseData> GetArchivedFile(HttpRequestData req, ClientContext clientContext, Microsoft.SharePoint.Client.List list, string documentId, string documentIdField)
        {
            HttpResponseData response;       

            // Validate if field exists in list
            try
            {
                if (list.Fields.GetByInternalNameOrTitle(documentIdField) == null)
                {
                    string msg = "The field specified does not exists in the library.";
                    Global.Log.LogError(new NullReferenceException(), msg + ". File unique id: {DocumentId}.", documentId);
                    response = req.CreateResponse(HttpStatusCode.NotFound);
                    response.WriteString(JsonConvert.SerializeObject(new
                    {
                        Message = msg,
                        Status = "Error"
                    }));
                    return response;
                }
            }
            catch (ArgumentException e)
            {
                Global.Log.LogError(e, e.Message + ". File unique id: {DocumentId}.", documentId);
                response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.WriteString(JsonConvert.SerializeObject(new
                {
                    Message = e.Message,
                    Status = "Error"
                }));
                return response;
            }

            // Get Item by CAML query
            var camlQuery = new CamlQuery
            {
                ViewXml = $"<View><Query><Where><Eq>" +
                $"<FieldRef Name='{documentIdField}'/>" +
                $"<Value Type='Text'>{documentId}</Value>" +
                $"</Eq></Where></Query><RowLimit>100</RowLimit></View>"
            };

            // Get items from query
            var collListItem = _sharePointService.GetListItems(clientContext, list, camlQuery);
            _sharePointService.Load(clientContext, collListItem);
            _sharePointService.ExecuteQuery(clientContext);

            // Get logs from app insights if no items found in SharePoint
            if (collListItem.Count == 0)
            {
                Global.Log.LogWarning("File with unique id: {DocumentId} was not found in SharePoint, proceeding to get logs from app insights", documentId);
                // Return events from app insights                
                return await GetEventsFromAppInsights(req, documentId);
            }

            // Get file from the item found
            Microsoft.SharePoint.Client.ListItem listItem = collListItem.FirstOrDefault();           
            return GetFileFromListItem(clientContext, req, listItem, documentId);            
        }

        private HttpResponseData GetFileFromListItem(ClientContext clientContext, HttpRequestData req, Microsoft.SharePoint.Client.ListItem listItem, string documentId)
        {
            HttpResponseData response;
            try
            {
                // Get file
                var file = listItem.File;
                clientContext.Load(file);
                clientContext.ExecuteQuery();

                if (file != null)
                {
                    // Return stream
                    using var memoryStream = new MemoryStream();
                    // Get stream
                    var stream = file.OpenBinaryStream();
                    clientContext.ExecuteQuery();

                    if (stream != null && stream.Value != null)
                    {
                        stream.Value.CopyTo(memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        var fileArray = memoryStream.ToArray();
                        Global.Log.LogInformation("Successfully retrieved file requested. File unique id: {DocumentId}. File name: {Filename} ", documentId, file.Name);
                        response = req.CreateResponse(HttpStatusCode.OK);
                        response.Headers.Add("Content-Type", "application/octet-stream");
                        response.WriteBytes(fileArray);
                        return response;
                    }
                    else
                    {
                        string msg = "Could not open file in binary stream.";
                        Global.Log.LogError(new NullReferenceException(), msg + " File unique id: {DocumentId}.", documentId);
                        response = req.CreateResponse(HttpStatusCode.Conflict);
                        response.WriteString(JsonConvert.SerializeObject(new
                        {
                            Message = msg,
                            Status = "Error"
                        }));
                        return response;
                    }
                }
                else
                {
                    string msg = "Could not retrieve file from item";
                    Global.Log.LogError(new NullReferenceException(), msg + ". File unique id: {DocumentId}.", documentId);
                    response = req.CreateResponse(HttpStatusCode.Conflict);
                    response.WriteString(JsonConvert.SerializeObject(new
                    {
                        Message = msg,
                        Status = "Error"
                    }));
                    return response;
                }               
            }
            catch (Exception ex)
            {
                var msg = "Error trying to get file from listitem";
                Global.Log.LogError(ex, msg + ". File unique id: {DocumentId}. Error: {ErrorMessage}", documentId, ex.Message);
                var exResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                exResponse.WriteString(JsonConvert.SerializeObject(new
                {
                    Message = msg,
                    Status = "Error"
                }));
                return exResponse;
            }
        }

        private async Task<HttpResponseData> GetEventsFromAppInsights(HttpRequestData req, string documentId)
        {
            var events = new List<AppInsightsEvent>();

            try
            {
                // Get events
                events = await _appInsightsService.GetCustomEventsByDocumentId("GetFile", documentId);
            }
            catch (Exception e)
            {
                var msg = "Error trying to authenticate against App insights";
                Global.Log.LogError(e, msg + ". File unique id: {DocumentId}. Error: {ErrorMessage}", documentId, e.Message);
                var exResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                exResponse.WriteString(JsonConvert.SerializeObject(new
                {
                    Message = msg,
                    Status = "Error"
                }));
                return exResponse;
            }

            HttpResponseData response;
            if (events.Count == 1 && events.FirstOrDefault().LogLevel.Equals("EmptyData"))
            {
                response = req.CreateResponse(HttpStatusCode.NotFound);                
            }
            else
            {
                response = req.CreateResponse(HttpStatusCode.Accepted);               
            }

            response.WriteString(JsonConvert.SerializeObject(new
            {
                Message = events,
                Status = "InProgress"
            }));

            return response;
        }
    }
}