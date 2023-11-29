using Azure.Messaging.ServiceBus;
using MCOM.Models;
using MCOM.Models.Provisioning;
using MCOM.Services;
using MCOM.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PnP.Core.Admin.Model.SharePoint;
using PnP.Core.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MCOM.Functions
{
    public class CreateWorkload
    {
        private readonly IPnPContextFactory _pnpContextFactory;
        private IMicrosoft365Service _microsoft365Service;
        private readonly ILogger<CreateWorkload> _logger;


        public CreateWorkload(ILogger<CreateWorkload> logger, IPnPContextFactory pnpContextFactory, IMicrosoft365Service microsoft365Service)
        {
            _logger = logger;
            _pnpContextFactory = pnpContextFactory;
            _microsoft365Service = microsoft365Service;
        }

        [Function(nameof(CreateWorkload))]
        [ServiceBusOutput("created", Connection = "ServiceBusConnectionString")]
        public async Task<string> Run([ServiceBusTrigger("approved", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message)
        {
            try
            {
                GlobalEnvironment.SetEnvironmentVariables(_logger);
            }
            catch (Exception e)
            {
                string msg = "Config values missing or bad formatted in app config.";
                Global.Log.LogError(e, msg + "Error: {ErrorMessage}", e.Message);
                throw new ArgumentException(msg, e);
            }

            System.Diagnostics.Activity.Current?.AddTag("MCOMOperation", "CreateWorkload");

            using (Global.Log.BeginScope("Operation {MCOMOperationTrace} processed request for {MCOMLogSource}.", "CreateWorkload", "Provisioning"))
            {
                try
                {
                    // Parse the JSON request body to get parameters
                    var body = message.Body.ToString();
                    WorkloadCreationRequestPayload workloadData = JsonConvert.DeserializeObject<WorkloadCreationRequestPayload>(body);

                    // Validate request before continuiung
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
                                    workloadData.Site.SiteConfig.ExternalSharing,
                                    Language.English, workloadData.Site.SiteUsers.Owners.First());

                                // Add site information to the response
                                if (createdSite.SiteId != Guid.Empty)
                                {
                                    workloadData.Site.SiteConfig.SiteGuid = createdSite.SiteId;
                                    workloadData.Site.SiteConfig.GroupId = createdSite.GroupId;
                                    workloadData.Site.SiteConfig.TeamId = createdSite.TeamId;
                                    workloadData.Site.SiteConfig.CreatedDate = DateTime.Now;

                                    Global.Log.LogInformation("SharePoint communication site created successfully.");
                                }
                            }
                            else if (workloadData.Site.SiteConfig.SiteType == SiteType.TeamSite)
                            {
                                // Create the SharePoint team site
                                var createdSite = await _microsoft365Service.CreateTeamSite(pnpContext,
                                    workloadData.Site.SiteConfig.Alias,
                                    workloadData.Site.SiteConfig.SiteName,
                                    workloadData.Site.SiteConfig.Description,
                                    workloadData.Site.SiteConfig.SensitivityLabel,
                                    workloadData.Site.SiteConfig.ExternalSharing,
                                    Language.English,
                                    workloadData.Site.GroupUsers.Owners);

                                // Add site information to the response
                                if (createdSite.SiteId != Guid.Empty)
                                {
                                    workloadData.Site.SiteConfig.SiteGuid = createdSite.SiteId;
                                    workloadData.Site.SiteConfig.GroupId = createdSite.GroupId;
                                    workloadData.Site.SiteConfig.TeamId = createdSite.TeamId;
                                    workloadData.Site.SiteConfig.CreatedDate = DateTime.Now;
                                    Global.Log.LogInformation("SharePoint team site created successfully.");
                                }                        
                            }                            

                            if (workloadData.Team != null)
                            {
                                // The "Teams" object is present in the request, proceeding link to group
                                if (workloadData.Site.SiteConfig.GroupId != Guid.Empty)
                                {
                                    if(workloadData.Site.SiteConfig.TeamId == Guid.Empty)
                                    {
                                        var createdTeam = await _microsoft365Service.CreateTeamFromGroup(pnpContext,
                                                                               workloadData.Site.SiteConfig.GroupId);
                                        if (createdTeam.TeamId != Guid.Empty)
                                        {
                                            workloadData.Site.SiteConfig.TeamId = createdTeam.TeamId;
                                            Global.Log.LogInformation($"Team created successfully. Id: {createdTeam.TeamId}");
                                        }
                                    } else
                                    {
                                        Global.Log.LogWarning($"The team for the site {workloadData.Site.SiteConfig.Alias} already exists from before");
                                    }                                                                      
                                }
                            }
                        }

                        // return the json payload of workload data
                        return JsonConvert.SerializeObject(workloadData, Formatting.Indented, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });
                    }
                }                
                catch(InvalidRequestException invEx)
                {
                    throw;
                }
                catch(JsonSerializationException jex)
                {
                    throw;
                }
                catch (UnavailableUrlException siteException)
                {
                    throw;
                }
                catch (SiteCreationException siteException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw;
                }
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
