using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class PaymentCallbackDto
    {
        [Required]
        public string TransactionId { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public decimal Amount { get; set; }
    }
}
