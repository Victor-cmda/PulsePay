using Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models
{
    public class BankAccount
    {
        public Guid Id { get; set; }
        public Guid SellerId { get; set; }
        public string BankName { get; set; }
        public string BankCode { get; set; }
        public BankAccountType AccountType { get; set; }

        // Campos específicos para TED
        public string? AccountNumber { get; set; }
        public string? BranchNumber { get; set; }

        // Campos específicos para PIX
        public string? PixKey { get; set; }
        public PixKeyType? PixKeyType { get; set; }

        public string DocumentNumber { get; set; }
        public string AccountHolderName { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }

        // Relacionamentos
        public virtual ICollection<Withdraw> Withdraws { get; set; }
    }
}