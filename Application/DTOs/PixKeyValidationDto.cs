using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class PixValidationRequestDto
    {
        [Required(ErrorMessage = "A chave PIX é obrigatória")]
        public string PixKey { get; set; }

        [Required(ErrorMessage = "O tipo de chave PIX é obrigatório")]
        public string PixKeyType { get; set; } // CPF, CNPJ, EMAIL, PHONE, RANDOM
    }

    public class PixKeyValidationDto
    {
        public bool IsValid { get; set; }
        public string PixKey { get; set; }
        public string PixKeyType { get; set; }
        public string ValidationId { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    }
}
