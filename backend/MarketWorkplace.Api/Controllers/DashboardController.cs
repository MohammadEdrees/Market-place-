using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Api.Controllers;

/// <summary>Aggregated figures rendered by the dashboard's Overview page.</summary>
/// <remarks>Requires a valid bearer token (<c>POST /api/auth/login</c>).</remarks>
[ApiController]
[Authorize]
[Route("api/dashboard")]
[Tags("Dashboard")]
[Produces("application/json")]
public class DashboardController(DashboardService dashboardService) : ControllerBase
{
    /// <summary>Everything the dashboard page needs in a single call.</summary>
    /// <returns>
    /// Metric cards, the rolling 12-month revenue trend, the sales split by category,
    /// the six most recent orders and the current low-stock count.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    public ActionResult<DashboardResponse> Get() => dashboardService.Get();

    /// <summary>Revenue and order count per month for the last 12 months.</summary>
    /// <returns>Twelve points ordered from the oldest month to the newest.</returns>
    [HttpGet("revenue-trend")]
    [ProducesResponseType(typeof(IEnumerable<TrendPointDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TrendPointDto>> GetRevenueTrend() => dashboardService.GetRevenueTrend();
}
