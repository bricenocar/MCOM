using MCOM.Models;
using MCOM.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace MCOM.Provisioning.Functions
{
    public class ValidateSiteUrl
    {
        private readonly ILogger _logger;

        public ValidateSiteUrl(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ValidateSiteUrl>();
        }

        [Function("ValidateSiteUrl")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, [FromQuery] string siteUrl)
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

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "ValidateSiteUrl");
            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "ValidateSiteUrl", "Provisioning"))
            {
                // Temp test
                var response = string.Equals(siteUrl, "https://www.statoilsrm.sharepoint.com/sites/test", StringComparison.OrdinalIgnoreCase) ? "false" : "true";

                // Temp true response
                return HttpUtilities.HttpResponse(req, HttpStatusCode.OK, response);
            }
        }
    }
}
