using MarketWorkplace.Application.Common;
using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Services;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Api.Controllers;

/// <summary>Service listings (cost, contact info, location, offers) that clients can reserve.</summary>
/// <remarks>
/// Requires a valid bearer token. Creating and editing services is limited to mobile
/// <c>Provider</c> users (service provider / seller) and dashboard admins; everyone else
/// browses and reserves. See <see cref="Access"/> for the full rules.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/services")]
[Tags("Services")]
[Produces("application/json")]
public class ServicesController(ServicesService servicesService) : ControllerBase
{
    /// <summary>Lists one page of services with optional filters and sorting.</summary>
    /// <param name="search">Case-insensitive match against title, description, category or location.</param>
    /// <param name="category">Exact category match, e.g. <c>Repair</c>.</param>
    /// <param name="providerId">Only services offered by this user.</param>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="pageSize">Rows per page, clamped to 1–100 (default 20).</param>
    /// <param name="sortBy">Sort column: <c>id</c>, <c>title</c>, <c>category</c>, <c>cost</c>, <c>providerId</c>, <c>location</c> or <c>createdAt</c>; unknown values sort by id.</param>
    /// <param name="sortDir"><c>asc</c> (default) or <c>desc</c>.</param>
    /// <returns>One page of services plus the filtered total for the paginator.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<Service>), StatusCodes.Status200OK)]
    public ActionResult<PagedResponse<Service>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int? providerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDir = "asc") =>
        servicesService.GetAll(search, category, providerId, page, pageSize, sortBy, sortDir);

    /// <summary>Gets a single service by its identifier.</summary>
    /// <param name="id">The service identifier.</param>
    /// <returns>The service, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Service), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Service> GetById(int id) => servicesService.GetById(id);

    /// <summary>Distinct categories across all services, used to populate filter dropdowns.</summary>
    /// <returns>Category names in alphabetical order.</returns>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetCategories() => servicesService.GetCategories();

    /// <summary>The caller's own service listings.</summary>
    /// <returns>Services offered by the signed-in user.</returns>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IEnumerable<Service>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Service>> GetMine() => servicesService.GetMine(User);

    /// <summary>Creates a service listing.</summary>
    /// <param name="input">The service to create; required fields are enforced by data annotations.</param>
    /// <returns>The created service with its generated identifier.</returns>
    /// <response code="201">Service created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">The caller may not create listings.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Service), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<Service> Create([FromBody] ServiceInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return servicesService.Create(User, input);
    }

    /// <summary>Uploads one or more images into the service's gallery (stored under <c>wwwroot/images</c>).</summary>
    /// <param name="id">The service identifier.</param>
    /// <param name="files">Multipart <c>files</c> field; png/jpg/webp/gif up to 5 MB each.</param>
    /// <returns>The created image records with their web paths.</returns>
    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(List<ListingImage>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ListingImage>>> UploadImages(int id, [FromForm] List<IFormFile>? files) =>
        await servicesService.UploadImages(User, id, files);

    /// <summary>Removes one image from the service's gallery and deletes its file.</summary>
    /// <param name="id">The service identifier.</param>
    /// <param name="imageId">The image identifier.</param>
    /// <returns><c>204 No Content</c>, or <c>404</c> when the image is not part of this service.</returns>
    [HttpDelete("{id:int}/images/{imageId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteImage(int id, int imageId) => servicesService.DeleteImage(User, id, imageId);

    /// <summary>Updates an existing service listing.</summary>
    /// <param name="id">The identifier of the service to update.</param>
    /// <param name="input">The new field values.</param>
    /// <returns>The updated service, <c>404</c> when missing, or <c>400</c> when validation fails.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Service), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<Service> Update(int id, [FromBody] ServiceInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return servicesService.Update(User, id, input);
    }

    /// <summary>Deletes a service listing.</summary>
    /// <param name="id">The identifier of the service to delete.</param>
    /// <returns><c>204 No Content</c> on success, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id) => servicesService.Delete(User, id);
}
