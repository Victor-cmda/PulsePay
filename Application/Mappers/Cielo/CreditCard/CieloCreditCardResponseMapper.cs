using Application.DTOs.CreditCard;
using Domain.Entities.Cielo.CreditCard;

namespace Application.Mappers.GetNet.CreditCard
{
    public class CieloCreditCardResponseMapper : IResponseMapper<CieloCreditCardResponse, PaymentCreditCardResponseDto>
    {
        public PaymentCreditCardResponseDto Map(CieloCreditCardResponse response)
        {
            return new PaymentCreditCardResponseDto
            {
                Credit = new Credit
                {
                    AuthorizedAt = DateTime.Now,
                    AuthorizationCode = int.Parse(response.AuthorizationCode),
                    Message = response.ReturnMessage,
                    TransactionId = int.Parse(response.Tid),
                },
                OrderId = response.ProofOfSale,
                Status = response.Status.ToString(),
                ReceivedAt = DateTime.Now,
            };
        }
    }
}
