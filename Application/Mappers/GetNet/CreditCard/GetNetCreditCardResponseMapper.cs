using Application.DTOs.CreditCard.Payment;
using Domain.Entities.GetNet.CreditCard.Payment;

namespace Application.Mappers.GetNet.CreditCard
{
    public class GetNetCreditCardResponseMapper : IResponseMapper<GetNetCreditCardResponse, PaymentCreditCardResponseDto>
    {
        public PaymentCreditCardResponseDto Map(GetNetCreditCardResponse response)
        {
            return new PaymentCreditCardResponseDto
            {
                Id = Guid.NewGuid(),
                Amount = response.Amount,
                Status = response.Status,
                OrderId = response.OrderId.ToString(),
                Credit = new Credit
                {
                    AuthorizationCode = response.Credit.AuthorizationCode,
                    AuthorizedAt = response.Credit.AuthorizedAt,
                    TransactionId = (int)response.Credit.TransactionId,
                    Message = response.Credit.ReasonMessage
                }
            };
        }
    }
}
