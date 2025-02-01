using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;
using System.ComponentModel.DataAnnotations;
using ValidationException = Shared.Exceptions.ValidationException;

namespace Presentation.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly ILogger<WalletController> _logger;

        public WalletController(
            IWalletService walletService,
            ILogger<WalletController> logger)
        {
            _walletService = walletService;
            _logger = logger;
        }

        [HttpGet("{sellerId}")]
        [ProducesResponseType(typeof(WalletResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWallet(Guid sellerId)
        {
            try
            {
                var wallet = await _walletService.GetWalletAsync(sellerId);
                return Ok(wallet);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(WalletResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateWallet(WalletCreateDto createDto)
        {
            try
            {
                var wallet = await _walletService.CreateWalletAsync(createDto);
                return CreatedAtAction(nameof(GetWallet), new { sellerId = wallet.SellerId }, wallet);
            }
            catch (ConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{sellerId}/balance")]
        [ProducesResponseType(typeof(WalletResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateBalance(Guid sellerId, WalletUpdateDto updateDto)
        {
            try
            {
                var wallet = await _walletService.UpdateBalanceAsync(sellerId, updateDto);
                return Ok(wallet);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{sellerId}/add-funds")]
        [ProducesResponseType(typeof(WalletResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddFunds(Guid sellerId, [FromBody] decimal amount)
        {
            try
            {
                var wallet = await _walletService.AddFundsAsync(sellerId, amount);
                return Ok(wallet);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{sellerId}/deduct-funds")]
        [ProducesResponseType(typeof(WalletResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeductFunds(Guid sellerId, [FromBody] decimal amount)
        {
            try
            {
                var wallet = await _walletService.DeductFundsAsync(sellerId, amount);
                return Ok(wallet);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InsufficientFundsException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

}
