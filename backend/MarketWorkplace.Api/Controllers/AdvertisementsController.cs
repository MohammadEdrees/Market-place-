using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Services;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Api.Controllers;

/// <summary>Advertisement slides backing the dashboard's Advertisements page and the mobile home slider.</summary>
/// <remarks>Reads need a valid bearer token; writes are limited to dashboard admins.</remarks>
[ApiController]
[Authorize]
[Route("api/advertisements")]
[Tags("Advertisements")]
[Produces("application/json")]
public class AdvertisementsController(AdvertisementsService advertisementsService) : ControllerBase
{
    /// <summary>Every slide in display order — the dashboard's Advertisements table.</summary>
    /// <returns>All advertisements, including disabled and scheduled ones.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Advertisement>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Advertisement>> GetAll() => advertisementsService.GetAll();

    /// <summary>The slides currently visible in the mobile slider (enabled and inside their schedule window).</summary>
    /// <returns>Active advertisements in display order.</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<Advertisement>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Advertisement>> GetActive() => advertisementsService.GetActive();

    /// <summary>Gets a single advertisement by its identifier.</summary>
    /// <param name="id">The advertisement identifier.</param>
    /// <returns>The advertisement, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Advertisement), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Advertisement> GetById(int id) => advertisementsService.GetById(id);

    /// <summary>Creates an advertisement without an image (admin callers only).</summary>
    /// <param name="input">Title, optional subtitle/target and the schedule window.</param>
    /// <returns>The created advertisement; upload its image via <c>POST /api/advertisements/{id}/image</c>.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Advertisement), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<Advertisement> Create([FromBody] AdvertisementInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return advertisementsService.Create(User, input);
    }

    /// <summary>Updates an advertisement (admin callers only); the image is managed separately.</summary>
    /// <param name="id">The identifier of the advertisement to update.</param>
    /// <param name="input">The new field values.</param>
    /// <returns>The updated advertisement, <c>404</c> when missing, or <c>400</c> when validation fails.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Advertisement), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<Advertisement> Update(int id, [FromBody] AdvertisementInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return advertisementsService.Update(User, id, input);
    }

    /// <summary>Deletes an advertisement and its image file (admin callers only).</summary>
    /// <param name="id">The advertisement identifier.</param>
    /// <returns><c>204 No Content</c> on success, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id) => advertisementsService.Delete(User, id);

    /// <summary>Uploads (or replaces) the advertisement's banner image (stored under <c>wwwroot/images/ads</c>).</summary>
    /// <param name="id">The advertisement identifier.</param>
    /// <param name="file">Multipart <c>file</c> field; png/jpg/webp/gif up to 5 MB.
    /// No <c>[FromForm]</c> attribute — ApiExplorer infers <c>BindingSource.FormFile</c> from
    /// <see cref="IFormFile"/> itself (the attribute would break Swashbuckle's form description).</param>
    /// <returns>The updated advertisement with its new image URL.</returns>
    [HttpPost("{id:int}/image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(Advertisement), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Advertisement>> UploadImage(int id, IFormFile? file) =>
        await advertisementsService.UploadImage(User, id, file);
}
