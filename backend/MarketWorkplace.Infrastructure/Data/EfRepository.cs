using MarketWorkplace.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MarketWorkplace.Infrastructure.Data;

/// <summary>EF Core implementation of the generic repository over the shared scoped context.</summary>
public class EfRepository<TEntity>(MarketDbContext db) : IRepository<TEntity> where TEntity : class
{
    public IQueryable<TEntity> Query() => db.Set<TEntity>();

    public IQueryable<TEntity> QueryReadOnly() => db.Set<TEntity>().AsNoTracking();

    public void Add(TEntity entity) => db.Set<TEntity>().Add(entity);

    public void AddRange(IEnumerable<TEntity> entities) => db.Set<TEntity>().AddRange(entities);

    public void Remove(TEntity entity) => db.Set<TEntity>().Remove(entity);

    public void RemoveRange(IEnumerable<TEntity> entities) => db.Set<TEntity>().RemoveRange(entities);

    /// <summary>Commits this and any other pending change in the request's shared context.</summary>
    public int SaveChanges() => db.SaveChanges();

    /// <summary>
    /// Runs <paramref name="action"/> in a database transaction (commits on return, rolls back on
    /// throw), so multi-step writes like a restore either fully apply or leave no trace.
    /// </summary>
    public void InTransaction(Action action)
    {
        using var transaction = db.Database.BeginTransaction();
        action();
        transaction.Commit();
    }
}
