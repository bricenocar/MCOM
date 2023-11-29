using MCOM.Models;
using MCOM.Models.Provisioning;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace MCOM.Provisioning.Functions
{
    public class GetAvailableTemplates
    {
        private readonly ILogger _logger;

        public GetAvailableTemplates(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetAvailableTemplates>();
        }

        [Function("GetAvailableTemplates")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            [SqlInput(commandText: "usp_GetAllProvisioningTemplates",
                commandType: System.Data.CommandType.StoredProcedure,
                parameters: "@workloadId={Query.workloadId}",
                connectionStringSetting: "MCOMGovernanceDatabaseConnection")]
            IEnumerable<AvailableTemplate> availableTemplates)
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

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "GetAvailableTemplates");            
            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "GetAvailableTemplates", "Provisioning"))
            {
                HttpResponseData? response = null;
                try
                {
                    response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/json");
                    response.WriteString(JsonConvert.SerializeObject(availableTemplates));
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
