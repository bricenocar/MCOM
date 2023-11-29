using MCOM.Models;
using MCOM.Models.Provisioning;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace MCOM.Provisioning.Functions
{
    public class GetSensitivityLabels
    {
        private readonly ILogger _logger;

        public GetSensitivityLabels(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetSensitivityLabels>();
        }

        [Function("GetSensitivityLabels")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
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

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "GetSensitivityLabels");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "GetSensitivityLabels", "Provisioning"))
            {
                HttpResponseData? response = null;
                try
                {
                    // Temporary static code to build the the options
                    var optionalValues = new List<SensitivityLabel>()
                {
                    new SensitivityLabel()
                    {
                        Id = "1a35b130-2a01-4e7b-9f60-ecd689a7c456",
                        Label = "Open site",
                        SecurityClassification = "-1;#Open|7665b879-63a0-497a-a32e-f57a6d371b8b",
                        SiteClassification = "Open"
                    },
                    new SensitivityLabel()
                    {
                        Id = "b0f41c21-09d5-40bc-8a41-acdbce140f44",
                        Label = "Internal site",
                        SecurityClassification = "-1;#Internal|3361fef0-33ac-457d-8a1d-df19735ffcb1",
                        SiteClassification = "Internal"
                    },
                    new SensitivityLabel()
                    {
                        Id = "44d13a3e-a0d8-4432-b639-a59e40495802",
                        Label = "Restricted site - Allow guests",
                        SecurityClassification = "-1;#Restricted|2778d0cb-d518-40da-b77b-8925576cf660",
                        SiteClassification = "Restricted"
                    },
                    new SensitivityLabel()
                    {
                        Id = "cf159781-f145-4602-8f2f-b029fcb74f84",
                        Label = "Restricted site - No guests",
                        SecurityClassification = "-1;#Restricted|2778d0cb-d518-40da-b77b-8925576cf660",
                        SiteClassification = "Restricted"
                    },
                    new SensitivityLabel()
                    {
                        Id = "ea54145d-d4f5-4b40-a12b-a8302e5b0960",
                        Label = "Confidential site - Allow guests",
                        SecurityClassification = "-1;#Confidential|ea5c4c07-1021-4ed5-8387-57b122f482d2",
                        SiteClassification = "Confidential"
                    },
                    new SensitivityLabel()
                    {
                        Id = "1b078699-150e-4fbb-a4f0-3b4e1ab18e07",
                        Label = "Confidential site - No guests",
                        SecurityClassification = "-1;#Confidential|ea5c4c07-1021-4ed5-8387-57b122f482d2",
                        SiteClassification = "Confidential"
                    }
                };

                    response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/json");
                    response.WriteString(JsonConvert.SerializeObject(optionalValues));
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
