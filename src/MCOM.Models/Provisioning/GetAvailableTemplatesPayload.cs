namespace MCOM.Models.Provisioning
{
    public class GetAvailableTemplatesPayload
    {
        public string OptionId { get; set; }
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }        
        public string FileBlobName { get; set; }
        public string FileBlobPath { get; set; }
    }
}
