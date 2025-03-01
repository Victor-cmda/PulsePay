using Application.DTOs;
using Application.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.API
{
    [ApiController]
    [Route("api/bank")]
    [Authorize(Policy = "UserPolicy")]
    public class BankAccountController : ControllerBase
    {
        private readonly IBankAccountService _bankAccountService;
        private readonly ILogger<BankAccountController> _logger;

        public BankAccountController(
            IBankAccountService bankAccountService,
            ILogger<BankAccountController> logger)
        {
            _bankAccountService = bankAccountService;
            _logger = logger;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BankAccountResponseDto>> GetBankAccount(Guid id, CancellationToken cancellationToken = default)
        {
            var bankAccount = await _bankAccountService.GetBankAccountAsync(id, cancellationToken);

            if (!await IsBankAccountOwnerOrAdmin(bankAccount.SellerId))
            {
                return Forbid();
            }

            return Ok(bankAccount);
        }

        [HttpGet("seller/{sellerId:guid}")]
        [ProducesResponseType(typeof(IEnumerable<BankAccountResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<BankAccountResponseDto>>> GetSellerBankAccounts(Guid sellerId, CancellationToken cancellationToken = default)
        {
            if (!await IsBankAccountOwnerOrAdmin(sellerId))
            {
                return Forbid();
            }

            var bankAccounts = await _bankAccountService.GetSellerBankAccountsAsync(sellerId, cancellationToken);
            return Ok(bankAccounts);
        }

        [HttpPost]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BankAccountResponseDto>> CreateBankAccount([FromBody] BankAccountCreateDto createDto, CancellationToken cancellationToken = default)
        {
            if (!await IsBankAccountOwnerOrAdmin(createDto.SellerId))
            {
                return Forbid();
            }

            var bankAccount = await _bankAccountService.CreateBankAccountAsync(createDto, cancellationToken);

            return Ok(bankAccount);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BankAccountResponseDto>> UpdateBankAccount(Guid id, [FromBody] BankAccountUpdateDto updateDto, CancellationToken cancellationToken = default)
        {
            var bankAccount = await _bankAccountService.GetBankAccountAsync(id, cancellationToken);

            if (!await IsBankAccountOwnerOrAdmin(bankAccount.SellerId))
            {
                return Forbid();
            }

            var updatedBankAccount = await _bankAccountService.UpdateBankAccountAsync(id, updateDto, cancellationToken);
            return Ok(updatedBankAccount);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteBankAccount(Guid id, CancellationToken cancellationToken = default)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _bankAccountService.DeleteBankAccountAsync(id, currentUserId, cancellationToken);

            return NoContent();
        }

        [HttpPost("{id:guid}/verify")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyBankAccount(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await _bankAccountService.VerifyBankAccountAsync(id, cancellationToken);
            return Ok(new { verified = result });
        }

        [HttpPost("validate")]
        [ProducesResponseType(typeof(BankAccountValidationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BankAccountValidationDto>> ValidateBankAccount([FromBody] BankAccountCreateDto createDto, CancellationToken cancellationToken = default)
        {
            var validation = await _bankAccountService.ValidateBankAccountAsync(createDto, cancellationToken);
            return Ok(validation);
        }

        #region Helper Methods

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new Shared.Exceptions.UnauthorizedException("User not authenticated or invalid user ID");
            }
            return userId;
        }

        private async Task<bool> IsBankAccountOwnerOrAdmin(Guid resourceOwnerId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == resourceOwnerId)
            {
                return true;
            }

            return User.IsInRole("Admin");
        }
        #endregion
    }
}