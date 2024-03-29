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
    public class GetProvisioningDashboard
    {
        private readonly ILogger _logger;

        public GetProvisioningDashboard(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetProvisioningDashboard>();
        }

        [Function("GetProvisioningDashboard")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getprovisioningdashboard/{user:alpha=none}")] HttpRequestData req, string? user,
            [SqlInput(commandText: "Proc_GetDashBoardItems",
                commandType: System.Data.CommandType.StoredProcedure,
                parameters: "@user={user}",
                connectionStringSetting: "MCOMGovernanceDatabaseConnection")]
            IEnumerable<DashboardItem> dashboardItems)
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

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "GetProvisioningDashboard");
            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "GetProvisioningDashboard", "Provisioning"))
            {
                HttpResponseData? response = null;
                try
                {
                    response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/json");
                    response.WriteString(JsonConvert.SerializeObject(dashboardItems));
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
