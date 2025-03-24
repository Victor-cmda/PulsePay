using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public string KeyOwnerName { get; set; }
        public string KeyOwnerDocument { get; set; }
        public string BankName { get; set; }
        public string ValidationId { get; set; }
        public string ErrorMessage { get; set; }
    }
}
