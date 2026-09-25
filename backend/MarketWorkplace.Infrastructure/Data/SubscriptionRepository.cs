using MarketWorkplace.Application.Common;
using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketWorkplace.Infrastructure.Data;

/// <summary>Subscription repository; adds the query that eagerly loads the subscribed user.</summary>
public class SubscriptionRepository(MarketDbContext db) : EfRepository<Subscription>(db), ISubscriptionRepository
{
    public IQueryable<Subscription> WithUser() => QueryReadOnly().Include(s => s.User);
}
