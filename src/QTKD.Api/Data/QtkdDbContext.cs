using Microsoft.EntityFrameworkCore;
using QTKD.Api.Entities;

namespace QTKD.Api.Data;

public class QtkdDbContext : DbContext
{
    public QtkdDbContext(DbContextOptions<QtkdDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractAttachment> ContractAttachments => Set<ContractAttachment>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceItem> PriceItems => Set<PriceItem>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ContractHistory> ContractHistories => Set<ContractHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------- Customer ----------
        modelBuilder.Entity<Customer>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        // ---------- Contract ----------
        modelBuilder.Entity<Contract>(e =>
        {
            e.Property(x => x.ContractNo).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.ContractNo).IsUnique();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.ServiceGroup).HasMaxLength(200);

            // Contract - Customer (n-1)
            e.HasOne(x => x.Customer)
                .WithMany(c => c.Contracts)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- ContractAttachment ----------
        modelBuilder.Entity<ContractAttachment>(e =>
        {
            e.Property(x => x.FileName).IsRequired().HasMaxLength(260);
            e.HasOne(x => x.Contract)
                .WithMany(c => c.Attachments)
                .HasForeignKey(x => x.ContractId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- PriceList / PriceItem ----------
        modelBuilder.Entity<PriceList>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<PriceItem>(e =>
        {
            // Khóa chính ghép
            e.HasKey(x => new { x.PriceListId, x.ServiceCode });
            e.Property(x => x.ServiceCode).IsRequired().HasMaxLength(50);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.ToTable(t => t.HasCheckConstraint("CK_PriceItems_UnitPrice", "\"UnitPrice\" > 0"));

            // PriceList - PriceItem (1-n)
            e.HasOne(x => x.PriceList)
                .WithMany(p => p.Items)
                .HasForeignKey(x => x.PriceListId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- ApprovalRequest ----------
        modelBuilder.Entity<ApprovalRequest>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Contract)
                .WithMany(c => c.ApprovalRequests)
                .HasForeignKey(x => x.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            // Mỗi hợp đồng chỉ có tối đa 1 yêu cầu đang Pending (giả định G7, chặn gửi trùng ở mức DB)
            e.HasIndex(x => x.ContractId)
                .IsUnique()
                .HasFilter("\"Status\" = 'Pending'");
        });

        // ---------- ContractHistory ----------
        modelBuilder.Entity<ContractHistory>(e =>
        {
            e.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(30);
            e.HasOne(x => x.Contract)
                .WithMany(c => c.Histories)
                .HasForeignKey(x => x.ContractId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Dữ liệu mẫu (tuần 05) ----------
        modelBuilder.Entity<Customer>().HasData(
            new Customer { Id = 1, Name = "Công ty Minh Phát", Status = CustomerStatus.Active },
            new Customer { Id = 2, Name = "Công ty Hải Âu", Status = CustomerStatus.Active }
        );

        // Hợp đồng mẫu cố ý chưa có Content và chưa có tệp đính kèm,
        // để tuần 06 demo được: gửi thất bại -> bổ sung -> gửi thành công.
        modelBuilder.Entity<Contract>().HasData(
            new Contract
            {
                Id = 1,
                ContractNo = "HD-2026-018",
                CustomerId = 1,
                StartDate = new DateOnly(2026, 10, 1),
                EndDate = new DateOnly(2027, 9, 30),
                Status = ContractStatus.Draft,
                ServiceGroup = "Dịch vụ cảng",
                Content = null
            }
        );

        modelBuilder.Entity<PriceList>().HasData(
            new PriceList { Id = 1, Name = "Bảng giá dịch vụ cảng 2026", EffectiveDate = new DateOnly(2026, 10, 1) }
        );

        modelBuilder.Entity<PriceItem>().HasData(
            new PriceItem { PriceListId = 1, ServiceCode = "CANG-01", UnitPrice = 1500000m },
            new PriceItem { PriceListId = 1, ServiceCode = "CANG-02", UnitPrice = 800000m }
        );
    }
}
