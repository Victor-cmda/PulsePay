using Application.DTOs.BankSlip;
using Application.DTOs.CreditCard.Payment;
using Application.DTOs.Pix;

namespace Application.Interfaces
{
    public interface IPaymentGateway
    {
        Task<PaymentPixResponseDto> ProcessPixPayment(PaymentPixRequestDto paymentRequest, Guid sellerId, string authToken);
        Task<PaymentBankSlipResponseDto> ProcessBankSlipPayment(PaymentBankSlipRequestDto paymentRequest, Guid sellerId, string authToken);
        Task<PaymentCreditCardResponseDto> ProcessCreditCardPayment(PaymentCreditCardRequestDto paymentRequest, Guid sellerId, string authToken);

    }
}
