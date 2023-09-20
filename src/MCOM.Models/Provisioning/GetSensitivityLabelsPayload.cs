namespace MCOM.Models.Provisioning
{
    public class GetSensitivityLabelsPayload
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string SecurityClassification { get; set; }
        public string SiteClassification { get; set; }
    }
}
