using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Common.Responses;
using Shared.Exceptions;
using System.Net;
using System.Security.Claims;

namespace Presentation.API
{
    [ApiController]
    [Route("api/customer-payouts")]
    [Authorize]
    public class CustomerPayoutController : ControllerBase
    {
        private readonly ICustomerPayoutService _customerPayoutService;
        private readonly ILogger<CustomerPayoutController> _logger;

        public CustomerPayoutController(
            ICustomerPayoutService customerPayoutService,
            ILogger<CustomerPayoutController> logger)
        {
            _customerPayoutService = customerPayoutService;
            _logger = logger;
        }

        [HttpPost("validate")]
        [Authorize(Policy = "ClientPolicy")]
        [ProducesResponseType(typeof(ApiResponse<PixKeyValidationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ValidatePixKey([FromBody] PixValidationRequestDto request)
        {
            try
            {
                if (!Request.Headers.TryGetValue("SellerId", out var sellerIdHeader) || !Guid.TryParse(sellerIdHeader, out Guid sellerId))
                {
                    return BadRequest(new { Error = "Invalid or missing SellerId in header." });
                }

                var validation = await _customerPayoutService.ValidatePixKeyAsync(request);
                return Ok(new ApiResponse<PixKeyValidationDto>(validation));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar chave PIX");
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpPost("payment")]
        [Authorize(Policy = "ClientPolicy")]
        [ProducesResponseType(typeof(ApiResponse<CustomerPayoutResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePayment([FromBody] CustomerPayoutCreateDto request)
        {
            try
            {
                if (!Request.Headers.TryGetValue("SellerId", out var sellerIdHeader) || !Guid.TryParse(sellerIdHeader, out Guid sellerId))
                {
                    return BadRequest(new { Error = "Invalid or missing SellerId in header." });
                }

                var payment = await _customerPayoutService.CreatePayoutAsync(request, sellerId);
                return CreatedAtAction(nameof(GetPayment), new { id = payment.Id },
                    new ApiResponse<CustomerPayoutResponseDto>(payment));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (ConflictException ex)
            {
                return Conflict(new ApiResponse<object>(HttpStatusCode.Conflict, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar pagamento PIX");
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "ClientPolicy")]
        [ProducesResponseType(typeof(ApiResponse<CustomerPayoutResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPayment(Guid id)
        {
            try
            {
                if (!Request.Headers.TryGetValue("SellerId", out var sellerIdHeader) || !Guid.TryParse(sellerIdHeader, out Guid sellerId))
                {
                    return BadRequest(new { Error = "Invalid or missing SellerId in header." });
                }

                var payment = await _customerPayoutService.GetPayoutAsync(id);
                return Ok(new ApiResponse<CustomerPayoutResponseDto>(payment));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter pagamento PIX {PaymentId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }
    }
}
