using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MCOM.Models;
using MCOM.Services;
using MCOM.Utilities;
using MCOM.Extensions;

namespace MCOM.Functions
{
    public class PostScanRequest
    {
        private readonly IBlobService _blobService;
        private IGraphService _graphService;

        public PostScanRequest(IBlobService blobService, IGraphService graphService)
        {
            _blobService = blobService;
            _graphService = graphService;
        }

        [Function("PostScanRequest")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("PostScanRequest");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                string msg = "Config values missing or bad formatted in app config.";
                Global.Log.LogError(e, msg + "Error: {ErrorMessage}", e.Message);
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.WriteString(msg);
                return response;
            }

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "PostScanRequest");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "PostScanRequest", "ScanOnDemand"))
            {
                // Get the request object
                HttpResponseData response = null;
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestBody);

                // Validate data parameters
                if(!data.TryGetValue("SiteId", out var siteId) || !data.TryGetValue("WebId", out var webId) || !data.TryGetValue("ListId", out var listId) || !data.TryGetValue("ItemId", out var itemId))
                {
                    Global.Log.LogError(new ArgumentNullException(), "Missing data parameters on the request body");
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    response.WriteString("Missing data parameters on the request body");
                    return response;
                }       

                try
                {
                    // Generate new order number
                    var orderNumber = Guid.NewGuid();

                    // Add new properties to the json object    
                    data.Add("OrderNumber", orderNumber);
                    data.Add("Status", "Requested"); // TODO ENUM

                    // Get SharePoint listitem fields and add them to the json object
                    var listItem = await _graphService.GetListItemAsync(Global.SharePointDomain, siteId.ToString(), webId.ToString(), listId.ToString(), itemId.ToString());
                    var listItemFields = listItem.Fields.AdditionalData;

                    // Get string values from listItemFields
                    var dataFields = new Dictionary<string, object>();
                    listItemFields.ForEach(x => dataFields.Add(x.Key, x.Value.ToString()));

                    // Merge Dictionaries
                    data.AddRangeNewOnly(dataFields);

                    // Convert to json string
                    var jsonMetadata = JsonConvert.SerializeObject(data);

                    Global.Log.LogInformation("Proceed to save the metadata file into scanrequests container");

                    // Get blob client
                    var metadataUri = new Uri($"https://{Global.BlobStorageAccountName}.blob.core.windows.net/scanrequests/metadata/{orderNumber}.json");
                    var blobClient = _blobService.GetBlobClient(metadataUri);
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonMetadata)))
                    {
                        await blobClient.UploadAsync(stream, Global.BlobOverwriteExistingFile);
                    }

                    Global.Log.LogInformation("Successfully uploaded metadata file without errors. DocumentId: {DocumentId}", orderNumber);

                    response = req.CreateResponse(HttpStatusCode.OK);
                    response.WriteString(jsonMetadata);
                    return response;
                }
                catch (RequestFailedException rEx)
                {
                    if (rEx.ErrorCode.Equals("BlobAlreadyExists"))
                    {
                        Global.Log.LogError(rEx, "File already exists in staging area. Overwrite existing file is set to {OverwriteSetting}.", Global.BlobOverwriteExistingFile);
                        response = req.CreateResponse(HttpStatusCode.Conflict);
                        response.WriteString("Settings dictate that existing files cannot be overwritten and this file already exists in the staging area. Message:" + rEx.Message);
                        return response;
                    }
                    else if (rEx.ErrorCode.Equals("ContainerNotFound"))
                    {
                        Global.Log.LogError(rEx, "The path to staging area ({SourceSystem}) was not found.", "scanrequest");
                        response = req.CreateResponse(HttpStatusCode.FailedDependency);
                        response.WriteString($"ErrorCode: {rEx.ErrorCode}. ErrorMessage: {rEx.Message}");
                        return response;
                    }
                    else
                    {
                        Global.Log.LogError(rEx, "A request failed exception occured. {ErrorMessage}", rEx.Message);
                        response = req.CreateResponse(HttpStatusCode.InternalServerError);
                        response.WriteString($"ErrorCode: {rEx.ErrorCode}. ErrorMessage: {rEx.Message}");
                        return response;
                    }
                }
                catch (Exception ex)
                {
                    Global.Log.LogError(ex, "Error message:{ErrorMessage}. StackTrace: {ErrorStackTrace}", ex.Message, ex.StackTrace);
                    response = req.CreateResponse(HttpStatusCode.InternalServerError);
                    response.WriteString($"Error Message: {ex.Message}. StackTrace: {ex.StackTrace}");
                    return response;
                }
            }
        }
    }
}
