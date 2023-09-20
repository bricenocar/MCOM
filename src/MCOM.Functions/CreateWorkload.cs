using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using PnP.Framework.Sites;
using MCOM.Utilities;
using MCOM.Models.Provisioning;
using MCOM.Models;
using MCOM.Services;
using Newtonsoft.Json;
using System.IO;
using Microsoft.SharePoint.Client.WebParts;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace MCOM.Functions
{
    public class CreateWorkload
    {
        private IGraphService _graphService;
        private ISharePointService _sharePointService;
        private IAppInsightsService _appInsightsService;
        private IAzureService _azureService;

        public CreateWorkload(IAzureService azureService, ISharePointService sharePointService, IGraphService graphService, IAppInsightsService appInsightsService)
        {
            _azureService = azureService;
            _graphService = graphService;
            _sharePointService = sharePointService;
            _appInsightsService = appInsightsService;
        }

        [Function("CreateWorkload")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("CreateWorkload");
           
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

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "CreateSite");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "CreateWorkload", "Provisioning"))
            {
                // Initialize graph service and app insights service
                await _graphService.GetGraphServiceClientAsync();
                await _appInsightsService.GetApplicationInsightsDataClientAsync();

                // Parse the JSON request body to get parameters
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                WorkloadCreationRequestPayload workloadData = JsonConvert.DeserializeObject<WorkloadCreationRequestPayload>(requestBody);

                // Check if there is a site in the request payload
                if (workloadData.Site != null)
                {
                    // The "Site" object is present in the request, proceeding to create
                    // Define SharePoint site and label details
                    var site = new Microsoft.Graph.Site
                    {                        
                        Name = workloadData.Site.SiteConfig.SiteName,
                        DisplayName = workloadData.Site.SiteConfig.SiteName,
                        WebUrl = workloadData.Site.SiteConfig.SiteURL                        
                    };



                    logger.LogInformation("SharePoint site created successfully.");
                }

                if (workloadData.Teams != null)
                {
                    // The "Teams" object is present in the request, proceeding to create or link to a Site

                }
            }

            try
            {                
                var tenant = "statoilintegrationtest";
                var targetTenantUrl = $"https://{tenant}.sharepoint.com/";

                using var context = new ClientContext(targetTenantUrl);
                // Get token using managed identity
                var accessToken = await AzureUtilities.GetAzureServiceTokenAsync(targetTenantUrl);
                context.ExecutingWebRequest += (sender, e) =>
                {
                    e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken.Token;
                };

                var communicationContext = await context.CreateSiteAsync(new CommunicationSiteCollectionCreationInformation
                {
                    Title = "MCOMTest", // Mandatory
                    Description = "description", // Mandatory
                    Lcid = 1033, // Mandatory
                                 // ShareByEmailEnabled = false, // Optional
                                 // Classification = "classification", // Optional
                    SiteDesign = CommunicationSiteDesign.Blank, // Mandatory
                    Url = $"{targetTenantUrl}sites/mymoderncommunicationsite", // Mandatory
                });
                communicationContext.Load(communicationContext.Web, w => w.Url);
                communicationContext.ExecuteQueryRetry();

                logger.LogInformation($"Communication site web url. {communicationContext.Web.Url}");
            }
            catch (Exception ex)
            {
                response = req.CreateResponse(HttpStatusCode.InternalServerError);                
                response.WriteString(ex.Message);
            }
            
            response.WriteString("Site created!");
            return response;
        }
    }
}
