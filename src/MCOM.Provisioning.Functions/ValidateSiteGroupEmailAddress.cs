using MCOM.Models;
using MCOM.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace MCOM.Provisioning.Functions
{
    public class ValidateSiteGroupEmailAddress
    {
        private readonly ILogger _logger;

        public ValidateSiteGroupEmailAddress(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ValidateSiteGroupEmailAddress>();
        }

        [Function("ValidateSiteGroupEmailAddress")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, [FromQuery] string siteGroupName)
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

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "ValidateSiteGroupEmailAddress");
            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "ValidateSiteGroupEmailAddress", "Provisioning"))
            {
                HttpResponseData? response = null;
                try
                {
                    // Temp test
                    var result = string.Equals(siteGroupName, "test", StringComparison.OrdinalIgnoreCase) ? "false" : "true";
                    response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/json");
                    response.WriteString(JsonConvert.SerializeObject(result));
                    return response;
                }
                catch (Exception ex)
                {
                    Global.Log.LogError(ex, "Error: {ErrorMessage}", ex.Message);
                    return HttpUtilities.HttpResponse(req, HttpStatusCode.InternalServerError, ex.Message);
                }
            }
        }
    }
}
