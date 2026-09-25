using MarketWorkplace.Domain.Entities;

namespace MarketWorkplace.Application.Interfaces;

/// <summary>Service queries that need the gallery eagerly loaded.</summary>
public interface IServiceRepository : IRepository<Service>
{
    /// <summary>Catalogue reads: no tracking, images included.</summary>
    IQueryable<Service> Catalog();
}
