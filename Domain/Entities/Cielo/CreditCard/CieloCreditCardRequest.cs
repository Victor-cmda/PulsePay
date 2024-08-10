namespace Domain.Entities.Cielo.CreditCard
{
    public class CieloCreditCardRequest
    {
        public string MerchantOrderId { get; set; }
        public CustomerRequest Customer { get; set; }
        public PaymentRequest Payment { get; set; }
    }

    public class CustomerRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Birthdate { get; set; }
        public string Identity { get; set; }
        public string IdentityType { get; set; }
        public AddressRequest Address { get; set; }
        public AddressRequest DeliveryAddress { get; set; }
        public BillingRequest Billing { get; set; }
    }

    public class AddressRequest
    {
        public string Street { get; set; }
        public string Number { get; set; }
        public string Complement { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
    }

    public class BillingRequest
    {
        public string Street { get; set; }
        public string Number { get; set; }
        public string Complement { get; set; }
        public string Neighborhood { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }
    }

    public class PaymentRequest
    {
        public string Currency { get; set; }
        public string Country { get; set; }
        public string Provider { get; set; }
        public decimal ServiceTaxAmount { get; set; }
        public int Installments { get; set; }
        public string Interest { get; set; }
        public bool Capture { get; set; }
        public bool Authenticate { get; set; }
        public bool Recurrent { get; set; }
        public string SoftDescriptor { get; set; }
        public CreditCardRequest CreditCard { get; set; }
        public bool IsCryptoCurrencyNegotiation { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public AirlineDataRequest AirlineData { get; set; }
        public InitiatedTransactionIndicatorRequest InitiatedTransactionIndicator { get; set; }
    }

    public class CreditCardRequest
    {
        public string CardNumber { get; set; }
        public string Holder { get; set; }
        public string ExpirationDate { get; set; }
        public string SecurityCode { get; set; }
        public bool SaveCard { get; set; }
        public string Brand { get; set; }
        public CardOnFileRequest CardOnFile { get; set; }
    }

    public class CardOnFileRequest
    {
        public string Usage { get; set; }
        public string Reason { get; set; }
    }

    public class AirlineDataRequest
    {
        public string TicketNumber { get; set; }
    }

    public class InitiatedTransactionIndicatorRequest
    {
        public string Category { get; set; }
        public string Subcategory { get; set; }
    }
}

