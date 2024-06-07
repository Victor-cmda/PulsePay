using Domain.Entities.GetNet.Pix;

namespace Domain.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponse> GeneratePixPayment(decimal amount, string currency, string orderId, string customerId);
    }
}
