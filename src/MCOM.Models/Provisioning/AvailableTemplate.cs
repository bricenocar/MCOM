namespace MCOM.Models.Provisioning
{
    public class AvailableTemplate
    {
        public int TemplateId { get; set; }
        public int WorkLoadId { get; set; }
        public string Icon { get; set; }
        public string Name { get; set; }
        public string Markup { get; set; }        
        public string Location { get; set; }
    }
}
