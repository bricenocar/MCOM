using MCOM.Models;
using MCOM.Services;
using MCOM.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace MCOM.Functions
{
    public class GetSPOData
    {
        private IAzureService _azureService;

        public GetSPOData(IAzureService azureService)
        {
            _azureService = azureService;
        }

        [Function("GetSPOData")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("GetSPOData");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                Global.Log.LogError(e, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", e.Message);
                throw;
            }

            Activity.Current?.AddTag("MCOMOperation", "GetSPOData");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "GetSPOData", "MCOM"))
            {
                try
                {
                    // Read request body
                    string url = await new StreamReader(req.Body).ReadToEndAsync();

                    if (string.IsNullOrEmpty(url))
                    {
                        var missingParamResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                        missingParamResponse.Headers.Add("Content-Type", "application/json");
                        missingParamResponse.WriteString("Missing url param!");

                        return missingParamResponse;
                    }

                    // Get SharePoint token using managed identity
                    var sharepointUri = new Uri(Global.SharePointUrl);
                    var azureAdToken = await _azureService.GetAzureServiceTokenAsync(sharepointUri);

                    // Validate
                    if (azureAdToken.Token != null)
                    {
                        Global.Log.LogInformation("Sending request to Office 365...");

                        // Get data from Office 365
                        var data = await GetDataFromOffice365(url, azureAdToken.Token);

                        // Build and send response back
                        var response = req.CreateResponse(HttpStatusCode.OK);
                        response.Headers.Add("Content-Type", "application/json");
                        response.WriteString(data);

                        return response;
                    }
                }
                catch (Exception ex)
                {
                    Global.Log.LogError(ex, "Error generating the JWT. Error: {ErrorMessage}", ex.Message);
                }
            }

            // Generate error response
            var failResponse = req.CreateResponse(HttpStatusCode.Forbidden);
            failResponse.Headers.Add("Content-Type", "application/json");
            failResponse.WriteString("Error getting data from Office 365");

            return failResponse;
        }

        private async Task<string> GetDataFromOffice365(string url, string token)
        {
            var headers = new Dictionary<string, string>()
            {
                {"Authorization",  $"Bearer {token}"}
            };
            var httpResponse = await HttpClientUtilities.SendAsync(url, headers);

            // Check response
            httpResponse.EnsureSuccessStatusCode();

            // Get data as string from response
            var data = await httpResponse.Content.ReadAsStringAsync();

            return data;
        }

        private class TokenObject
        {
            public string? Token { get; set; }
            public string? ExpiresOn { get; set; }
        }
    }
}
