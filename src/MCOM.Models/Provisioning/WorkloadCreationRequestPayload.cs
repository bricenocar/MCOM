using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MCOM.Models.Provisioning
{
    public class WorkloadCreationRequestPayload
    {
        public Site Site { get; set; }
        public Teams Team { get; set; }
        public Request Request { get; set; }
    }

    public class Request
    {
        public int RequestId { get; set; }
        public int WorkloadId { get; set; }
        public string MessageId { get; set; }
        public Requester Requester { get; set; }
        public string RequestDate { get; set; }
        public string RequestOrderedThrough { get; set; }
        public Approver Approver { get; set; }
        public bool Bulk { get; set; }
        public string ProvisioningTemplateUrl { get; set; }
    }

    public class Requester
    {
        public string Email { get; set; }
        public int BusinessAreaId { get; set; }
        public int RoleId { get; set; }
    }

    public class Approver
    {
        public string Email { get; set; }
        public string Comments { get; set; }
    }

    public class Teams
    {
        public string TeamsName { get; set; }
    }

    public class Site
    {

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
        public List<User> Members { get; set; }
        public List<User> Owners { get; set; }
    }

    public class User
    {
        public string Value { get; set; }
    }

    public class SiteConfig
    {
        public SiteType SiteType { get; set; }
        public int TemplateId { get; set; }
        public string Description { get; set; }
        public string GroupEmailAddress { get; set; }
        public string SiteName { get; set; }
        public string Alias { get; set; }
        public bool ExternalSharing { get; set; }
        public bool IsPublic { get; set; }
        public string SiteURL { get; set; }
        public string SiteClassification { get; set; }
        public Guid SensitivityLabel { get; set; }
        public Guid SiteGuid { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid GroupId { get; set; }
        public Guid TeamId { get; set; }
    }

    public class SiteMetadata
    {
        [JsonConverter(typeof(NullToEmptyStringDictionaryConverter))]
        public Dictionary<string, string> EIMMetadata { get; set; }
        public Dictionary<string, string> TemplateMetadata { get; set; }
        public OptionalMetadata[] OptionalMetadata { get; set; }
    }

    public class OptionalMetadata
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string InternalName { get; set; }
        public string TermValues { get; set; }
    }

    public class SiteUsers
    {
        public List<User> Owners { get; set; }
        public List<User> Members { get; set; }
        public List<User> Visitors { get; set; }
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
        public Guid TeamId { get; set; }
    }

    public class CreatedTeam
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; }
    }
}
