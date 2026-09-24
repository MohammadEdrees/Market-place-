using MarketWorkplace.Api.Auth;
using MarketWorkplace.Api.Data;
using MarketWorkplace.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketWorkplace.Api.Controllers;

/// <summary>User profiles: contact details, the provider directory and self-service edits.</summary>
/// <remarks>Clients use this to view a seller/provider's details and get in touch.</remarks>
[ApiController]
[Authorize]
[Route("api/users")]
[Tags("Users")]
[Produces("application/json")]
public class UsersController(MarketDbContext db) : ControllerBase
{
    /// <summary>Lists one page of user profiles with optional filters and sorting (never includes password hashes).</summary>
    /// <param name="search">Case-insensitive match against name, email or location.</param>
    /// <param name="role">Exact role match, e.g. <c>Provider</c>.</param>
    /// <param name="type"><c>Dashboard</c> or <c>Mobile</c>.</param>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="pageSize">Rows per page, clamped to 1–100 (default 20).</param>
    /// <param name="sortBy">Sort column: <c>id</c>, <c>name</c>, <c>email</c>, <c>role</c> or <c>type</c>; unknown values sort by id.</param>
    /// <param name="sortDir"><c>asc</c> (default) or <c>desc</c>.</param>
    /// <returns>One page of profiles — dashboard admins see everyone, other callers get the provider directory.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserProfileDto>), StatusCodes.Status200OK)]
    public ActionResult<PagedResponse<UserProfileDto>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDir = "asc")
    {
        page = Paging.NormalizePage(page);
        pageSize = Paging.NormalizePageSize(pageSize);

        IEnumerable<User> users = db.Users.AsNoTracking().AsEnumerable();

        if (!Access.IsAdmin(User))
        {
            users = users.Where(u => u.Type == "Mobile" && u.Role == "Provider");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            users = users.Where(u =>
                u.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (u.Location ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            users = users.Where(u => u.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            users = users.Where(u => u.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        var total = users.Count();
        var items = ApplySort(users, sortBy, sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToProfile)
            .ToList();

        return Ok(new PagedResponse<UserProfileDto>(items, total, page, pageSize));
    }

    /// <summary>Whitelisted sort keys for the user list; unknown values fall back to the id.</summary>
    private static IOrderedEnumerable<User> ApplySort(IEnumerable<User> users, string? sortBy, bool desc)
    {
        IComparer<User> comparer = (sortBy ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "name" => Comparer<User>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)),
            "email" => Comparer<User>.Create((a, b) => string.Compare(a.Email, b.Email, StringComparison.OrdinalIgnoreCase)),
            "role" => Comparer<User>.Create((a, b) => string.Compare(a.Role, b.Role, StringComparison.OrdinalIgnoreCase)),
            "type" => Comparer<User>.Create((a, b) => string.Compare(a.Type, b.Type, StringComparison.OrdinalIgnoreCase)),
            "location" => Comparer<User>.Create((a, b) => string.Compare(a.Location ?? string.Empty, b.Location ?? string.Empty, StringComparison.OrdinalIgnoreCase)),
            _ => Comparer<User>.Create((a, b) => a.Id.CompareTo(b.Id)),
        };

        return desc ? users.OrderByDescending(u => u, comparer) : users.OrderBy(u => u, comparer);
    }

    /// <summary>Gets one user's profile, including contact info (phone / location / bio).</summary>
    /// <param name="id">The user identifier (e.g. the seller behind a product).</param>
    /// <returns>The profile, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<UserProfileDto> GetById(int id)
    {
        var user = db.Users.AsNoTracking().FirstOrDefault(u => u.Id == id);
        return user is null ? NotFound() : Ok(ToProfile(user));
    }

    /// <summary>Updates the caller's own display name and contact fields.</summary>
    /// <remarks>Null leaves a field unchanged; send an empty string to clear it.
    /// Email, role and type are not editable here.</remarks>
    /// <param name="input">The fields to change.</param>
    /// <returns>The updated profile.</returns>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public ActionResult<UserProfileDto> UpdateMe([FromBody] UserUpdateInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = db.Users.FirstOrDefault(u => u.Id == Access.UserId(User));
        if (user is null)
        {
            return Unauthorized();
        }

        if (input.Name is not null)
        {
            user.Name = input.Name.Trim();
        }

        if (input.Phone is not null)
        {
            user.Phone = input.Phone.Trim();
        }

        if (input.Location is not null)
        {
            user.Location = input.Location.Trim();
        }

        if (input.Bio is not null)
        {
            user.Bio = input.Bio.Trim();
        }

        db.SaveChanges();
        return Ok(ToProfile(user));
    }

    private static UserProfileDto ToProfile(User user) =>
        new(user.Id, user.Email, user.Name, user.Role, user.Type, user.Phone, user.Location, user.Bio);
}
