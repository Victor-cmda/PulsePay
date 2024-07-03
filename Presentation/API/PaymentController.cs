using Application.DTOs.BankSlip;
using Application.DTOs.CreditCard.Payment;
using Application.DTOs.Pix;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Middleware;
using System;

namespace Presentation.API
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;
        private readonly string _fileBasePath;
        public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger, IConfiguration configuration)
        {
            _paymentService = paymentService;
            _fileBasePath = configuration["FileStorage:BasePath"];
            _logger = logger;
        }

        //[Authorize(Policy = "ClientPolicy")]
        [HttpPost("pix")]
        [ValidateSellerId]
        public async Task<IActionResult> GeneratePixPayment([FromBody] PaymentPixRequestDto paymentRequest)
        {
            try
            {
                if (!Request.Headers.TryGetValue("SellerId", out var sellerIdHeader) || !Guid.TryParse(sellerIdHeader, out Guid sellerId))
                {
                    return BadRequest(new { Error = "Invalid or missing SellerId in header." });
                }

                _logger.LogInformation("Payment request by pix");

                var response = await _paymentService.GeneratePixPayment(paymentRequest, sellerId);

                return Ok(new { Message = "Pagamento processado com sucesso", Details = response });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        //[Authorize(Policy = "ClientPolicy")]
        [HttpPost("boleto")]
        [ValidateSellerId]
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

        [HttpPost("card/credit")]
        [ValidateSellerId]
        public async Task<IActionResult> GenerateCreditCardPayment([FromBody] PaymentCreditCardRequestDto paymentRequest)
        {
            try
            {
                if (!Request.Headers.TryGetValue("SellerId", out var sellerIdHeader) || !Guid.TryParse(sellerIdHeader, out Guid sellerId))
                {
                    return BadRequest(new { Error = "Invalid or missing SellerId in header." });
                }

                _logger.LogInformation("Payment request by bank slip");
                var response = await _paymentService.GenerateCreditCardPayment(paymentRequest, sellerId);

                return Ok(new { Message = "Pagamento processado com sucesso", Details = response });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("boleto/{Id}/pdf")]
        public async Task<IActionResult> GetBoletoFilePdf([FromRoute] string Id)
        {
            try
            {
                var fileName = $"boleto_{Id}.pdf";
                var filePath = Path.Combine(_fileBasePath, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound();
                }

                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                return File(memory, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
