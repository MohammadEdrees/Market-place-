using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;

namespace MarketWorkplace.Application.Common;

/// <summary>Keeps the managed category list in sync with categories typed into listing forms.</summary>
public static class CategorySync
{
    /// <summary>
    /// Adds <paramref name="name"/> to the managed list when no category of
    /// <paramref name="kind"/> uses it yet (case-insensitive), so a category introduced
    /// through the API or a listing form immediately shows up in the filter chips.
    /// No-op for blank names and categories that already exist.
    /// </summary>
    public static void EnsureExists(IRepository<Category> categories, string name, string kind)
    {
        name = name.Trim();
        if (name.Length == 0)
        {
            return;
        }

        var exists = categories.QueryReadOnly()
            .Any(c => c.Kind == kind && c.Name.ToLower() == name.ToLower());
        if (exists)
        {
            return;
        }

        categories.Add(new Category
        {
            Id = categories.QueryReadOnly().Any() ? categories.QueryReadOnly().Max(c => c.Id) + 1 : 1,
            Name = name,
            Kind = kind,
            CreatedAt = DateTime.UtcNow,
        });
        categories.SaveChanges();
    }
}
