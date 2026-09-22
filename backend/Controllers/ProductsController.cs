using MarketWorkplace.Api.Data;
using MarketWorkplace.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Api.Controllers;

/// <summary>Product catalogue CRUD backing the dashboard's Products page.</summary>
[ApiController]
[Route("api/products")]
[Tags("Products")]
[Produces("application/json")]
public class ProductsController(InMemoryStore store) : ControllerBase
{
    /// <summary>Lists products, optionally filtered by search text and category.</summary>
    /// <param name="search">Case-insensitive match against name, SKU or category.</param>
    /// <param name="category">Exact category match, e.g. <c>Audio</c>.</param>
    /// <returns>The products matching the filters (all products when no filter is supplied).</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Product>> GetAll([FromQuery] string? search, [FromQuery] string? category)
    {
        IEnumerable<Product> products = store.GetProducts();

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

        return Ok(products.ToList());
    }

    /// <summary>Gets a single product by its identifier.</summary>
    /// <param name="id">The product identifier.</param>
    /// <returns>The product, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Product> GetById(int id)
    {
        var product = store.GetProduct(id);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>Distinct categories across the catalogue, used to populate the table filter.</summary>
    /// <returns>Category names in alphabetical order.</returns>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetCategories() =>
        Ok(store.GetProducts().Select(p => p.Category).Distinct().OrderBy(c => c));

    /// <summary>Creates a product.</summary>
    /// <param name="input">The product to create; required fields are enforced by data annotations.</param>
    /// <returns>The created product with its generated identifier.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<Product> Create([FromBody] ProductInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var created = store.CreateProduct(new Product
        {
            Name = input.Name.Trim(),
            Sku = input.Sku.Trim(),
            Category = input.Category.Trim(),
            Price = input.Price,
            Stock = input.Stock,
        });

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

        var updated = store.UpdateProduct(id, new Product
        {
            Name = input.Name.Trim(),
            Sku = input.Sku.Trim(),
            Category = input.Category.Trim(),
            Price = input.Price,
            Stock = input.Stock,
        });

        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Deletes a product.</summary>
    /// <param name="id">The identifier of the product to delete.</param>
    /// <returns><c>204 No Content</c> on success, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id) => store.DeleteProduct(id) ? NoContent() : NotFound();
}
