using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using MCOM.Data.DBContexts;
using MCOM.Models;
using MCOM.Utilities;
using Newtonsoft.Json;

namespace MCOM.ScanOnDemand.Functions
{
    public class GetScanRequests
    {
        private readonly GovernanceDBContext _dbContext;

        public GetScanRequests(GovernanceDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Function("GetScanRequests")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("GetScanRequests");

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                string msg = "Config values missing or bad formatted in app config.";
                Global.Log.LogError(e, msg + "Error: {ErrorMessage}", e.Message);
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.WriteString(msg);
                return response;
            }

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "GetScanRequests");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "GetScanRequests", "ScanOnDemand"))
            {
                HttpResponseData response = null;

                try
                {
                    var scanRequests = _dbContext.McomscanRequests.ToList();
                    var json = JsonConvert.SerializeObject(scanRequests);

                    response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "text/json; charset=utf-8");
                    response.WriteString(json);
                }
                catch (Exception ex)
                {
                    response = req.CreateResponse(HttpStatusCode.InternalServerError);
                    response.Headers.Add("Content-Type", "text/json; charset=utf-8");
                    response.WriteString("Error when getting data from the governance DB");

                    Global.Log.LogError(ex, "Error when getting data from the governance DB", "scanrequests");
                }

                return response;
            }
        }
    }
}
