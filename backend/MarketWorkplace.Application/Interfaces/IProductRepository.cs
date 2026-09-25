using MarketWorkplace.Domain.Entities;

namespace MarketWorkplace.Application.Interfaces;

/// <summary>Product queries that need the gallery eagerly loaded.</summary>
public interface IProductRepository : IRepository<Product>
{
    /// <summary>Catalogue reads: no tracking, images included.</summary>
    IQueryable<Product> Catalog();
}
