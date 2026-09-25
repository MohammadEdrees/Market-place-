using System.Security.Claims;
using MarketWorkplace.Application.Common;
using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static MarketWorkplace.Application.Common.ServiceResults;

namespace MarketWorkplace.Application.Services;

/// <summary>Service listing CRUD (backs <c>ServicesController</c>).</summary>
public class ServicesService(IServiceRepository serviceList, IImageStore images, IRepository<ListingImage> listingImages, IRepository<Category> categories)
{
    /// <summary>Lists one page of services with optional filters and sorting.</summary>
    public ActionResult<PagedResponse<Service>> GetAll(
        string? search,
        string? category,
        int? providerId,
        int page = 1,
        int pageSize = Paging.DefaultPageSize,
        string? sortBy = null,
        string sortDir = "asc")
    {
        page = Paging.NormalizePage(page);
        pageSize = Paging.NormalizePageSize(pageSize);

        IEnumerable<Service> services = serviceList.Catalog().AsEnumerable();

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
    public ActionResult<Service> GetById(int id)
    {
        var service = serviceList.Catalog().FirstOrDefault(s => s.Id == id);
        return service is null ? NotFound() : Ok(service);
    }

    /// <summary>Managed service categories, used to populate filter dropdowns and the forms.</summary>
    /// <remarks>Served from the category list (kept in sync with listings at startup), so a
    /// category added from the dashboard appears even before any service carries it.</remarks>
    public ActionResult<IEnumerable<string>> GetCategories() =>
        Ok(categories.QueryReadOnly()
            .Where(c => c.Kind == Category.ServiceKind)
            .AsEnumerable()
            .Select(c => c.Name)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList());

    /// <summary>The caller's own service listings.</summary>
    public ActionResult<IEnumerable<Service>> GetMine(ClaimsPrincipal caller)
    {
        var userId = Access.UserId(caller);
        return Ok(serviceList.Catalog().AsEnumerable()
            .Where(s => s.ProviderId == userId)
            .OrderBy(s => s.Id)
            .ToList());
    }

    /// <summary>Creates a service listing.</summary>
    public ActionResult<Service> Create(ClaimsPrincipal caller, ServiceInput input)
    {
        if (!Access.CanList(caller))
        {
            return Forbid();
        }

        var created = new Service
        {
            Id = serviceList.QueryReadOnly().Any() ? serviceList.QueryReadOnly().Max(s => s.Id) + 1 : 1,
            ProviderId = Access.UserId(caller) ?? 0,
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

        serviceList.Add(created);
        serviceList.SaveChanges();

        // A category typed straight into the form joins the managed list immediately.
        CategorySync.EnsureExists(categories, created.Category, Category.ServiceKind);

        return CreatedAtAction("GetById", new { id = created.Id }, created);
    }

    /// <summary>Uploads one or more images into the service's gallery (stored under <c>wwwroot/images</c>).</summary>
    public async Task<ActionResult<List<ListingImage>>> UploadImages(ClaimsPrincipal caller, int id, List<IFormFile>? files)
    {
        var service = serviceList.Query().FirstOrDefault(s => s.Id == id);
        if (service is null)
        {
            return NotFound();
        }

        if (!Access.IsAdmin(caller) && service.ProviderId != Access.UserId(caller))
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
            var error = ImageUpload.Validate(file);
            if (error is not null)
            {
                return Problem(title: "Invalid image", detail: error, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        var sortOrder = listingImages.QueryReadOnly().Count(i => i.ServiceId == id);
        if (sortOrder + files.Count > ImageUpload.MaxPerListing)
        {
            return Problem(
                title: "Gallery full",
                detail: $"A listing can hold at most {ImageUpload.MaxPerListing} images.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var nextId = listingImages.QueryReadOnly().Any() ? listingImages.QueryReadOnly().Max(i => i.Id) + 1 : 1;
        var created = new List<ListingImage>(files.Count);
        foreach (var file in files)
        {
            var path = await images.SaveAsync(file, "services");
            created.Add(new ListingImage
            {
                Id = nextId++,
                ServiceId = id,
                Path = path,
                SortOrder = sortOrder++,
                CreatedAt = DateTime.UtcNow,
            });
        }

        listingImages.AddRange(created);
        serviceList.SaveChanges();

        return Created($"/api/services/{id}/images", created);
    }

    /// <summary>Removes one image from the service's gallery and deletes its file.</summary>
    public IActionResult DeleteImage(ClaimsPrincipal caller, int id, int imageId)
    {
        var service = serviceList.Query().FirstOrDefault(s => s.Id == id);
        if (service is null)
        {
            return NotFound();
        }

        if (!Access.IsAdmin(caller) && service.ProviderId != Access.UserId(caller))
        {
            return Forbid();
        }

        var image = listingImages.Query().FirstOrDefault(i => i.Id == imageId && i.ServiceId == id);
        if (image is null)
        {
            return NotFound();
        }

        listingImages.Remove(image);
        serviceList.SaveChanges();
        images.Delete(image.Path);
        return NoContent();
    }

    /// <summary>Updates an existing service listing.</summary>
    public ActionResult<Service> Update(ClaimsPrincipal caller, int id, ServiceInput input)
    {
        var service = serviceList.Query().FirstOrDefault(s => s.Id == id);
        if (service is null)
        {
            return NotFound();
        }

        if (!Access.IsAdmin(caller) && service.ProviderId != Access.UserId(caller))
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
        serviceList.SaveChanges();
        CategorySync.EnsureExists(categories, service.Category, Category.ServiceKind);

        return Ok(service);
    }

    /// <summary>Deletes a service listing.</summary>
    public IActionResult Delete(ClaimsPrincipal caller, int id)
    {
        var service = serviceList.Query().FirstOrDefault(s => s.Id == id);
        if (service is null)
        {
            return NotFound();
        }

        if (!Access.IsAdmin(caller) && service.ProviderId != Access.UserId(caller))
        {
            return Forbid();
        }

        // Drop the gallery rows and their files before the listing disappears.
        var serviceImages = listingImages.Query().Where(i => i.ServiceId == id).ToList();
        foreach (var image in serviceImages)
        {
            images.Delete(image.Path);
        }
        listingImages.RemoveRange(serviceImages);

        serviceList.Remove(service);
        serviceList.SaveChanges();
        return NoContent();
    }
}
