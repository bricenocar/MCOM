using MCOM.Models;
using MCOM.Models.InformationProtection;
using MCOM.Models.Provisioning;
using MCOM.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using PnP.Core;
using PnP.Core.Admin.Model.SharePoint;
using PnP.Core.Admin.Model.Teams;
using PnP.Core.Services;
using PnP.Framework.Provisioning.Model;
using PnP.Framework.Provisioning.ObjectHandlers;
using PnP.Framework.Provisioning.Providers.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MCOM.Services
{
    public interface IMicrosoft365Service
    {
        Task<CreatedSite> CreateCommunicationSite(PnPContext context, string url, string title, string description, string siteClassification, Guid sensitivityLabel, PnP.Core.Admin.Model.SharePoint.Language language = PnP.Core.Admin.Model.SharePoint.Language.English, string owner = null);
        Task<CreatedSite> CreateTeamSite(PnPContext context, string url, string alias, string title, string description, Guid sensitivityLabel, PnP.Core.Admin.Model.SharePoint.Language language = PnP.Core.Admin.Model.SharePoint.Language.English, List<string> owners = null, bool isPublic = true);
        Task<CreatedTeam> CreateTeamFromGroup(PnPContext context, Guid groupId);
        ProvisioningTemplate GetProvisioningTemplate(Stream xmlTemplate);
        bool ApplyProvisioningTemplateAsync(PnPContext pnpContext, ProvisioningTemplate provisioningTemplate, string siteUrl);
        Task<bool> CheckIfSiteExists(PnPContext context, string url);
        Task<bool> HideAddTeamsPrompt(PnPContext context, string siteUrl);
        Task<List<SensitivityLabel>> GetSensitivityLabels(PnPContext context);        
    }
    
    public class Microsoft365Service : IMicrosoft365Service
    {
        #region Site and Site collections
        // / <summary> 
        // / Create a communication site
        // / </summary>
        // / <param name="context">PnPContext</param>
        // / <param name="url">URL of the site to create</param>
        // / <param name="title">Title of the site to create</param>
        // / <param name="description">Description of the site to create</param>
        // / <param name="SiteClassification">Site classification of the site to create</param>
        // / <param name="sensitivityLabel">Sensitivity label of the site to create</param>
        // / <param name="language">Language of the site to create</param>
        // / <param name="owner">Owner of the site to create</param>
        // / <returns>Created site ID</returns>
        public async Task<CreatedSite> CreateCommunicationSite(PnPContext context, string url, string title, string description, string siteClassification, Guid sensitivityLabel, PnP.Core.Admin.Model.SharePoint.Language language = PnP.Core.Admin.Model.SharePoint.Language.English, string owner = null)
        {
            try
            {
                // Create communication site
                var fullUrl = StringUtilities.GetFullUrl(url);
                var communicationSiteToCreate = new CommunicationSiteOptions(new Uri(fullUrl), title)
                {
                    Description = description,
                    Language = language,
                    SensitivityLabelId = sensitivityLabel, 
                    Owner = owner                    
                };

                // use pnp core admin to check if site collection already exists
                bool siteExists = await CheckIfSiteExists(context, fullUrl);

                // Create the site collection creation options
                SiteCreationOptions siteCreationOptions = new SiteCreationOptions()
                {
                    WaitForAsyncProvisioning = true,                    
                };

                Global.Log.LogInformation($"Creating site: {communicationSiteToCreate.Url}");

                // Create the site collection and get the context for the newly created site collection, this will be used to do the actual work
                using (var newSiteContext = await context.GetSiteCollectionManager().CreateSiteCollectionAsync(communicationSiteToCreate, siteCreationOptions))
                {
                    // use pnp to get site url
                    var web = await newSiteContext.Web.GetAsync(w => w.Url, w => w.Title);
                    var newSiteUrl = web.Url;
                    var newSiteTitle = web.Title;

                    // use pnp to get site info
                    var site = await newSiteContext.Site.GetAsync(s => s.Id);
                    var newSiteId = site.Id;

                    var createdSite = new CreatedSite();
                    createdSite.SiteId = newSiteId;
                    createdSite.SiteUrl = newSiteUrl.ToString();

                    // Log to application insights
                    Global.Log.LogInformation("Site created: {0}, Url of new site: {1}", newSiteTitle, newSiteUrl);
                    return createdSite;

                }
            }
            catch (UnavailableUrlException siteException)
            {
                Global.Log.LogError(siteException, siteException.Message);
                throw;
            }
            catch (MicrosoftGraphServiceException gex)
            {
                var errorMessage = "";
                if (gex.Error != null)
                {
                    MicrosoftGraphError error = gex.Error as MicrosoftGraphError;
                    errorMessage = error.Message;
                    Global.Log.LogError(gex, errorMessage);
                }
                else
                {
                    errorMessage = gex.Message;
                }

                throw new SiteCreationException(url, errorMessage);
            }
            catch (SharePointRestServiceException spEx)
            {
                var errorMessage = "";
                if (spEx.Error != null)
                {
                    SharePointRestError error = spEx.Error as SharePointRestError;
                    errorMessage = error.Message;
                    Global.Log.LogError(spEx, errorMessage);
                }
                else
                {
                    errorMessage = spEx.Message;
                }
                throw new SiteCreationException(url, errorMessage);
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, ex.Message);
                throw new SiteCreationException(url, ex.Message);
            }
        }

        // / <summary> 
        // / Create a team site
        // / </summary>
        // / <param name="context">PnPContext</param>
        // / <param name="url">URL of the site to create</param>
        // / <param name="title">Title of the site to create</param>
        // / <param name="alias">Alias of the site to create (Group Name)</param>
        // / <param name="description">Description of the site to create</param>
        // / <param name="SiteClassification">Site classification of the site to create</param>
        // / <param name="sensitivityLabel">Sensitivity label of the site to create</param>
        // / <param name="language">Language of the site to create</param>
        // / <param name="owners">List of owners of the site to create</param>
        // / <returns>Created site ID</returns>
        public async Task<CreatedSite> CreateTeamSite(PnPContext context, string url, string alias, string title, string description, Guid sensitivityLabel, PnP.Core.Admin.Model.SharePoint.Language language = PnP.Core.Admin.Model.SharePoint.Language.English, List<string> owners = null, bool isPublic = true)
        {            
            try
            {
                // Create communication site
                var fullUrl = StringUtilities.GetFullUrl(alias);
                var teamSiteToCreate = new TeamSiteOptions(StringUtilities.NormalizeSiteAlias(alias), title)
                {                    
                    Description = description,
                    Language = language,
                    SensitivityLabelId = sensitivityLabel,
                    WelcomeEmailDisabled = true,
                    IsPublic = isPublic
                };

                // Add owners to the site
                if (owners != null)
                {
                    teamSiteToCreate.Owners = owners.ToArray();
                }

                // use pnp core admin to check if site collection already exists
                try
                {
                    bool siteExists = await CheckIfSiteExists(context, fullUrl);
                }
                catch (UnavailableUrlException siteException)
                {
                    Global.Log.LogWarning("The site already exists, it will not be re-created");
                }

                // Create the site collection creation options
                SiteCreationOptions siteCreationOptions = new SiteCreationOptions()
                {
                    WaitForAsyncProvisioning = true                    
                };

                Global.Log.LogInformation($"Creating site: {teamSiteToCreate.DisplayName}");
               
                // Create the site collection and get the context for the newly created site collection, this will be used to do the actual work
                using (var newSiteContext = await context.GetSiteCollectionManager().CreateSiteCollectionAsync(teamSiteToCreate, siteCreationOptions))
                {
                    // use pnp to get web info
                    var web = await newSiteContext.Web.GetAsync(w => w.Url, w => w.Title, w => w.Id);
                    var newSiteUrl = web.Url;
                    var newSiteTitle = web.Title;

                    // use pnp to get site info
                    var site = await newSiteContext.Site.GetAsync(s => s.Id);
                    var newSiteId = site.Id;

                    // Log to application insights
                    Global.Log.LogInformation("Site id({0}) created: {1}, Url of new site: {2}", newSiteId, newSiteTitle, newSiteUrl);

                    // use pnp to get site group id
                    var microsoft365Group = await newSiteContext.Group.GetAsync();
                    var groupId = microsoft365Group.Id;

                    // Get the team Id
                    var team = await newSiteContext.Team.GetAsync();

                    // Prepare output
                    var createdSite = new CreatedSite();
                    createdSite.SiteId = newSiteId;
                    createdSite.GroupId = new Guid(groupId);
                    createdSite.SiteUrl = newSiteUrl.ToString();
                    if (team != null)
                    {
                        createdSite.TeamId = team.Id;
                    } else
                    {
                        createdSite.TeamId = Guid.Empty;
                    }

                    return createdSite;
                }
            }            
            catch (MicrosoftGraphServiceException gex)
            {
                var errorMessage = "";
                if(gex.Error != null)
                {
                    MicrosoftGraphError error = gex.Error as MicrosoftGraphError;
                    errorMessage = error.Message;
                    Global.Log.LogError(gex, errorMessage);
                }else
                {
                    errorMessage = gex.Message;
                }
                
                throw new SiteCreationException(url, errorMessage);
            }
            catch (SharePointRestServiceException spEx)
            {
                var errorMessage = "";
                if (spEx.Error != null)
                {
                    SharePointRestError error = spEx.Error as SharePointRestError;
                    errorMessage = error.Message;
                    Global.Log.LogError(spEx, errorMessage);
                }
                else
                {
                    errorMessage = spEx.Message;
                }
                throw new SiteCreationException(url, errorMessage);
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, ex.Message);
                throw new SiteCreationException(url, ex.Message);
            }            
        }

        public ProvisioningTemplate GetProvisioningTemplate(Stream xmlTemplate)
        {
            try
            {
                // Serialize the site template to an XML file
                var provider = new XMLStreamTemplateProvider();
                ProvisioningTemplate template = provider.GetTemplate(xmlTemplate);
                Global.Log.LogInformation($"Found the proivisioning template. {template.DisplayName}");
                return template;
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, ex.Message);
                throw;
            }            
        }

        public bool ApplyProvisioningTemplateAsync(PnPContext pnpContext, ProvisioningTemplate provisioningTemplate, string siteUrl)
        {
            var authManager = PnP.Framework.AuthenticationManager.CreateWithPnPCoreSdk(pnpContext);
            var fullUrl = StringUtilities.GetFullUrl(siteUrl);
            using (var clientContext = authManager.GetContext(fullUrl))
            {
                // Define the site template applying options
                var applyingInformation = new ProvisioningTemplateApplyingInformation();
                applyingInformation.HandlersToProcess = Handlers.All;

                // Apply template to web
                clientContext.Web.ApplyProvisioningTemplate(provisioningTemplate, applyingInformation);
            }
            return true;
        }

        // / <summary>
        // / Check if a site collection already exists
        // / </summary>
        // / <param name="context">PnPContext</param>
        // / <param name="url">URL of the site to check</param>
        // / <returns>True if the site collection exists, false otherwise</returns>
        public async Task<bool> CheckIfSiteExists(PnPContext context, string url)
        {
            bool exists;
            // use pnp core admin to check if site collection already exists
            exists = await context.GetSiteCollectionManager().SiteExistsAsync(new Uri(url));
            if (exists)
            {
                throw new UnavailableUrlException(url);
            }
            return exists;
        }

        // / <summary>
        // / Hide the add teams prompt in the left side of a side
        // / </summary>
        // / <param name="context">PnPContext</param>
        // / <returns>hidden</returns>
        public async Task<bool> HideAddTeamsPrompt(PnPContext context, string siteUrl)
        {
            bool hidden = false;
            try
            {
                // Check if the Add Teams prompt is hidden
                var isAddTeamsPromptHidden = await context.GetSiteCollectionManager().IsAddTeamsPromptHiddenAsync(new Uri(siteUrl));
                if (!isAddTeamsPromptHidden)
                {
                    // Hide the Add Teams prompt
                    hidden = await context.GetSiteCollectionManager().HideAddTeamsPromptAsync(new Uri(siteUrl));
                }
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, ex.Message);
                hidden = false;
            }
            return hidden;
        }
        #endregion

        #region Teams
        // / <summary>
        // / Create a Microsoft Teams team from an existing group
        // / </summary>
        // / <param name="context">PnPContext</param>
        // / <param name="groupId">Group ID</param>
        public async Task<CreatedTeam> CreateTeamFromGroup(PnPContext context, Guid groupId)
        {
            CreatedTeam team = new CreatedTeam();
            try
            {
                // create team options
                var teamOptions = new TeamForGroupOptions(groupId);

                // Check if teams exists

                // Create a Microsoft Teams team
                using (var teamContext = await context.GetTeamManager().CreateTeamAsync(teamOptions))
                {
                    // Post a message in the Teams general channel (API requires one of 'Teamwork.Migrate.All')
                    //await teamContext.Team.LoadAsync(p => p.PrimaryChannel);
                    //await teamContext.Team.PrimaryChannel.LoadAsync(p => p.Messages);
                    //await teamContext.Team.PrimaryChannel.Messages.AddAsync("Hi from the MCOM provisioning service!");

                    team.TeamId = teamContext.Team.Id;
                    team.TeamName = teamContext.Team.DisplayName;                    
                };
            }
            catch (MicrosoftGraphServiceException gex)
            {
                if (gex.Error != null)
                {
                    MicrosoftGraphError error = gex.Error as MicrosoftGraphError;
                    Global.Log.LogError(error.Message);
                    throw new TeamCreationException(groupId.ToString(), error.Message);
                }
                throw new TeamCreationException(groupId.ToString(), gex.Message);
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, ex.Message);
                throw;
            }
            return team;
        }
        #endregion

        #region Information Protection
        // comment this function
        // / <summary>
        // / Get all sensitivity labels
        // / </summary>
        // / <param name="context">PnPContext</param>
        // / <returns>List of sensitivity labels</returns>
        public async Task<List<SensitivityLabel>> GetSensitivityLabels(PnPContext context)
        {
            List<SensitivityLabel> sensitivityLabels = new List<SensitivityLabel>();
            try
            {
                // The permissions for the application need to be set to allow the application to read the sensitivity labels
                // skipping this until we get approval of permissions
                // sensitivityLabels = await context.GetMicrosoft365Admin().GetSensitivityLabelsAsync();
                sensitivityLabels.Add(new SensitivityLabel() { Id = Guid.Parse("e0f5f7cd-0254-4430-a0f2-a7139dffb529"), Name = "Open Site" });
                sensitivityLabels.Add(new SensitivityLabel() { Id = Guid.Parse("36d4c168-d682-4cf2-b30a-94831a69b6b8"), Name = "Internal Site" });
                sensitivityLabels.Add(new SensitivityLabel() { Id = Guid.Parse("ac2e480a-209a-4afd-9930-4767eb05d784"), Name = "Restricted site \\ Allow guests " });
                sensitivityLabels.Add(new SensitivityLabel() { Id = Guid.Parse("d8472a30-749c-40c3-8941-a470493a054b"), Name = "Restricted site \\ No guests" });
                sensitivityLabels.Add(new SensitivityLabel() { Id = Guid.Parse("4689068c-2d45-4a43-83d6-44c8d7d5f350"), Name = "Confidential site \\ Allow guests" });
                sensitivityLabels.Add(new SensitivityLabel() { Id = Guid.Parse("c9aa46f2-f060-4022-8f3a-cc32c0e821c9"), Name = "Confidential site \\ No guests" });

            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, ex.Message);
                throw;
            }
            return sensitivityLabels;
        }       
        #endregion

        #region Helper functions

        #endregion
    }
}
