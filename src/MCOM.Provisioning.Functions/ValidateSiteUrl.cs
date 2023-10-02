using System.Net;
using MCOM.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

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
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Temp true response
            return HttpUtilities.HttpResponse(req, HttpStatusCode.OK, "true");
        }
    }
}
