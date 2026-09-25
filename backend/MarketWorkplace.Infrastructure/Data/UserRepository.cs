using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketWorkplace.Infrastructure.Data;

/// <summary>User repository; adds queries that eagerly include the assigned role.</summary>
public class UserRepository(MarketDbContext db) : EfRepository<User>(db), IUserRepository
{
    public IQueryable<User> Profiles() => QueryReadOnly().Include(u => u.Role);

    public IQueryable<User> WithRole() => Query().Include(u => u.Role);
}
