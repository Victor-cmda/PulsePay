using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class PayoutConfirmationWithProofDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Value { get; set; }

        [Required(ErrorMessage = "É necessário fornecer uma referência do comprovante")]
        public string ProofReference { get; set; }

        public string Notes { get; set; }
    }
}
