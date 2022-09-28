using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MCOM.Extensions;
using MCOM.Models;
using MCOM.Models.ScanOnDemand;
using MCOM.Services;
using MCOM.Utilities;

namespace MCOM.Functions
{
    public class GetFileProperties
    {
        private IGraphService _graphService;

        public GetFileProperties(IGraphService graphService)
        {
            _graphService = graphService;
        }

        [Function("GetFileProperties")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("GetFileProperties");

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

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "GetFileProperties", "Functions"))
            {

                HttpResponseData response = null;

                try
                {
                    // Read from request
                    var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                    Global.Log.LogInformation($"RequestBody: {requestBody}");

                    var data = JsonConvert.DeserializeObject<ScanRequestPayload>(requestBody);

                    // Get SharePoint listitem fields and add them to the json object
                    var listItem = await _graphService.GetListItemAsync(Global.SharePointDomain, data.SiteId, data.WebId, data.ListId, data.ItemId);
                    var listItemFields = listItem.Fields.AdditionalData;

                    // Get string values from listItemFields and remove special properties
                    var fileMetaData = new Dictionary<string, object>()
                    {
                        { "FilePath", listItem.WebUrl }
                    };
                    listItemFields.Where(x => (!x.Key.StartsWith("_") || x.Key.Equals("_Comments", StringComparison.OrdinalIgnoreCase)) && !x.Key.StartsWith("@")).ForEach(x => fileMetaData.Add(x.Key, x.Value.ToString()));

                    // Convert to json string
                    var jsonMetadata = JsonConvert.SerializeObject(fileMetaData);

                    response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                    response.WriteString(JsonConvert.SerializeObject(new { Response = jsonMetadata }));

                    return response;
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
