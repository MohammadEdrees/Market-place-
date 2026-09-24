using MarketWorkplace.Api.Auth;
using MarketWorkplace.Api.Data;
using MarketWorkplace.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketWorkplace.Api.Controllers;

/// <summary>Orders: clients buy products and reserve services.</summary>
/// <remarks>
/// Dashboard admins see and manage every order. Other callers see orders they placed plus
/// orders for their own listings, and can update status only on orders for their listings.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/orders")]
[Tags("Orders")]
[Produces("application/json")]
public class OrdersController(MarketDbContext db) : ControllerBase
{
    /// <summary>Canonical status values accepted by status transitions.</summary>
    private static readonly string[] AllowedStatuses =
        ["Processing", "Confirmed", "Completed", "Cancelled", "Reserved", "Refunded"];

    /// <summary>Places an order: buy a product or reserve a service.</summary>
    /// <param name="input"><c>Kind</c> plus the <c>ProductId</c> to buy or <c>ServiceId</c> to reserve.</param>
    /// <returns>The created order (purchase starts as <c>Processing</c>, reservation as <c>Reserved</c>).</returns>
    /// <response code="201">Order placed.</response>
    /// <response code="400">Missing identifiers, invalid kind, or the product is out of stock.</response>
    /// <response code="404">The product or service does not exist.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Order), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Order> Place([FromBody] OrderInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var buyerId = Access.UserId(User);
        var customer = User.FindFirst("name")?.Value ?? string.Empty;
        var kind = input.Kind.Trim();

        Order order;

