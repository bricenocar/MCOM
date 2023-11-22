using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace MCOM.Provisioning.Functions
{
    public class CheckTrainingCompletion
    {
        [Function("CheckTrainingCompletion")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("CheckTrainingCompletion");
            logger.LogInformation("C# HTTP trigger function processed a request.");

            // Temporary reponse
            return HttpUtilities.HttpResponse(req, HttpStatusCode.OK, "true");
        }
    }
}
