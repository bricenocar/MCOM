using MCOM.Models;
using MCOM.Utilities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
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
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
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
                // Parse query parameters
                var query = QueryHelpers.ParseQuery(req.Url.Query);

                // Validate querystring
                string workloadId = query.Keys.Contains("workloadId") ? query["workloadId"] : string.Empty;
                if (string.IsNullOrEmpty(workloadId))
                {
                    return HttpUtilities.HttpResponse(req, HttpStatusCode.BadRequest, "Missing workloadId as query string");
                }

                // Generate prefix from random number (REPLACE CODE WITH NUMBER GENERATOR TOOL)
                var random = new Random();
                int randomNumber = random.Next(1000, 9999);

                // Generate prefix based on workloadId
                string prefix = string.Empty;
                switch(workloadId)
                {
                    case "1":                        
                        prefix = "SPT";
                        break;
                    case "2":
                        prefix = "SPT";
                        break;
                    case "3":
                        prefix = "SPT";
                        break;
                    case "4":
                        prefix = "SPC";
                        break;
                    case "5":
                        prefix = "SPT";
                        break;
                    case "6":
                        prefix = "SPT";
                        break;
                }

                // Code to generate prefix
                return HttpUtilities.HttpResponse(req, HttpStatusCode.OK, $"{prefix}-{randomNumber}-");
            }
        }
    }
}
