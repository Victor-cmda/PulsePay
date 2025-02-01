using Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CreateTransactionRequest
    {
        [Required(ErrorMessage = "WalletId é obrigatório")]
        public Guid WalletId { get; set; }

        [Required(ErrorMessage = "Valor é obrigatório")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Tipo de transação é obrigatório")]
        public TransactionType Type { get; set; }

        [Required(ErrorMessage = "Descrição é obrigatória")]
        [StringLength(500, ErrorMessage = "Descrição deve ter no máximo 500 caracteres")]
        public string Description { get; set; }

        [StringLength(100, ErrorMessage = "Referência deve ter no máximo 100 caracteres")]
        public string? Reference { get; set; }
    }

    public class UpdateTransactionStatusRequest
    {
        [Required(ErrorMessage = "Status é obrigatório")]
        public TransactionStatus Status { get; set; }

        [StringLength(500, ErrorMessage = "Motivo deve ter no máximo 500 caracteres")]
        public string? Reason { get; set; }
    }

    public class GetTransactionHistoryRequest
    {
        [Required(ErrorMessage = "Data inicial é obrigatória")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Data final é obrigatória")]
        public DateTime EndDate { get; set; }

        public TransactionStatus? Status { get; set; }
        public TransactionType? Type { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // Response DTOs
    public class TransactionResponse
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public TransactionStatus Status { get; set; }
        public string Description { get; set; }
        public string? Reference { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public class WalletBalanceResponse
    {
        public Guid WalletId { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class TransactionHistoryResponse
    {
        public IEnumerable<TransactionResponse> Transactions { get; set; }
        public PaginationMetadata Pagination { get; set; }
        public TransactionSummary Summary { get; set; }
    }

    public class PaginationMetadata
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }

    public class TransactionSummary
    {
        public decimal TotalCredits { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal NetAmount { get; set; }
        public int TotalTransactions { get; set; }
    }
}
