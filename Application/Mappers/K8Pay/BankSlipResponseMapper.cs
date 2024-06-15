using Application.DTOs.BankSlip;
using Domain.Entities.K8Pay.BankSlip;

namespace Application.Mappers.K8Pay
{
    public class K8PayBankSlipResponseMapper : IResponseMapper<Domain.Entities.K8Pay.BankSlip.K8PayBankSlipResponse, PaymentBankSlipResponseDto>
    {
        public PaymentBankSlipResponseDto Map(Domain.Entities.K8Pay.BankSlip.K8PayBankSlipResponse response)
        {
            return new PaymentBankSlipResponseDto
            {
                Status = response.Retorno,
                ErrorMessage = response.DetalhesErro,
                Identifier = response.Identificador,
                OurNumber = response.NossoNumero,
                DigitableLine = response.LinhaDigitavel,
                Barcode = response.CodigoBarras,
                Pdf = response.BoletoPDF,
                ErrorCode = response.CodigoErro
            };
        }
    }
}