        if (kind.Equals("Product", StringComparison.OrdinalIgnoreCase))
        {
            if (input.ProductId is null)
            {
                return Problem(
                    title: "ProductId is required",
                    detail: "Specify which product to buy.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var product = db.Products.FirstOrDefault(p => p.Id == input.ProductId);
            if (product is null)
            {
                return NotFound();
            }

            if (product.Stock <= 0)
            {
                return Problem(
                    title: "Out of stock",
                    detail: $"{product.Name} cannot be ordered right now.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // One unit leaves stock and counts as sold.
            product.Stock -= 1;
            product.Sold += 1;

            order = new Order
            {
                Kind = "Product",
                ProductId = product.Id,
                Customer = customer,
                Product = product.Name,
                Category = product.Category,
                Total = product.Price,
                Status = "Processing",
                Date = DateTime.UtcNow,
                BuyerId = buyerId,
            };
        }
        else if (kind.Equals("Service", StringComparison.OrdinalIgnoreCase))
        {
            if (input.ServiceId is null)
            {
                return Problem(
                    title: "ServiceId is required",
                    detail: "Specify which service to reserve.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var service = db.Services.FirstOrDefault(s => s.Id == input.ServiceId);
            if (service is null)
            {
                return NotFound();
            }

            if (!service.IsActive)
            {
                return Problem(
                    title: "Service unavailable",
                    detail: "This service cannot be reserved right now.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            order = new Order
            {
                Kind = "Service",
                ServiceId = service.Id,
                Customer = customer,
                Product = service.Title,
                Category = service.Category,
                Total = service.Cost,
                Status = "Reserved",
                Date = DateTime.UtcNow,
                BuyerId = buyerId,
            };
        }
        else
        {
            return Problem(
                title: "Invalid kind",
                detail: "Kind must be Product or Service.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        order.Id = db.Orders.Any() ? db.Orders.Max(o => o.Id) + 1 : 1;
        db.Orders.Add(order);
        db.SaveChanges();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>Lists one page of the orders visible to the caller, with filters and sorting.</summary>
    /// <param name="search">Case-insensitive match against customer, item name or category.</param>
    /// <param name="kind"><c>Product</c> (purchase) or <c>Service</c> (reservation).</param>
    /// <param name="status">Exact status match, e.g. <c>Reserved</c>.</param>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="pageSize">Rows per page, clamped to 1–100 (default 20).</param>
    /// <param name="sortBy">Sort column: <c>id</c>, <c>date</c>, <c>total</c>, <c>status</c>, <c>product</c> or <c>customer</c>; no value means newest first.</param>
    /// <param name="sortDir"><c>asc</c> (default) or <c>desc</c>.</param>
    /// <returns>One page of orders for dashboard admins, or of orders the caller placed/received on their listings.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<Order>), StatusCodes.Status200OK)]
    public ActionResult<PagedResponse<Order>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? kind,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDir = "asc")
    {
        page = Paging.NormalizePage(page);
        pageSize = Paging.NormalizePageSize(pageSize);

        IEnumerable<Order> orders = db.Orders.AsNoTracking().AsEnumerable();

        if (!Access.IsAdmin(User))
        {
            var userId = Access.UserId(User);
            var productIds = db.Products.AsNoTracking().AsEnumerable()
                .Where(p => p.SellerId == userId)
                .Select(p => p.Id)
                .ToHashSet();
            var serviceIds = db.Services.AsNoTracking().AsEnumerable()
                .Where(s => s.ProviderId == userId)
                .Select(s => s.Id)
                .ToHashSet();

            orders = orders.Where(o =>
                o.BuyerId == userId ||
                (o.ProductId is int productId && productIds.Contains(productId)) ||
                (o.ServiceId is int serviceId && serviceIds.Contains(serviceId)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            orders = orders.Where(o =>
                o.Customer.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                o.Product.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                o.Category.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(kind))
        {
            orders = orders.Where(o => o.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            orders = orders.Where(o => o.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        var total = orders.Count();
        var items = ApplySort(orders, sortBy, sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .ThenBy(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new PagedResponse<Order>(items, total, page, pageSize));
    }

    /// <summary>Whitelisted sort keys for the order list; no key means newest first, unknown keys fall back to the id.</summary>
    private static IOrderedEnumerable<Order> ApplySort(IEnumerable<Order> orders, string? sortBy, bool desc)
    {
        var field = (sortBy ?? string.Empty).Trim().ToLowerInvariant();
        if (field.Length == 0)
        {
            return orders.OrderByDescending(o => o.Date);
        }

        IComparer<Order> comparer = field switch
        {
            "date" => Comparer<Order>.Create((a, b) => a.Date.CompareTo(b.Date)),
            "total" => Comparer<Order>.Create((a, b) => a.Total.CompareTo(b.Total)),
            "status" => Comparer<Order>.Create((a, b) => string.Compare(a.Status, b.Status, StringComparison.OrdinalIgnoreCase)),
            "product" => Comparer<Order>.Create((a, b) => string.Compare(a.Product, b.Product, StringComparison.OrdinalIgnoreCase)),
            "customer" => Comparer<Order>.Create((a, b) => string.Compare(a.Customer, b.Customer, StringComparison.OrdinalIgnoreCase)),
            _ => Comparer<Order>.Create((a, b) => a.Id.CompareTo(b.Id)),
        };

        return desc ? orders.OrderByDescending(o => o, comparer) : orders.OrderBy(o => o, comparer);
    }

    /// <summary>Orders the caller placed, newest first.</summary>
    /// <returns>Only orders whose buyer is the signed-in user.</returns>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IEnumerable<Order>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Order>> GetMine()
    {
        var userId = Access.UserId(User);
        return Ok(db.Orders.AsNoTracking().AsEnumerable()
            .Where(o => o.BuyerId == userId)
            .OrderByDescending(o => o.Date)
            .ToList());
    }

    /// <summary>Gets a single order the caller may see (their own, their listings', or any for admins).</summary>
    /// <param name="id">The order identifier.</param>
    /// <returns>The order, <c>404</c> when missing, or <c>403</c> when not visible.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Order> GetById(int id)
    {
        var order = db.Orders.AsNoTracking().FirstOrDefault(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.BuyerId != Access.UserId(User) && !CanManage(order))
        {
            return Forbid();
        }

        return Ok(order);
    }

    /// <summary>Updates an order's status (processing, confirmed, completed, cancelled, reserved, refunded).</summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="input">The new status.</param>
    /// <returns>The updated order, <c>400</c> for unknown statuses, or <c>403</c> when the caller may not manage it.</returns>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Order> UpdateStatus(int id, [FromBody] OrderStatusInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var requested = input.Status.Trim();
        var canonical = AllowedStatuses.FirstOrDefault(s =>
            s.Equals(requested, StringComparison.OrdinalIgnoreCase));

        if (canonical is null)
        {
            return Problem(
                title: "Invalid status",
                detail: $"Allowed statuses: {string.Join(", ", AllowedStatuses)}.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var order = db.Orders.FirstOrDefault(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (!CanManage(order))
        {
            return Forbid();
        }

        order.Status = canonical;
        db.SaveChanges();
        return Ok(order);
    }

    /// <summary>True when the caller is a dashboard admin or owns the listing behind the order.</summary>
    private bool CanManage(Order order)
    {
        if (Access.IsAdmin(User))
        {
            return true;
        }

        var userId = Access.UserId(User);

        if (order.ProductId is int productId)
        {
            var sellerId = db.Products.AsNoTracking().FirstOrDefault(p => p.Id == productId)?.SellerId;
            if (sellerId == userId)
            {
                return true;
            }
        }

        if (order.ServiceId is int serviceId)
        {
            var providerId = db.Services.AsNoTracking().FirstOrDefault(s => s.Id == serviceId)?.ProviderId;
            if (providerId == userId)
            {
                return true;
            }
        }

        return false;
    }
}
