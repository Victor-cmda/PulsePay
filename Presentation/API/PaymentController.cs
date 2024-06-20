using Application.DTOs.BankSlip;
using Application.DTOs.Pix;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Middleware;

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

        //[Authorize(Policy = "ClientPolicy")]
        [HttpPost("pix")]
        [ValidateSellerId]
        public async Task<IActionResult> GeneratePixPayment([FromBody] PaymentPixRequestDto paymentRequest, Guid sellerId)
        {
            try
            {
                _logger.LogInformation("Payment request by pix");

                var response = await _paymentService.GeneratePixPayment(paymentRequest, sellerId);

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
                if (!Request.Headers.TryGetValue("SellerId", out var sellerIdHeader) || !Guid.TryParse(sellerIdHeader, out Guid sellerId))
                {
                    return BadRequest(new { Error = "Invalid or missing SellerId in header." });
                }

                _logger.LogInformation("Payment request by bank slip");
                var response = await _paymentService.GenerateBoletoPayment(paymentRequest, sellerId);

                return Ok(new { Message = "Pagamento processado com sucesso", Details = response });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
