namespace Domain.Entities.K8Pay.CreditCard
{
    public class K8PayCreditCardRequest
    {
        public string NomeImpresso { get; set; }
        public string DataValidade { get; set; }
        public string NumeroCartao { get; set; }
        public int Valor { get; set; }
        public int CartaoFormaPagamento { get; set; }
        public int QuantidadeParcelas { get; set; }
        public string ClienteDescricao { get; set; }
        public string ClienteIP { get; set; }
        public string ClienteCPFCNPJ { get; set; }
        public string ClienteNome { get; set; }
        public string ClienteEmail { get; set; }
        public string ClienteSexo { get; set; }
        public string ClienteDDD { get; set; }
        public string ClienteNumeroCelular { get; set; }
        public string ClienteEndereco { get; set; }
        public string ClienteComplemento { get; set; }
        public string ClienteNumero { get; set; }
        public string ClienteBairro { get; set; }
        public string ClienteCidade { get; set; }
        public string ClienteCEP { get; set; }
        public string ClienteUF { get; set; }
        public string NumeroPedido { get; set; }
        public bool Recorrencia { get; set; }
    }
}
