using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Common.Responses;
using Shared.Enums;
using Shared.Exceptions;
using System.Net;

namespace Presentation.API.Controllers
{
    [Authorize(Policy = "UserPolicy")]
    [ApiController]
    [Route("api/wallet-transactions")]
    [Produces("application/json")]
    public class WalletTransactionsController : ControllerBase
    {
        private readonly IWalletTransactionService _transactionService;
        private readonly ILogger<WalletTransactionsController> _logger;

        public WalletTransactionsController(
            IWalletTransactionService transactionService,
            ILogger<WalletTransactionsController> logger)
        {
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Cria uma nova transação de carteira
        /// </summary>
        /// <param name="request">Dados da transação</param>
        /// <response code="200">Transação criada com sucesso</response>
        /// <response code="400">Dados inválidos ou saldo insuficiente</response>
        /// <response code="404">Carteira não encontrada</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
        {
            try
            {
                _logger.LogInformation("Criando transação para carteira {WalletId}", request.WalletId);

                var transaction = await _transactionService.CreateTransactionAsync(
                    request.WalletId,
                    request.Amount,
                    request.Type,
                    request.Description,
                    request.Reference
                );

                return Ok(new ApiResponse<WalletTransactionDto>(transaction));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validação falhou ao criar transação");
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (InsufficientFundsException ex)
            {
                _logger.LogWarning(ex, "Saldo insuficiente para transação na carteira {WalletId}", request.WalletId);
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada {WalletId}", request.WalletId);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar transação para carteira {WalletId}", request.WalletId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Obtém uma transação pelo ID
        /// </summary>
        /// <param name="transactionId">ID da transação</param>
        /// <response code="200">Transação encontrada</response>
        /// <response code="404">Transação não encontrada</response>
        [HttpGet("{transactionId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTransaction(Guid transactionId)
        {
            try
            {
                _logger.LogInformation("Obtendo transação {TransactionId}", transactionId);

                var transaction = await _transactionService.GetTransactionByIdAsync(transactionId);

                return Ok(new ApiResponse<WalletTransactionDto>(transaction));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Transação não encontrada {TransactionId}", transactionId);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter transação {TransactionId}", transactionId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Obtém o saldo atual da carteira
        /// </summary>
        /// <param name="walletId">ID da carteira</param>
        /// <response code="200">Saldo obtido com sucesso</response>
        /// <response code="404">Carteira não encontrada</response>
        [HttpGet("wallet/{walletId:guid}/balance")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBalance(Guid walletId)
        {
            try
            {
                _logger.LogInformation("Obtendo saldo da carteira {WalletId}", walletId);

                var balance = await _transactionService.GetWalletBalanceAsync(walletId);

                return Ok(new ApiResponse<WalletBalanceDto>(balance));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada {WalletId}", walletId);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter saldo da carteira {WalletId}", walletId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Obtém o histórico de transações de uma carteira
        /// </summary>
        /// <param name="walletId">ID da carteira</param>
        /// <param name="request">Parâmetros para filtragem</param>
        /// <response code="200">Histórico obtido com sucesso</response>
        /// <response code="404">Carteira não encontrada</response>
        [HttpGet("wallet/{walletId:guid}/history")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTransactionHistory(
            Guid walletId,
            [FromQuery] GetTransactionHistoryRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Obtendo histórico de transações para carteira {WalletId} entre {StartDate} e {EndDate}",
                    walletId, request.StartDate, request.EndDate);

                var transactions = await _transactionService.GetTransactionHistoryAsync(
                    walletId,
                    request.StartDate,
                    request.EndDate,
                    request.Type,
                    request.Status,
                    request.Page,
                    request.PageSize
                );

                // Calcular o sumário
                var summary = new TransactionSummaryDto
                {
                    TotalCredits = transactions
                        .Where(t => t.Type == TransactionType.Credit.ToString() || t.Type == TransactionType.Deposit.ToString())
                        .Sum(t => t.Amount),

                    TotalDebits = transactions
                        .Where(t => t.Type == TransactionType.Debit.ToString() || t.Type == TransactionType.Withdraw.ToString())
                        .Sum(t => t.Amount),

                    TotalTransactions = transactions.Count
                };

                summary.NetAmount = summary.TotalCredits - summary.TotalDebits;

                var response = new TransactionHistoryResponseDto
                {
                    Transactions = transactions,
                    Summary = summary,
                    Pagination = new PaginationMetadataDto
                    {
                        CurrentPage = request.Page,
                        PageSize = request.PageSize,
                        TotalCount = transactions.Count,
                        TotalPages = (int)Math.Ceiling(transactions.Count / (double)request.PageSize)
                    }
                };

                return Ok(new ApiResponse<TransactionHistoryResponseDto>(response));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada {WalletId}", walletId);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter histórico de transações para carteira {WalletId}", walletId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Atualiza o status de uma transação
        /// </summary>
        /// <param name="transactionId">ID da transação</param>
        /// <param name="request">Dados para atualização do status</param>
        /// <response code="200">Status atualizado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="404">Transação não encontrada</response>
        [HttpPut("{transactionId:guid}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTransactionStatus(
            Guid transactionId,
            [FromBody] UpdateTransactionStatusRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Atualizando status da transação {TransactionId} para {Status}",
                    transactionId, request.Status);

                WalletTransactionDto transaction = request.Status switch
                {
                    TransactionStatus.Cancelled => await _transactionService.CancelTransactionAsync(transactionId, request.Reason),
                    TransactionStatus.Completed => await _transactionService.ProcessTransactionAsync(transactionId),
                    _ => throw new ValidationException("Status de transação inválido")
                };

                return Ok(new ApiResponse<WalletTransactionDto>(transaction));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validação falhou ao atualizar status da transação {TransactionId}", transactionId);
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Transação não encontrada {TransactionId}", transactionId);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar status da transação {TransactionId}", transactionId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }
    }
}