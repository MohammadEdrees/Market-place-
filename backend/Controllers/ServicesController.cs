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
public class ServicesController(MarketDbContext db) : ControllerBase
{
    /// <summary>Lists services, optionally filtered by text, category or provider.</summary>
    /// <param name="search">Case-insensitive match against title, description, category or location.</param>
    /// <param name="category">Exact category match, e.g. <c>Repair</c>.</param>
    /// <param name="providerId">Only services offered by this user.</param>
    /// <returns>The services matching the filters.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Service>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Service>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int? providerId)
    {
        IEnumerable<Service> services = db.Services.AsNoTracking().AsEnumerable().OrderBy(s => s.Id);

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

        return Ok(services.ToList());
    }

    /// <summary>Gets a single service by its identifier.</summary>
    /// <param name="id">The service identifier.</param>
    /// <returns>The service, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Service), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Service> GetById(int id)
    {
        var service = db.Services.AsNoTracking().FirstOrDefault(s => s.Id == id);
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
        return Ok(db.Services.AsNoTracking().AsEnumerable()
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

        db.Services.Remove(service);
        db.SaveChanges();
        return NoContent();
    }
}
