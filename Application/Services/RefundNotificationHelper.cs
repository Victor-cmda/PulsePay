using Application.DTOs;
using Domain.Models;
using System;

namespace Application.Services
{
    public static class RefundNotificationHelper
    {
        public static NotificationDto CreateRefundNotification(Refund refund, string eventType)
        {
            var notification = new NotificationDto
            {
                TransactionId = refund.TransactionId.ToString(),
                PaymentId = refund.Id.ToString(),
                PaymentType = "REFUND",
                CustomerId = refund.SellerId.ToString(),
                Status = refund.Status.ToString(),
                Description = $"Refund {refund.Status} - {refund.Reason}",
                TransactionTimestamp = DateTime.UtcNow,
                OrderId = refund.ExternalReference ?? refund.Id.ToString(),

                RefundId = $"RF-{refund.Id}",
                RefundReason = refund.Reason,
                Amount = refund.Amount
            };

            switch (refund.Status.ToString().ToUpper())
            {
                case "COMPLETED":
                    notification.CompletedAt = DateTime.UtcNow;
                    break;
                case "FAILED":
                    notification.FailedAt = DateTime.UtcNow;
                    notification.FailReason = refund.FailReason;
                    break;
            }

            return notification;
        }
    }
}