using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Services;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
public class OrdersController(OrdersService ordersService) : ControllerBase
{
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

        return ordersService.Place(User, input);
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
        [FromQuery] string sortDir = "asc") =>
        ordersService.GetAll(User, search, kind, status, page, pageSize, sortBy, sortDir);

    /// <summary>Orders the caller placed, newest first.</summary>
    /// <returns>Only orders whose buyer is the signed-in user.</returns>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IEnumerable<Order>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Order>> GetMine() => ordersService.GetMine(User);

    /// <summary>Gets a single order the caller may see (their own, their listings', or any for admins).</summary>
    /// <param name="id">The order identifier.</param>
    /// <returns>The order, <c>404</c> when missing, or <c>403</c> when not visible.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Order> GetById(int id) => ordersService.GetById(User, id);

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

        return ordersService.UpdateStatus(User, id, input);
    }
}
