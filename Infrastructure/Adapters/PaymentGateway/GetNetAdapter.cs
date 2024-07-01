using Application.DTOs.BankSlip;
using Application.DTOs.Pix;
using Application.Interfaces;
using Domain.Entities.GetNet.Pix;
using Domain.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Infrastructure.Adapters.PaymentGateway
{
    public class GetNetAdapter : IPaymentGateway
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly string _sellerId;
        private readonly IResponseMapperFactory _responseMapperFactory;
        private readonly ITransactionService _transactionService;


        public GetNetAdapter(HttpClient httpClient, IConfiguration configuration, IResponseMapperFactory responseMapperFactory, ITransactionService transactionService)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["PaymentApiSettings:GetNet:BaseUrl"];
            _sellerId = configuration["PaymentApiSettings:GetNet:SellerId"];
            _responseMapperFactory = responseMapperFactory;
            _transactionService = transactionService;
        }

        public async Task<PaymentPixResponseDto> ProcessPixPayment(PaymentPixRequestDto paymentRequest, Guid sellerId, string authToken)
        {
            ConfigureHttpClientHeaders(authToken);

            var requestMapped = _responseMapperFactory.CreateMapper<PaymentPixRequestDto, GetNetPixRequest>().Map(paymentRequest);
            var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "payments/qrcode/pix", requestMapped);
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erro na resposta: {jsonResponseString}");
                throw new Exception("Falha ao processar pagamento PIX.");
            }

            var paymentResponse = await Task.Run(() => Newtonsoft.Json.JsonConvert.DeserializeObject<GetNetPixResponse>(jsonResponseString));
            var mapper = _responseMapperFactory.CreateMapper<GetNetPixResponse, PaymentPixResponseDto>();
            var result = mapper.Map(paymentResponse);

            var jsonResponseObject = JObject.Parse(jsonResponseString);

            var transaction = new Transaction
            {
                Id = result.Id,
                TransactionId = result.TransactionId,
                Amount = paymentRequest.Amount,
                PaymentType = "PIX",
                Status = result.Status,
                CreatedAt = DateTime.UtcNow,
                Details = jsonResponseObject,
                DocumentType = paymentRequest.DocumentType,
                DocumentCustomer = paymentRequest.Document,
                EmailCustumer = paymentRequest.Email,
                NameCustumer = paymentRequest.Name,
                SellerId = sellerId,
                GatewayType = "GetNet"
            };

            await _transactionService.CreateTransactionAsync(transaction);

            return result;
        }

        public async Task<PaymentBankSlipResponseDto> ProcessBankSlipPayment(PaymentBankSlipRequestDto paymentRequest, Guid sellerId, string authToken)
        {
            throw new NotImplementedException();
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
