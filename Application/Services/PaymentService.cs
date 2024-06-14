using Application.DTOs.BankSlip;
using Application.DTOs.Pix;
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

        public async Task<PaymentResponse> GeneratePixPayment(PaymentPixRequestDto paymentRequest)
        {
            var authService = _authFactory.CreateAuthentication();
            var token = await authService.GetTokenAsync();

            var gateway = _gatewayFactory.CreateGateway();
            return await gateway.ProcessPixPayment(paymentRequest, token);
        }

        public async Task<PaymentResponse> GenerateBoletoPayment(PaymentBankSlipRequestDto paymentRequest)
        {
            var authService = _authFactory.CreateAuthentication();
            var token = await authService.GetTokenAsync();

            var gateway = _gatewayFactory.CreateGateway();
            return await gateway.ProcessBankSlipPayment(paymentRequest, token);
        }

    }

}
