using Domain.Entities.GetNet.Pix;

namespace Application.Interfaces
{
    public interface IPaymentGateway
    {
        Task<PaymentResponse> ProcessPayment(decimal amount, string currency, string orderId, string customerId, string authToken);
    }
}
