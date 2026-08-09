using BankCoreApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BankCoreApi.Data;

/// <summary>
/// Context Entity Framework Core untuk Core Banking Database.
/// Mengatur pemetaan tabel, constraint unik, relasi entitas, dan initial seeding data.
/// </summary>
public class BankDbContext : DbContext
{
    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Constraint Unik: NIK dan Email Customer harus unik
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Nik)
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();

        // Constraint Unik: Nomor Rekening harus unik
        modelBuilder.Entity<Account>()
            .HasIndex(a => a.AccountNumber)
            .IsUnique();

        // Relasi: Customer -> Accounts (1 to Many)
        modelBuilder.Entity<Account>()
            .HasOne(a => a.Customer)
            .WithMany(c => c.Accounts)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
