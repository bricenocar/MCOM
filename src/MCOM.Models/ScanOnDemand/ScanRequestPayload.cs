using System;

namespace MCOM.Models.ScanOnDemand
{
    public class ScanRequestPayload
    {
        public string ItemId { get; set; }
        public string SiteId { get; set; }
        public string WebId { get; set; }
        public string ListId { get; set; }
        public string WBS { get; set; }
        public string RequestedBy { get; set; }
        public string RequestedByName { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public DateTime RequestedDate { get; set; }
        public string DocumentName { get; set; }
        public string Vendor { get; set; }
        public string Comments { get; set; }
        public Guid OrderNumber { get; set; }
        public string Status { get; set; }
        public bool IsPhysical { get; set; }
        public string BusinessArea { get; set; }
        public string Priority { get; set; }
        public string CompanyCode { get; set; }
    }
}
