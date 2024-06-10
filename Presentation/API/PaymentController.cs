using Application.DTOs;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [Authorize(Policy = "ClientPolicy")]
        [HttpPost("pix")]
        public async Task<IActionResult> GeneratePixPayment([FromBody] PaymentPixRequestDto paymentRequest)
        {
            try
            {
                var response = await _paymentService.GeneratePixPayment(
                    paymentRequest.Amount,
                    paymentRequest.Currency,
                    paymentRequest.OrderId,
                    paymentRequest.CustomerId);

                return Ok(new { Message = "Pagamento processado com sucesso", Details = response });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
