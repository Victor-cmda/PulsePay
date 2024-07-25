using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<PaymentsController> _logger;
        public DashboardController(IDashboardService dashboardService, ILogger<PaymentsController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<DashboardDataDto>> GetDashboard(List<Guid> sellers)
        {
            try
            {
                var dashboardData = await _dashboardService.GetDashboard(sellers);
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
