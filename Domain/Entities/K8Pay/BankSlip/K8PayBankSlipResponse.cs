namespace Domain.Entities.K8Pay.BankSlip
{
    public class K8PayBankSlipResponse
    {
        public string Retorno { get; set; }
        public string DetalhesErro { get; set; }
        public string Identificador { get; set; }
        public string NossoNumero { get; set; }
        public string LinhaDigitavel { get; set; }
        public string CodigoBarras { get; set; }
        public string BoletoPDF { get; set; }
        public string CodigoErro { get; set; }
    }
}
