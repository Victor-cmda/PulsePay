namespace Domain.Entities.GetNet.CreditCard.Payment
{
    public class GetNetCreditCardResponse
    {
        public Guid PaymentId { get; set; }
        public Guid SellerId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public Guid OrderId { get; set; }
        public string Status { get; set; }
        public DateTime ReceivedAt { get; set; }
        public CreditResponse Credit { get; set; }
    }

    public class CreditResponse
    {
        public bool Delayed { get; set; }
        public int AuthorizationCode { get; set; }
        public DateTime AuthorizedAt { get; set; }
        public int ReasonCode { get; set; }
        public string ReasonMessage { get; set; }
        public string Acquirer { get; set; }
        public string SoftDescriptor { get; set; }
        public string Brand { get; set; }
        public int TerminalNsu { get; set; }
        public string AcquirerTransactionId { get; set; }
        public long TransactionId { get; set; }
        public string FirstInstallmentAmount { get; set; }
        public string OtherInstallmentAmount { get; set; }
        public string TotalInstallmentAmount { get; set; }
    }
}
