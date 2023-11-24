using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using MCOM.Models;
using MCOM.Services;
using Newtonsoft.Json;
using MCOM.Utilities;
using System.Diagnostics;

namespace MCOM.Functions
{
    public class GetJWT
    {
        private IAzureService _azureService;

        public GetJWT(IAzureService azureService)
        {
            _azureService = azureService;
        }

        [Function("GetJWT")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("GetJWT");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                Global.Log.LogError(e, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", e.Message);
                throw;
            }

            Activity.Current?.AddTag("MCOMOperation", "GetJWT");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "GetJWT", "Provisioning"))
            {
                try
                {
                    // Get SharePoint token using managed identity
                    var sharepointUri = new Uri(Global.SharePointUrl);
                    var accessToken = await _azureService.GetAzureServiceTokenAsync(sharepointUri);

                    var responseToReturn = req.CreateResponse(HttpStatusCode.OK);
                    responseToReturn.Headers.Add("Content-Type", "application/json");
                    responseToReturn.WriteString(JsonConvert.SerializeObject(accessToken));

                    return responseToReturn;
                }
                catch (Exception ex)
                {                    
                    Global.Log.LogError(ex, "Error generating the JWT. Error: {ErrorMessage}", ex.Message);
                }
            }

            // Generate error response
            var failResponse = req.CreateResponse(HttpStatusCode.Forbidden);
            failResponse.Headers.Add("Content-Type", "application/json");
            failResponse.WriteString("Error generating the JWT");

            return failResponse;
        }
    }
}
