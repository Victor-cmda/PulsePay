using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class PayoutConfirmationDto
    {
        [Required]
        public string PaymentProofId { get; set; }
    }

    public class PayoutRejectionDto
    {
        [Required]
        public string RejectionReason { get; set; }
    }
}
