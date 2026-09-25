using MarketWorkplace.Domain.Entities;

namespace MarketWorkplace.Application.Interfaces;

/// <summary>Subscription queries plus a convenience list of active subscriptions for a user.</summary>
public interface ISubscriptionRepository : IRepository<Subscription>
{
    /// <summary>All subscriptions with the subscribed user eagerly loaded, newest first.</summary>
    IQueryable<Subscription> WithUser();
}
