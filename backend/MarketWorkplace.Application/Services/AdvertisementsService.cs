using System.Security.Claims;
using MarketWorkplace.Application.Common;
using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static MarketWorkplace.Application.Common.ServiceResults;

namespace MarketWorkplace.Application.Services;

/// <summary>Advertisement slides CRUD (backs <c>AdvertisementsController</c>) for the mobile home slider.</summary>
public class AdvertisementsService(IRepository<Advertisement> advertisements, IImageStore images)
{
    /// <summary>Every slide in display order — the dashboard's Advertisements table.</summary>
    public ActionResult<IEnumerable<Advertisement>> GetAll() =>
        Ok(advertisements.QueryReadOnly().AsEnumerable()
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Id)
            .ToList());

    /// <summary>The slides currently shown in the mobile slider: enabled and inside their schedule window.</summary>
    public ActionResult<IEnumerable<Advertisement>> GetActive()
    {
        var now = DateTime.UtcNow;
        return Ok(advertisements.QueryReadOnly()
            .Where(a => a.IsActive &&
                        (a.StartsAt == null || a.StartsAt <= now) &&
                        (a.EndsAt == null || a.EndsAt > now))
            .AsEnumerable()
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Id)
            .ToList());
    }

    /// <summary>One slide by identifier, or <c>404</c>.</summary>
    public ActionResult<Advertisement> GetById(int id)
    {
        var advertisement = advertisements.QueryReadOnly().FirstOrDefault(a => a.Id == id);
        return advertisement is null ? NotFound() : Ok(advertisement);
    }

    /// <summary>Creates a slide (admin callers only).</summary>
    public ActionResult<Advertisement> Create(ClaimsPrincipal caller, AdvertisementInput input)
    {
        if (!Access.IsAdmin(caller))
        {
            return Forbid();
        }

        var error = Validate(input);
        if (error is not null)
        {
            return error;
        }

        var advertisement = new Advertisement
        {
            Id = advertisements.QueryReadOnly().Any() ? advertisements.QueryReadOnly().Max(a => a.Id) + 1 : 1,
            Title = input.Title.Trim(),
            Subtitle = NullIfBlank(input.Subtitle),
            TargetUrl = NullIfBlank(input.TargetUrl),
            IsActive = input.IsActive,
            StartsAt = input.StartsAt,
            EndsAt = input.EndsAt,
            SortOrder = input.SortOrder,
            CreatedAt = DateTime.UtcNow,
        };

        advertisements.Add(advertisement);
        advertisements.SaveChanges();

        return Created($"/api/advertisements/{advertisement.Id}", advertisement);
    }

    /// <summary>Updates a slide (admin callers only); the image is managed through the upload endpoint.</summary>
    public ActionResult<Advertisement> Update(ClaimsPrincipal caller, int id, AdvertisementInput input)
    {
        if (!Access.IsAdmin(caller)) return Forbid();

        var advertisement = advertisements.Query().FirstOrDefault(a => a.Id == id);
        if (advertisement is null) return NotFound();

        var error = Validate(input);
        if (error is not null)
        {
            return error;
        }

        advertisement.Title = input.Title.Trim();
        advertisement.Subtitle = NullIfBlank(input.Subtitle);
        advertisement.TargetUrl = NullIfBlank(input.TargetUrl);
        advertisement.IsActive = input.IsActive;
        advertisement.StartsAt = input.StartsAt;
        advertisement.EndsAt = input.EndsAt;
        advertisement.SortOrder = input.SortOrder;
        advertisements.SaveChanges();

        return Ok(advertisement);
    }

    /// <summary>Deletes a slide and its image file (admin callers only).</summary>
    public IActionResult Delete(ClaimsPrincipal caller, int id)
    {
        if (!Access.IsAdmin(caller)) return Forbid();

        var advertisement = advertisements.Query().FirstOrDefault(a => a.Id == id);
        if (advertisement is null) return NotFound();

        if (advertisement.ImagePath is not null)
        {
            images.Delete(advertisement.ImagePath);
        }

        advertisements.Remove(advertisement);
        advertisements.SaveChanges();
        return NoContent();
    }

    /// <summary>Uploads (or replaces) the slide's image, stored under <c>wwwroot/images/ads</c>.</summary>
    /// <remarks>The previous image file is removed so orphaned banners never pile up.</remarks>
    public async Task<ActionResult<Advertisement>> UploadImage(ClaimsPrincipal caller, int id, IFormFile? file)
    {
        if (!Access.IsAdmin(caller)) return Forbid();

        var advertisement = advertisements.Query().FirstOrDefault(a => a.Id == id);
        if (advertisement is null) return NotFound();

        if (file is null || file.Length == 0)
        {
            return Problem(title: "No image uploaded", detail: "Attach one file in the 'file' form field.", statusCode: StatusCodes.Status400BadRequest);
        }

        var validationError = ImageUpload.Validate(file);
        if (validationError is not null)
        {
            return Problem(title: "Invalid image", detail: validationError, statusCode: StatusCodes.Status400BadRequest);
        }

        if (advertisement.ImagePath is not null)
        {
            images.Delete(advertisement.ImagePath);
        }

        advertisement.ImagePath = await images.SaveAsync(file, "ads");
        advertisements.SaveChanges();

        return Ok(advertisement);
    }

    /// <summary>Title and schedule sanity shared by create and update.</summary>
    private static ObjectResult? Validate(AdvertisementInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            return Problem(title: "Invalid advertisement", detail: "Title is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (input.StartsAt is not null && input.EndsAt is not null && input.EndsAt <= input.StartsAt)
        {
            return Problem(title: "Invalid schedule", detail: "The end date must be after the start date.", statusCode: StatusCodes.Status400BadRequest);
        }

        return null;
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
