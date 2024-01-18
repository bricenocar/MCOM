namespace MCOM.Models.Provisioning
{
    public class SensitivityLabel
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string SecurityClassification { get; set; }
        public string SiteClassification { get; set; }
        public bool IsExternalSharingAllowed { get; set; }
        public string Privacy { get; set; }
        public string MCOMGuid { get; set; }
        public string DefaultPrivacy { get; set; }
    }
}
