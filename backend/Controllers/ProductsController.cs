using MarketWorkplace.Api.Auth;
using MarketWorkplace.Api.Data;
using MarketWorkplace.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketWorkplace.Api.Controllers;

/// <summary>Product catalogue CRUD backing the dashboard's Products page.</summary>
/// <remarks>Requires a valid bearer token (<c>POST /api/auth/login</c>).</remarks>
[ApiController]
[Authorize]
[Route("api/products")]
[Tags("Products")]
[Produces("application/json")]
public class ProductsController(MarketDbContext db) : ControllerBase
{
    /// <summary>Lists products, optionally filtered by search text and category.</summary>
    /// <param name="search">Case-insensitive match against name, SKU or category.</param>
    /// <param name="category">Exact category match, e.g. <c>Audio</c>.</param>
    /// <param name="sellerId">Only products owned by this user.</param>
    /// <returns>The products matching the filters (all products when no filter is supplied).</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Product>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int? sellerId)
    {
        IEnumerable<Product> products = db.Products.AsNoTracking().AsEnumerable().OrderBy(p => p.Id);

        if (!string.IsNullOrWhiteSpace(search))
        {
            products = products.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            products = products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        if (sellerId is not null)
        {
            products = products.Where(p => p.SellerId == sellerId);
        }

        return Ok(products.ToList());
    }

    /// <summary>The caller's own product listings.</summary>
    /// <returns>Products owned by the signed-in user (admins see their own too).</returns>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Product>> GetMine()
    {
        var userId = Access.UserId(User);
        return Ok(db.Products.AsNoTracking().AsEnumerable()
            .Where(p => p.SellerId == userId)
            .OrderBy(p => p.Id)
            .ToList());
    }

    /// <summary>Gets a single product by its identifier.</summary>
    /// <param name="id">The product identifier.</param>
    /// <returns>The product, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Product> GetById(int id)
    {
        var product = db.Products.AsNoTracking().FirstOrDefault(p => p.Id == id);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>Distinct categories across the catalogue, used to populate the table filter.</summary>
    /// <returns>Category names in alphabetical order.</returns>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetCategories() =>
        Ok(db.Products.AsNoTracking().AsEnumerable().Select(p => p.Category).Distinct().OrderBy(c => c));

    /// <summary>Creates a product.</summary>
    /// <param name="input">The product to create; required fields are enforced by data annotations.</param>
    /// <returns>The created product with its generated identifier.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<Product> Create([FromBody] ProductInput input)
    {
        if (!Access.CanList(User))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var created = new Product
        {
            Id = db.Products.Any() ? db.Products.Max(p => p.Id) + 1 : 1,
            Name = input.Name.Trim(),
            Sku = input.Sku.Trim(),
            Category = input.Category.Trim(),
            Price = input.Price,
            Stock = input.Stock,
            CreatedAt = DateTime.UtcNow,
            SellerId = Access.UserId(User),
        };

        db.Products.Add(created);
        db.SaveChanges();

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing product.</summary>
    /// <param name="id">The identifier of the product to update.</param>
    /// <param name="input">The new field values.</param>
    /// <returns>The updated product, <c>404</c> when missing, or <c>400</c> when validation fails.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<Product> Update(int id, [FromBody] ProductInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var product = db.Products.FirstOrDefault(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        if (!Access.IsAdmin(User) && product.SellerId != Access.UserId(User))
        {
            return Forbid();
        }

        product.Name = input.Name.Trim();
        product.Sku = input.Sku.Trim();
        product.Category = input.Category.Trim();
        product.Price = input.Price;
        product.Stock = input.Stock;
        db.SaveChanges();

        return Ok(product);
    }

    /// <summary>Deletes a product.</summary>
    /// <param name="id">The identifier of the product to delete.</param>
    /// <returns><c>204 No Content</c> on success, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id)
    {
        var product = db.Products.FirstOrDefault(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        if (!Access.IsAdmin(User) && product.SellerId != Access.UserId(User))
        {
            return Forbid();
        }

        db.Products.Remove(product);
        db.SaveChanges();
        return NoContent();
    }
}
