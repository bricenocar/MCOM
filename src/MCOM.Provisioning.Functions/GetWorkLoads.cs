using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MCOM.Models;
using MCOM.Utilities;
using MCOM.Services;
using MCOM.Controllers.Provisioning;

namespace MCOM.Provisioning.Functions
{
    public class GetWorkLoads
    {
        private readonly IDataBaseService _dataBaseService;

        public GetWorkLoads(IDataBaseService dataBaseService)
        {
            _dataBaseService = dataBaseService;
        }

        [Function("GetWorkLoads")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("GetWorkLoads");

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
                // Get workload datatable
                var dt = await _dataBaseService.ExecuteStoredProcedureAsync("Proc_GetWorkloads");

                // Get workloads
                var workLoads = WorkLoadController.GetWorkLoads(dt);

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString(JsonConvert.SerializeObject(workLoads));

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
