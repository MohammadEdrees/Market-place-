using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Services;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Api.Controllers;

/// <summary>Product catalogue CRUD backing the dashboard's Products page.</summary>
/// <remarks>Requires a valid bearer token (<c>POST /api/auth/login</c>).</remarks>
[ApiController]
[Authorize]
[Route("api/products")]
[Tags("Products")]
[Produces("application/json")]
public class ProductsController(ProductsService productsService) : ControllerBase
{
    /// <summary>Lists one page of products with optional filters and sorting.</summary>
    /// <param name="search">Case-insensitive match against name, SKU or category.</param>
    /// <param name="category">Exact category match, e.g. <c>Audio</c>.</param>
    /// <param name="sellerId">Only products owned by this user.</param>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="pageSize">Rows per page, clamped to 1–100 (default 20).</param>
    /// <param name="sortBy">Sort column: <c>id</c>, <c>name</c>, <c>sku</c>, <c>category</c>, <c>price</c>, <c>stock</c>, <c>sold</c> or <c>createdAt</c>; unknown values sort by id.</param>
    /// <param name="sortDir"><c>asc</c> (default) or <c>desc</c>.</param>
    /// <returns>One page of products plus the filtered total for the paginator.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<Product>), StatusCodes.Status200OK)]
    public ActionResult<PagedResponse<Product>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int? sellerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDir = "asc") =>
        productsService.GetAll(search, category, sellerId, page, pageSize, sortBy, sortDir);

    /// <summary>The caller's own product listings.</summary>
    /// <returns>Products owned by the signed-in user (admins see their own too).</returns>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Product>> GetMine() => productsService.GetMine(User);

    /// <summary>Gets a single product by its identifier.</summary>
    /// <param name="id">The product identifier.</param>
    /// <returns>The product, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Product> GetById(int id) => productsService.GetById(id);

    /// <summary>Managed product category names, used to populate the table filter.</summary>
    /// <returns>Category names in alphabetical order (from the managed category list).</returns>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetCategories() => productsService.GetCategories();

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

        return productsService.Create(User, input);
    }

    /// <summary>Uploads one or more images into the product's gallery (stored under <c>wwwroot/images</c>).</summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="files">Multipart <c>files</c> field; png/jpg/webp/gif up to 5 MB each.</param>
    /// <returns>The created image records with their web paths.</returns>
    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(List<ListingImage>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ListingImage>>> UploadImages(int id, [FromForm] List<IFormFile>? files) =>
        await productsService.UploadImages(User, id, files);

    /// <summary>Removes one image from the product's gallery and deletes its file.</summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="imageId">The image identifier.</param>
    /// <returns><c>204 No Content</c>, or <c>404</c> when the image is not part of this product.</returns>
    [HttpDelete("{id:int}/images/{imageId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteImage(int id, int imageId) => productsService.DeleteImage(User, id, imageId);

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

        return productsService.Update(User, id, input);
    }

    /// <summary>Deletes a product.</summary>
    /// <param name="id">The identifier of the product to delete.</param>
    /// <returns><c>204 No Content</c> on success, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id) => productsService.Delete(User, id);
}
