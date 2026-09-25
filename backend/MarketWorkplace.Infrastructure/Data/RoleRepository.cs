using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;

namespace MarketWorkplace.Infrastructure.Data;

/// <summary>Role repository; adds the case-insensitive name lookup used by account services.</summary>
public class RoleRepository(MarketDbContext db) : EfRepository<Role>(db), IRoleRepository
{
    /// <summary>Returns a tracked role so assigning it to a new user only writes the foreign key.</summary>
    public Role? FindByName(string name) =>
        // ToLower on both sides keeps it EF-translatable (string.Equals(StringComparison) is not).
        Query().FirstOrDefault(r => r.Name.ToLower() == name.ToLower());
}
