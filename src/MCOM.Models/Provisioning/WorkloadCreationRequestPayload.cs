using System;
using System.Collections.Generic;

namespace MCOM.Models.Provisioning
{
    public class WorkloadCreationRequestPayload
    {
        public Site Site { get; set; }
        public Teams Team { get; set; }
    }

    public class Teams
    {
        public string TeamsName { get; set; }
    }

    public class Site    {
        
        public GroupUsers GroupUsers { get; set; }
        public SiteConfig SiteConfig { get; set; }
        public SiteMetadata SiteMetadata { get; set; }
        public SiteUsers SiteUsers { get; set; }
    }

    public enum SiteType
    {
        CommunicationSite,
        TeamSite
    }

    public class GroupUsers
    {
        public List<string> Members { get; set; }
        public List<string> Owners { get; set; }
    }

    public class SiteConfig
    {
        public string Description { get; set; }
        public string GroupEmailAddress { get; set; }
        public string SiteName { get; set; }
        public string Alias { get; set; }
        public bool ExternalSharing { get; set; }
        public bool IsPublic { get; set; }
        public string SiteURL { get; set; }
        public string SiteClassification { get; set; }
        public Guid SensitivityLabel { get; set; }
        public SiteType SiteType { get; set; }
    }

    public class SiteMetadata
    {
        public EIMMetadata EIMMetadata { get; set; }
        public Dictionary<string, string> TemplateMetadata { get; set; }
        public Dictionary<string, string> OptionalMetadata { get; set; }
    }

    public class EIMMetadata
    {
        public string BCL1 { get; set; }
        public string BCL2 { get; set; }
        public string BusinessArea { get; set; }
        public string Country { get; set; }
        public string InformationType { get; set; }
        public string Block { get; set; }
        public string LegalEntity { get; set; }
        public string SecurityClassification { get; set; }
    }

    public class SiteUsers
    {
        public List<string> Owners { get; set; }
        public List<string> Members { get; set; }
        public List<string> Visitors { get; set; }
    }

    public class WorkloadCreationRequestResponse
    {
        public CreatedSite CreatedSite { get; set; }
        public CreatedTeam CreatedTeam { get; set; }
    }

    public class CreatedSite
    {
        public string SiteUrl { get; set; }
        public Guid SiteId { get; set; }
        public Guid GroupId { get; set; }
    }

    public class CreatedTeam
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; }
    }
}
