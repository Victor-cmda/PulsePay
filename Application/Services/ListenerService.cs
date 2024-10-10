using Application.Config;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;

namespace Application.Services
{
    public class ListenerService : IListenerService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly HttpClient _httpClient;
        private readonly PulseAuthApiSettings _userApiSettings;

        public ListenerService(ITransactionRepository transactionRepository, INotificationRepository notificationRepository, IHttpClientFactory httpClientFactory, IOptions<PulseAuthApiSettings> userApiSettings)
        {
            _transactionRepository = transactionRepository;
            _notificationRepository = notificationRepository;
            _httpClient = httpClientFactory.CreateClient("PulseAuthClient");
            _userApiSettings = userApiSettings.Value;
        }

        public async Task<bool> GenerateNotification(NotificationDto notification)
        {
            var transaction = await _transactionRepository.GetByPaymentIdAsync(notification.PaymentId);

            if (transaction == null)
            {
                return false;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, $"{_userApiSettings.BaseUrl}/user/callback/{transaction.SellerId}");

            request.Headers.Add("X-API-KEY", _userApiSettings.TransactionApiKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to retrieve callback");
            }

            var content = await response.Content.ReadAsStringAsync();
            var callback = JsonConvert.DeserializeObject<Callback>(content);

            if (callback == null)
            {
                throw new Exception("Failed to deserialize callback");
            }

            var notificationEntity = new Notification
            {
                TransactionId = transaction.Id,
                Status = notification.Status,
                SendStatus = "PENDING",
                Description = notification.Description,
                CreatedAt = DateTime.UtcNow,
                ClientUrl = callback.Registration,
            };

            await _notificationRepository.AddAsync(notificationEntity);

            transaction.Status = notification.Status;
            transaction.PaidAt = DateTime.UtcNow;
            await _transactionRepository.UpdateAsync(transaction);

            var notificationPayload = new NotificationClientDto
            {
                Id = transaction.Id,
                PaymentId = transaction.PaymentId,
                OrderId = transaction.OrderId,
                TransactionId = transaction.TransactionId,
                Status = notification.Status,
                Amount = transaction.Amount,
                PaidAt = transaction.PaidAt
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(notificationPayload), Encoding.UTF8, "application/json");

            try
            {
                var postResponse = await _httpClient.PostAsync(callback.Registration, jsonContent);

                if (postResponse.IsSuccessStatusCode)
                {
                    notificationEntity.SendStatus = "PAID";
                    await _notificationRepository.UpdateAsync(notificationEntity);
                }

                return true;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Network error while sending notification to client.", ex);
            }
        }
    }
}
