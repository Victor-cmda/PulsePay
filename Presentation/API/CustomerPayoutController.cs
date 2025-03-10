using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Common.Responses;
using Shared.Exceptions;
using System.Net;
using System.Security.Claims;
using XAct.Users;

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

        [HttpPost]
        [Authorize(Policy = "UserPolicy")]
        [ProducesResponseType(typeof(ApiResponse<CustomerPayoutDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> RequestPayout([FromBody] CustomerPayoutRequestDto request)
        {
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(sellerId))
                {
                    return Unauthorized(new ApiResponse<object>(HttpStatusCode.Unauthorized, "Usuário não autenticado"));
                }

                // Adicionar o ID do vendedor na requisição
                request.SellerId = Guid.Parse(sellerId);

                var payout = await _customerPayoutService.RequestPayoutAsync(request);
                return CreatedAtAction(nameof(GetPayout), new { id = payout.Id },
                    new ApiResponse<CustomerPayoutDto>(payout));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (InsufficientFundsException ex)
            {
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao solicitar pagamento para cliente");
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "UserPolicy")]
        [ProducesResponseType(typeof(ApiResponse<CustomerPayoutDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPayout(Guid id)
        {
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(sellerId))
                {
                    return Unauthorized(new ApiResponse<object>(HttpStatusCode.Unauthorized, "Usuário não autenticado"));
                }

                var payout = await _customerPayoutService.GetPayoutAsync(id, Guid.Parse(sellerId));
                return Ok(new ApiResponse<CustomerPayoutDto>(payout));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse<object>(HttpStatusCode.Unauthorized, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter pagamento {PayoutId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpGet("seller")]
        [Authorize(Policy = "UserPolicy")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CustomerPayoutDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSellerPayouts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(sellerId))
                {
                    return Unauthorized(new ApiResponse<object>(HttpStatusCode.Unauthorized, "Usuário não autenticado"));
                }

                var payouts = await _customerPayoutService.GetPayoutsBySellerIdAsync(Guid.Parse(sellerId), page, pageSize);
                return Ok(new ApiResponse<IEnumerable<CustomerPayoutDto>>(payouts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter pagamentos do vendedor");
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }


        [HttpGet("admin/pending")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CustomerPayoutDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingPayouts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var payouts = await _customerPayoutService.GetPendingPayoutsAsync(page, pageSize);
                return Ok(new ApiResponse<IEnumerable<CustomerPayoutDto>>(payouts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter pagamentos pendentes");
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpPost("admin/{id:guid}/confirm")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(ApiResponse<CustomerPayoutDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ConfirmPayout(Guid id, [FromBody] PayoutConfirmationDto confirmationDto)
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var payout = await _customerPayoutService.ConfirmPayoutAsync(id, confirmationDto.Value, adminId);
                return Ok(new ApiResponse<CustomerPayoutDto>(payout));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao confirmar pagamento {PayoutId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpPost("admin/{id:guid}/reject")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(ApiResponse<CustomerPayoutDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectPayout(Guid id, [FromBody] string reason)
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var payout = await _customerPayoutService.RejectPayoutAsync(id, reason, adminId);
                return Ok(new ApiResponse<CustomerPayoutDto>(payout));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao rejeitar pagamento {PayoutId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }
    }
}
