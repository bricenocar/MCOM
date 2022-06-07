using System;

namespace MCOM.Models.EntityFramework.Governance
{
    public partial class McomscanExecution
    {
        public string? Filename { get; set; }
        public DateTime? Datescanned { get; set; }
        public Guid? RequestId { get; set; }
        public int? Size { get; set; }

        public virtual McomscanRequest? Request { get; set; }
    }
}
