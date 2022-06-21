using System;

namespace MCOM.Models.EntityFramework.Governance
{
    public partial class McomscanExecution
    {
        public string? Filename { get; set; }
        public DateTime? Datescanned { get; set; }
        public Guid? RequestId { get; set; }
        public int? Size { get; set; }
        public string? Documentdate { get; set; }
        public string? Author { get; set; }
        public string? License { get; set; }
        public string? Field { get; set; }
        public string? Well { get; set; }
        public string? Installation { get; set; }

        public virtual McomscanRequest? Request { get; set; }
    }
}
