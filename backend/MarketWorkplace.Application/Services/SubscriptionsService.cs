using System.Security.Claims;
using MarketWorkplace.Application.Common;
using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static MarketWorkplace.Application.Common.ServiceResults;

namespace MarketWorkplace.Application.Services;

/// <summary>Subscription plan CRUD (backs <c>SubscriptionsController</c>).</summary>
/// <remarks>
/// Every subscription is linked to a <see cref="User"/> account; the dashboard shows
/// which account it belongs to (mobile users are the primary subscribers) and the
/// users page surfaces the active subscription next to a profile.
/// </remarks>
public class SubscriptionsService(
    ISubscriptionRepository subscriptions,
    IRepository<User> users)
{
    /// <summary>All subscriptions, newest first, with the subscribed user.</summary>
    public ActionResult<IEnumerable<SubscriptionDto>> GetAll()
    {
        var items = subscriptions.WithUser()
            .OrderByDescending(s => s.CreatedAt)
            .Select(ToDto)
            .ToList();
        return Ok(items);
    }

    /// <summary>One subscription by its identifier.</summary>
    public ActionResult<SubscriptionDto> GetById(int id)
    {
        var sub = subscriptions.WithUser().FirstOrDefault(s => s.Id == id);
        return sub is null ? NotFound() : Ok(ToDto(sub));
    }

    /// <summary>Subscriptions belonging to one account.</summary>
    public ActionResult<IEnumerable<SubscriptionDto>> GetByUser(int userId)
    {
        if (users.QueryReadOnly().FirstOrDefault(u => u.Id == userId) is null)
        {
            return NotFound();
        }

        var items = subscriptions.WithUser()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(ToDto)
            .ToList();
        return Ok(items);
    }

    /// <summary>Creates a subscription (admin callers only).</summary>
    public ActionResult<SubscriptionDto> Create(ClaimsPrincipal caller, SubscriptionCreateInput input)
    {
        if (!Access.IsAdmin(caller))
        {
            return Forbid();
        }

        if (users.QueryReadOnly().FirstOrDefault(u => u.Id == input.UserId) is null)
        {
            return Problem(
                title: "Unknown user",
                detail: $"No account with id {input.UserId}.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        ValidatePlan(input.Plan); // throws via helper

        var now = DateTime.UtcNow;
        var subscription = new Subscription
        {
            UserId = input.UserId,
            Plan = input.Plan.Trim(),
            Price = input.Price,
            BillingCycle = input.BillingCycle.Trim(),
            Status = Subscription.Active,
            StartsAt = input.StartsAt ?? now,
            EndsAt = input.EndsAt,
            AutoRenew = input.AutoRenew,
            CreatedAt = now,
        };

        subscriptions.Add(subscription);
        subscriptions.SaveChanges();

        return CreatedAtAction("GetById", new { id = subscription.Id }, ToDto(subscription));
    }

    /// <summary>Updates a subscription (admin callers only).</summary>
    public ActionResult<SubscriptionDto> Update(ClaimsPrincipal caller, int id, SubscriptionUpdateInput input)
    {
        if (!Access.IsAdmin(caller))
        {
            return Forbid();
        }

        var sub = subscriptions.Query().FirstOrDefault(s => s.Id == id);
        if (sub is null)
        {
            return NotFound();
        }

        ValidatePlan(input.Plan);

        sub.Plan = input.Plan.Trim();
        sub.Price = input.Price;
        sub.BillingCycle = input.BillingCycle.Trim();
        sub.Status = input.Status.Trim();
        sub.EndsAt = input.EndsAt;
        sub.AutoRenew = input.AutoRenew;

        subscriptions.SaveChanges();
        return Ok(ToDto(sub));
    }

    /// <summary>Deletes a subscription (admin callers only).</summary>
    public IActionResult Delete(ClaimsPrincipal caller, int id)
    {
        if (!Access.IsAdmin(caller))
        {
            return Forbid();
        }

        var sub = subscriptions.Query().FirstOrDefault(s => s.Id == id);
        if (sub is null)
        {
            return NotFound();
        }

        subscriptions.Remove(sub);
        subscriptions.SaveChanges();
        return NoContent();
    }

    /// <summary>Returns the <see cref="Subscription"/> plan constants and rejects the rest.</summary>
    private static void ValidatePlan(string plan)
    {
        if (plan is not (Subscription.BasicPlan or Subscription.PremiumPlan or Subscription.EnterprisePlan))
        {
            throw new InvalidOperationException(
                $"'{plan}' is not a valid plan; use '{Subscription.BasicPlan}', '{Subscription.PremiumPlan}' or '{Subscription.EnterprisePlan}'.");
        }
    }

    private static SubscriptionDto ToDto(Subscription s) =>
        new(s.Id, s.UserId, s.User.Name, s.User.Email, s.User.Type,
            s.Plan, s.Price, s.BillingCycle, s.Status,
            s.StartsAt, s.EndsAt, s.AutoRenew, s.CreatedAt);
}
