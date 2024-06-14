using Application.DTOs.BankSlip;
using Application.DTOs.Pix;
using Domain.Entities.GetNet.Pix;

namespace Application.Interfaces
{
    public interface IPaymentGateway
    {
        Task<PaymentResponse> ProcessPixPayment(PaymentPixRequestDto paymentRequest, string authToken);
        Task<PaymentResponse> ProcessBankSlipPayment(PaymentBankSlipRequestDto paymentRequest, string authToken);

    }
}
