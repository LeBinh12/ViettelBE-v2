using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<ServicePackage> ServicePackages { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<BlockchainLedger> BlockchainLedgers { get; set; }
        public DbSet<Category> Categories { get; set; }
        
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔹 Relationships
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Customer)
                .WithMany()
                .HasForeignKey(i => i.CustemerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Package)
                .WithMany()
                .HasForeignKey(i => i.PackageId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BlockchainLedger>()
                .HasOne(b => b.Invoice)
                .WithOne()
                .HasForeignKey<BlockchainLedger>(b => b.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ServicePackage>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Packages)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // 🔹 Table names
            modelBuilder.Entity<UserAccount>().ToTable("UserAccounts");
            modelBuilder.Entity<Customer>().ToTable("Customers");
            modelBuilder.Entity<ServicePackage>().ToTable("ServicePackages");
            modelBuilder.Entity<Invoice>().ToTable("Invoices");
            modelBuilder.Entity<BlockchainLedger>().ToTable("BlockchainLedgers");
            modelBuilder.Entity<Category>().ToTable("Categories");

            // 🔹 Ignore các cột dư thừa của IdentityUser để tránh lỗi
            modelBuilder.Entity<UserAccount>().ToTable("UserAccounts");

            // Ignore các cột dư thừa của IdentityUser
            modelBuilder.Entity<UserAccount>(entity =>
            {
                entity.Ignore(u => u.NormalizedUserName);
                entity.Ignore(u => u.NormalizedEmail);
                entity.Ignore(u => u.ConcurrencyStamp);
                entity.Ignore(u => u.SecurityStamp);
                entity.Ignore(u => u.EmailConfirmed);
                entity.Ignore(u => u.PhoneNumberConfirmed);
                entity.Ignore(u => u.PhoneNumber);
                entity.Ignore(u => u.TwoFactorEnabled);
                entity.Ignore(u => u.LockoutEnabled);
                entity.Ignore(u => u.LockoutEnd);
                entity.Ignore(u => u.AccessFailedCount);
            });

        }
    }
}
