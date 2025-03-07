using System.ComponentModel.DataAnnotations;

namespace Presentation.API.Common.Requests
{
    /// <summary>
    /// Modelo de requisição para operações de fundos (depósitos e saques)
    /// </summary>
    public class FundsOperationRequest
    {
        [Required(ErrorMessage = "O valor é obrigatório")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Amount { get; set; }

        [StringLength(500, ErrorMessage = "A descrição não pode exceder 500 caracteres")]
        public string Description { get; set; }

        [StringLength(100, ErrorMessage = "A referência não pode exceder 100 caracteres")]
        public string Reference { get; set; }
    }
}
