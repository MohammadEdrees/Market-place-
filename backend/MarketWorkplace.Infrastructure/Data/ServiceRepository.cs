using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketWorkplace.Infrastructure.Data;

/// <summary>Service repository; adds the catalogue query that eagerly includes gallery images.</summary>
public class ServiceRepository(MarketDbContext db) : EfRepository<Service>(db), IServiceRepository
{
    public IQueryable<Service> Catalog() => QueryReadOnly().Include(s => s.Images);
}
