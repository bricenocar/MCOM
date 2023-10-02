using System.Net;
using MCOM.Models.Provisioning;
using MCOM.Models;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;

namespace MCOM.Provisioning.Functions
{
    public class GetSensitivityLabels
    {
        public GetSensitivityLabels()
        {

        }

        [Function("GetSensitivityLabels")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, FunctionContext context)
        {
            var logger = context.GetLogger("GetSensitivityLabels");

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
                // Temporary static code to build the the options
                var optionalValues = new List<GetSensitivityLabelsPayload>()
                {
                    new GetSensitivityLabelsPayload()
                    {
                        Id = "1a35b130-2a01-4e7b-9f60-ecd689a7c456",
                        Label = "Open site",
                        SecurityClassification = "Open",
                        SiteClassification = "Open"
                    },
                    new GetSensitivityLabelsPayload()
                    {
                        Id = "b0f41c21-09d5-40bc-8a41-acdbce140f44",
                        Label = "Internal site",
                        SecurityClassification = "Internal",
                        SiteClassification = "Internal"
                    },
                    new GetSensitivityLabelsPayload()
                    {
                        Id = "44d13a3e-a0d8-4432-b639-a59e40495802",
                        Label = "Restricted site - Allow guests",
                        SecurityClassification = "Restricted",
                        SiteClassification = "Restricted"
                    },
                    new GetSensitivityLabelsPayload()
                    {
                        Id = "cf159781-f145-4602-8f2f-b029fcb74f84",
                        Label = "Restricted site - No guests",
                        SecurityClassification = "Restricted",
                        SiteClassification = "Restricted"
                    },
                    new GetSensitivityLabelsPayload()
                    {
                        Id = "ea54145d-d4f5-4b40-a12b-a8302e5b0960",
                        Label = "Confidential site - Allow guests",
                        SecurityClassification = "Confidential",
                        SiteClassification = "Confidential"
                    },
                    new GetSensitivityLabelsPayload()
                    {
                        Id = "1b078699-150e-4fbb-a4f0-3b4e1ab18e07",
                        Label = "Confidential site - No guests",
                        SecurityClassification = "Confidential",
                        SiteClassification = "Confidential"
                    }
                };

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString(JsonConvert.SerializeObject(optionalValues));

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
