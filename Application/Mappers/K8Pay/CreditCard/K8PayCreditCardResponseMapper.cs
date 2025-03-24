using Application.DTOs.CreditCard;
using Domain.Entities.K8Pay.CreditCard;

namespace Application.Mappers.K8Pay
{
    public class K8PayCreditCardResponseMapper : IResponseMapper<K8PayCreditCardResponse, PaymentCreditCardResponseDto>
    {
        public PaymentCreditCardResponseDto Map(K8PayCreditCardResponse response)
        {
            return new PaymentCreditCardResponseDto
            {
                Status = response.Retorno,
                OrderId = response.Identificador,
                ReceivedAt = DateTime.Now,
                Credit = new Credit
                {
                    AuthorizationCode = int.Parse(response.CodigoAutorizacao),
                    AuthorizedAt = DateTime.Now,
                    Message = response.DetalhesErro,
                    TransactionId = int.Parse(response.NsuOperacao)
                }
            };
        }
    }
}
