using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Common.Responses;
using Shared.Exceptions;
using System.Net;
using System.Security.Claims;

namespace Presentation.API
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly IWalletTransactionService _transactionService;
        private readonly ICustomerPayoutService _customerPayoutService;
        private readonly IBankAccountService _bankAccountService;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(
            IWalletService walletService,
            IWalletTransactionService transactionService,
            ICustomerPayoutService customerPayoutService,
            IBankAccountService bankAccountService,
            ILogger<AdminDashboardController> logger)
        {
            _walletService = walletService;
            _transactionService = transactionService;
            _customerPayoutService = customerPayoutService;
            _bankAccountService = bankAccountService;
            _logger = logger;
        }

        [HttpGet("dashboard/summary")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardSummary()
        {
            try
            {
                var totalWallets = await _walletService.GetTotalWalletCountAsync();
                var totalPending = await _transactionService.GetTotalPendingTransactionsAsync();
                var totalBalance = await _walletService.GetTotalSystemBalanceAsync();
                var totalAccounts = await _bankAccountService.GetTotalAccountsCountAsync();

                var summary = new
                {
                    TotalWallets = totalWallets,
                    TotalPendingTransactions = totalPending,
                    TotalSystemBalance = totalBalance,
                    TotalBankAccounts = totalAccounts,
                    LastUpdated = DateTime.UtcNow
                };

                return Ok(new ApiResponse<object>(summary));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter resumo do painel administrativo");
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpGet("transactions/pending")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<WalletTransactionDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPendingTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var transactions = await _transactionService.GetAllPendingTransactionsAsync(page, pageSize);
                return Ok(new ApiResponse<IEnumerable<WalletTransactionDto>>(transactions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter transações pendentes");
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpPost("transactions/{transactionId:guid}/approve")]
        [ProducesResponseType(typeof(ApiResponse<WalletTransactionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApproveTransaction(Guid transactionId)
        {
            try
            {
                var transaction = await _transactionService.ProcessTransactionAsync(transactionId);
                return Ok(new ApiResponse<WalletTransactionDto>(transaction));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Transação não encontrada {TransactionId}", transactionId);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao aprovar transação {TransactionId}", transactionId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpPost("transactions/{transactionId:guid}/reject")]
        [ProducesResponseType(typeof(ApiResponse<WalletTransactionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RejectTransaction(Guid transactionId, [FromBody] string reason)
        {
            try
            {
                var transaction = await _transactionService.CancelTransactionAsync(transactionId, reason);
                return Ok(new ApiResponse<WalletTransactionDto>(transaction));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Transação não encontrada {TransactionId}", transactionId);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao rejeitar transação {TransactionId}", transactionId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpGet("check-status")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult CheckAdminStatus()
        {
            return Ok(new ApiResponse<object>(new { isAdmin = true, message = "Você tem permissões de administrador." }));
        }

        [HttpGet("bank-accounts/unverified")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<BankAccountResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUnverifiedBankAccounts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var accounts = await _bankAccountService.GetUnverifiedAccountsAsync(page, pageSize);
                return Ok(new ApiResponse<IEnumerable<BankAccountResponseDto>>(accounts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter contas bancárias não verificadas");
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }


        [HttpPost("bank-accounts/{id:guid}/verify")]
        [ProducesResponseType(typeof(ApiResponse<BankAccountResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyBankAccount(Guid id)
        {
            try
            {
                await _bankAccountService.VerifyBankAccountAsync(id);

                var account = await _bankAccountService.GetBankAccountAsync(id);

                // Notificar o usuário (você precisará implementar isso)
                // await _notificationService.SendBankAccountVerifiedNotificationAsync(account.SellerId, id);

                return Ok(new ApiResponse<BankAccountResponseDto>(account));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar conta bancária {BankAccountId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpPost("bank-accounts/{id:guid}/reject")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectBankAccount(Guid id, [FromBody] string reason)
        {
            try
            {
                // Implementar método para rejeitar a conta bancária
                await _bankAccountService.RejectBankAccountAsync(id, reason);

                // Notificar o usuário (você precisará implementar isso)
                // await _notificationService.SendBankAccountRejectedNotificationAsync(id, reason);

                return Ok(new ApiResponse<object>(new { message = $"Conta bancária {id} rejeitada com sucesso" }));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao rejeitar conta bancária {BankAccountId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        [HttpPost("admin/{id:guid}/confirm")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(ApiResponse<CustomerPayoutDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ConfirmPayout(Guid id, [FromBody] PayoutConfirmationWithProofDto confirmationDto)
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Registrar o comprovante (poderia salvar o arquivo em algum storage)
                string proofReference = !string.IsNullOrEmpty(confirmationDto.ProofReference)
                    ? confirmationDto.ProofReference
                : $"MANUAL_{Guid.NewGuid()}";

                var payout = await _customerPayoutService.ConfirmPayoutAsync(id, confirmationDto.Value, proofReference, adminId);
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
    }
}
