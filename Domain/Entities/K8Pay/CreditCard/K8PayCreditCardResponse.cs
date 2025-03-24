namespace Domain.Entities.K8Pay.CreditCard
{
    public class K8PayCreditCardResponse
    {
        public string Retorno { get; set; }
        public string DetalhesErro { get; set; }
        public string Identificador { get; set; }
        public string CodigoAutorizacao { get; set; }
        public string NsuOperacao { get; set; }
        public string Adquirente { get; set; }
        public string NumeroAutorizacao { get; set; }
    }
}
