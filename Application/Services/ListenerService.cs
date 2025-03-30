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

            if (IsCompletedStatus(notification.Status))
            {
                transaction.PaidAt = DateTime.UtcNow;
            }

            await _transactionRepository.UpdateAsync(transaction);

            string clientNotificationStatus = MapStatusToClientStatus(notification.Status, notification.PaymentType);

            var notificationPayload = new NotificationClientDto
            {
                Id = transaction.Id,
                PaymentId = transaction.PaymentId,
                OrderId = transaction.OrderId,
                TransactionId = transaction.TransactionId,
                Status = clientNotificationStatus,
                Amount = transaction.Amount,
                PaidAt = transaction.PaidAt,
                Type = notification.PaymentType?.ToLower() ?? "payment"
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

        private bool IsCompletedStatus(string status)
        {
            return status.Equals("APPROVED", StringComparison.OrdinalIgnoreCase) ||
                   status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase) ||
                   status.Equals("PAID", StringComparison.OrdinalIgnoreCase);
        }

        private string MapStatusToClientStatus(string status, string paymentType)
        {
            string type = (paymentType ?? "PAYMENT").ToUpper();

            switch (type)
            {
                case "PAYMENT":
                    return status.ToUpper() switch
                    {
                        "PENDING" => "PENDING",
                        "APPROVED" => "PAID",
                        "COMPLETED" => "PAID",
                        "RECEIVED" => "RECEIVED",
                        "CREATED" => "CREATED",
                        "FAILED" => "FAILED",
                        "CANCELLED" => "CANCELLED",
                        _ => "UNKNOWN"
                    };

                case "PAYOUT":
                    return status.ToUpper() switch
                    {
                        "PENDING" => "PENDING",
                        "APPROVED" => "PROCESSING",
                        "COMPLETED" => "COMPLETED",
                        "REJECTED" => "REJECTED",
                        "FAILED" => "FAILED",
                        _ => "UNKNOWN"
                    };

                case "REFUND":
                    return status.ToUpper() switch
                    {
                        "PENDING" => "PENDING",
                        "PROCESSING" => "PROCESSING",
                        "COMPLETED" => "COMPLETED",
                        "FAILED" => "FAILED",
                        _ => "UNKNOWN"
                    };

                case "WITHDRAW":
                    return status.ToUpper() switch
                    {
                        "PENDING" => "PENDING",
                        "APPROVED" => "APPROVED",
                        "PROCESSING" => "PROCESSING",
                        "COMPLETED" => "COMPLETED",
                        "FAILED" => "FAILED",
                        "REJECTED" => "REJECTED",
                        _ => "UNKNOWN"
                    };

                default:
                    return status.ToUpper() switch
                    {
                        "PENDING" => "PENDING",
                        "APPROVED" => "PAID",
                        "COMPLETED" => "COMPLETED",
                        "FAILED" => "FAILED",
                        _ => "UNKNOWN"
                    };
            }
        }
    }
}