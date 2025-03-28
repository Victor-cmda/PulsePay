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
    [Route("api/admin/refund")]
    [Authorize(Policy = "AdminPolicy")]
    public class AdminRefundController : ControllerBase
    {
        private readonly IRefundService _refundService;
        private readonly ILogger<AdminRefundController> _logger;

        public AdminRefundController(
            IRefundService refundService,
            ILogger<AdminRefundController> logger)
        {
            _refundService = refundService;
            _logger = logger;
        }

        [HttpPost("{id:guid}/approve")]
        [ProducesResponseType(typeof(ApiResponse<RefundResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveRefund(Guid id)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var refund = await _refundService.ApproveRefundAsync(id, adminId);
                return Ok(new ApiResponse<RefundResponseDto>(refund));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Estorno não encontrado {RefundId}", id);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validação falhou ao aprovar estorno {RefundId}", id);
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao aprovar estorno {RefundId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpPost("{id:guid}/reject")]
        [ProducesResponseType(typeof(ApiResponse<RefundResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectRefund(Guid id, [FromBody] string reason)
        {
            try
            {
                // Obter o ID do administrador atual
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var refund = await _refundService.RejectRefundAsync(id, reason, adminId);
                return Ok(new ApiResponse<RefundResponseDto>(refund));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Estorno não encontrado {RefundId}", id);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validação falhou ao rejeitar estorno {RefundId}", id);
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao rejeitar estorno {RefundId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpPost("{id:guid}/complete")]
        [ProducesResponseType(typeof(ApiResponse<RefundResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompleteRefund(Guid id, [FromBody] string transactionReceipt)
        {
            try
            {
                var refund = await _refundService.CompleteRefundAsync(id, transactionReceipt);
                return Ok(new ApiResponse<RefundResponseDto>(refund));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Estorno não encontrado {RefundId}", id);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validação falhou ao completar estorno {RefundId}", id);
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao completar estorno {RefundId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }
    }
}