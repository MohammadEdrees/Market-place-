using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketWorkplace.Infrastructure.Data;

/// <summary>Product repository; adds the catalogue query that eagerly includes gallery images.</summary>
public class ProductRepository(MarketDbContext db) : EfRepository<Product>(db), IProductRepository
{
    public IQueryable<Product> Catalog() => QueryReadOnly().Include(p => p.Images);
}
