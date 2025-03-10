using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Common.Responses;
using Shared.Exceptions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Presentation.API
{
    [ApiController]
    [Route("api/admin/pix")]
    [Authorize(Policy = "AdminPolicy")]
    public class PixAdminController : ControllerBase
    {
        private readonly ICustomerPayoutService _customerPayoutService;
        private readonly ILogger<PixAdminController> _logger;

        public PixAdminController(
            ICustomerPayoutService customerPayoutService,
            ILogger<PixAdminController> logger)
        {
            _customerPayoutService = customerPayoutService;
            _logger = logger;
        }

        [HttpGet("pending")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CustomerPayoutResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var payments = await _customerPayoutService.GetPendingPayoutsAsync(page, pageSize);
                return Ok(new ApiResponse<IEnumerable<CustomerPayoutResponseDto>>(payments));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter pagamentos pendentes");
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpPost("{payoutId:guid}/confirm")]
        [ProducesResponseType(typeof(ApiResponse<CustomerPayoutResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConfirmPayment(Guid payoutId, [FromBody] PayoutConfirmationDto request)
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var payment = await _customerPayoutService.ConfirmPayoutAsync(payoutId, request.PaymentProofId, adminId);
                return Ok(new ApiResponse<CustomerPayoutResponseDto>(payment));
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
                _logger.LogError(ex, "Erro ao confirmar pagamento PIX {PayoutId}", payoutId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpPost("{payoutId:guid}/reject")]
        [ProducesResponseType(typeof(ApiResponse<CustomerPayoutResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectPayment(Guid payoutId, [FromBody] PayoutRejectionDto request)
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var payment = await _customerPayoutService.RejectPayoutAsync(payoutId, request.RejectionReason, adminId);
                return Ok(new ApiResponse<CustomerPayoutResponseDto>(payment));
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
                _logger.LogError(ex, "Erro ao rejeitar pagamento PIX {PayoutId}", payoutId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }
    }
}