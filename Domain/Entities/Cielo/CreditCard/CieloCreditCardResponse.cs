namespace Domain.Entities.Cielo.CreditCard
{
    public class CieloCreditCardResponse
    {
        public string ProofOfSale { get; set; }
        public string Tid { get; set; }
        public string AuthorizationCode { get; set; }
        public string SoftDescriptor { get; set; }
        public Guid PaymentId { get; set; }
        public string ECI { get; set; }
        public byte Status { get; set; }
        public string ReturnCode { get; set; }
        public string ReturnMessage { get; set; }
        public string MerchantAdviceCode { get; set; }
        public bool TryAutomaticCancellation { get; set; }
        public PaymentAccountReference PaymentAccountReference { get; set; }
    }

    public class PaymentAccountReference
    {
        public string PaymentAccountReferenceValue { get; set; }
    }
}

