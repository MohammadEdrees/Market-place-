using System.Security.Claims;
using MarketWorkplace.Application.Common;
using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static MarketWorkplace.Application.Common.ServiceResults;

namespace MarketWorkplace.Application.Services;

/// <summary>Managed category list (backs <c>CategoriesController</c>): the chips, forms and filters read it.</summary>
public class CategoriesService(
    IRepository<Category> categories,
    IProductRepository products,
    IServiceRepository services)
{
    /// <summary>All categories (optionally of one kind) with their live listing counts, Product kind first.</summary>
    public ActionResult<IEnumerable<CategoryDto>> GetAll(string? kind)
    {
        var query = categories.QueryReadOnly();
        if (!string.IsNullOrWhiteSpace(kind))
        {
            query = query.Where(c => c.Kind == kind);
        }

        var listingCounts = ListingCounts();
        var items = query.AsEnumerable()
            .OrderBy(c => c.Kind == Category.ProductKind ? 0 : 1)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => ToDto(c, listingCounts))
            .ToList();

        return Ok(items);
    }

    /// <summary>One category with its listing count, or <c>404</c>.</summary>
    public ActionResult<CategoryDto> GetById(int id)
    {
        var category = categories.QueryReadOnly().FirstOrDefault(c => c.Id == id);
        return category is null ? NotFound() : Ok(ToDto(category, ListingCounts()));
    }

    /// <summary>Creates a category (admin callers only); the name must be unique per kind.</summary>
    public ActionResult<CategoryDto> Create(ClaimsPrincipal caller, CategoryCreateInput input)
    {
        if (!Access.IsAdmin(caller))
        {
            return Forbid();
        }

        var error = ValidateKind(input.Kind);
        if (error is not null)
        {
            return error;
        }

        var name = input.Name.Trim();
        if (name.Length == 0)
        {
            return Problem(title: "Invalid category", detail: "Name is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (categories.QueryReadOnly().Any(c => c.Kind == input.Kind && c.Name.ToLower() == name.ToLower()))
        {
            return Problem(
                title: "Category already exists",
                detail: $"Another {input.Kind.ToLower()} category already uses this name.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var category = new Category
        {
            Id = categories.QueryReadOnly().Any() ? categories.QueryReadOnly().Max(c => c.Id) + 1 : 1,
            Name = name,
            Kind = input.Kind,
            CreatedAt = DateTime.UtcNow,
        };

        categories.Add(category);
        categories.SaveChanges();

        return CreatedAtAction("GetById", new { id = category.Id }, ToDto(category, new Dictionary<string, int>()));
    }

    /// <summary>Renames a category (admin callers only).</summary>
    /// <remarks>The new name cascades onto every product/service that used the old one, so the
    /// filter chips and the listings never drift apart; order rows keep their historical value.</remarks>
    public ActionResult<CategoryDto> Update(ClaimsPrincipal caller, int id, CategoryUpdateInput input)
    {
        if (!Access.IsAdmin(caller)) return Forbid();

        var category = categories.Query().FirstOrDefault(c => c.Id == id);
        if (category is null) return NotFound();

        var name = input.Name.Trim();
        if (name.Length == 0)
            return Problem(title: "Invalid category", detail: "Name is required.", statusCode: StatusCodes.Status400BadRequest);
        if (categories.QueryReadOnly().Any(c => c.Id != id && c.Kind == category.Kind && c.Name.ToLower() == name.ToLower()))
            return Problem(title: "Category already exists", detail: "Another category of this kind already uses this name.", statusCode: StatusCodes.Status409Conflict);

        if (!string.Equals(category.Name, name, StringComparison.Ordinal))
        {
            // Cascade the rename so listings keep matching their chip — only within the
            // category's own kind, since product and service categories are independent lists.
            if (category.Kind == Category.ServiceKind)
            {
                foreach (var service in services.Query().Where(s => s.Category == category.Name).ToList())
                {
                    service.Category = name;
                }
            }
            else
            {
                foreach (var product in products.Query().Where(p => p.Category == category.Name).ToList())
                {
                    product.Category = name;
                }
            }
            category.Name = name;
        }

        categories.SaveChanges();
        return Ok(ToDto(category, ListingCounts()));
    }

    /// <summary>Deletes a category no listing uses (admin callers only).</summary>
    /// <remarks>Returns <c>409 Conflict</c> while products/services still carry the name —
    /// move or rename them first (renaming this category to something else also frees it).</remarks>
    public IActionResult Delete(ClaimsPrincipal caller, int id)
    {
        if (!Access.IsAdmin(caller)) return Forbid();

        var category = categories.Query().FirstOrDefault(c => c.Id == id);
        if (category is null) return NotFound();

        var count = category.Kind == Category.ServiceKind
            ? services.QueryReadOnly().Count(s => s.Category == category.Name)
            : products.QueryReadOnly().Count(p => p.Category == category.Name);

        if (count > 0)
        {
            return Problem(
                title: "Category in use",
                detail: $"Reassign the {count} listing(s) using this category before deleting it.",
                statusCode: StatusCodes.Status409Conflict);
        }

        categories.Remove(category);
        categories.SaveChanges();
        return NoContent();
    }

    /// <summary>Rejects anything that is not <c>Product</c> or <c>Service</c> (case-sensitive, as seeded).</summary>
    private static ObjectResult? ValidateKind(string kind) =>
        kind is not (Category.ProductKind or Category.ServiceKind)
            ? Problem(
                title: "Invalid kind",
                detail: $"'{kind}' is not a valid category kind; use '{Category.ProductKind}' or '{Category.ServiceKind}'.",
                statusCode: StatusCodes.Status400BadRequest)
            : null;

    /// <summary>Usage per kind and name so the DTO can show how many listings of that kind carry each category.</summary>
    private Dictionary<string, int> ListingCounts()
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in products.QueryReadOnly().AsEnumerable().GroupBy(p => p.Category))
        {
            counts[$"{Category.ProductKind}|{group.Key}"] = group.Count();
        }
        foreach (var group in services.QueryReadOnly().AsEnumerable().GroupBy(s => s.Category))
        {
            counts[$"{Category.ServiceKind}|{group.Key}"] = group.Count();
        }
        return counts;
    }

    private static CategoryDto ToDto(Category category, IReadOnlyDictionary<string, int> listingCounts) =>
        new(category.Id, category.Name, category.Kind, listingCounts.GetValueOrDefault($"{category.Kind}|{category.Name}"), category.CreatedAt);
}
