using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<Withdraw> Withdraws { get; set; }
        public DbSet<Deposit> Deposits { get; set; }
        public DbSet<CustomerPayout> CustomerPayouts { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Refund> Refunds { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("Transactions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id").IsRequired();
                entity.Property(e => e.PaymentType).HasColumnName("PaymentType").IsRequired().HasMaxLength(50);
                entity.Property(e => e.OrderId).HasColumnName("OrderId").IsRequired().HasMaxLength(50);
                entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("decimal(10,2)");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc)).IsRequired();
                entity.Property(e => e.Status).HasColumnName("Status").HasMaxLength(20);
                entity.Property(e => e.Details).HasColumnName("Details").HasColumnType("jsonb");
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
                entity.Property(e => e.WalletType).HasConversion<string>().HasMaxLength(20).IsRequired();
                entity.Property(e => e.IsDefault).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.LastUpdateAt).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasIndex(e => new { e.SellerId, e.WalletType })
                      .HasName("IX_Wallets_SellerId_WalletType");
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
                entity.HasIndex(e => new { e.BankCode, e.AccountNumber, e.BranchNumber }).HasName("IX_BankAccounts_AccountDetails");
                entity.HasIndex(e => new { e.PixKey, e.PixKeyType }).HasName("IX_BankAccounts_PixKey").IsUnique().HasFilter("\"PIXKey\" IS NOT NULL AND \"PIXKeyType\" IS NOT NULL");
                entity.HasIndex(e => e.SellerId).HasName("IX_BankAccounts_SellerId");
            });

            modelBuilder.Entity<Withdraw>(entity =>
            {
                entity.ToTable("Withdraws");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.WithdrawMethod).HasMaxLength(50);
                entity.Property(e => e.RejectionReason).HasMaxLength(500);
                entity.Property(e => e.ApprovedBy).HasMaxLength(50);

                entity.HasOne(e => e.BankAccount)
                    .WithMany()
                    .HasForeignKey(e => e.BankAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Deposit>(entity =>
            {
                entity.ToTable("Deposits");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.TransactionId).HasMaxLength(100);
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);

                entity.HasIndex(e => e.TransactionId);
            });

            modelBuilder.Entity<CustomerPayout>(entity =>
            {
                entity.ToTable("CustomerPayouts");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.SellerId).IsRequired();
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
                entity.Property(e => e.RequestedAt).IsRequired();
                entity.Property(e => e.ProcessedAt);

                entity.Property(e => e.PixKey).HasMaxLength(100).IsRequired();
                entity.Property(e => e.PixKeyType).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.RejectionReason).HasMaxLength(500);

                entity.Property(e => e.ValidationId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ValidatedAt);

                entity.Property(e => e.ConfirmedByAdminId).HasMaxLength(50);
                entity.Property(e => e.ConfirmedAt);
                entity.Property(e => e.PaymentId).HasMaxLength(100);
                entity.Property(e => e.PaymentProofId).HasMaxLength(100);

                entity.HasIndex(e => e.SellerId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ValidationId).IsUnique();
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id").IsRequired();
                entity.Property(e => e.Status).HasColumnName("Status").IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasColumnName("Description").HasMaxLength(500);
                entity.Property(e => e.SendAttempts).HasColumnName("SendAttempts").IsRequired();
                entity.Property(e => e.SendStatus).HasColumnName("SendStatus").IsRequired().HasMaxLength(20);
                entity.Property(e => e.NextAttempt).HasColumnName("NextAttempt").HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc)).IsRequired();
                entity.Property(e => e.LastAttempt).HasColumnName("LastAttempt");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc)).IsRequired();
                entity.Property(e => e.ClientUrl).HasColumnName("ClientUrl").HasMaxLength(200);

                entity.HasOne(e => e.Transaction)
                    .WithMany()
                    .HasForeignKey(e => e.TransactionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Refund>(entity =>
            {
                entity.ToTable("Refunds");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.Reason).HasMaxLength(100);

                entity.HasOne(e => e.Transaction)
                    .WithMany()
                    .HasForeignKey(e => e.TransactionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
