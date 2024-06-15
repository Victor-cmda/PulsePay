namespace Domain.Entities.GetNet.BankSlip
{
    public class CustomerBankSlipDto
    {
        public string FirstName { get; set; }
        public string Name { get; set; }
        public string DocumentType { get; set; }
        public string DocumentNumber { get; set; }
        public BillingAddressDto BillingAddress { get; set; }
    }
}
