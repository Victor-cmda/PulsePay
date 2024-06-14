using Application.DTOs.BankSlip;
using Application.DTOs.Pix;
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
        private readonly ILogger<PaymentsController> _logger;
        public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [Authorize(Policy = "ClientPolicy")]
        [HttpPost("pix")]
        public async Task<IActionResult> GeneratePixPayment([FromBody] PaymentPixRequestDto paymentRequest)
        {
            try
            {
                _logger.LogInformation("Pix Payment request log");
                var response = await _paymentService.GeneratePixPayment(paymentRequest);

                return Ok(new { Message = "Pagamento processado com sucesso", Details = response });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [Authorize(Policy = "ClientPolicy")]
        [HttpPost("boleto")]
        public async Task<IActionResult> GenerateBoletoPayment([FromBody] PaymentBankSlipRequestDto paymentRequest)
        {
            try
            {
                var response = await _paymentService.GenerateBoletoPayment(paymentRequest);

                return Ok(new { Message = "Pagamento processado com sucesso", Details = response });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
