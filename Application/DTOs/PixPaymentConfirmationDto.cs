namespace Application.DTOs
{
    public class PixPaymentConfirmationDto
    {
        public bool Success { get; set; }
        public string PaymentId { get; set; }
        public string PaymentProofId { get; set; }
        public string ErrorMessage { get; set; }
    }
}
