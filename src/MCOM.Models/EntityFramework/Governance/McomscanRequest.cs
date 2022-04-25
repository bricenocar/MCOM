using System;
using System.Collections.Generic;

namespace MCOM.Models.EntityFramework.Governance
{
    public partial class McomscanRequest
    {
        public McomscanRequest()
        {
            McomscanExecutions = new HashSet<McomscanExecution>();
            McomscanRequestMessages = new HashSet<McomscanRequestMessage>();
        }

        public Guid Id { get; set; }
        public string? Requester { get; set; }
        public string? SiteUrl { get; set; }
        public string? LibraryId { get; set; }
        public DateTime? Requestdate { get; set; }
        public string? Wbs { get; set; }
        public string? Businessunit { get; set; }
        public int? Itemid { get; set; }
        public string? Documentname { get; set; }
        public string? Vendor { get; set; }
        public string? Comments { get; set; }
        public string? Ordernumber { get; set; }
        public string? Status { get; set; }
        public bool? Isphysical { get; set; }

        public virtual ICollection<McomscanExecution> McomscanExecutions { get; set; }
        public virtual ICollection<McomscanRequestMessage> McomscanRequestMessages { get; set; }
    }
}
