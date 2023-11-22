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
    public class GetPurposeValues
    {
        private readonly ILogger _logger;

        public GetPurposeValues(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetPurposeValues>();
        }

        [Function("GetPurposeValues")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            [SqlInput(commandText: "usp_GetAllProvisioningPurposes",
                commandType: System.Data.CommandType.StoredProcedure,
                parameters: "",
                connectionStringSetting: "MCOMGovernanceDatabaseConnection")] IEnumerable<PurposeValue> purposes)
        {

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(_logger);
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, "Config values missing or bad formatted in app config. Error: {ErrorMessage}", ex.Message);
                return HttpUtilities.HttpResponse(req, HttpStatusCode.InternalServerError, "false");
            }

            // Do any required handling after getting purposes

            try
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString(JsonConvert.SerializeObject(purposes));
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
