using Application.DTOs.BankSlip;
using Application.DTOs.Pix;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Adapters.PaymentGateway
{
    public class K8PayAdapter : IPaymentGateway
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly string _sellerId;

        public K8PayAdapter(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["PaymentApiSettings:K8Pay:BaseUrl"];
            _sellerId = configuration["PaymentApiSettings:K8Pay:SellerId"];
        }

        public Task<PaymentBankSlipResponseDto> ProcessBankSlipPayment(PaymentBankSlipRequestDto paymentRequest, Guid sellerId, string authToken)
        {
            throw new NotImplementedException();
        }

        public Task<PaymentPixResponseDto> ProcessPixPayment(PaymentPixRequestDto paymentRequest, Guid sellerId, string authToken)
        {
            throw new NotImplementedException();
        }
    }
}
