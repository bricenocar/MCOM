using System;

namespace MCOM.Models
{
    public class ScanRequestPayload
    {
        public string Id { get; set; }
        public string WBS { get; set; }
        public string RequestedBy { get; set; }
        public DateTime RequestedDate { get; set; }
        public string DocumentName { get; set; }
        public string Vendor { get; set; }
        public string Comments { get; set; }
        public Guid OrderNumber { get; set; }
        public string Status { get; set; }
        public bool IsPhysical { get; set; }
    }
}
