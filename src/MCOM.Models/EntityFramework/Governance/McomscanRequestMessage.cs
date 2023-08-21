using System;

namespace MCOM.Models.EntityFramework.Governance
{
    public partial class McomscanRequestMessage
    {
        public Guid Id { get; set; }
        public string Creator { get; set; }
        public DateTime? Createddate { get; set; }
        public string Description { get; set; }
        public Guid? Requestid { get; set; }

        public virtual McomscanRequest? Request { get; set; }
    }
}
