using MCOM.Models;
using Microsoft.Extensions.Logging;
using PnP.Core.Admin.Model.Microsoft365;
using PnP.Core.Admin.Model.SharePoint;
using PnP.Core.Admin.Model.Teams;
using PnP.Core.Model.SharePoint;
using PnP.Core.Model.Teams;
using PnP.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MCOM.Services
{
    public interface IMicrosoft365Service
    {
        Task<bool> CreateCommunicationSite(PnPContext context, string url, string title, string description, Guid sensitivityLabel, Language language);
    }
    
    public class Microsoft365Service : IMicrosoft365Service
    {
        public Microsoft365Service() { }

        #region Site and Site collections
        // / <summary> 
        // / Create a communication site
        // / </summary>
        // / <param name="context">PnPContext</param>
        // / <param name="url">URL of the site to create</param>
        // / <param name="title">Title of the site to create</param>
        // / <param name="description">Description of the site to create</param>
        // / <param name="language">Language of the site to create</param>
        public async Task<bool> CreateCommunicationSite(PnPContext context, string url, string title, string description, Guid sensitivityLabel, Language language)
        {
            bool created = false;
            try
            {
                // Create communication site
                var communicationSiteToCreate = new CommunicationSiteOptions(new Uri(url), title)
                {
                    Description = description,
                    Language = language,
                    SensitivityLabelId = sensitivityLabel                    
                };

                // use pnp core admin to check if site collection already exists
                bool siteExists = await CheckIfSiteExists(context, url);
                if (siteExists)
                {
                    return false;                
                }                

                // Create the site collection creation options
                SiteCreationOptions siteCreationOptions = new SiteCreationOptions()
                {
                    WaitForAsyncProvisioning = true,                    
                };

                // Create the site collection and get the context for the newly created site collection, this will be used to do the actual work
                using (var newSiteContext = await context.GetSiteCollectionManager().CreateSiteCollectionAsync(communicationSiteToCreate, siteCreationOptions))
                {
                    // Do work on the created site collection via the newSiteContext
                    created = true;

                    // use pnp to get site url
                    var site = await newSiteContext.Web.GetAsync(w => w.Url);
                    var newSiteUrl = site.Url;
                    var newSiteTitle = site.Title;

                    // Log to application insights
                    Global.Log.LogInformation("Site created: {0}, Url of new site: {1}", newSiteTitle, newSiteUrl);
                }
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, ex.Message);
                created = false;
            }
            return created;
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
            try
            {
                // use pnp core admin to check if site collection already exists
                exists = await context.GetSiteCollectionManager().SiteExistsAsync(new Uri(url));
                if (exists)
                {
                    var message = $"Site collection {url} already exists.";
                    Global.Log.LogError(new ArgumentException(message), message);
                }
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, ex.Message);
                exists = false;
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
        public async Task<bool> CreateTeamFromGroup(PnPContext context, Guid groupId)
        {
            bool created = false;
            try
            {
                // create team options
                var teamOptions = new TeamForGroupOptions(groupId);

                // Create a Microsoft Teams team
                using (var team = await context.GetTeamManager().CreateTeamAsync(teamOptions))
                {
                    // Post a message in the Teams general channel
                    await context.Team.LoadAsync(p => p.PrimaryChannel);
                    await context.Team.PrimaryChannel.LoadAsync(p => p.Messages);
                    await context.Team.PrimaryChannel.Messages.AddAsync("Hi from the MCOM provisioning service!");
                };
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, ex.Message);
                created = false;
            }
            return created;
        }
        #endregion

        #region Governance
        // comment this function
        // / <summary>
        // / Get all sensitivity labels
        // / </summary>
        // / <param name="context">PnPContext</param>
        // / <returns>List of sensitivity labels</returns>
        public async Task<List<ISensitivityLabel>> GetSensitivityLabels(PnPContext context)
        {
            List<ISensitivityLabel> sensitivityLabels = new List<ISensitivityLabel>();
            try
            {
                sensitivityLabels = await context.GetMicrosoft365Admin().GetSensitivityLabelsAsync();
            }
            catch (Exception ex)
            {
                Global.Log.LogError(ex, ex.Message);
            }
            return sensitivityLabels;
        }        
        #endregion
    }
}
