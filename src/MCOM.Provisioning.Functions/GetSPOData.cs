using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using MCOM.Models.Azure;
using MCOM.Utilities;
using Microsoft.Extensions.Logging;
using MCOM.Models;
using System.Diagnostics;

namespace MCOM.Provisioning.Functions
{
    public class GetSPOData
    {
        // TODO: Temp solution. Move to a secure place please!!!!!!!!!!!!!!!!!!!!!!!!!
        private readonly string functionUrl = "https://function-mcom-inttest.azurewebsites.net/api/GetSPOData";
        private readonly string tenantId = "e78a86b8-aa34-41fe-a537-9392c8870bf0";
        private readonly string authUrl = "https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        private readonly string clientId = "43db244a-1e1a-4414-b60c-8279f3f7e6ff";
        private readonly string clientSecret = "MFw8Q~y_zwrQvzrts3ggTBB0PWs64hj.qBy6QabY";
        private readonly string scope = "api://5e0a5f0a-db01-473c-b448-0e9711ed8f9a/.default";
        private readonly string grantType = "client_credentials";

        private readonly ILogger _logger;

        // Constructor
        public GetSPOData(ILoggerFactory loggerFactory)
        {
            this.authUrl = this.authUrl.Replace("{tenantId}", tenantId);
            _logger = loggerFactory.CreateLogger<GetSPOData>();
        }

        [Function("GetSPOData")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, FunctionContext context, [FromQuery] string url, [FromQuery] bool statucCheck)
        {
            if (statucCheck)
            {
                var checkResponse = req.CreateResponse(HttpStatusCode.OK);
                checkResponse.Headers.Add("Content-Type", "application/json");
                checkResponse.WriteString("true");

                return checkResponse;
            }

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

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "GetSPOData", "Provisioning"))
            {
                try
                {
                    // Get JWT
                    var azureAdToken = await GetJWTAsync();

                    // Validate
                    if (azureAdToken != null && azureAdToken.access_token != null)
                    {
                        // Get data from Office 365
                        var data = await GetDataFromOffice365($"{functionUrl}?url={url}", azureAdToken.access_token);

                        // Build and send response back
                        var response = req.CreateResponse(HttpStatusCode.OK);
                        response.Headers.Add("Content-Type", "application/json");
                        response.WriteString(data);

                        return response;
                    }
                }
                catch (Exception ex)
                {
                    Global.Log.LogError(ex, "Error getting the JWT. Error: {ErrorMessage}", ex.Message);
                }
            }

            // Generate error response
            var failResponse = req.CreateResponse(HttpStatusCode.Forbidden);
            failResponse.Headers.Add("Content-Type", "application/json");
            failResponse.WriteString("Error getting the JWT");

            return failResponse;
        }

        private async Task<BearerToken?> GetJWTAsync()
        {
            var collection = new List<KeyValuePair<string, string>>
            {
                new("client_id", clientId),
                new("client_secret", clientSecret),
                new("scope", scope),
                new("grant_type", grantType)
            };

            // Get AD token
            var httpAzureADResponse = await HttpClientUtilities.SendAsync(authUrl, collection);

            httpAzureADResponse.EnsureSuccessStatusCode();

            // Get token object
            var tokenObject = await httpAzureADResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<BearerToken>(tokenObject);
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
