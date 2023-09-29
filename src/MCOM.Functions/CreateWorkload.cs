using MCOM.Models;
using MCOM.Models.Provisioning;
using MCOM.Services;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using PnP.Core.Services;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MCOM.Functions
{
    public class CreateWorkload
    {
        private IGraphService _graphService;
        private ISharePointService _sharePointService;
        private IAppInsightsService _appInsightsService;
        private IAzureService _azureService;
        private readonly IPnPContextFactory _pnpContextFactory;
        private IMicrosoft365Service _microsoft365Service;

        public CreateWorkload(IPnPContextFactory pnpContextFactory, IAzureService azureService, IMicrosoft365Service microsoft365Service, ISharePointService sharePointService, IGraphService graphService, IAppInsightsService appInsightsService)
        {
            _azureService = azureService;
            _graphService = graphService;
            _sharePointService = sharePointService;
            _appInsightsService = appInsightsService;
            _pnpContextFactory = pnpContextFactory;
            _microsoft365Service = microsoft365Service;
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
                    using (var pnpContext = await _pnpContextFactory.CreateAsync("Default"))
                    {
                        await _microsoft365Service.CreateCommunicationSite(pnpContext, Global.SharePointUrl, 
                            workloadData.Site.SiteConfig.SiteName, 
                            workloadData.Site.SiteConfig.SiteName,
                            workloadData.Site.SiteConfig.SensitivityLabel,
                            PnP.Core.Admin.Model.SharePoint.Language.English);
                    }
                    Global.Log.LogInformation("SharePoint site created successfully.");
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
