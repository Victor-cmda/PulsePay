using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Text;

namespace Application.BackgroundService
{


    public class NotificationRetryService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

        public NotificationRetryService(IHttpClientFactory httpClientFactory, IServiceScopeFactory serviceScopeFactory)
        {
            _httpClientFactory = httpClientFactory;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("NotificationRetryService running.");
                await ProcessPendingNotificationsAsync(stoppingToken);
                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task ProcessPendingNotificationsAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

                var pendingNotifications = await notificationRepository.GetPendingNotificationsAsync();

                foreach (var notification in pendingNotifications)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        notification.SendAttempts++;
                        notification.LastAttempt = DateTime.Now;
                        notification.NextAttempt = DateTime.Now.AddMinutes(5);
                        await notificationRepository.UpdateAsync(notification);

                        var client = _httpClientFactory.CreateClient();
                        var notificationPayload = new
                        {
                            paymentId = notification.Transaction.PaymentId,
                            status = notification.Status,
                            amount = notification.Transaction.Amount,
                            paidAt = notification.Transaction.PaidAt,
                            orderId = notification.Transaction.OrderId,
                            id = notification.Transaction.Id
                        };

                        var jsonContent = new StringContent(JsonConvert.SerializeObject(notificationPayload), Encoding.UTF8, "application/json");
                        var response = await client.PostAsync(notification.ClientUrl, jsonContent, stoppingToken);

                        if (response.IsSuccessStatusCode)
                        {
                            notification.SendStatus = "PAID";
                            await notificationRepository.UpdateAsync(notification);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending notification {notification.Id}: {ex.Message}");
                    }
                }
            }
        }
    }
}

