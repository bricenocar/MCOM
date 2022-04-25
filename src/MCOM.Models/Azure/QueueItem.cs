using System.Collections.Generic;

namespace MCOM.Models.Azure
{
    public class QueueItem
    {
        public string ClientUrl { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public FeedbackItem Content { get; set; }
        public string Source { get; set; }
    }

    public class FeedbackItem
    {
        public string DriveId { get; set; }
        public string DocumentId { get; set; }
    }
}
