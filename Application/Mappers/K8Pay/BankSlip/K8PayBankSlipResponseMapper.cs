using Application.DTOs.BankSlip;
using Domain.Entities.K8Pay.BankSlip;

namespace Application.Mappers.K8Pay.BankSlip
{
    public class K8PayBankSlipResponseMapper : IResponseMapper<K8PayBankSlipResponse, PaymentBankSlipResponseDto>
    {
        public PaymentBankSlipResponseDto Map(K8PayBankSlipResponse response)
        {
            return new PaymentBankSlipResponseDto
            {
                Id = Guid.NewGuid(),
                OrderId = response.Identificador,
                TypefulLine = response.LinhaDigitavel,
                BarCode = response.CodigoBarras,
                HrefPdf = response.BoletoPDF,
            };
        }
    }
}
