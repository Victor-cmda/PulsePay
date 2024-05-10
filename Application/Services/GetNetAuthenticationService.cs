using Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Application.Services
{
    public class GetNetAuthenticationService : IAuthenticationPaymentApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _baseUrl;
        private readonly IMemoryCache _memoryCache;
        private const string TokenCacheKey = "GetNetAuthToken";

        public GetNetAuthenticationService(HttpClient httpClient, IConfiguration configuration, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
            _clientId = configuration["PaymentApiSettings:GetNet:ClientId"];
            _clientSecret = configuration["PaymentApiSettings:GetNet:ClientSecret"];
            _baseUrl = configuration["PaymentApiSettings:GetNet:AuthBasicUrl"];
        }

        public async Task<string> GetTokenAsync()
        {
            if (_memoryCache.TryGetValue(TokenCacheKey, out string cachedToken))
            {
                return cachedToken;
            }

            string credentials = $"{_clientId}:{_clientSecret}";
            string encodedCredentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("scope", "oob"),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.PostAsync(_baseUrl, requestBody);
            response.EnsureSuccessStatusCode();
            var responseStream = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(responseStream);
            string newToken = tokenData.GetProperty("access_token").GetString();
            _memoryCache.Set(TokenCacheKey, newToken, TimeSpan.FromSeconds(3600));

            return newToken;
        }
    }
}
