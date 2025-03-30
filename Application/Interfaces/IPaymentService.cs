using Application.DTOs;
using Application.DTOs.BankSlip;
using Application.DTOs.CreditCard;
using Application.DTOs.Pix;

namespace Domain.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentPixResponseDto> GeneratePixPayment(PaymentPixRequestDto paymentRequest, Guid SellerId);
        Task<PaymentBankSlipResponseDto> GenerateBoletoPayment(PaymentBankSlipRequestDto paymentRequest, Guid SellerId);
        Task<PaymentCreditCardResponseDto> GenerateCreditCardPayment(PaymentCreditCardRequestDto paymentRequest, Guid SellerId);
    }
}
