using System;
using System.Collections.Generic;

namespace MCOM.Models.EntityFramework.Governance
{
    public partial class McomscanRequest
    {
        public McomscanRequest()
        {
            McomscanRequestMessages = new HashSet<McomscanRequestMessage>();
        }

        public Guid Id { get; set; }
        public string? Requester { get; set; }
        public string? Webid { get; set; }
        public string? Listid { get; set; }
        public string? Siteid { get; set; }
        public DateTime? Requestdate { get; set; }
        public string? Wbs { get; set; }
        public string? Businessunit { get; set; }
        public string? Itemid { get; set; }
        public string? Documentname { get; set; }
        public string? Vendor { get; set; }
        public string? Comments { get; set; }
        public string? Ordernumber { get; set; }
        public string? Status { get; set; }
        public bool? Isphysical { get; set; }
        public string? Filemetadata { get; set; }

        public virtual ICollection<McomscanRequestMessage> McomscanRequestMessages { get; set; }
    }
}
