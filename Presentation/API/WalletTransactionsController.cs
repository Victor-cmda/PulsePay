using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Exceptions;

namespace API.Controllers
{
    [Authorize(Policy = "UserPolicy")]
    [ApiController]
    [Route("api/[controller]")]
    public class WalletTransactionsController : ControllerBase
    {
        private readonly IWalletTransactionService _transactionService;
        private readonly ILogger<WalletTransactionsController> _logger;

        public WalletTransactionsController(
            IWalletTransactionService transactionService,
            ILogger<WalletTransactionsController> logger)
        {
            _transactionService = transactionService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
        {
            try
            {
                var transaction = await _transactionService.CreateTransactionAsync(
                    request.WalletId,
                    request.Amount,
                    request.Type,
                    request.Description,
                    request.Reference
                );

                var response = new TransactionResponse
                {
                    Id = transaction.Id,
                    WalletId = transaction.WalletId,
                    Amount = transaction.Amount,
                    Type = transaction.Type,
                    Status = transaction.Status,
                    Description = transaction.Description,
                    Reference = transaction.Reference,
                    CreatedAt = transaction.CreatedAt,
                    ProcessedAt = transaction.ProcessedAt
                };

                return Ok(response);
            }
            catch (Exception ex) when (ex is ValidationException || ex is InsufficientFundsException)
            {
                return BadRequest(new ErrorResponse(ex.Message));
            }
        }

        [HttpGet("{transactionId}")]
        public async Task<IActionResult> GetTransaction(Guid transactionId)
        {
            try
            {
                var transaction = await _transactionService.GetTransactionByIdAsync(transactionId);
                var response = new TransactionResponse
                {
                    Id = transaction.Id,
                    WalletId = transaction.WalletId,
                    Amount = transaction.Amount,
                    Type = transaction.Type,
                    Status = transaction.Status,
                    Description = transaction.Description,
                    Reference = transaction.Reference,
                    CreatedAt = transaction.CreatedAt,
                    ProcessedAt = transaction.ProcessedAt
                };

                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse(ex.Message));
            }
        }

        [HttpGet("wallet/{walletId}/balance")]
        public async Task<IActionResult> GetBalance(Guid walletId)
        {
            var balance = await _transactionService.GetWalletBalanceAsync(walletId);
            var response = new WalletBalanceResponse
            {
                WalletId = walletId,
                CurrentBalance = balance,
                LastUpdated = DateTime.UtcNow
            };
            return Ok(response);
        }

        [HttpGet("wallet/{walletId}/history")]
        public async Task<IActionResult> GetTransactionHistory(
            Guid walletId,
            [FromQuery] GetTransactionHistoryRequest request)
        {
            var transactions = await _transactionService.GetTransactionHistoryAsync(
                walletId,
                request.StartDate,
                request.EndDate
            );

            var transactionResponses = transactions.Select(t => new TransactionResponse
            {
                Id = t.Id,
                WalletId = t.WalletId,
                Amount = t.Amount,
                Type = t.Type,
                Status = t.Status,
                Description = t.Description,
                Reference = t.Reference,
                CreatedAt = t.CreatedAt,
                ProcessedAt = t.ProcessedAt
            }).ToList();

            // Calcular o sumário
            var summary = new TransactionSummary
            {
                TotalCredits = transactions.Where(t => t.Type == TransactionType.Credit).Sum(t => t.Amount),
                TotalDebits = transactions.Where(t => t.Type == TransactionType.Debit).Sum(t => t.Amount),
                TotalTransactions = transactions.Count()
            };
            summary.NetAmount = summary.TotalCredits - summary.TotalDebits;

            // Aplicar paginação
            var paginatedTransactions = transactionResponses
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize);

            var response = new TransactionHistoryResponse
            {
                Transactions = paginatedTransactions,
                Summary = summary,
                Pagination = new PaginationMetadata
                {
                    CurrentPage = request.Page,
                    PageSize = request.PageSize,
                    TotalCount = transactions.Count(),
                    TotalPages = (int)Math.Ceiling(transactions.Count() / (double)request.PageSize)
                }
            };

            return Ok(response);
        }

        [HttpPut("{transactionId}/status")]
        public async Task<IActionResult> UpdateTransactionStatus(
            Guid transactionId,
            [FromBody] UpdateTransactionStatusRequest request)
        {
            try
            {
                var transaction = request.Status switch
                {
                    TransactionStatus.Cancelled => await _transactionService.CancelTransactionAsync(transactionId, request.Reason),
                    TransactionStatus.Completed => await _transactionService.ProcessTransactionAsync(transactionId),
                    _ => throw new ValidationException("Status de transação inválido")
                };

                var response = new TransactionResponse
                {
                    Id = transaction.Id,
                    WalletId = transaction.WalletId,
                    Amount = transaction.Amount,
                    Type = transaction.Type,
                    Status = transaction.Status,
                    Description = transaction.Description,
                    Reference = transaction.Reference,
                    CreatedAt = transaction.CreatedAt,
                    ProcessedAt = transaction.ProcessedAt
                };

                return Ok(response);
            }
            catch (Exception ex) when (ex is ValidationException || ex is NotFoundException)
            {
                return BadRequest(new ErrorResponse(ex.Message));
            }
        }
    }

    public class ErrorResponse
    {
        public string Message { get; set; }

        public ErrorResponse(string message)
        {
            Message = message;
        }
    }
}
