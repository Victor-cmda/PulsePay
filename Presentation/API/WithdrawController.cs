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
    // Em Presentation/API/WithdrawController.cs

    [ApiController]
    [Route("api/withdraws")]
    [Authorize]
    public class WithdrawController : ControllerBase
    {
        private readonly IWithdrawService _withdrawService;
        private readonly ILogger<WithdrawController> _logger;

        public WithdrawController(
            IWithdrawService withdrawService,
            ILogger<WithdrawController> logger)
        {
            _withdrawService = withdrawService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Policy = "UserPolicy")]
        [ProducesResponseType(typeof(ApiResponse<WithdrawDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> RequestWithdraw([FromBody] WithdrawRequestDto request)
        {
            try
            {
                var withdraw = await _withdrawService.RequestWithdrawAsync(request);
                return CreatedAtAction(nameof(GetWithdraw), new { id = withdraw.Id },
                    new ApiResponse<WithdrawDto>(withdraw));
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
                _logger.LogError(ex, "Erro ao solicitar saque");
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "UserPolicy")]
        [ProducesResponseType(typeof(ApiResponse<WithdrawDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWithdraw(Guid id)
        {
            try
            {
                var withdraw = await _withdrawService.GetWithdrawAsync(id);
                return Ok(new ApiResponse<WithdrawDto>(withdraw));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter saque {WithdrawId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpGet("seller/{sellerId:guid}")]
        [Authorize(Policy = "UserPolicy")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<WithdrawDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWithdrawsBySellerId(
            Guid sellerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var withdraws = await _withdrawService.GetWithdrawsBySellerIdAsync(sellerId, page, pageSize);
                return Ok(new ApiResponse<IEnumerable<WithdrawDto>>(withdraws));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter saques do vendedor {SellerId}", sellerId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        // Endpoints para administradores

        [HttpGet("pending")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<WithdrawDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingWithdraws(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var withdraws = await _withdrawService.GetPendingWithdrawsAsync(page, pageSize);
                return Ok(new ApiResponse<IEnumerable<WithdrawDto>>(withdraws));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter saques pendentes");
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpPost("{id:guid}/approve")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(ApiResponse<WithdrawDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ApproveWithdraw(Guid id)
        {
            try
            {
                // Obter o ID do administrador atual
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var withdraw = await _withdrawService.ApproveWithdrawAsync(id, adminId);
                return Ok(new ApiResponse<WithdrawDto>(withdraw));
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
                _logger.LogError(ex, "Erro ao aprovar saque {WithdrawId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpPost("{id:guid}/reject")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(ApiResponse<WithdrawDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectWithdraw(Guid id, [FromBody] string reason)
        {
            try
            {
                // Obter o ID do administrador atual
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var withdraw = await _withdrawService.RejectWithdrawAsync(id, reason, adminId);
                return Ok(new ApiResponse<WithdrawDto>(withdraw));
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
                _logger.LogError(ex, "Erro ao rejeitar saque {WithdrawId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpPost("{id:guid}/process")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(ApiResponse<WithdrawDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ProcessWithdraw(Guid id, [FromBody] string transactionReceipt)
        {
            try
            {
                var withdraw = await _withdrawService.ProcessWithdrawAsync(id, transactionReceipt);
                return Ok(new ApiResponse<WithdrawDto>(withdraw));
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
                _logger.LogError(ex, "Erro ao processar saque {WithdrawId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError,
                    "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }
    }
}
