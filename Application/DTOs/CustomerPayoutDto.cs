using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CustomerPayoutCreateDto
    {
        [Required(ErrorMessage = "O ID de validação é obrigatório")]
        public string ValidationId { get; set; }

        [Required(ErrorMessage = "O valor é obrigatório")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Amount { get; set; }

        [StringLength(500, ErrorMessage = "A descrição não pode exceder 500 caracteres")]
        public string Description { get; set; }
    }

    public class CustomerPayoutResponseDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string PixKey { get; set; }
        public string PixKeyType { get; set; }
        public string ValidationId { get; set; }
        public string PaymentId { get; set; }
        //public string KeyOwnerName { get; set; }
        //public string KeyOwnerDocument { get; set; }
        //public string BankName { get; set; }
    }
}
