namespace Domain.Entities.GetNet.BankSlip
{
    public class GetNetBankSlipResponse
    {
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public string Identifier { get; set; }
        public string OurNumber { get; set; }
        public string DigitableLine { get; set; }
        public string Barcode { get; set; }
        public string Pdf { get; set; }
        public string ErrorCode { get; set; }
    }
}
