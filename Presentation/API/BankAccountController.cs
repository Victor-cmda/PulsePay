using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;

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
            try
            {
                var bankAccount = await _bankAccountService.GetBankAccountAsync(id, cancellationToken);
                return Ok(bankAccount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bank account with ID {BankAccountId}", id);
                return StatusCode(500, "An error occurred while retrieving the bank account.");
            }
        }

        [HttpGet("seller/{sellerId:guid}")]
        [ProducesResponseType(typeof(IEnumerable<BankAccountResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<BankAccountResponseDto>>> GetSellerBankAccounts(Guid sellerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var bankAccounts = await _bankAccountService.GetSellerBankAccountsAsync(sellerId, cancellationToken);
                return Ok(bankAccounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bank accounts for seller with ID {SellerId}", sellerId);
                return StatusCode(500, "An error occurred while retrieving the bank accounts.");
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BankAccountResponseDto>> CreateBankAccount([FromBody] BankAccountCreateDto createDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var bankAccount = await _bankAccountService.CreateBankAccountAsync(createDto, cancellationToken);
                return StatusCode(201, bankAccount);
            }
            catch (ConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bank account for seller {SellerId}", createDto.SellerId);
                return StatusCode(500, "An error occurred while creating the bank account.");
            }
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BankAccountResponseDto>> UpdateBankAccount(Guid id, [FromBody] BankAccountUpdateDto updateDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var updatedBankAccount = await _bankAccountService.UpdateBankAccountAsync(id, updateDto, cancellationToken);
                return Ok(updatedBankAccount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bank account with ID {BankAccountId}", id);
                return StatusCode(500, "An error occurred while updating the bank account.");
            }
        }

        [HttpDelete("{sellerId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteBankAccount(Guid id, Guid sellerId, CancellationToken cancellationToken = default)
        {
            try
            {
                await _bankAccountService.DeleteBankAccountAsync(id, sellerId, cancellationToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bank account with ID {BankAccountId}", id);
                return StatusCode(500, "An error occurred while deleting the bank account.");
            }
        }

        [HttpPost("{id:guid}/verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyBankAccount(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _bankAccountService.VerifyBankAccountAsync(id, cancellationToken);
                return Ok(new { verified = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying bank account with ID {BankAccountId}", id);
                return StatusCode(500, "An error occurred while verifying the bank account.");
            }
        }

        [HttpPost("validate")]
        [ProducesResponseType(typeof(BankAccountValidationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BankAccountValidationDto>> ValidateBankAccount([FromBody] BankAccountCreateDto createDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var validation = await _bankAccountService.ValidateBankAccountAsync(createDto, cancellationToken);
                return Ok(validation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating bank account");
                return StatusCode(500, "An error occurred while validating the bank account.");
            }
        }
    }
}