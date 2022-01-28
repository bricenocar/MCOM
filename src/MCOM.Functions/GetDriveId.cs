using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MCOM.Models;
using MCOM.Services;
using MCOM.Utilities;

namespace MCOM.Functions
{
    public class GetDriveId
    {
        private IGraphService _graphService;

        public GetDriveId(IGraphService graphService)
        {
            _graphService = graphService;
        }

        [Function("GetDriveId")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("GetDriveId");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                Global.Log.LogError(e, "An error occured when setting environmental variables", e);
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                resp.WriteString("Config values missing or bad formatted in app config");
                return resp;
            }

            // Init graph
            await _graphService.GetGraphServiceClientAsync();

            // Parse query parameters
            var query = QueryHelpers.ParseQuery(req.Url.Query);
           
            // Get parameters from body in case og POST
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Get query params
            string siteUrl = query.Keys.Contains("siteUrl") ? query["siteUrl"] : data?.siteUrl;
            string libraryName = query.Keys.Contains("libraryName") ? query["libraryName"] : data?.libraryName;

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "GetDriveId", "Archiving"))
            {
                HttpResponseData response = null;
                if (string.IsNullOrEmpty(siteUrl) || string.IsNullOrEmpty(libraryName))
                {
                    Global.Log.LogError(new NullReferenceException(), "Request is missing mandatory parameter for siteUrl or libraryName");
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    response.WriteString("Request is missing mandatory parameter siteUrl or libraryName");
                    return response;
                }

                var uri = new Uri(siteUrl);
                var now = DateTime.Now;
                 
                try
                {
                    var drives = await _graphService.GetDriveCollectionPageAsync(uri);
                    var drive = drives.FirstOrDefault(d => d.Name.Equals(libraryName, StringComparison.InvariantCultureIgnoreCase));
                    if (drive != null)
                    {
                        Global.Log.LogInformation("Drive id [{DriveID}] found for {LibraryUrl}", drive.Id, drive.WebUrl);
                        response = req.CreateResponse(HttpStatusCode.OK);
                        response.WriteString(JsonConvert.SerializeObject(new { DriveId = drive.Id }));
                        return response;
                    }
                }
                catch (Exception e)
                {
                    Global.Log.LogError(e, "An error occured when retrieving drives from {SiteUrl}. Error message:{ErrorMessage}. StackTrace: {ErrorStackTrace}", uri.OriginalString, e.Message, e.StackTrace);
                    response = req.CreateResponse(HttpStatusCode.InternalServerError);
                    response.WriteString(e.Message);
                    return response;
                }

                Global.Log.LogError(new NullReferenceException(), "Library not found at {LibraryUrl}", libraryName);
                response = req.CreateResponse(HttpStatusCode.NotFound);
                response.WriteString($"Library not found at {libraryName}");
                return response;
            }            
        }
    }
}
