using Application.DTOs.BankSlip;
using Application.DTOs.Pix;
using Domain.Entities.GetNet.Pix;

namespace Domain.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponse> GeneratePixPayment(PaymentPixRequestDto paymentRequest);
        Task<PaymentResponse> GenerateBoletoPayment(PaymentBankSlipRequestDto paymentRequest);

    }
}
