using Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Application.Services
{
    public class K8PayAuthenticationService : IAuthenticationPaymentApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _baseUrl;
        private readonly IMemoryCache _memoryCache;
        private const string TokenCacheKey = "K8PayAuthToken";

        public K8PayAuthenticationService(HttpClient httpClient, IConfiguration configuration, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
            _clientId = configuration["PaymentApiSettings:K8Pay:ClientId"];
            _clientSecret = configuration["PaymentApiSettings:K8Pay:ClientSecret"];
            _baseUrl = configuration["PaymentApiSettings:K8Pay:AuthBasicUrl"];
        }

        public async Task<string> GetTokenAsync()
        {
            if (_memoryCache.TryGetValue(TokenCacheKey, out string cachedToken))
            {
                return cachedToken;
            }

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", _clientId),
                new KeyValuePair<string, string>("password", _clientSecret),
                new KeyValuePair<string, string>("grant_type", "password")
            });

            var response = await _httpClient.PostAsync(_baseUrl, requestBody);
            response.EnsureSuccessStatusCode();
            var responseStream = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(responseStream);
            string newToken = tokenData.GetProperty("access_token").GetString();
            _memoryCache.Set(TokenCacheKey, newToken, TimeSpan.FromSeconds(3500));

            return newToken;
        }

    }
}
