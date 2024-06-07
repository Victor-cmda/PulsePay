using System.Net.Http.Headers;
using Application.Interfaces;
using Domain.Entities.GetNet.Pix;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Adapters.PaymentGateway
{
    public class GetNetAdapter : IPaymentGateway
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly string _sellerId;

        public GetNetAdapter(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["PaymentApiSettings:GetNet:BaseUrl"];
            _sellerId = configuration["PaymentApiSettings:GetNet:SellerId"];
        }

        public async Task<PaymentResponse> ProcessPayment(decimal amount, string currency, string orderId, string customerId, string authToken)
        {
            var paymentRequest = new PaymentRequest
            {
                amount = amount,
                currency = currency,
                order_id = orderId,
                customer_id = customerId
            };

             ConfigureHttpClientHeaders(authToken);

            var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "payments/qrcode/pix", paymentRequest);
            if (!response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Erro na resposta: " + responseContent);

                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                var responseString = Encoding.UTF8.GetString(responseBytes);
                Console.WriteLine("Erro na resposta: " + responseString);

                throw new Exception("Falha ao processar pagamento PIX.");
            }

            var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
            return new PaymentResponse
            {
                payment_id = paymentResponse.payment_id,
                status = paymentResponse.status,
                description = paymentResponse.description,
                additional_data = paymentResponse.additional_data
            };
        }

        private void ConfigureHttpClientHeaders(string authToken)
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            _httpClient.DefaultRequestHeaders.Add("seller_id", _sellerId);
        }
    }
}
