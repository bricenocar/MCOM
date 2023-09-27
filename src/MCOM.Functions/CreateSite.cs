using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using PnP.Framework.Sites;
using MCOM.Utilities;

namespace MCOM.Functions
{
    public static class CreateSite
    {
        [Function("CreateSite")]
        public static async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("CreateSite");
            logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

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
