namespace Application.DTOs.BankSlip
{
    public class PaymentBankSlipRequestDto
    {
        public decimal Amount { get; set; }
        public string OrderId { get; set; }
        public CustomerDto Customer { get; set; }
    }

    public class CustomerDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string DocumentType { get; set; }
        public string DocumentNumber { get; set; }
        public BillingAddressDto BillingAddress { get; set; }
    }

    public class BillingAddressDto
    {
        public string Street { get; set; }
        public string Number { get; set; }
        public string? Complement { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
    }
}
