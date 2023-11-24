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
        private readonly string functionUrl = "https://function-mcom-inttest.azurewebsites.net/api/GetJWT?code=iRUkfAPXCgyW-OG_JbabWiI9bNCc1v1Zt56j4Eoc5EJKAzFuE1He4Q==";
        private readonly string tenantId = "e78a86b8-aa34-41fe-a537-9392c8870bf0";
        private readonly string authUrl = "https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        private readonly string clientId = "43db244a-1e1a-4414-b60c-8279f3f7e6ff";
        private readonly string clientSecret = "MFw8Q~y_zwrQvzrts3ggTBB0PWs64hj.qBy6QabY";
        private readonly string scope = "api://5e0a5f0a-db01-473c-b448-0e9711ed8f9a/.default";
        private readonly string grantType = "client_credentials";

        // Constructor
        public GetSPOData()
        {
            this.authUrl = this.authUrl.Replace("{tenantId}", tenantId);
        }

        [Function("GetSPOData")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, FunctionContext context, [FromQuery] string url)
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

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "GetSPOData", "Provisioning"))
            {
                try
                {
                    // Get JWT
                    var azureAdToken = await GetJWTAsync();

                    // Validate
                    if (azureAdToken != null && azureAdToken.access_token != null)
                    {
                        var tokenObj = await GetOffice365Token(azureAdToken.access_token);
                        var token = tokenObj?.Token;
                        if (token != null)
                        {
                            // Get data from Office 365
                            var data = await GetDataFromOffice365(url, token);

                            // Build and send response back
                            var response = req.CreateResponse(HttpStatusCode.OK);
                            response.Headers.Add("Content-Type", "application/json");
                            response.WriteString(data);

                            return response;
                        }
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

        private async Task<TokenObject?> GetOffice365Token(string azureAdToken)
        {
            // Get Office365 token
            var headers = new Dictionary<string, string>()
            {
                {"Authorization",  $"Bearer {azureAdToken}"}
            };
            var httpMcomFunctionResponse = await HttpClientUtilities.SendAsync(functionUrl, headers);

            // Check response
            httpMcomFunctionResponse.EnsureSuccessStatusCode();

            // Get token response and token
            var tokenResponse = await httpMcomFunctionResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TokenObject>(tokenResponse);
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
