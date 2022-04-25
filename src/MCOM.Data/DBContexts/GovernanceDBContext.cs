using Microsoft.EntityFrameworkCore;
using MCOM.Models.EntityFramework.Governance;

namespace MCOM.Data.DBContexts
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
                entity.ToTable("MCOMScanExecution");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.Filename)
                    .HasMaxLength(255)
                    .HasColumnName("filename");

                entity.Property(e => e.RequestId).HasColumnName("requestId");

                entity.HasOne(d => d.Request)
                    .WithMany(p => p.McomscanExecutions)
                    .HasForeignKey(d => d.RequestId)
                    .HasConstraintName("FK_MCOMScanExecution_MCOMScanRequest");
            });

            _ = modelBuilder.Entity<McomscanRequest>(entity =>
              {
                  entity.ToTable("MCOMScanRequest");

                  entity.Property(e => e.Id)
                      .ValueGeneratedNever()
                      .HasColumnName("id");

                  entity.Property(e => e.Businessunit)
                      .HasMaxLength(255)
                      .HasColumnName("businessunit");

                  entity.Property(e => e.Comments).HasColumnName("comments");

                  entity.Property(e => e.Documentname).HasColumnName("documentname");

                  entity.Property(e => e.Isphysical).HasColumnName("isphysical");

                  entity.Property(e => e.Itemid).HasColumnName("itemid");

                  entity.Property(e => e.LibraryId).HasMaxLength(255);

                  entity.Property(e => e.Ordernumber)
                      .HasMaxLength(50)
                      .HasColumnName("ordernumber");

                  entity.Property(e => e.Requestdate)
                      .HasColumnType("datetime")
                      .HasColumnName("requestdate");

                  entity.Property(e => e.Requester)
                      .HasMaxLength(150)
                      .HasColumnName("requester");

                  entity.Property(e => e.Status)
                      .HasMaxLength(50)
                      .HasColumnName("status");

                  entity.Property(e => e.Vendor)
                      .HasMaxLength(255)
                      .HasColumnName("vendor");

                  entity.Property(e => e.Wbs)
                      .HasMaxLength(100)
                      .HasColumnName("wbs");
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
