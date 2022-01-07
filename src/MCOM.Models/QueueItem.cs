namespace MCOM.Models
{
    public class QueueItem
    {
        public string ResponseUrl { get; set; }
        public FeedbackItem Item  { get; set; }
    }

    public class FeedbackItem
    {
        public string DriveId { get; set; }
        public string DocumentId { get; set; }
    }
}
