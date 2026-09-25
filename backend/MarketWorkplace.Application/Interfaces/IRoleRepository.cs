using MarketWorkplace.Domain.Entities;

namespace MarketWorkplace.Application.Interfaces;

/// <summary>Role lookups used when resolving account role names to rows.</summary>
public interface IRoleRepository : IRepository<Role>
{
    /// <summary>The role with this name (case-insensitive), or <c>null</c> when unknown.</summary>
    Role? FindByName(string name);
}
