using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MCOM.Provisioning.Functions
{
    public class CheckTrainingCompletion
    {
        [Function("CheckTrainingCompletion")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("CheckTrainingCompletion");
            logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var data = new HttpResponse() { Value = true };
            var jsonMetadata = JsonConvert.SerializeObject(data);
            response.WriteString(jsonMetadata);

            return response;
        }

        private class HttpResponse
        {
            public bool Value { get; set; }
        }
    }
}
