using Application.DTOs.BankSlip;
using Application.DTOs.CreditCard;
using Application.DTOs.Pix;
using Application.Interfaces;
using Domain.Entities.Cielo.CreditCard;
using Domain.Entities.GetNet.CreditCard.Payment;
using Domain.Entities.GetNet.CreditCard.Token;
using Domain.Entities.GetNet.CreditCard.Verification;
using Domain.Entities.GetNet.Pix;
using Domain.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.Adapters.PaymentGateway
{
    public class CieloAdapter : IPaymentGateway
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly string _sellerId;
        private readonly IResponseMapperFactory _responseMapperFactory;
        private readonly ITransactionService _transactionService;


        public CieloAdapter(HttpClient httpClient, IConfiguration configuration, IResponseMapperFactory responseMapperFactory, ITransactionService transactionService)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["PaymentApiSettings:Cielo:BaseUrl"];
            _sellerId = configuration["PaymentApiSettings:Cielo:SellerId"];
            _responseMapperFactory = responseMapperFactory;
            _transactionService = transactionService;
        }

        #region private methods
        private void ConfigureHttpClientHeaders(string authToken)
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
            _httpClient.DefaultRequestHeaders.Add("MerchantId", "c03d5f2c-b4d7-402f-8c64-e7a3402e8a04");
            _httpClient.DefaultRequestHeaders.Add("MerchantKey", "ROCFVAVZHMKVMSWFFEXVNSBCXDGKTMKVDROTIJIE");
            //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        }
        #endregion


        public async Task<PaymentCreditCardResponseDto> ProcessCreditCardPayment(PaymentCreditCardRequestDto paymentRequest, Guid sellerId, string authToken)
        {
            ConfigureHttpClientHeaders(authToken);

            var requestMapped = _responseMapperFactory.CreateMapper<PaymentCreditCardRequestDto, CieloCreditCardRequest>().Map(paymentRequest);
            var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "1/sales", requestMapped);
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erro na resposta: {jsonResponseString}");
                throw new Exception("Falha ao processar pagamento Cartão de crédito.");
            }

            var paymentResponse = await Task.Run(() => Newtonsoft.Json.JsonConvert.DeserializeObject<CieloCreditCardResponse>(jsonResponseString));
            var mapper = _responseMapperFactory.CreateMapper<CieloCreditCardResponse, PaymentCreditCardResponseDto>();
            var result = mapper.Map(paymentResponse);

            var jsonResponseObject = JObject.Parse(jsonResponseString);

            var transaction = new Transaction
            {
                Id = result.Id,
                TransactionId = result.Id.ToString(),
                Amount = paymentRequest.Amount,
                PaymentType = "CREDITCARD",
                Status = result.Status,
                CreatedAt = DateTime.UtcNow,
                Details = jsonResponseObject,
                DocumentType = paymentRequest.Customer.DocumentType,
                DocumentCustomer = paymentRequest.Customer.Document,
                EmailCustumer = paymentRequest.Customer.Email,
                NameCustumer = paymentRequest.Customer.Name,
                SellerId = sellerId,
                GatewayType = "Cielo"
            };

            await _transactionService.CreateTransactionAsync(transaction);

            return result;
        }

        public async Task<PaymentPixResponseDto> ProcessPixPayment(PaymentPixRequestDto paymentRequest, Guid sellerId, string authToken)
        {
            throw new NotImplementedException();
        }

        public async Task<PaymentBankSlipResponseDto> ProcessBankSlipPayment(PaymentBankSlipRequestDto paymentRequest, Guid sellerId, string authToken)
        {
            throw new NotImplementedException();
        }
    }
}
