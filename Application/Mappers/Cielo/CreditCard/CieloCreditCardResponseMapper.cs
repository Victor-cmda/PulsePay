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
                Id = Guid.NewGuid(), 
                Amount = response.Payment.Amount / 100,
                Status = GetStatusDescription(response.Payment.Status), 
                OrderId = response.MerchantOrderId,
                ReceivedAt = DateTime.Parse(response.Payment.ReceivedDate),
                Credit = new Credit
                {
                    AuthorizationCode = 1,
                    AuthorizedAt = DateTime.Now,
                    TransactionId = 1,
                    Message = response.Payment.ReturnMessage
                }
            };
        }

        private string GetStatusDescription(byte status)
        {
            return status switch
            {
                0 => "NotFinished",
                1 => "Authorized",
                2 => "PaymentConfirmed",
                3 => "Denied",
                10 => "Voided",
                11 => "Refunded",
                12 => "Pending",
                13 => "Aborted",
                20 => "Scheduled",
                _ => "Unknown"
            };
        }
    }
}
