using System.Security.Claims;
using MarketWorkplace.Application.Common;
using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static MarketWorkplace.Application.Common.ServiceResults;

namespace MarketWorkplace.Application.Services;

/// <summary>Order placement, visibility rules and status transitions (backs <c>OrdersController</c>).</summary>
public class OrdersService(IRepository<Order> orderList, IProductRepository products, IServiceRepository serviceList)
{
    /// <summary>Canonical status values accepted by status transitions.</summary>
    private static readonly string[] AllowedStatuses =
        ["Processing", "Confirmed", "Completed", "Cancelled", "Reserved", "Refunded"];

    /// <summary>Places an order: buy a product or reserve a service.</summary>
    public ActionResult<Order> Place(ClaimsPrincipal caller, OrderInput input)
    {
        var buyerId = Access.UserId(caller);
        var customer = caller.FindFirst("name")?.Value ?? string.Empty;
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

            var product = products.Query().FirstOrDefault(p => p.Id == input.ProductId);
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

            var service = serviceList.Query().FirstOrDefault(s => s.Id == input.ServiceId);
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

        order.Id = orderList.QueryReadOnly().Any() ? orderList.QueryReadOnly().Max(o => o.Id) + 1 : 1;
        orderList.Add(order);
        orderList.SaveChanges();

        return CreatedAtAction("GetById", new { id = order.Id }, order);
    }

    /// <summary>Lists one page of the orders visible to the caller, with filters and sorting.</summary>
    public ActionResult<PagedResponse<Order>> GetAll(
        ClaimsPrincipal caller,
        string? search,
        string? kind,
        string? status,
        int page = 1,
        int pageSize = Paging.DefaultPageSize,
        string? sortBy = null,
        string sortDir = "asc")
    {
        page = Paging.NormalizePage(page);
        pageSize = Paging.NormalizePageSize(pageSize);

        IEnumerable<Order> orders = orderList.QueryReadOnly().AsEnumerable();

        if (!Access.IsAdmin(caller))
        {
            var userId = Access.UserId(caller);
            var productIds = products.QueryReadOnly().AsEnumerable()
                .Where(p => p.SellerId == userId)
                .Select(p => p.Id)
                .ToHashSet();
            var serviceIds = serviceList.QueryReadOnly().AsEnumerable()
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
    public ActionResult<IEnumerable<Order>> GetMine(ClaimsPrincipal caller)
    {
        var userId = Access.UserId(caller);
        return Ok(orderList.QueryReadOnly().AsEnumerable()
            .Where(o => o.BuyerId == userId)
            .OrderByDescending(o => o.Date)
            .ToList());
    }

    /// <summary>Gets a single order the caller may see (their own, their listings', or any for admins).</summary>
    public ActionResult<Order> GetById(ClaimsPrincipal caller, int id)
    {
        var order = orderList.QueryReadOnly().FirstOrDefault(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.BuyerId != Access.UserId(caller) && !CanManage(caller, order))
        {
            return Forbid();
        }

        return Ok(order);
    }

    /// <summary>Updates an order's status (processing, confirmed, completed, cancelled, reserved, refunded).</summary>
    public ActionResult<Order> UpdateStatus(ClaimsPrincipal caller, int id, OrderStatusInput input)
    {
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

        var order = orderList.Query().FirstOrDefault(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (!CanManage(caller, order))
        {
            return Forbid();
        }

        order.Status = canonical;
        orderList.SaveChanges();
        return Ok(order);
    }

    /// <summary>True when the caller is a dashboard admin or owns the listing behind the order.</summary>
    private bool CanManage(ClaimsPrincipal caller, Order order)
    {
        if (Access.IsAdmin(caller))
        {
            return true;
        }

        var userId = Access.UserId(caller);

        if (order.ProductId is int productId)
        {
            var sellerId = products.QueryReadOnly().FirstOrDefault(p => p.Id == productId)?.SellerId;
            if (sellerId == userId)
            {
                return true;
            }
        }

        if (order.ServiceId is int serviceId)
        {
            var providerId = serviceList.QueryReadOnly().FirstOrDefault(s => s.Id == serviceId)?.ProviderId;
            if (providerId == userId)
            {
                return true;
            }
        }

        return false;
    }
}
