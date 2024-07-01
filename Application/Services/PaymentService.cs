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

        public async Task<PaymentPixResponseDto> GeneratePixPayment(PaymentPixRequestDto paymentRequest, Guid sellerId)
        {
            var authService = _authFactory.CreateAuthentication("Pix");
            var token = await authService.GetTokenAsync();
            var gateway = _gatewayFactory.CreateGateway("Pix");
            return await gateway.ProcessPixPayment(paymentRequest, sellerId, token);
        }

        public async Task<PaymentBankSlipResponseDto> GenerateBoletoPayment(PaymentBankSlipRequestDto paymentRequest, Guid sellerId)
        {
            var authService = _authFactory.CreateAuthentication("BankSlip");
            var token = await authService.GetTokenAsync();
            var gateway = _gatewayFactory.CreateGateway("BankSlip");
            return await gateway.ProcessBankSlipPayment(paymentRequest, sellerId, token);
        }

    }

}
