using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;

namespace Presentation.API
{
    [Authorize(Policy = "UserPolicy")]
    [ApiController]
    [Route("api/withdraw")]
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
        public async Task<IActionResult> RequestWithdraw(WithdrawCreateDto createDto)
        {
            try
            {
                var withdraw = await _withdrawService.RequestWithdrawAsync(createDto);
                return CreatedAtAction(nameof(GetWithdraw), new { id = withdraw.Id }, withdraw);
            }
            catch (InsufficientFundsException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWithdraw(Guid id)
        {
            try
            {
                var withdraw = await _withdrawService.GetWithdrawAsync(id);
                return Ok(withdraw);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("seller/{sellerId}")]
        public async Task<IActionResult> GetWithdrawsBySeller(
            Guid sellerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var withdraws = await _withdrawService.GetWithdrawsBySellerAsync(sellerId, page, pageSize);
            return Ok(withdraws);
        }

        [HttpPut("{id}/process")]
        public async Task<IActionResult> ProcessWithdraw(Guid id, WithdrawUpdateDto updateDto)
        {
            try
            {
                var withdraw = await _withdrawService.ProcessWithdrawAsync(id, updateDto);
                return Ok(withdraw);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("summary/{sellerId}")]
        public async Task<IActionResult> GetWithdrawSummary(
            Guid sellerId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var summary = await _withdrawService.GetWithdrawSummaryAsync(sellerId, startDate, endDate);
            return Ok(summary);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingWithdraws()
        {
            var pendingWithdraws = await _withdrawService.GetPendingWithdrawsAsync();
            return Ok(pendingWithdraws);
        }
    }

}
