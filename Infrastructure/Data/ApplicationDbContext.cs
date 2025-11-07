using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        // ✅ Constructor
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ✅ Các bảng (DbSet)
        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<ServicePackage> ServicePackages { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<BlockchainLedger> BlockchainLedgers { get; set; }
        public DbSet<Category> Categories { get; set; }


        // ✅ Cấu hình chi tiết (Fluent API)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔹 Invoice ↔ Customer (1-nhiều)
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Customer)
                .WithMany() // hoặc .WithMany(c => c.Invoices) nếu bạn thêm List<Invoice> trong Customer
                .HasForeignKey(i => i.CustemerId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 Invoice ↔ ServicePackage (1-nhiều)
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Package)
                .WithMany()
                .HasForeignKey(i => i.PackageId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 BlockchainLedger ↔ Invoice (1-1)
            modelBuilder.Entity<BlockchainLedger>()
                .HasOne(b => b.Invoice)
                .WithOne()
                .HasForeignKey<BlockchainLedger>(b => b.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔹 Kiểu Enum lưu dưới dạng chuỗi
            modelBuilder.Entity<Invoice>()
                .Property(i => i.Status)
                .HasConversion<string>();

            // 🔹 ServicePackage ↔ Category (nhiều-1)
            modelBuilder.Entity<ServicePackage>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Packages)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull); // Nếu xóa Category thì ServicePackage vẫn giữ nguyên


            // 🔹 Đặt tên bảng trong DB (tùy chọn)
            modelBuilder.Entity<UserAccount>().ToTable("UserAccounts");
            modelBuilder.Entity<Customer>().ToTable("Customers");
            modelBuilder.Entity<ServicePackage>().ToTable("ServicePackages");
            modelBuilder.Entity<Invoice>().ToTable("Invoices");
            modelBuilder.Entity<BlockchainLedger>().ToTable("BlockchainLedgers");
            modelBuilder.Entity<Category>().ToTable("Categories");

        }
    }
}
