using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;
using System.Security.Claims;

namespace Presentation.API
{
    [ApiController]
    [Route("api/[controller]")]
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

        [HttpGet("{id}")]
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
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bank account {Id}", id);
                return StatusCode(500, new ErrorResponse("An error occurred while processing your request."));
            }
        }

        [HttpGet("seller/{sellerId}")]
        [ProducesResponseType(typeof(IEnumerable<BankAccountResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<BankAccountResponseDto>>> GetSellerBankAccounts(Guid sellerId, CancellationToken cancellationToken = default)
        {
            if (!IsResourceOwner(sellerId))
            {
                return Forbid();
            }

            try
            {
                var bankAccounts = await _bankAccountService.GetSellerBankAccountsAsync(sellerId, cancellationToken);
                return Ok(bankAccounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bank accounts for seller {SellerId}", sellerId);
                return StatusCode(500, new ErrorResponse("An error occurred while processing your request."));
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BankAccountResponseDto>> CreateBankAccount([FromBody] BankAccountCreateDto createDto, CancellationToken cancellationToken = default)
        {
            if (!IsResourceOwner(createDto.SellerId))
            {
                return Forbid();
            }

            try
            {
                // Validação da conta bancária
                var validation = await _bankAccountService.ValidateBankAccountAsync(createDto, cancellationToken);
                if (!validation.IsValid)
                {
                    return BadRequest(new ValidationErrorResponse(validation.ValidationErrors));
                }

                var bankAccount = await _bankAccountService.CreateBankAccountAsync(createDto, cancellationToken);
                return CreatedAtAction(
                    nameof(GetBankAccount),
                    new { id = bankAccount.Id },
                    bankAccount);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ErrorResponse(ex.Message));
            }
            catch (ConflictException ex)
            {
                return Conflict(new ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bank account for seller {SellerId}", createDto.SellerId);
                return StatusCode(500, new ErrorResponse("An error occurred while processing your request."));
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BankAccountResponseDto>> UpdateBankAccount(Guid id, [FromBody] BankAccountUpdateDto updateDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var bankAccount = await _bankAccountService.GetBankAccountAsync(id, cancellationToken);

                if (!IsResourceOwner(bankAccount.SellerId))
                {
                    return Forbid();
                }

                var updatedBankAccount = await _bankAccountService.UpdateBankAccountAsync(id, updateDto, cancellationToken);
                return Ok(updatedBankAccount);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ErrorResponse(ex.Message));
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bank account {Id}", id);
                return StatusCode(500, new ErrorResponse("An error occurred while processing your request."));
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteBankAccount(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _bankAccountService.DeleteBankAccountAsync(id, currentUserId, cancellationToken);

                if (result)
                    return NoContent();

                return NotFound(new ErrorResponse($"Bank account with ID {id} not found"));
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bank account {Id}", id);
                return StatusCode(500, new ErrorResponse("An error occurred while processing your request."));
            }
        }

        [HttpPost("{id}/verify")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyBankAccount(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _bankAccountService.VerifyBankAccountAsync(id, cancellationToken);
                return Ok(new { verified = result });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying bank account {Id}", id);
                return StatusCode(500, new ErrorResponse("An error occurred while processing your request."));
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
                _logger.LogError(ex, "Error validating bank account data");
                return StatusCode(500, new ErrorResponse("An error occurred while processing your request."));
            }
        }

        #region Helper Methods

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim ?? throw new UnauthorizedException("User not authenticated"));
        }

        private bool IsResourceOwner(Guid resourceOwnerId)
        {
            var currentUserId = GetCurrentUserId();
            return currentUserId == resourceOwnerId;
        }

        #endregion
    }
}