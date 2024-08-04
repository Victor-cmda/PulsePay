using Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Application.Services
{
    public class CieloAuthenticationService : IAuthenticationPaymentApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _merchantId;
        private readonly string _merchantKey;
        private readonly string _baseUrl;
        private readonly IMemoryCache _memoryCache;
        private const string TokenCacheKey = "CieloAuthToken";

        public CieloAuthenticationService(HttpClient httpClient, IConfiguration configuration, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
            _merchantId = configuration["PaymentApiSettings:Cielo:MerchantId"];
            _merchantKey = configuration["PaymentApiSettings:Cielo:MerchantKey"];
            _baseUrl = configuration["PaymentApiSettings:Cielo:AuthBasicUrl"];
        }

        public async Task<string> GetTokenAsync()
        {
            return "";
            if (_memoryCache.TryGetValue(TokenCacheKey, out string cachedToken))
            {
                return cachedToken;
            }

            string credentials = $"{_merchantId}:{_merchantKey}";
            string encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var requestBody = new
            {
                EstablishmentCode = "1006993069",
                MerchantName = "Loja Exemplo Ltda",
                MCC = "5912"
            };

            var jsonRequestBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_baseUrl, content);
            response.EnsureSuccessStatusCode();
            var responseStream = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(responseStream);
            string newToken = tokenData.GetProperty("access_token").GetString();
            _memoryCache.Set(TokenCacheKey, newToken, TimeSpan.FromSeconds(3500));

            return newToken;
        }
    }
}
