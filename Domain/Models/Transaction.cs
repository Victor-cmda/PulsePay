using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models
{
    public class Transaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public string NameCustumer { get; set; }

        [Required]
        [StringLength(50)]
        public string EmailCustumer { get; set; }

        [Required]
        [StringLength(10)]
        public string DocumentType { get; set; }

        [Required]
        [StringLength(30)]
        public string DocumentCustomer { get; set; }

        [Required]
        [StringLength(50)]
        public string GatewayType { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentType { get; set; }

        [Required]
        [StringLength(100)]
        public string OrderId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        [Required]
        public Guid SellerId { get; set; }

        [StringLength(20)]
        public string Status { get; set; }

        public string? Description { get; set; }

        public string CustomerId { get; set; }

        public string PaymentId { get; set; }
        public string TransactionId { get; set; }

        public JObject Details { get; set; }
    }
}
