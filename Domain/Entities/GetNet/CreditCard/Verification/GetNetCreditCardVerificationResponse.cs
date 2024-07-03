namespace Domain.Entities.GetNet.CreditCard.Verification
{
    public class GetNetCreditCardVerificationResponse
    {
        public string status { get; set; }
        public string verification_id { get; set; }
        public string authorization_code { get; set; }
    }
}
