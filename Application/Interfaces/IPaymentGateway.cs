using Application.DTOs.BankSlip;
using Application.DTOs.Pix;
using Domain.Entities.GetNet.Pix;
using Domain.Entities.K8Pay.BankSlip;

namespace Application.Interfaces
{
    public interface IPaymentGateway
    {
        Task<PaymentPixResponseDto> ProcessPixPayment(PaymentPixRequestDto paymentRequest, Guid sellerId, string authToken);
        Task<PaymentBankSlipResponseDto> ProcessBankSlipPayment(PaymentBankSlipRequestDto paymentRequest, Guid sellerId, string authToken);

    }
}
