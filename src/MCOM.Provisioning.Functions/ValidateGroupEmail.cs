using System.Net;
using MCOM.Models;
using MCOM.Utilities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace MCOM.Provisioning.Functions
{
    public class ValidateGroupEmail
    {
        private readonly ILogger _logger;
        private readonly string tenantId = "e78a86b8-aa34-41fe-a537-9392c8870bf0";
        private readonly string authUrl = "https://login.microsoftonline.com/e78a86b8-aa34-41fe-a537-9392c8870bf0/oauth2/v2.0/token";
        private readonly string clientId = "43db244a-1e1a-4414-b60c-8279f3f7e6ff";
        private readonly string clientSecret = "MFw8Q~y_zwrQvzrts3ggTBB0PWs64hj.qBy6QabY";
        private readonly string scope = "api://5e0a5f0a-db01-473c-b448-0e9711ed8f9a/.default";
        private readonly string grantType = "client_credentials";
        public ValidateGroupEmail(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ValidateGroupEmail>();
        }

        [Function("ValidateGroupEmail")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            try
            {
                GlobalEnvironment.SetEnvironmentVariables(_logger);
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", ex.Message);
                return HttpUtilities.HttpResponse(req, HttpStatusCode.InternalServerError, ex.Message);
            }

            // Parse query parameters
            var query = QueryHelpers.ParseQuery(req.Url.Query);

            // Get parameters from body in case og POST
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic? data = JsonConvert.DeserializeObject(requestBody);

            HttpResponseData? response;

            // Get url from query or body
            string? groupalias = query != null ? query.Keys.Contains("groupalias") ? query["groupalias"] : data?.groupalias : "";
            if(groupalias.IsNullOrEmpty())
            {
                response = req.CreateResponse(HttpStatusCode.BadRequest);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString("The parameter groupalias is empty, could not validate group name");
            }
            
            try
            {
                string? functionUrl = $"https://function-mcom-inttest.azurewebsites.net/api/ValidateGroupEmail?groupalias={groupalias}";
                // Get JWT
                var azureAdToken = await HttpClientUtilities.GetTokenAsync(clientId, clientSecret, scope, grantType, authUrl);

                // Validate
                if (azureAdToken != null && azureAdToken.access_token != null)
                {
                    // Get data from Office 365
                    var responseBody = await GetDataFromOffice365(functionUrl, azureAdToken.access_token);

                    // Build and send response back
                    response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/json");
                    response.WriteString(responseBody);
                }
                else
                {
                    // Build and send response back
                    response = req.CreateResponse(HttpStatusCode.Unauthorized);
                    response.Headers.Add("Content-Type", "application/json");
                    response.WriteString("Could not authorize you");
                }
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, "Error getting the JWT. Error: {ErrorMessage}", ex.Message);
                // Build and send response back
                response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString(ex.Message);
            }
            return response;
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
    }
}
