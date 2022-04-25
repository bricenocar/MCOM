using System;

namespace MCOM.Models.EntityFramework.Governance
{
    public partial class McomscanExecution
    {
        public Guid Id { get; set; }
        public string? Filename { get; set; }
        public Guid? RequestId { get; set; }

        public virtual McomscanRequest? Request { get; set; }
    }
}
