using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Common.Responses;
using Shared.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.Net;
using ValidationException = Shared.Exceptions.ValidationException;

namespace Presentation.API
{
    [ApiController]
    [Route("api/deposits")]
    [Authorize]
    public class DepositController : ControllerBase
    {
        private readonly IDepositService _depositService;
        private readonly ILogger<DepositController> _logger;

        public DepositController(
            IDepositService depositService,
            ILogger<DepositController> logger)
        {
            _depositService = depositService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Policy = "UserPolicy")]
        [ProducesResponseType(typeof(ApiResponse<DepositDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateDeposit([FromBody] DepositRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.SellerName) ||
                    string.IsNullOrEmpty(request.SellerEmail) ||
                    string.IsNullOrEmpty(request.SellerDocument) ||
                    string.IsNullOrEmpty(request.SellerDocumentType))
                {
                    return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest,
                        "Informações do vendedor são obrigatórias: Nome, Email, Documento e Tipo de Documento"));
                }

                var deposit = await _depositService.CreateDepositRequestAsync(request);
                return CreatedAtAction(nameof(GetDeposit), new { id = deposit.Id },
                    new ApiResponse<DepositDto>(deposit));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar solicitação de depósito");
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "UserPolicy")]
        [ProducesResponseType(typeof(ApiResponse<DepositDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeposit(Guid id)
        {
            try
            {
                var deposit = await _depositService.GetDepositAsync(id);
                return Ok(new ApiResponse<DepositDto>(deposit));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter depósito {DepositId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpGet("seller/{sellerId:guid}")]
        [Authorize(Policy = "UserPolicy")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<DepositDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDepositsBySellerId(
            Guid sellerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var deposits = await _depositService.GetDepositsBySellerIdAsync(sellerId, page, pageSize);
                return Ok(new ApiResponse<IEnumerable<DepositDto>>(deposits));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter depósitos do vendedor {SellerId}", sellerId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        // Webhook para processamento de callbacks de pagamento
        [HttpPost("callback")]
        [AllowAnonymous] // Este endpoint precisa ser público para receber callbacks
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ProcessCallback([FromBody] PaymentCallbackDto callback)
        {
            try
            {
                _logger.LogInformation("Callback de pagamento recebido: {TransactionId}, Status: {Status}, Valor: {Amount}",
                    callback.TransactionId, callback.Status, callback.Amount);

                await _depositService.ProcessDepositCallbackAsync(
                    callback.TransactionId,
                    callback.Status,
                    callback.Amount);

                return Ok();
            }
            catch (NotFoundException)
            {
                // Não retornamos erro quando a transação não é encontrada
                // Isso pode ser um callback duplicado ou para outro serviço
                return Ok();
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validação falhou para callback: {TransactionId}", callback.TransactionId);
                return Ok(); // Sempre retorne 200 OK para callbacks, mesmo com erro
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar callback: {TransactionId}", callback.TransactionId);
                return Ok(); // Sempre retorne 200 OK para callbacks, mesmo com erro
            }
        }
    }
}
