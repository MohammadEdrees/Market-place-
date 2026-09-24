using MarketWorkplace.Api.Auth;
using MarketWorkplace.Api.Data;
using MarketWorkplace.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
public class ServicesController(MarketDbContext db, ImageStore store) : ControllerBase
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
        [FromQuery] string sortDir = "asc")
    {
        page = Paging.NormalizePage(page);
        pageSize = Paging.NormalizePageSize(pageSize);

        IEnumerable<Service> services = db.Services.AsNoTracking().Include(s => s.Images).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            services = services.Where(s =>
                s.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Location.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            services = services.Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        if (providerId is not null)
        {
            services = services.Where(s => s.ProviderId == providerId);
        }

        var total = services.Count();
        var items = ApplySort(services, sortBy, sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new PagedResponse<Service>(items, total, page, pageSize));
    }

    /// <summary>Whitelisted sort keys for the service list; unknown values fall back to the id.</summary>
    private static IOrderedEnumerable<Service> ApplySort(IEnumerable<Service> services, string? sortBy, bool desc)
    {
        IComparer<Service> comparer = (sortBy ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "title" => Comparer<Service>.Create((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase)),
            "category" => Comparer<Service>.Create((a, b) => string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase)),
            "cost" => Comparer<Service>.Create((a, b) => a.Cost.CompareTo(b.Cost)),
            "providerid" => Comparer<Service>.Create((a, b) => a.ProviderId.CompareTo(b.ProviderId)),
            "location" => Comparer<Service>.Create((a, b) => string.Compare(a.Location, b.Location, StringComparison.OrdinalIgnoreCase)),
            "createdat" => Comparer<Service>.Create((a, b) => a.CreatedAt.CompareTo(b.CreatedAt)),
            _ => Comparer<Service>.Create((a, b) => a.Id.CompareTo(b.Id)),
        };

        return desc ? services.OrderByDescending(s => s, comparer) : services.OrderBy(s => s, comparer);
    }

    /// <summary>Gets a single service by its identifier.</summary>
    /// <param name="id">The service identifier.</param>
    /// <returns>The service, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Service), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Service> GetById(int id)
    {
        var service = db.Services.AsNoTracking().Include(s => s.Images).FirstOrDefault(s => s.Id == id);
        return service is null ? NotFound() : Ok(service);
    }

    /// <summary>Distinct categories across all services, used to populate filter dropdowns.</summary>
    /// <returns>Category names in alphabetical order.</returns>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetCategories() =>
        Ok(db.Services.AsNoTracking().AsEnumerable().Select(s => s.Category).Distinct().OrderBy(c => c));

    /// <summary>The caller's own service listings.</summary>
    /// <returns>Services offered by the signed-in user.</returns>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IEnumerable<Service>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Service>> GetMine()
    {
        var userId = Access.UserId(User);
        return Ok(db.Services.AsNoTracking().Include(s => s.Images).AsEnumerable()
            .Where(s => s.ProviderId == userId)
            .OrderBy(s => s.Id)
            .ToList());
    }

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
        if (!Access.CanList(User))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var created = new Service
        {
            Id = db.Services.Any() ? db.Services.Max(s => s.Id) + 1 : 1,
            ProviderId = Access.UserId(User) ?? 0,
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            Category = input.Category.Trim(),
            Cost = input.Cost,
            ContactInfo = input.ContactInfo.Trim(),
            Location = input.Location.Trim(),
            Offers = input.Offers.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        db.Services.Add(created);
        db.SaveChanges();

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
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
    public async Task<ActionResult<List<ListingImage>>> UploadImages(int id, [FromForm] List<IFormFile>? files)
    {
        var service = db.Services.FirstOrDefault(s => s.Id == id);
        if (service is null)
        {
            return NotFound();
        }

        if (!Access.IsAdmin(User) && service.ProviderId != Access.UserId(User))
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

        foreach (var file in files)
        {
            var error = ImageStore.Validate(file);
            if (error is not null)
            {
                return Problem(title: "Invalid image", detail: error, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        var sortOrder = db.Images.Count(i => i.ServiceId == id);
        if (sortOrder + files.Count > ImageStore.MaxPerListing)
        {
            return Problem(
                title: "Gallery full",
                detail: $"A listing can hold at most {ImageStore.MaxPerListing} images.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var nextId = db.Images.Any() ? db.Images.Max(i => i.Id) + 1 : 1;
        var created = new List<ListingImage>(files.Count);
        foreach (var file in files)
        {
            var path = await store.SaveAsync(file, "services");
            created.Add(new ListingImage
            {
                Id = nextId++,
                ServiceId = id,
                Path = path,
                SortOrder = sortOrder++,
                CreatedAt = DateTime.UtcNow,
            });
        }

        db.Images.AddRange(created);
        db.SaveChanges();

        return Created($"/api/services/{id}/images", created);
    }

    /// <summary>Removes one image from the service's gallery and deletes its file.</summary>
    /// <param name="id">The service identifier.</param>
    /// <param name="imageId">The image identifier.</param>
    /// <returns><c>204 No Content</c>, or <c>404</c> when the image is not part of this service.</returns>
    [HttpDelete("{id:int}/images/{imageId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteImage(int id, int imageId)
    {
        var service = db.Services.FirstOrDefault(s => s.Id == id);
        if (service is null)
        {
            return NotFound();
        }

        if (!Access.IsAdmin(User) && service.ProviderId != Access.UserId(User))
        {
            return Forbid();
        }

        var image = db.Images.FirstOrDefault(i => i.Id == imageId && i.ServiceId == id);
        if (image is null)
        {
            return NotFound();
        }

        db.Images.Remove(image);
        db.SaveChanges();
        store.Delete(image.Path);
        return NoContent();
    }

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

        var service = db.Services.FirstOrDefault(s => s.Id == id);
        if (service is null)
        {
            return NotFound();
        }

        if (!Access.IsAdmin(User) && service.ProviderId != Access.UserId(User))
        {
            return Forbid();
        }

        service.Title = input.Title.Trim();
        service.Description = input.Description.Trim();
        service.Category = input.Category.Trim();
        service.Cost = input.Cost;
        service.ContactInfo = input.ContactInfo.Trim();
        service.Location = input.Location.Trim();
        service.Offers = input.Offers.Trim();
        db.SaveChanges();

        return Ok(service);
    }

    /// <summary>Deletes a service listing.</summary>
    /// <param name="id">The identifier of the service to delete.</param>
    /// <returns><c>204 No Content</c> on success, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id)
    {
        var service = db.Services.FirstOrDefault(s => s.Id == id);
        if (service is null)
        {
            return NotFound();
        }

        if (!Access.IsAdmin(User) && service.ProviderId != Access.UserId(User))
        {
            return Forbid();
        }

        // Drop the gallery rows and their files before the listing disappears.
        var images = db.Images.Where(i => i.ServiceId == id).ToList();
        foreach (var image in images)
        {
            store.Delete(image.Path);
        }
        db.Images.RemoveRange(images);

        db.Services.Remove(service);
        db.SaveChanges();
        return NoContent();
    }
}
