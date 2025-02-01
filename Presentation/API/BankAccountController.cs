using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ValidationException = Shared.Exceptions.ValidationException;

namespace Presentation.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetBankAccount(Guid id)
        {
            try
            {
                var bankAccount = await _bankAccountService.GetBankAccountAsync(id);
                return Ok(bankAccount);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bank account {Id}", id);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("seller/{sellerId}")]
        [ProducesResponseType(typeof(IEnumerable<BankAccountResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetSellerBankAccounts(Guid sellerId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (currentUserId != sellerId.ToString())
                {
                    return Forbid();
                }

                var bankAccounts = await _bankAccountService.GetSellerBankAccountsAsync(sellerId);
                return Ok(bankAccounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bank accounts for seller {SellerId}", sellerId);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateBankAccount([FromBody] BankAccountCreateDto createDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (currentUserId != createDto.SellerId.ToString())
                {
                    return Forbid();
                }

                // Validar a conta bancária antes de criar
                var validation = await _bankAccountService.ValidateBankAccountAsync(createDto);
                if (!validation.IsValid)
                {
                    return BadRequest(new { errors = validation.ValidationErrors });
                }

                var bankAccount = await _bankAccountService.CreateBankAccountAsync(createDto);
                return CreatedAtAction(
                    nameof(GetBankAccount),
                    new { id = bankAccount.Id },
                    bankAccount);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bank account for seller {SellerId}", createDto.SellerId);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateBankAccount(Guid id, [FromBody] BankAccountUpdateDto updateDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var bankAccount = await _bankAccountService.GetBankAccountAsync(id);

                if (currentUserId != bankAccount.SellerId.ToString())
                {
                    return Forbid();
                }

                var updatedBankAccount = await _bankAccountService.UpdateBankAccountAsync(id, updateDto);
                return Ok(updatedBankAccount);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bank account {Id}", id);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteBankAccount(Guid id)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var result = await _bankAccountService.DeleteBankAccountAsync(id, currentUserId);

                if (result)
                    return NoContent();

                return NotFound(new { message = $"Bank account with ID {id} not found" });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bank account {Id}", id);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("{id}/verify")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerifyBankAccount(Guid id)
        {
            try
            {
                var result = await _bankAccountService.VerifyBankAccountAsync(id);
                return Ok(new { verified = result });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying bank account {Id}", id);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("validate")]
        [ProducesResponseType(typeof(BankAccountValidationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ValidateBankAccount([FromBody] BankAccountCreateDto createDto)
        {
            try
            {
                var validation = await _bankAccountService.ValidateBankAccountAsync(createDto);
                return Ok(validation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating bank account data");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }
    }
}
