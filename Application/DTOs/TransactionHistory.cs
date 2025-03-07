namespace Application.DTOs
{
    /// <summary>
    /// DTO para resposta com histórico de transações
    /// </summary>
    public class TransactionHistoryResponseDto
    {
        public IEnumerable<WalletTransactionDto> Transactions { get; set; }
        public PaginationMetadataDto Pagination { get; set; }
        public TransactionSummaryDto Summary { get; set; }
    }

    /// <summary>
    /// DTO com metadados de paginação
    /// </summary>
    public class PaginationMetadataDto
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }

    /// <summary>
    /// DTO com sumário das transações
    /// </summary>
    public class TransactionSummaryDto
    {
        public decimal TotalCredits { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal NetAmount { get; set; }
        public int TotalTransactions { get; set; }
    }
}
