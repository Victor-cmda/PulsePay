using Application.DTOs;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("process")]
        [Authorize(Policy = "ClientPolicy")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequestDto paymentRequest)
        {
            try
            {
                var response = await _paymentService.ProcessPayment(
                    paymentRequest.Type,
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
