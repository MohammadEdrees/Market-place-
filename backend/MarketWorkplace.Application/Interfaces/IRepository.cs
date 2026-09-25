namespace MarketWorkplace.Application.Interfaces;

/// <summary>Data-access contract for one entity type; implemented in Infrastructure (EF Core).</summary>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>Tracking query — returned entities can be modified and saved.</summary>
    IQueryable<TEntity> Query();

    /// <summary>Read-only query (no change tracking) for lists, counts and projections.</summary>
    IQueryable<TEntity> QueryReadOnly();

    void Add(TEntity entity);

    void AddRange(IEnumerable<TEntity> entities);

    void Remove(TEntity entity);

    void RemoveRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Persists pending changes. All repositories of a request share one scoped
    /// <c>MarketDbContext</c>, so saving through any of them commits the whole unit of work.
    /// </summary>
    int SaveChanges();

    /// <summary>
    /// Runs <paramref name="action"/> inside a database transaction: every <see cref="SaveChanges"/>
    /// call inside it joins the transaction, which commits when the action returns and rolls back
    /// if it throws. Used by restore, where a partial write would corrupt the dataset.
    /// </summary>
    void InTransaction(Action action);
}
