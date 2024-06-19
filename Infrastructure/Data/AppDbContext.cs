using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Transaction> Transactions { get; set; }
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
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").IsRequired();
                entity.Property(e => e.Status).HasColumnName("Status").HasMaxLength(20);
                entity.Property(e => e.Details).HasColumnName("Details").HasConversion(
                    v => v.ToString(),
                    v => JObject.Parse(v));
            });
        }
    }
}
