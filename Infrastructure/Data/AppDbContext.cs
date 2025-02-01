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
                entity.Property(e => e.Details).HasColumnName("Details").HasConversion(
                    v => v.ToString(),
                    v => JObject.Parse(v));
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
                entity.Property(e => e.Id).HasColumnName("Id").IsRequired();
                entity.Property(e => e.WalletId).HasColumnName("WalletId").IsRequired();
                entity.Property(e => e.SellerId).HasColumnName("SellerId").IsRequired();
                entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.TransactionType).HasColumnName("TransactionType").HasMaxLength(20).IsRequired();
                entity.Property(e => e.Status).HasColumnName("Status").HasMaxLength(20).IsRequired();
                entity.Property(e => e.ReferenceId).HasColumnName("ReferenceId");
                entity.Property(e => e.ReferenceType).HasColumnName("ReferenceType").HasMaxLength(50);
                entity.Property(e => e.PreviousBalance).HasColumnName("PreviousBalance").HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.NewBalance).HasColumnName("NewBalance").HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").IsRequired();
            });

            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.ToTable("Wallets");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id").IsRequired();
                entity.Property(e => e.SellerId).HasColumnName("SellerId").IsRequired();
                entity.Property(e => e.AvailableBalance).HasColumnName("AvailableBalance").HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.PendingBalance).HasColumnName("PendingBalance").HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.TotalBalance).HasColumnName("TotalBalance").HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.LastUpdateAt).HasColumnName("LastUpdateAt").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").IsRequired();
            });

            modelBuilder.Entity<BankAccount>(entity =>
            {
                entity.ToTable("BankAccounts");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id").IsRequired();
                entity.Property(e => e.SellerId).HasColumnName("SellerId").IsRequired();
                entity.Property(e => e.BankName).HasColumnName("BankName").HasMaxLength(50).IsRequired();
                entity.Property(e => e.AccountType).HasColumnName("AccountType").HasMaxLength(20).IsRequired();
                entity.Property(e => e.AccountNumber).HasColumnName("AccountNumber").HasMaxLength(20).IsRequired();
                entity.Property(e => e.BranchNumber).HasColumnName("BranchNumber").HasMaxLength(10).IsRequired();
                entity.Property(e => e.PIXKey).HasColumnName("PIXKey").HasMaxLength(20);
                entity.Property(e => e.PIXKeyType).HasColumnName("PIXKeyType").HasMaxLength(20);
                entity.Property(e => e.IsDefault).HasColumnName("IsDefault").IsRequired();
                entity.Property(e => e.IsActive).HasColumnName("IsActive").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").IsRequired();
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
            });
        }
    }
}
