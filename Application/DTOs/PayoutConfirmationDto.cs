using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class PayoutConfirmationDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Value { get; set; }
    }
}
