using MarketWorkplace.Api.Data;
using MarketWorkplace.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(InMemoryStore store) : ControllerBase
{
    /// <summary>List products, optionally filtered by search text and category.</summary>
    [HttpGet]
    public ActionResult<IEnumerable<Product>> GetAll([FromQuery] string? search, [FromQuery] string? category)
    {
        IEnumerable<Product> products = store.GetProducts();

        if (!string.IsNullOrWhiteSpace(search))
        {
            products = products.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            products = products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        return Ok(products.ToList());
    }

    [HttpGet("{id:int}")]
    public ActionResult<Product> GetById(int id)
    {
        var product = store.GetProduct(id);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>Distinct categories, used to populate the table filter.</summary>
    [HttpGet("categories")]
    public ActionResult<IEnumerable<string>> GetCategories() =>
        Ok(store.GetProducts().Select(p => p.Category).Distinct().OrderBy(c => c));

    [HttpPost]
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

    [HttpPut("{id:int}")]
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

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id) => store.DeleteProduct(id) ? NoContent() : NotFound();
}
