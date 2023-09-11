using System.Net;
using MCOM.Models.Provisioning;
using MCOM.Models;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MCOM.Provisioning.Functions
{
    public class GetPurposeValues
    {
        private readonly ILogger _logger;

        public GetPurposeValues(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetPurposeValues>();
        }

        [Function("GetPurposeValues")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("GetPurposeValues");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", ex.Message);
                return HttpUtilities.HttpResponse(req, HttpStatusCode.InternalServerError, "false");
            }

            try
            {
                // Temporary static code to build the purposes
                var purposeValues = new List<GetPurposeValuesPayload>()
                {
                    new GetPurposeValuesPayload()
                    {
                        Id = "1",
                        Title = "Collaborate with team"
                    },
                    new GetPurposeValuesPayload()
                    {
                        Id = "2",
                        Title = "Communicate information"
                    },
                    new GetPurposeValuesPayload()
                    {
                        Id = "3",
                        Title = "Store and share files"
                    },
                    new GetPurposeValuesPayload()
                    {
                        Id = "4",
                        Title = "Chat with group"
                    },
                    new GetPurposeValuesPayload()
                    {
                        Id = "5",
                        Title = "Collaborate with external user"
                    },
                    new GetPurposeValuesPayload()
                    {
                        Id = "6",
                        Title = "Conversation in small group"
                    },
                    new GetPurposeValuesPayload()
                    {
                        Id = "7",
                        Title = "Intranet site for a project"
                    }
                };

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString(JsonConvert.SerializeObject(purposeValues));

                return response;
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, "Error: {ErrorMessage}", ex.Message);
                return HttpUtilities.HttpResponse(req, HttpStatusCode.InternalServerError, "false");
            }
        }
    }
}
