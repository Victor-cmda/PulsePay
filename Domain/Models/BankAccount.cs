using Shared.Enums;

namespace Domain.Models
{
    public class BankAccount
    {
        public Guid Id { get; set; }
        public Guid SellerId { get; set; }
        public string BankName { get; set; }
        public string BankCode { get; set; }
        public BankAccountType AccountType { get; set; }
        public string DocumentNumber { get; set; }
        public string AccountHolderName { get; set; }
        public bool IsVerified { get; set; }

        public string? AccountNumber { get; set; }
        public string? BranchNumber { get; set; }

        public string? PixKey { get; set; }
        public PixKeyType? PixKeyType { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }

        public string? Status { get; set; } = "Pending";
        public string? RejectionReason { get; set; }


        public bool IsTedAccount => AccountType == BankAccountType.TED;
        public bool IsPixAccount => AccountType == BankAccountType.PIX;

        public bool HasValidTedInformation()
        {
            return IsTedAccount &&
                   !string.IsNullOrEmpty(AccountNumber) &&
                   !string.IsNullOrEmpty(BranchNumber);
        }

        public bool HasValidPixInformation()
        {
            return IsPixAccount &&
                   !string.IsNullOrEmpty(PixKey) &&
                   PixKeyType.HasValue;
        }

        public static BankAccount CreateTedAccount(
            Guid sellerId,
            string bankName,
            string bankCode,
            string accountNumber,
            string branchNumber,
            string documentNumber,
            string accountHolderName)
        {
            return new BankAccount
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                BankName = bankName,
                BankCode = bankCode,
                AccountType = BankAccountType.TED,
                AccountNumber = accountNumber,
                BranchNumber = branchNumber,
                DocumentNumber = documentNumber,
                AccountHolderName = accountHolderName,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };
        }

        public static BankAccount CreatePixAccount(
            Guid sellerId,
            string bankName,
            string bankCode,
            string pixKey,
            PixKeyType pixKeyType,
            string documentNumber,
            string accountHolderName)
        {
            return new BankAccount
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                BankName = bankName,
                BankCode = bankCode,
                AccountType = BankAccountType.PIX,
                PixKey = pixKey,
                PixKeyType = pixKeyType,
                DocumentNumber = documentNumber,
                AccountHolderName = accountHolderName,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };
        }
    }
}