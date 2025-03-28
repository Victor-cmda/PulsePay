using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Common.Responses;
using Presentation.Middleware;
using Shared.Exceptions;
using System.Net;

namespace Presentation.API
{
    [ApiController]
    [Route("api/refund")]
    public class RefundController : ControllerBase
    {
        private readonly IRefundService _refundService;
        private readonly ILogger<RefundController> _logger;

        public RefundController(IRefundService refundService, ILogger<RefundController> logger)
        {
            _refundService = refundService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Policy = "ClientPolicy")]
        [ValidateSellerId]
        [ProducesResponseType(typeof(ApiResponse<RefundResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RequestRefund([FromBody] RefundRequestDto request)
        {
            try
            {
                if (!Request.Headers.TryGetValue("SellerId", out var sellerIdHeader) || !Guid.TryParse(sellerIdHeader, out Guid sellerId))
                {
                    return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, "Invalid or missing SellerId in header."));
                }

                _logger.LogInformation("Solicitação de estorno recebida para transação {TransactionId}", request.TransactionId);

                var refund = await _refundService.RequestRefundAsync(request, sellerId);

                return CreatedAtAction(nameof(GetRefundStatus), new { id = refund.Id },
                    new ApiResponse<RefundResponseDto>(refund));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validação falhou para solicitação de estorno");
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Transação não encontrada");
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar solicitação de estorno");
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "ClientPolicy")]
        [ValidateSellerId]
        [ProducesResponseType(typeof(ApiResponse<RefundResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRefundStatus(Guid id)
        {
            try
            {
                var refund = await _refundService.GetRefundStatusAsync(id);
                return Ok(new ApiResponse<RefundResponseDto>(refund));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Estorno não encontrado {RefundId}", id);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter status do estorno {RefundId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpGet("seller/{sellerId:guid}")]
        [Authorize(Policy = "ClientPolicy")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<RefundResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRefundsBySellerId(
            Guid sellerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var refunds = await _refundService.GetRefundsBySellerIdAsync(sellerId, page, pageSize);
                return Ok(new ApiResponse<IEnumerable<RefundResponseDto>>(refunds));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estornos do vendedor {SellerId}", sellerId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }
    }
}