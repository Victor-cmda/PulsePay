using Application.Interfaces;
using Domain.Entities.GetNet.Pix;
using Domain.Interfaces;

namespace Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentGatewayFactory _gatewayFactory;
        private readonly IAuthenticationFactory _authFactory;

        public PaymentService(IPaymentGatewayFactory gatewayFactory, IAuthenticationFactory authFactory)
        {
            _gatewayFactory = gatewayFactory;
            _authFactory = authFactory;
        }

        public async Task<PaymentResponse> ProcessPayment(string type, decimal amount, string currency, string orderId, string customerId)
        {
            var authService = _authFactory.CreateAuthentication(type);
            var token = await authService.GetTokenAsync();

            var gateway = _gatewayFactory.CreateGateway(type);
            return await gateway.ProcessPayment(amount, currency, orderId, customerId, token);
        }

    }

}
