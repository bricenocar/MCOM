using MCOM.Models;
using MCOM.Models.Provisioning;
using MCOM.Services;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PnP.Core.Admin.Model.SharePoint;
using PnP.Core.Services;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MCOM.Functions
{
    public class CreateWorkload
    {
        private readonly IPnPContextFactory _pnpContextFactory;
        private IMicrosoft365Service _microsoft365Service;

        public CreateWorkload(IPnPContextFactory pnpContextFactory, IMicrosoft365Service microsoft365Service)
        {
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

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "CreateWorkload");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "CreateWorkload", "Provisioning"))
            {
                HttpResponseData response = null;
                WorkloadCreationRequestResponse workloadCreationRequestResponse = new WorkloadCreationRequestResponse();

                try
                {
                    // Initialize response
                    response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/json");

                    // Parse the JSON request body to get parameters
                    var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    WorkloadCreationRequestPayload workloadData = JsonConvert.DeserializeObject<WorkloadCreationRequestPayload>(requestBody);

                    // Validate request vefore continuiung
                    ValidateRequestPayload(workloadData);

                    // Create the PnP Context
                    using (var pnpContext = await _pnpContextFactory.CreateAsync("Default"))
                    {

                        // Check if there is a site in the request payload
                        if (workloadData.Site != null)
                        {

                            if (workloadData.Site.SiteConfig.SiteType == SiteType.CommunicationSite)
                            {
                                // Create the SharePoint communication site
                                var createdSite = await _microsoft365Service.CreateCommunicationSite(pnpContext,
                                    workloadData.Site.SiteConfig.SiteURL,
                                    workloadData.Site.SiteConfig.SiteName,
                                    workloadData.Site.SiteConfig.Description,
                                    workloadData.Site.SiteConfig.SiteClassification,
                                    workloadData.Site.SiteConfig.SensitivityLabel,
                                    Language.English, workloadData.Site.SiteUsers.Owners.First());

                                // Add site information to the response
                                if (createdSite.SiteId != Guid.Empty)
                                {
                                    workloadCreationRequestResponse.CreatedSite = createdSite;
                                    Global.Log.LogInformation("SharePoint communication site created successfully.");
                                }
                            }
                            else if (workloadData.Site.SiteConfig.SiteType == SiteType.TeamSite)
                            {
                                // Create the SharePoint team site
                                var createdSite = await _microsoft365Service.CreateTeamSite(pnpContext,
                                    workloadData.Site.SiteConfig.SiteURL,
                                    workloadData.Site.SiteConfig.Alias,
                                    workloadData.Site.SiteConfig.SiteName,                                    
                                    workloadData.Site.SiteConfig.Description,
                                    workloadData.Site.SiteConfig.SensitivityLabel,
                                    Language.English,
                                    workloadData.Site.GroupUsers.Owners);

                                // Add site information to the response
                                if (createdSite.SiteId != Guid.Empty)
                                {
                                    workloadCreationRequestResponse.CreatedSite = createdSite;
                                    Global.Log.LogInformation("SharePoint team site created successfully.");
                                }
                            }                            

                            if (workloadData.Team != null)
                            {
                                // The "Teams" object is present in the request, proceeding link to group
                                if (workloadCreationRequestResponse.CreatedSite.GroupId != Guid.Empty)
                                {
                                    var createdTeam = await _microsoft365Service.CreateTeamFromGroup(pnpContext,
                                        workloadCreationRequestResponse.CreatedSite.GroupId);
                                    if (createdTeam.TeamId != Guid.Empty)
                                    {
                                        workloadCreationRequestResponse.CreatedTeam = createdTeam;
                                        Global.Log.LogInformation($"Team created successfully. Id: {createdTeam.TeamId}");
                                    }                                    
                                }
                            }
                        }
                        //return response with workloadCreationRequestResponse
                        response.WriteString(JsonConvert.SerializeObject(workloadCreationRequestResponse));
                    }
                }                
                catch(InvalidRequestException invEx)
                {
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    response.WriteString(invEx.Message);
                }
                catch(JsonSerializationException jex)
                {
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    response.WriteString(jex.Message);
                }
                catch (UnavailableUrlException siteException)
                {
                    response = req.CreateResponse(HttpStatusCode.Locked);
                    response.WriteString(siteException.Message);
                }
                catch (SiteCreationException siteException)
                {
                    response = req.CreateResponse(HttpStatusCode.Conflict);
                    response.WriteString(siteException.Message);
                }
                catch (Exception ex)
                {
                    Global.Log.LogError(ex, ex.Message);
                    response = req.CreateResponse(HttpStatusCode.InternalServerError);
                    response.WriteString(ex.Message);
                }
                return response;
            }
        }

        /// <summary>
        /// Validate the request payload
        private static void ValidateRequestPayload(WorkloadCreationRequestPayload workloadData)
        {
            // Validate the request
            if (workloadData == null)
            {
                throw new InvalidRequestException("body", "There is no body present");
            }

            if (workloadData.Site == null || workloadData.Site.SiteConfig == null)
            {
                throw new InvalidRequestException("There is no site object or site config present in the request");
            }

            if (workloadData.Site.SiteConfig.SiteType == SiteType.CommunicationSite && 
                workloadData.Site.SiteUsers.Owners.Count == 0)
            {
                throw new InvalidRequestException("There communication site requires a site owner");
            }

            if (workloadData.Team != null && workloadData.Site.GroupUsers.Owners.Count == 0)
            {
                throw new InvalidRequestException("There are no group owners present in the request. It is mandatory when creating Teams");
            }
        }
    }
}
