using Application.DTOs.BankSlip;
using Application.DTOs.Pix;
using Domain.Entities.GetNet.Pix;
using Domain.Entities.K8Pay.BankSlip;

namespace Application.Interfaces
{
    public interface IPaymentGateway
    {
        Task<PaymentPixResponseDto> ProcessPixPayment(PaymentPixRequestDto paymentRequest, string authToken);
        Task<PaymentBankSlipResponseDto> ProcessBankSlipPayment(PaymentBankSlipRequestDto paymentRequest, string authToken);

    }
}
