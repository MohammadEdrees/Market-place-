using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Api.Controllers;

/// <summary>Subscription plans purchased by accounts (mobile users are the primary
/// subscribers); the dashboard's Settings page surfaces these next to each user.</summary>
/// <remarks>Reads need a valid bearer token; writes are limited to dashboard admins.</remarks>
[ApiController]
[Authorize]
[Route("api/subscriptions")]
[Tags("Subscriptions")]
[Produces("application/json")]
public class SubscriptionsController(SubscriptionsService subscriptionsService) : ControllerBase
{
    /// <summary>All subscriptions, newest first, with the subscribed user.</summary>
    /// <returns>The stored subscriptions.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<SubscriptionDto>> GetAll() => subscriptionsService.GetAll();

    /// <summary>One subscription by its identifier.</summary>
    /// <param name="id">The subscription identifier.</param>
    /// <returns>The subscription, or <c>404 Not Found</c>.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<SubscriptionDto> GetById(int id) => subscriptionsService.GetById(id);

    /// <summary>Subscriptions belonging to one account.</summary>
    /// <param name="userId">The account identifier.</param>
    /// <returns>That account's subscriptions.</returns>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<SubscriptionDto>> GetByUser(int userId) =>
        subscriptionsService.GetByUser(userId);

    /// <summary>Creates a subscription (admin callers only).</summary>
    /// <param name="input">The account, plan, price and billing cadence.</param>
    /// <returns>The created subscription, <c>403</c> for non-admins, or <c>400</c> for an unknown account.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<SubscriptionDto> Create([FromBody] SubscriptionCreateInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return subscriptionsService.Create(User, input);
    }

    /// <summary>Updates a subscription (admin callers only).</summary>
    /// <param name="id">The subscription identifier.</param>
    /// <param name="input">The updated plan details.</param>
    /// <returns>The updated subscription, <c>404</c> when missing.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<SubscriptionDto> Update(int id, [FromBody] SubscriptionUpdateInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return subscriptionsService.Update(User, id, input);
    }

    /// <summary>Deletes a subscription (admin callers only).</summary>
    /// <param name="id">The subscription identifier.</param>
    /// <returns><c>204 No Content</c>, <c>404</c> when missing, or <c>403</c>.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id) => subscriptionsService.Delete(User, id);
}
