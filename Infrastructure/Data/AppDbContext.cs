using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Notification> Notifications { get; set; }

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
                entity.Property(e => e.Details).HasColumnName("Details").HasConversion(
                    v => v.ToString(),
                    v => JObject.Parse(v));
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
        }
    }
}
