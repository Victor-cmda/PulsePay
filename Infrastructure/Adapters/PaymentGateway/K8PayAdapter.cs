using Application.DTOs.BankSlip;
using Application.DTOs.Pix;
using Application.Interfaces;
using Domain.Entities.GetNet.Pix;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Domain.Models;
using Domain.Entities.K8Pay.BankSlip;

namespace Infrastructure.Adapters.PaymentGateway
{
    public class K8PayAdapter : IPaymentGateway
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly string _sellerId;
        private readonly IResponseMapperFactory _responseMapperFactory;
        private readonly ITransactionService _transactionService;

        public K8PayAdapter(HttpClient httpClient, IConfiguration configuration, IResponseMapperFactory responseMapperFactory, ITransactionService transactionService)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["PaymentApiSettings:K8Pay:BaseUrl"];
            _sellerId = configuration["PaymentApiSettings:K8Pay:SellerId"];
            _responseMapperFactory = responseMapperFactory;
            _transactionService = transactionService;
        }

        public async Task<PaymentBankSlipResponseDto> ProcessBankSlipPayment(PaymentBankSlipRequestDto paymentRequest, Guid sellerId, string authToken)
        {
            ConfigureHttpClientHeaders(authToken);

            var requestMapped = _responseMapperFactory.CreateMapper<PaymentBankSlipRequestDto, K8PayBankSlipRequest>().Map(paymentRequest);
            var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "api/CriaTransacaoBoleto", requestMapped);
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erro na resposta: {jsonResponseString}");
                throw new Exception("Falha ao processar pagamento Boleto.");
            }

            var paymentResponse = await Task.Run(() => Newtonsoft.Json.JsonConvert.DeserializeObject<K8PayBankSlipResponse>(jsonResponseString));
            var mapper = _responseMapperFactory.CreateMapper<K8PayBankSlipResponse, PaymentBankSlipResponseDto>();
            var result = mapper.Map(paymentResponse);

            var jsonResponseObject = JObject.Parse(jsonResponseString);

            var transaction = new Transaction
            {
                TransactionId = paymentResponse.Identificador,
                Amount = paymentRequest.Amount,
                PaymentType = "BANKSLIP",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                Details = jsonResponseObject,
                DocumentType = paymentRequest.Customer.DocumentType,
                DocumentCustomer = paymentRequest.Customer.DocumentNumber,
                EmailCustumer = paymentRequest.Customer.Email,
                NameCustumer = paymentRequest.Customer.Email,
                SellerId = sellerId
            };

            await _transactionService.CreateTransactionAsync(transaction);

            return result;
        }

        public async Task<PaymentPixResponseDto> ProcessPixPayment(PaymentPixRequestDto paymentRequest, Guid sellerId, string authToken)
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
