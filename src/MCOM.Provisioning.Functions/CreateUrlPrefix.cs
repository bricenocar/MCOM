using MCOM.Models;
using MCOM.Utilities;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json;
using System.Net;

namespace MCOM.Provisioning.Functions
{
    public class CreateUrlPrefix
    {
        private readonly ILogger _logger;

        public CreateUrlPrefix(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CreateUrlPrefix>();
        }

        [Function("CreateUrlPrefix")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            try
            {
                GlobalEnvironment.SetEnvironmentVariables(_logger);
            }
            catch (Exception ex)
            {
                var msg = "Config values missing or bad formatted in app config.";
                Global.Log.LogError(ex, msg + "Error: {ErrorMessage}", ex.Message);
                return HttpUtilities.HttpResponse(req, HttpStatusCode.InternalServerError, ex.Message);
            }

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "CreateUrlPrefix");
            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "CreateUrlPrefix", "Provisioning"))
            {
                HttpResponseData? response = null;
                var responseBody = new CreateUrlPrefixRespone();

                // Parse query parameters
                var query = QueryHelpers.ParseQuery(req.Url.Query);

                // Validate querystring
                string workloadId = query.Keys.Contains("workloadId") ? query["workloadId"] : string.Empty;
                if (string.IsNullOrEmpty(workloadId))
                {
                    responseBody.Valid = false;
                    responseBody.Value = "Missing workloadId as query string"; 
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    response.Headers.Add("Content-Type", "application/json");
                    response.WriteString(JsonConvert.SerializeObject(responseBody));
                    return response;
                }

                // Generate prefix based on workloadId
                string prefixUrl = "https://uniquenumberfunctionapp.azurewebsites.net/api/GetNextUniqueNumber?code=sNB7J1UMlt4AjSw9CfymA_Mi59j2xADA-PH8qyqeVCNyAzFui5QKiA==&SEQUENCE=PRE_FIX_PRO_DAT&SCOPE=MCOM&STRREP=000000";
                switch(workloadId)
                {
                    case "1":
                        prefixUrl += "&FIXSTR=SPT";
                        break;
                    case "2":
                        prefixUrl += "&FIXSTR=SPT";
                        break;
                    case "3":
                        prefixUrl += "&FIXSTR=SPT";
                        break;
                    case "4":
                        prefixUrl += "&FIXSTR=SPC";
                        break;
                    case "5":
                        prefixUrl += "&FIXSTR=SPT";
                        break;
                    case "6":
                        prefixUrl += "&FIXSTR=SPT";
                        break;
                }

                string prefix;
                var httpResponse = await HttpClientUtilities.SendAsync(prefixUrl);

                // Check response
                httpResponse.EnsureSuccessStatusCode();

                // Get data as string from response
                prefix = await httpResponse.Content.ReadAsStringAsync();

                // Code to generate prefix
                responseBody.Valid = true;
                responseBody.Value = prefix;
                response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString(JsonConvert.SerializeObject(responseBody));
                return response;
            }
        }
    }

    public class CreateUrlPrefixRespone
    {
        public string Value { get; set; }
        public bool Valid { get; set; }
    }
}
