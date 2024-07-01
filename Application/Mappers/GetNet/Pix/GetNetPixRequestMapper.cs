using Application.DTOs.Pix;
using Domain.Entities.GetNet.Pix;

namespace Application.Mappers.GetNet.Pix
{
    public class GetNetPixRequestMapper : IResponseMapper<PaymentPixRequestDto, GetNetPixRequest>
    {
        public GetNetPixRequest Map(PaymentPixRequestDto response)
        {
            return new GetNetPixRequest
            {
                amount = response.Amount,
                currency = "BRL",
                customer_id = response.CustomerId,
                order_id = response.OrderId,
            };
        }
    }
}
