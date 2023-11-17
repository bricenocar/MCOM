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
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("GetJWT");

            // Get SharePoint token using managed identity
            var sharepointUri = new Uri(Global.SharePointUrl);
            var accessToken = await _azureService.GetAzureServiceTokenAsync(sharepointUri);

            var responseToReturn = req.CreateResponse(HttpStatusCode.OK);
            responseToReturn.Headers.Add("Content-Type", "application/json");
            responseToReturn.WriteString(JsonConvert.SerializeObject(accessToken));

            return responseToReturn;
        }
    }
}
