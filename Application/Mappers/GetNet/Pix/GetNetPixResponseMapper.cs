using Application.DTOs.Pix;
using Domain.Entities.GetNet.Pix;

namespace Application.Mappers.GetNet.Pix
{
    public class GetNetPixResponseMapper : IResponseMapper<GetNetPixResponse, PaymentPixResponseDto>
    {
        public PaymentPixResponseDto Map(GetNetPixResponse response)
        {
            return new PaymentPixResponseDto
            {
                PaymentId = response.payment_id,
                Status = response.status,
                Description = response.description,
                TransactionId = response.additional_data.transaction_id,
                QrCode = response.additional_data.qr_code,
                ExpirationQrCode = response.additional_data.creation_date_qrcode,
            };
        }
    }
}
