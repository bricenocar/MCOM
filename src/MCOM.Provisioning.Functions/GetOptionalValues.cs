using System.Net;
using MCOM.Models;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MCOM.Provisioning.Functions
{
    public class GetOptionalValues
    {      
        public GetOptionalValues()
        {
           
        }

        [Function("GetOptionalValues")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("ValidateTemplate");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception ex)
            {
                var msg = "Config values missing or bad formatted in app config.";
                Global.Log.LogError(ex, msg + "Error: {ErrorMessage}", ex.Message);
                return HttpUtilities.HttpResponse(req, HttpStatusCode.InternalServerError, "false");
            }

            try
            {
                // Temporary static code to build the templates
                var availableTemplates = new List<GetAvailableTemplatesPayload>()
                {
                    new GetAvailableTemplatesPayload()
                    {
                        FileName = "Json Template",
                        FilePath = "templates/JsonFile.json"
                    },
                    new GetAvailableTemplatesPayload()
                    {
                        FileName = "Xml Template",
                        FilePath = "templates/XmlFile.xml"
                    }
                };

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString(JsonConvert.SerializeObject(availableTemplates));

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
