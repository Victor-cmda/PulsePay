using Application.DTOs.BankSlip;
using Application.DTOs.Pix;
using Domain.Entities.GetNet.Pix;

namespace Domain.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentPixResponseDto> GeneratePixPayment(PaymentPixRequestDto paymentRequest);
        Task<PaymentBankSlipResponseDto> GenerateBoletoPayment(PaymentBankSlipRequestDto paymentRequest);

    }
}
