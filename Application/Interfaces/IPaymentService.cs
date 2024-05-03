using Domain.Entities.GetNet.Pix;

namespace Domain.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponse> ProcessPayment(string type, decimal amount, string currency, string orderId, string customerId);
    }
}
