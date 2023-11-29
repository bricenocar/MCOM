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
    public class GetRecommendedWorkloads
    {
        private readonly ILogger _logger;

        public GetRecommendedWorkloads(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetRecommendedWorkloads>();
        }

        [Function("GetRecommendedWorkloads")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            [SqlInput(commandText: "usp_GetRecommendedWorkloads",                
                commandType: System.Data.CommandType.StoredProcedure,
                parameters: "@selectedPurposes={Query.purposes}",
                connectionStringSetting: "MCOMGovernanceDatabaseConnection")] 
            IEnumerable<RecommendedWorkload> workloads)
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

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "GetRecommendedWorkloads");            
            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "GetRecommendedWorkloads", "Provisioning"))
            {
                HttpResponseData? response = null;
                try
                {
                    response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/json");
                    response.WriteString(JsonConvert.SerializeObject(workloads));
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
