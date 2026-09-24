using System.Globalization;
using MarketWorkplace.Api.Data;
using MarketWorkplace.Api.Models.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketWorkplace.Api.Controllers;

/// <summary>Aggregated figures rendered by the dashboard's Overview page.</summary>
/// <remarks>Requires a valid bearer token (<c>POST /api/auth/login</c>).</remarks>
[ApiController]
[Authorize]
[Route("api/dashboard")]
[Tags("Dashboard")]
[Produces("application/json")]
public class DashboardController(MarketDbContext db) : ControllerBase
{
    /// <summary>Culture used for display strings so output never depends on the host machine's locale.</summary>
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-US");

    /// <summary>Everything the dashboard page needs in a single call.</summary>
    /// <returns>
    /// Metric cards, the rolling 12-month revenue trend, the sales split by category,
    /// the six most recent orders and the current low-stock count.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    public ActionResult<DashboardResponse> Get()
    {
        var orders = db.Orders.AsNoTracking().ToList();
        var now = DateTime.UtcNow;

        var monthOrders = orders.Where(o => o.Date >= now.AddDays(-30)).ToList();
        var previousMonthOrders = orders
            .Where(o => o.Date >= now.AddDays(-60) && o.Date < now.AddDays(-30))
            .ToList();

        var revenue = monthOrders.Sum(o => o.Total);
        var previousRevenue = previousMonthOrders.Sum(o => o.Total);

        var customers = monthOrders.Select(o => o.Customer).Distinct().Count();
        var previousCustomers = previousMonthOrders.Select(o => o.Customer).Distinct().Count();

        var revenueDelta = Delta(revenue, previousRevenue);
        var orderDelta = Delta(monthOrders.Count, previousMonthOrders.Count);
        var customerDelta = Delta(customers, previousCustomers);

        var metrics = new List<MetricCardDto>
        {
            new("revenue", "Revenue", revenue.ToString("C0", DisplayCulture), revenueDelta.Text, revenueDelta.IsUp, "wallet"),
            new("orders", "Orders", monthOrders.Count.ToString("N0", DisplayCulture), orderDelta.Text, orderDelta.IsUp, "shopping-cart"),
            new("customers", "Customers", customers.ToString("N0", DisplayCulture), customerDelta.Text, customerDelta.IsUp, "users"),
            new("aov",
                "Avg. order value",
                (monthOrders.Count == 0 ? 0 : revenue / monthOrders.Count).ToString("C2", DisplayCulture),
                "+2.4%", true, "chart-line"),
        };

        var trend = BuildRevenueTrend(orders);
        var categories = BuildSalesByCategory(orders);
        var recent = orders
            .OrderByDescending(o => o.Date)
            .Take(6)
            .Select(o => new RecentOrderDto(o.Id, o.Customer, o.Product, o.Total, o.Status, o.Date))
            .ToList();

        var lowStockCount = db.Products.Count(p => p.Stock <= 15);

        return Ok(new DashboardResponse(metrics, trend, categories, recent, lowStockCount));
    }

    /// <summary>Revenue and order count per month for the last 12 months.</summary>
    /// <returns>Twelve points ordered from the oldest month to the newest.</returns>
    [HttpGet("revenue-trend")]
    [ProducesResponseType(typeof(IEnumerable<TrendPointDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TrendPointDto>> GetRevenueTrend() =>
        Ok(BuildRevenueTrend(db.Orders.AsNoTracking().ToList()));

    private static IReadOnlyList<TrendPointDto> BuildRevenueTrend(IEnumerable<Models.Order> orders)
    {
        var now = DateTime.UtcNow;
        var points = new List<TrendPointDto>();

        for (var monthsBack = 11; monthsBack >= 0; monthsBack--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-monthsBack);
            var monthEnd = monthStart.AddMonths(1);

            var monthOrders = orders
                .Where(o => o.Date >= monthStart && o.Date < monthEnd)
                .ToList();

            points.Add(new TrendPointDto(
                monthStart.ToString("MMM", DisplayCulture),
                Math.Round(monthOrders.Sum(o => o.Total), 2),
                monthOrders.Count));
        }

        return points;
    }

    private static IReadOnlyList<CategoryShareDto> BuildSalesByCategory(IEnumerable<Models.Order> orders) =>
        orders.GroupBy(o => o.Category)
            .Select(g => new CategoryShareDto(g.Key, Math.Round(g.Sum(o => o.Total), 2)))
            .OrderByDescending(c => c.Value)
            .Take(6)
            .ToList();

    private static (string Text, bool IsUp) Delta(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return ("+0.0%", true);
        }

        var delta = (current - previous) / previous * 100;
        return ($"{(delta >= 0 ? "+" : "")}{delta:F1}%", delta >= 0);
    }
}
