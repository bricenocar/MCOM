using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using MCOM.Models;
using MCOM.Services;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PnP.Core.Services;

namespace MCOM.Functions
{
    public class GetSensitivityLabels
    {
        private readonly IPnPContextFactory _pnpContextFactory;
        private IMicrosoft365Service _microsoft365Service;
        private IGraphService _graphService;

        public GetSensitivityLabels(IPnPContextFactory pnpContextFactory, IMicrosoft365Service microsoft365Service, IGraphService graphService)
        {
            _pnpContextFactory = pnpContextFactory;
            _microsoft365Service = microsoft365Service;
            _graphService = graphService;
        }

        [Function("GetSensitivityLabels")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("CreateWorkload");
            HttpResponseData response = null;

            try
            {
                GlobalEnvironment.SetEnvironmentVariables(logger);
            }
            catch (Exception e)
            {
                string msg = "Config values missing or bad formatted in app config.";
                Global.Log.LogError(e, msg + "Error: {ErrorMessage}", e.Message);
                response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.WriteString(msg);
                return response;
            }

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "GetSensitivityLabels");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "GetSensitivityLabels", "Provisioning"))
            {
                // Initialize response
                response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");

                // Initialize graph service
                await _graphService.GetGraphServiceClientAsync();

                try
                {
                    using var pnpContext = await _pnpContextFactory.CreateAsync("Default");
                    Global.Log.LogInformation("Getting sensitivity labels");
                    //var sensitivityLabels = await _graphService.GetSensitivityLabels(pnpContext);
                    var sensitivityLabels = _microsoft365Service.GetSensitivityLabels(pnpContext);
                    await response.WriteStringAsync(JsonSerializer.Serialize(sensitivityLabels));
                }
                catch (Exception ex)
                {
                    Global.Log.LogError(ex, ex.Message);
                    response = req.CreateResponse(HttpStatusCode.InternalServerError);
                    response.WriteString(ex.Message);
                }                
            }
            return response;
        }
    }
}
