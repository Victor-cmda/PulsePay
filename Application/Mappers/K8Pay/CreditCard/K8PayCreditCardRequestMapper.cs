using Application.DTOs.CreditCard;
using Domain.Entities.K8Pay.CreditCard;

namespace Application.Mappers.K8Pay
{
    public class K8PayCreditCardRequestMapper : IResponseMapper<PaymentCreditCardRequestDto, K8PayCreditCardRequest>
    {
        public K8PayCreditCardRequest Map(PaymentCreditCardRequestDto response)
        {
            return new K8PayCreditCardRequest
            {
                NomeImpresso = response.Card.CardHolderName,
                DataValidade = $"{response.Card.ExpirationYear}{response.Card.ExpirationMonth}",
                NumeroCartao = response.Card.CardNumber,
                Valor = response.Amount,
                CartaoFormaPagamento = 0,
                QuantidadeParcelas = 0,
                ClienteDescricao = $"{response.Customer.Name} - {response.Customer.Document}",
                ClienteIP = "127.0.0.1",
                ClienteCPFCNPJ = response.Customer.Document,
                ClienteNome = response.Customer.Name,
                ClienteEmail = response.Customer.Email,
                ClienteDDD = response.Customer.PhoneNumber.Substring(0, 2),
                ClienteNumeroCelular = response.Customer.PhoneNumber.Substring(2),
                ClienteEndereco = response.Customer.BillingAddress.Street,
                ClienteComplemento = response.Customer.BillingAddress.Complement,
                ClienteNumero = response.Customer.BillingAddress.Number,
                ClienteBairro = response.Customer.BillingAddress.District,
                ClienteCidade = response.Customer.BillingAddress.City,
                ClienteCEP = response.Customer.BillingAddress.PostalCode,
                ClienteUF = response.Customer.BillingAddress.State,
                NumeroPedido = response.OrderId,
                Recorrencia = false
            };
        }
    }
}
