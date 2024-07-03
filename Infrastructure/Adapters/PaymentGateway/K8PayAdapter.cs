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
using System.Security.Cryptography;
using System.Text;
using Infrastructure.Services;
using Application.DTOs.CreditCard.Payment;

namespace Infrastructure.Adapters.PaymentGateway
{
    public class K8PayAdapter : IPaymentGateway
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly string _sellerId;
        private readonly string _aesKey;
        private readonly IResponseMapperFactory _responseMapperFactory;
        private readonly ITransactionService _transactionService;
        private readonly FileService _fileService;

        public K8PayAdapter(HttpClient httpClient, IConfiguration configuration, IResponseMapperFactory responseMapperFactory, ITransactionService transactionService, FileService fileService)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["PaymentApiSettings:K8Pay:BaseUrl"];
            _sellerId = configuration["PaymentApiSettings:K8Pay:SellerId"];
            _aesKey = configuration["PaymentApiSettings:K8Pay:AesKey"];
            _responseMapperFactory = responseMapperFactory;
            _transactionService = transactionService;
            _fileService = fileService;
        }

        #region private methods
        private string DecryptAES128(string cipherText, string key)
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }

        private void ConfigureHttpClientHeaders(string authToken)
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            _httpClient.DefaultRequestHeaders.Add("seller_id", _sellerId);
        }

        #endregion

        public async Task<PaymentBankSlipResponseDto> ProcessBankSlipPayment(PaymentBankSlipRequestDto paymentRequest, Guid sellerId, string authToken)
        {
            ConfigureHttpClientHeaders(authToken);

            var requestMapped = _responseMapperFactory.CreateMapper<PaymentBankSlipRequestDto, K8PayBankSlipRequest>().Map(paymentRequest);
            var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl + "CriaTransacaoBoleto", requestMapped);
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erro na resposta: {jsonResponseString}");
                throw new Exception("Falha ao processar pagamento Boleto.");
            }

            var decryptedResponseString = DecryptAES128(jsonResponseString, _aesKey);

            var paymentResponse = await Task.Run(() => Newtonsoft.Json.JsonConvert.DeserializeObject<K8PayBankSlipResponse>(decryptedResponseString));
            var mapper = _responseMapperFactory.CreateMapper<K8PayBankSlipResponse, PaymentBankSlipResponseDto>();

            var jsonResponseObject = JObject.Parse(decryptedResponseString);

            var result = mapper.Map(paymentResponse);

            string base64Boleto = paymentResponse.BoletoPDF.ToString();
            string fileName = $"boleto_{result.Id}.pdf";
            string filePath = await _fileService.SaveBase64AsPdfAsync(base64Boleto, fileName);
            string downloadLink = $"api/payment/boleto/{result.Id}/pdf";
            result.HrefPdf = downloadLink;

            var transaction = new Transaction
            {
                Id = result.Id,
                TransactionId = paymentResponse.Identificador,
                Amount = paymentRequest.Amount,
                PaymentType = "BANKSLIP",
                Status = "Pendente",
                CreatedAt = DateTime.UtcNow,
                Details = jsonResponseObject,
                DocumentType = paymentRequest.Customer.DocumentType,
                DocumentCustomer = paymentRequest.Customer.DocumentNumber,
                EmailCustumer = paymentRequest.Customer.Email,
                NameCustumer = paymentRequest.Customer.Email,
                SellerId = sellerId,
                GatewayType = "K8Pay"
            };

            await _transactionService.CreateTransactionAsync(transaction);

            return result;
        }
        public async Task<PaymentPixResponseDto> ProcessPixPayment(PaymentPixRequestDto paymentRequest, Guid sellerId, string authToken)
        {
            throw new NotImplementedException();
        }

        public Task<PaymentCreditCardResponseDto> ProcessCreditCardPayment(PaymentCreditCardRequestDto paymentRequest, Guid sellerId, string authToken)
        {
            throw new NotImplementedException();
        }
    }
}
