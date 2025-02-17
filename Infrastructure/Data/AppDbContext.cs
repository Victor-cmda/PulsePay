using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Withdraw> Withdraws { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("Transactions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id").IsRequired();
                entity.Property(e => e.PaymentType).HasColumnName("PaymentType").IsRequired().HasMaxLength(50);
                entity.Property(e => e.TransactionId).HasColumnName("TransactionId").IsRequired().HasMaxLength(50);
                entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("decimal(10,2)");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc)).IsRequired();
                entity.Property(e => e.Status).HasColumnName("Status").HasMaxLength(20);
                entity.Property(e => e.Details).HasColumnName("Details").HasColumnType("jsonb");
            });


            modelBuilder.Entity<Withdraw>(entity =>
            {
                entity.ToTable("Withdraws");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id").IsRequired();
                entity.Property(e => e.SellerId).HasColumnName("SellerId").IsRequired();
                entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Status).HasColumnName("Status").HasMaxLength(20).IsRequired();
                entity.Property(e => e.WithdrawMethod).HasColumnName("WithdrawMethod").HasMaxLength(50).IsRequired();
                entity.Property(e => e.RequestedAt).HasColumnName("RequestedAt").IsRequired();
                entity.Property(e => e.ProcessedAt).HasColumnName("ProcessedAt");
                entity.Property(e => e.BankAccountId).HasColumnName("BankAccountId").IsRequired();
                entity.Property(e => e.FailureReason).HasColumnName("FailureReason").HasMaxLength(500);
                entity.Property(e => e.TransactionReceipt).HasColumnName("TransactionReceipt");

                entity.HasOne(w => w.BankAccount)
                    .WithMany()
                    .HasForeignKey(w => w.BankAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WalletTransaction>(entity =>
            {
                entity.ToTable("WalletTransactions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.WalletId).IsRequired();
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
                entity.Property(e => e.Reference).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.ProcessedAt).IsRequired(false);
                entity.HasOne(e => e.Wallet).WithMany(w => w.Transactions).HasForeignKey(e => e.WalletId).OnDelete(DeleteBehavior.Restrict);
            });


            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.ToTable("Wallets");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd().IsRequired();
                entity.Property(e => e.SellerId).IsRequired();
                entity.Property(e => e.AvailableBalance).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.PendingBalance).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.TotalBalance).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.LastUpdateAt).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            modelBuilder.Entity<BankAccount>(entity =>
            {
                entity.ToTable("BankAccounts");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id").IsRequired();
                entity.Property(e => e.SellerId).HasColumnName("SellerId").IsRequired();
                entity.Property(e => e.BankName).HasColumnName("BankName").HasMaxLength(50).IsRequired();
                entity.Property(e => e.BankCode).HasColumnName("BankCode").HasMaxLength(10).IsRequired();
                entity.Property(e => e.AccountType).HasColumnName("AccountType").HasConversion<string>().HasMaxLength(20).IsRequired();
                entity.Property(e => e.AccountNumber).HasColumnName("AccountNumber").HasMaxLength(20); entity.Property(e => e.BranchNumber).HasColumnName("BranchNumber").HasMaxLength(10);
                entity.Property(e => e.PixKey).HasColumnName("PIXKey").HasMaxLength(100);
                entity.Property(e => e.PixKeyType).HasColumnName("PIXKeyType").HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.DocumentNumber).HasColumnName("DocumentNumber").HasMaxLength(20).IsRequired();
                entity.Property(e => e.AccountHolderName).HasColumnName("AccountHolderName").HasMaxLength(100).IsRequired();
                entity.Property(e => e.IsVerified).HasColumnName("IsVerified").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").IsRequired();
                entity.Property(e => e.LastUpdatedAt).HasColumnName("LastUpdatedAt").IsRequired();
                entity.HasMany(e => e.Withdraws).WithOne(w => w.BankAccount).HasForeignKey(w => w.BankAccountId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.BankCode, e.AccountNumber, e.BranchNumber }).HasName("IX_BankAccounts_AccountDetails");
                entity.HasIndex(e => new { e.PixKey, e.PixKeyType }).HasName("IX_BankAccounts_PixKey").IsUnique().HasFilter("\"PIXKey\" IS NOT NULL AND \"PIXKeyType\" IS NOT NULL");
                entity.HasIndex(e => e.SellerId).HasName("IX_BankAccounts_SellerId");
            });
        }
    }
}
