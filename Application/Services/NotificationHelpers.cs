using Application.DTOs;
using Domain.Models;

namespace Application.Services
{
    public static class NotificationHelpers
    {
        public static NotificationDto CreatePaymentNotification(Transaction transaction, string status, string description = null)
        {
            var notification = new NotificationDto
            {
                TransactionId = transaction.TransactionId,
                PaymentId = transaction.PaymentId,
                PaymentType = "PAYMENT",
                CustomerId = transaction.CustomerId,
                Status = status,
                Description = description ?? $"Payment {status}",
                TransactionTimestamp = DateTime.UtcNow,
                OrderId = transaction.OrderId,
                Amount = transaction.Amount
            };

            switch (status.ToUpper())
            {
                case "COMPLETED":
                case "APPROVED":
                case "PAID":
                    notification.CompletedAt = DateTime.UtcNow;
                    break;
                case "FAILED":
                    notification.FailedAt = DateTime.UtcNow;
                    notification.FailReason = "Transaction failed";
                    break;
            }

            return notification;
        }

        public static NotificationDto CreatePayoutNotification(CustomerPayout payout)
        {
            var status = payout.Status.ToString();

            var notification = new NotificationDto
            {
                TransactionId = payout.ValidationId,
                PaymentId = payout.Id.ToString(),
                PaymentType = "PAYOUT",
                CustomerId = payout.SellerId.ToString(),
                Status = status,
                Description = payout.Description ?? $"Payout {status}",
                TransactionTimestamp = DateTime.UtcNow,
                OrderId = payout.PaymentId ?? payout.Id.ToString(),
                Amount = payout.Amount
            };

            switch (status.ToUpper())
            {
                case "COMPLETED":
                    notification.CompletedAt = payout.ProcessedAt ?? DateTime.UtcNow;
                    break;
                case "FAILED":
                    notification.FailedAt = payout.ProcessedAt ?? DateTime.UtcNow;
                    notification.FailReason = payout.RejectionReason;
                    break;
                case "REJECTED":
                    notification.FailedAt = payout.ProcessedAt ?? DateTime.UtcNow;
                    notification.FailReason = payout.RejectionReason;
                    break;
            }

            return notification;
        }

        public static NotificationDto CreateWithdrawNotification(Withdraw withdraw)
        {
            var status = withdraw.Status.ToString();

            var notification = new NotificationDto
            {
                TransactionId = withdraw.Id.ToString(),
                PaymentId = withdraw.Id.ToString(),
                PaymentType = "WITHDRAW",
                CustomerId = withdraw.SellerId.ToString(),
                Status = status,
                Description = $"Withdrawal {status}",
                TransactionTimestamp = DateTime.UtcNow,
                OrderId = withdraw.Id.ToString(),
                Amount = withdraw.Amount
            };

            switch (status.ToUpper())
            {
                case "COMPLETED":
                    notification.CompletedAt = withdraw.ProcessedAt ?? DateTime.UtcNow;
                    break;
                case "FAILED":
                case "REJECTED":
                    notification.FailedAt = withdraw.ProcessedAt ?? DateTime.UtcNow;
                    notification.FailReason = withdraw.RejectionReason;
                    break;
            }

            return notification;
        }
    }
}