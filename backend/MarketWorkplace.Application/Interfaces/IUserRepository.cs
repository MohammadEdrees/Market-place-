using MarketWorkplace.Domain.Entities;

namespace MarketWorkplace.Application.Interfaces;

/// <summary>User queries that need the assigned <see cref="Role"/> eagerly loaded.</summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>User lists and projections: no tracking, role included.</summary>
    IQueryable<User> Profiles();

    /// <summary>Single user for an edit that returns its profile: tracked, role included.</summary>
    IQueryable<User> WithRole();
}
