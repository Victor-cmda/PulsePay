namespace Application.DTOs
{
    public class WalletCreateDto
    {
        public Guid SellerId { get; set; }
    }

    public class WalletUpdateDto
    {
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
    }

    public class WalletResponseDto
    {
        public Guid Id { get; set; }
        public Guid SellerId { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal TotalBalance { get; set; }
        public DateTime LastUpdateAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
