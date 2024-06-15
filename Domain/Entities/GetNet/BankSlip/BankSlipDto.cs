namespace Domain.Entities.GetNet.BankSlip
{
    public class BankSlipDto
    {
        public string DocumentNumber { get; set; }
        public string ExpirationDate { get; set; }
        public string Instructions { get; set; }
        public string Provider { get; set; }
    }
}
