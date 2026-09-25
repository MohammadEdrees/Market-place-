using System.Security.Claims;
using MarketWorkplace.Application.Common;
using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static MarketWorkplace.Application.Common.ServiceResults;

namespace MarketWorkplace.Application.Services;

/// <summary>Product catalogue CRUD (backs <c>ProductsController</c>).</summary>
public class ProductsService(IProductRepository products, IImageStore images, IRepository<ListingImage> listingImages, IRepository<Category> categories)
{
    /// <summary>Lists one page of products with optional filters and sorting.</summary>
    public ActionResult<PagedResponse<Product>> GetAll(
        string? search,
        string? category,
        int? sellerId,
        int page = 1,
        int pageSize = Paging.DefaultPageSize,
        string? sortBy = null,
        string sortDir = "asc")
    {
        page = Paging.NormalizePage(page);
        pageSize = Paging.NormalizePageSize(pageSize);

        IEnumerable<Product> filtered = products.Catalog().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            filtered = filtered.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        if (sellerId is not null)
        {
            filtered = filtered.Where(p => p.SellerId == sellerId);
        }

        var total = filtered.Count();
        var items = ApplySort(filtered, sortBy, sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new PagedResponse<Product>(items, total, page, pageSize));
    }

    /// <summary>Whitelisted sort keys for the product list; unknown values fall back to the id.</summary>
    private static IOrderedEnumerable<Product> ApplySort(IEnumerable<Product> products, string? sortBy, bool desc)
    {
        IComparer<Product> comparer = (sortBy ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "name" => Comparer<Product>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)),
            "sku" => Comparer<Product>.Create((a, b) => string.Compare(a.Sku, b.Sku, StringComparison.OrdinalIgnoreCase)),
            "category" => Comparer<Product>.Create((a, b) => string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase)),
            "price" => Comparer<Product>.Create((a, b) => a.Price.CompareTo(b.Price)),
            "stock" => Comparer<Product>.Create((a, b) => a.Stock.CompareTo(b.Stock)),
            "sold" => Comparer<Product>.Create((a, b) => a.Sold.CompareTo(b.Sold)),
            "createdat" => Comparer<Product>.Create((a, b) => a.CreatedAt.CompareTo(b.CreatedAt)),
            _ => Comparer<Product>.Create((a, b) => a.Id.CompareTo(b.Id)),
        };

        return desc ? products.OrderByDescending(p => p, comparer) : products.OrderBy(p => p, comparer);
    }

    /// <summary>The caller's own product listings.</summary>
    public ActionResult<IEnumerable<Product>> GetMine(ClaimsPrincipal caller)
    {
        var userId = Access.UserId(caller);
        return Ok(products.Catalog().AsEnumerable()
            .Where(p => p.SellerId == userId)
            .OrderBy(p => p.Id)
            .ToList());
    }

    /// <summary>Gets a single product by its identifier.</summary>
    public ActionResult<Product> GetById(int id)
    {
        var product = products.Catalog().FirstOrDefault(p => p.Id == id);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>Managed product categories, used to populate the table filter and the forms.</summary>
    /// <remarks>Served from the category list (kept in sync with listings at startup), so a
    /// category added from the dashboard appears even before any product carries it.</remarks>
    public ActionResult<IEnumerable<string>> GetCategories() =>
        Ok(categories.QueryReadOnly()
            .Where(c => c.Kind == Category.ProductKind)
            .AsEnumerable()
            .Select(c => c.Name)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList());

    /// <summary>Creates a product.</summary>
    public ActionResult<Product> Create(ClaimsPrincipal caller, ProductInput input)
    {
        if (!Access.CanList(caller))
        {
            return Forbid();
        }

        var created = new Product
        {
            Id = products.QueryReadOnly().Any() ? products.QueryReadOnly().Max(p => p.Id) + 1 : 1,
            Name = input.Name.Trim(),
            Sku = input.Sku.Trim(),
            Category = input.Category.Trim(),
            Price = input.Price,
            Stock = input.Stock,
            CreatedAt = DateTime.UtcNow,
            SellerId = Access.UserId(caller),
        };

        products.Add(created);
        products.SaveChanges();

        // A category typed straight into the form joins the managed list immediately.
        CategorySync.EnsureExists(categories, created.Category, Category.ProductKind);

        return CreatedAtAction("GetById", new { id = created.Id }, created);
    }

    /// <summary>Uploads one or more images into the product's gallery (stored under <c>wwwroot/images</c>).</summary>
    public async Task<ActionResult<List<ListingImage>>> UploadImages(ClaimsPrincipal caller, int id, List<IFormFile>? files)
    {
        var product = products.Query().FirstOrDefault(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        if (!Access.IsAdmin(caller) && product.SellerId != Access.UserId(caller))
        {
            return Forbid();
        }

        if (files is null || files.Count == 0)
        {
            return Problem(
                title: "No image uploaded",
                detail: "Attach at least one file in the 'files' form field.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Validate everything before touching disk so a bad file never leaves half a gallery.
        foreach (var file in files)
        {
            var error = ImageUpload.Validate(file);
            if (error is not null)
            {
                return Problem(title: "Invalid image", detail: error, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        var sortOrder = listingImages.QueryReadOnly().Count(i => i.ProductId == id);
        if (sortOrder + files.Count > ImageUpload.MaxPerListing)
        {
            return Problem(
                title: "Gallery full",
                detail: $"A listing can hold at most {ImageUpload.MaxPerListing} images.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var nextId = listingImages.QueryReadOnly().Any() ? listingImages.QueryReadOnly().Max(i => i.Id) + 1 : 1;
        var created = new List<ListingImage>(files.Count);
        foreach (var file in files)
        {
            var path = await images.SaveAsync(file, "products");
            created.Add(new ListingImage
            {
                Id = nextId++,
                ProductId = id,
                Path = path,
                SortOrder = sortOrder++,
                CreatedAt = DateTime.UtcNow,
            });
        }

        listingImages.AddRange(created);
        products.SaveChanges();

        return Created($"/api/products/{id}/images", created);
    }

    /// <summary>Removes one image from the product's gallery and deletes its file.</summary>
    public IActionResult DeleteImage(ClaimsPrincipal caller, int id, int imageId)
    {
        var product = products.Query().FirstOrDefault(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        if (!Access.IsAdmin(caller) && product.SellerId != Access.UserId(caller))
        {
            return Forbid();
        }

        var image = listingImages.Query().FirstOrDefault(i => i.Id == imageId && i.ProductId == id);
        if (image is null)
        {
            return NotFound();
        }

        listingImages.Remove(image);
        products.SaveChanges();
        images.Delete(image.Path);
        return NoContent();
    }

    /// <summary>Updates an existing product.</summary>
    public ActionResult<Product> Update(ClaimsPrincipal caller, int id, ProductInput input)
    {
        var product = products.Query().FirstOrDefault(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        if (!Access.IsAdmin(caller) && product.SellerId != Access.UserId(caller))
        {
            return Forbid();
        }

        product.Name = input.Name.Trim();
        product.Sku = input.Sku.Trim();
        product.Category = input.Category.Trim();
        product.Price = input.Price;
        product.Stock = input.Stock;
        products.SaveChanges();
        CategorySync.EnsureExists(categories, product.Category, Category.ProductKind);

        return Ok(product);
    }

    /// <summary>Deletes a product.</summary>
    public IActionResult Delete(ClaimsPrincipal caller, int id)
    {
        var product = products.Query().FirstOrDefault(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        if (!Access.IsAdmin(caller) && product.SellerId != Access.UserId(caller))
        {
            return Forbid();
        }

        // Drop the gallery rows and their files before the listing disappears.
        var productImages = listingImages.Query().Where(i => i.ProductId == id).ToList();
        foreach (var image in productImages)
        {
            images.Delete(image.Path);
        }
        listingImages.RemoveRange(productImages);

        products.Remove(product);
        products.SaveChanges();
        return NoContent();
    }
}
