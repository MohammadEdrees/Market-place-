using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Api.Controllers;

/// <summary>Managed category list backing the dashboard's Categories page and every filter chip.</summary>
/// <remarks>Reads need a valid bearer token; writes are limited to dashboard admins.</remarks>
[ApiController]
[Authorize]
[Route("api/categories")]
[Tags("Categories")]
[Produces("application/json")]
public class CategoriesController(CategoriesService categoriesService) : ControllerBase
{
    /// <summary>All categories with their live listing counts, ordered Product kind first then name.</summary>
    /// <param name="kind">Optional filter: <c>Product</c> or <c>Service</c>.</param>
    /// <returns>The managed categories in display order.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<CategoryDto>> GetAll([FromQuery] string? kind) =>
        categoriesService.GetAll(kind);

    /// <summary>Gets a single category by its identifier.</summary>
    /// <param name="id">The category identifier.</param>
    /// <returns>The category, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CategoryDto> GetById(int id) => categoriesService.GetById(id);

    /// <summary>Creates a category (admin callers only).</summary>
    /// <param name="input">The category name and the catalogue kind it belongs to.</param>
    /// <returns>The created category, <c>403</c> for non-admins, or <c>409</c> when the name is taken.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<CategoryDto> Create([FromBody] CategoryCreateInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return categoriesService.Create(User, input);
    }

    /// <summary>Renames a category (admin callers only); the new name cascades to existing listings.</summary>
    /// <param name="id">The identifier of the category to rename.</param>
    /// <param name="input">The new name (the kind never changes).</param>
    /// <returns>The updated category, <c>404</c> when missing, or <c>409</c> when the name is taken.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<CategoryDto> Update(int id, [FromBody] CategoryUpdateInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return categoriesService.Update(User, id, input);
    }

    /// <summary>Deletes a category no listing uses (admin callers only).</summary>
    /// <param name="id">The category identifier.</param>
    /// <returns><c>204 No Content</c>, <c>404</c> when missing, or <c>409</c> while listings still use it.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Delete(int id) => categoriesService.Delete(User, id);
}
