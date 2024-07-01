namespace Domain.Entities.K8Pay.BankSlip
{
    public class K8PayBankSlipRequest
    {
        public decimal Valor { get; set; }
        public DateTime DataVencimento { get; set; }
        public string URLConfirmacao { get; set; }
        public string ClienteDescricao { get; set; }
        public string ClienteIP { get; set; }
        public string ClienteCPFCNPJ { get; set; }
        public string ClienteNome { get; set; }
        public string ClienteEmail { get; set; }
        public string ClienteEndereco { get; set; }
        public string ClienteBairro { get; set; }
        public string ClienteCidade { get; set; }
        public string ClienteCEP { get; set; }
        public string ClienteUF { get; set; }
        public string ClienteDDD { get; set; }
        public string ClienteNumeroCelular { get; set; }
        public string NumeroPedido { get; set; }
        public string TipoMulta { get; set; }
        public decimal ValorMulta { get; set; }
        public string TipoJuros { get; set; }
        public decimal ValorJuros { get; set; }
        public string TipoDesconto { get; set; }
        public decimal ValorDesconto { get; set; }
        public DateTime DataDesconto { get; set; }
        public decimal ValorDesconto2 { get; set; }
        public DateTime DataDesconto2 { get; set; }
        public decimal ValorDesconto3 { get; set; }
        public DateTime DataDesconto3 { get; set; }
        public string Mensagem2 { get; set; }
        public bool RetornarBase64 { get; set; }
        public bool EntradaCNAB { get; set; }
    }
}
