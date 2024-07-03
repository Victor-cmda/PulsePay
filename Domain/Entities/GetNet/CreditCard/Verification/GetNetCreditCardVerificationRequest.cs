namespace Domain.Entities.GetNet.CreditCard.Verification
{
    public class GetNetCreditCardVerificationRequest
    {
        public string number_token { get; set; }
        public string brand { get; set; }
        public string cardholder_name { get; set; }
        public string expiration_month { get; set; }
        public string expiration_year { get; set; }
        public string security_code { get; set; }
    }
}
