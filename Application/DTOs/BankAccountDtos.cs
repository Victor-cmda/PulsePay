using Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.DTOs
{
    public class BankAccountCreateDto
    {
        public Guid SellerId { get; set; }
        public string BankName { get; set; }
        public string BankCode { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BankAccountType AccountType { get; set; }

        public string AccountNumber { get; set; }
        public string BranchNumber { get; set; }

        public string PixKey { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PixKeyType? PixKeyType { get; set; }

        public string DocumentNumber { get; set; }
        public string AccountHolderName { get; set; }
    }

    public class BankAccountUpdateDto
    {
        public string BankName { get; set; }
        public string BankCode { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BankAccountType AccountType { get; set; }

        public string AccountNumber { get; set; }
        public string BranchNumber { get; set; }

        public string PixKey { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PixKeyType? PixKeyType { get; set; }

        public string DocumentNumber { get; set; }
        public string AccountHolderName { get; set; }
    }

    public class BankAccountResponseDto
    {
        public Guid Id { get; set; }
        public Guid SellerId { get; set; }
        public string BankName { get; set; }
        public string BankCode { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BankAccountType AccountType { get; set; }

        public string AccountNumber { get; set; }
        public string BranchNumber { get; set; }

        public string PixKey { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PixKeyType? PixKeyType { get; set; }

        public string DocumentNumber { get; set; }
        public string AccountHolderName { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }

    public class BankAccountValidationDto
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }
}
