using Application.DTOs.BankSlip;
using Application.DTOs.CreditCard.Payment;
using Application.DTOs.Pix;
using Application.Interfaces;
using Domain.Entities.GetNet.CreditCard.Payment;
using Domain.Entities.GetNet.CreditCard.Token;
using Domain.Entities.GetNet.CreditCard.Verification;
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

        #region private methods

        private void ConfigureHttpClientHeaders(string authToken)
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            _httpClient.DefaultRequestHeaders.Add("seller_id", _sellerId);
        }

        private async Task<GetNetCreditCardTokenResponse> GenerateCardTokenAsync(PaymentCreditCardRequestDto paymentRequest)
        {
            var cardTokenRequest = new GetNetCreditCardTokenRequest
            {
                card_number = paymentRequest.Card.CardNumber,
                customer_id = paymentRequest.Customer.Id
            };

            var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "tokens/card", cardTokenRequest);
            string jsonResponseString;
            using (var responseStream = await response.Content.ReadAsStreamAsync())
            using (var decompressedStream = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress))
            using (var reader = new StreamReader(decompressedStream))
            {
                jsonResponseString = await reader.ReadToEndAsync();
            }

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erro na resposta: {jsonResponseString}");
                throw new Exception("Falha ao gerar token do Cartão de Crédito.");
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<GetNetCreditCardTokenResponse>(jsonResponseString);
        }

        private async Task VerifyCardAsync(PaymentCreditCardRequestDto paymentRequest, string numberToken)
        {
            var cardVerificationRequest = new GetNetCreditCardVerificationRequest
            {
                number_token = numberToken,
                cardholder_name = paymentRequest.Card.CardHolderName,
                brand = paymentRequest.Card.CardBrand,
                expiration_month = paymentRequest.Card.ExpirationMonth,
                expiration_year = paymentRequest.Card.ExpirationYear,
                security_code = paymentRequest.Card.SecurityCode.ToString()
            };

            var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "cards/verification", cardVerificationRequest);
            var jsonResponseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erro na resposta: {jsonResponseString}");
                throw new Exception("Falha ao verificar Cartão de Crédito.");
            }
        }

        private string GenerateSessionId(string establishmentCode, string orderId)
        {
            return $"{establishmentCode}{orderId}";
        }

        #endregion

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
        public async Task<PaymentCreditCardResponseDto> ProcessCreditCardPayment(PaymentCreditCardRequestDto paymentRequest, Guid sellerId, string authToken)
        {
            ConfigureHttpClientHeaders(authToken);

            var sessionId = GenerateSessionId(sellerId.ToString(), paymentRequest.Order.Id); 
            string captureUrl = $"https://h.online-metrix.net/fp/tags.js?org_id=1snn5n9w&session_id={sessionId}";

            var cardTokenResponse = await GenerateCardTokenAsync(paymentRequest);
            await VerifyCardAsync(paymentRequest, cardTokenResponse.number_token);

            var requestMapped = _responseMapperFactory.CreateMapper<PaymentCreditCardRequestDto, GetNetCreditCardRequest>().Map(paymentRequest);

            requestMapped.credit.card.number_token = cardTokenResponse.number_token;
            requestMapped.seller_id = sellerId.ToString();

            requestMapped.device = new Device
            {
                device_id = sessionId,
                ip_address = "127.0.0.1"
            };

            var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "payments/credit", requestMapped);
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erro na resposta: {jsonResponseString}");
                throw new Exception("Falha ao processar pagamento com Cartão de Crédito.");
            }

            var paymentResponse = await Task.Run(() => Newtonsoft.Json.JsonConvert.DeserializeObject<GetNetCreditCardResponse>(jsonResponseString));
            var mapper = _responseMapperFactory.CreateMapper<GetNetCreditCardResponse, PaymentCreditCardResponseDto>();
            var result = mapper.Map(paymentResponse);

            var jsonResponseObject = JObject.Parse(jsonResponseString);

            var transaction = new Transaction
            {
                Id = result.Id,
                TransactionId = result.Credit.TransactionId.ToString(),
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
                GatewayType = "GetNet"
            };

            await _transactionService.CreateTransactionAsync(transaction);

            return result;
        }
        
        public async Task<PaymentBankSlipResponseDto> ProcessBankSlipPayment(PaymentBankSlipRequestDto paymentRequest, Guid sellerId, string authToken)
        {
            throw new NotImplementedException();
        }
    }
}
