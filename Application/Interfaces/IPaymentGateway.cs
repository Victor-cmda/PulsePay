using Application.DTOs.BankSlip;
using Domain.Entities.GetNet.Pix;

namespace Application.Interfaces
{
    public interface IPaymentGateway
    {
        Task<PaymentResponse> ProcessPixPayment(decimal amount, string currency, string orderId, string customerId, string authToken);
        Task<PaymentResponse> ProcessBankSlipPayment(PaymentBankSlipRequestDto paymentRequest, string authToken);

    }
}
