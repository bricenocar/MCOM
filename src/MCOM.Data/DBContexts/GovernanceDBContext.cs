using Microsoft.EntityFrameworkCore;
using MCOM.Models.EntityFramework.Governance;

namespace MCOM.Data
{
    public partial class GovernanceDBContext : DbContext
    {
        public GovernanceDBContext()
        {
        }

        public GovernanceDBContext(DbContextOptions<GovernanceDBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<McomscanExecution> McomscanExecutions { get; set; } = null!;
        public virtual DbSet<McomscanRequest> McomscanRequests { get; set; } = null!;
        public virtual DbSet<McomscanRequestMessage> McomscanRequestMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<McomscanExecution>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("MCOMScanExecution");

                entity.Property(e => e.Author)
                    .HasMaxLength(255)
                    .HasColumnName("author");

                entity.Property(e => e.Datescanned)
                    .HasColumnType("datetime")
                    .HasColumnName("datescanned");

                entity.Property(e => e.Documentdate)
                    .HasMaxLength(255)
                    .HasColumnName("documentdate");

                entity.Property(e => e.Field)
                    .HasMaxLength(255)
                    .HasColumnName("field");

                entity.Property(e => e.Filename)
                    .HasMaxLength(255)
                    .HasColumnName("filename");

                entity.Property(e => e.Installation)
                    .HasMaxLength(255)
                    .HasColumnName("installation");

                entity.Property(e => e.License)
                    .HasMaxLength(255)
                    .HasColumnName("license");

                entity.Property(e => e.RequestId).HasColumnName("requestId");

                entity.Property(e => e.Size).HasColumnName("size");

                entity.Property(e => e.Well)
                    .HasMaxLength(255)
                    .HasColumnName("well");

                entity.HasOne(d => d.Request)
                    .WithMany()
                    .HasForeignKey(d => d.RequestId)
                    .HasConstraintName("FK_MCOMScanExecution_MCOMScanRequest");
            });

            modelBuilder.Entity<McomscanRequest>(entity =>
            {
                entity.ToTable("MCOMScanRequest");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Businessunit)
                    .HasMaxLength(255)
                    .HasColumnName("businessunit");

                entity.Property(e => e.Comments).HasColumnName("comments");

                entity.Property(e => e.Companycode)
                    .HasMaxLength(255)
                    .HasColumnName("companycode");

                entity.Property(e => e.Creator)
                    .HasMaxLength(255)
                    .HasColumnName("creator");

                entity.Property(e => e.Documentname).HasColumnName("documentname");

                entity.Property(e => e.Filemetadata).HasColumnName("filemetadata");

                entity.Property(e => e.Isphysical).HasColumnName("isphysical");

                entity.Property(e => e.Itemid).HasColumnName("itemid");

                entity.Property(e => e.Listid)
                    .HasMaxLength(255)
                    .HasColumnName("listid");

                entity.Property(e => e.Ordernumber)
                    .HasMaxLength(50)
                    .HasColumnName("ordernumber");

                entity.Property(e => e.Priority)
                    .HasMaxLength(255)
                    .HasColumnName("priority");

                entity.Property(e => e.Requestdate)
                    .HasColumnType("datetime")
                    .HasColumnName("requestdate");

                entity.Property(e => e.Requester)
                    .HasMaxLength(150)
                    .HasColumnName("requester");

                entity.Property(e => e.Siteid)
                    .HasMaxLength(255)
                    .HasColumnName("siteid");

                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasColumnName("status");

                entity.Property(e => e.Vendor)
                    .HasMaxLength(255)
                    .HasColumnName("vendor");

                entity.Property(e => e.Wbs)
                    .HasMaxLength(100)
                    .HasColumnName("wbs");

                entity.Property(e => e.Webid)
                    .HasMaxLength(255)
                    .HasColumnName("webid");
            });

            modelBuilder.Entity<McomscanRequestMessage>(entity =>
            {
                entity.ToTable("MCOMScanRequestMessage");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Createddate)
                    .HasColumnType("datetime")
                    .HasColumnName("createddate");

                entity.Property(e => e.Creator)
                    .HasMaxLength(255)
                    .HasColumnName("creator");

                entity.Property(e => e.Description).HasColumnName("description");

                entity.Property(e => e.Requestid).HasColumnName("requestid");

                entity.HasOne(d => d.Request)
                    .WithMany(p => p.McomscanRequestMessages)
                    .HasForeignKey(d => d.Requestid)
                    .HasConstraintName("FK_RequestMessage_Request");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
