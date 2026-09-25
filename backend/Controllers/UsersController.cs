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
public class UsersController(MarketDbContext db, ImageStore store) : ControllerBase
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

    /// <summary>Creates a dashboard or mobile user account (<c>SuperAdmin</c>, <c>Admin</c> or <c>Manager</c> only).</summary>
    /// <remarks>The platform is derived from the role: <c>Provider</c>/<c>Client</c> become <c>Mobile</c> accounts,
    /// every other role becomes <c>Dashboard</c>.</remarks>
    /// <param name="input">Name, email, password and role for the new account.</param>
    /// <returns>The created profile (never the password hash).</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<UserProfileDto> Create([FromBody] UserCreateInput input)
    {
        if (!Access.IsAdmin(User))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var email = input.Email.Trim();
        if (db.Users.AsEnumerable().Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            return Problem(
                title: "Email already registered",
                detail: "An account with this email already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var role = input.Role.Trim().ToLowerInvariant() switch
        {
            "superadmin" => "SuperAdmin",
            "admin" => "Admin",
            "manager" => "Manager",
            "viewer" => "Viewer",
            "provider" => "Provider",
            "client" => "Client",
            _ => string.Empty,
        };

        if (role.Length == 0)
        {
            return Problem(
                title: "Invalid role",
                detail: "Role must be one of SuperAdmin, Admin, Manager, Viewer, Provider or Client.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var created = new User
        {
            Id = db.Users.Any() ? db.Users.Max(u => u.Id) + 1 : 1,
            Email = email,
            Name = input.Name.Trim(),
            PasswordHash = PasswordHasher.Hash(input.Password),
            Role = role,
            // Provider/Client are mobile accounts; dashboard roles run the web dashboard.
            Type = role is "Provider" or "Client" ? "Mobile" : "Dashboard",
            Phone = NullIfBlank(input.Phone),
            Location = NullIfBlank(input.Location),
            Bio = NullIfBlank(input.Bio),
        };

        db.Users.Add(created);
        db.SaveChanges();

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToProfile(created));
    }

    /// <summary>Edits any user — profile fields, email, role and an optional password reset (dashboard admins only).</summary>
    /// <remarks>The platform is re-derived from the role (<c>Provider</c>/<c>Client</c> → <c>Mobile</c>).
    /// Leave <c>password</c> null or empty to keep the current hash.</remarks>
    /// <param name="id">The user to edit.</param>
    /// <param name="input">New field values; name/email/role are required.</param>
    /// <returns>The updated profile.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<UserProfileDto> Update(int id, [FromBody] UserEditInput input)
    {
        if (!Access.IsAdmin(User)) return Forbid();

        var user = db.Users.FirstOrDefault(u => u.Id == id);
        if (user is null) return NotFound();

        var name = NullIfBlank(input.Name);
        if (name is null)
            return Problem(title: "Invalid user", detail: "Name is required.", statusCode: StatusCodes.Status400BadRequest);

        var email = NullIfBlank(input.Email);
        if (email is null)
            return Problem(title: "Invalid user", detail: "Email is required.", statusCode: StatusCodes.Status400BadRequest);
        if (db.Users.Any(u => u.Id != id && u.Email.ToLower() == email.ToLower()))
            return Problem(title: "Email in use", detail: "Another account already uses this email.", statusCode: StatusCodes.Status409Conflict);

        var role = NullIfBlank(input.Role);
        if (role is null)
            return Problem(title: "Invalid user", detail: "Role is required.", statusCode: StatusCodes.Status400BadRequest);
        role = role.ToLower() switch
        {
            "superadmin" => "SuperAdmin",
            "admin" => "Admin",
            "manager" => "Manager",
            "viewer" => "Viewer",
            "provider" => "Provider",
            "client" => "Client",
            _ => role
        };
        if (role is not ("SuperAdmin" or "Admin" or "Manager" or "Viewer" or "Provider" or "Client"))
            return Problem(title: "Invalid user", detail: "Role must be SuperAdmin, Admin, Manager, Viewer, Provider or Client.", statusCode: StatusCodes.Status400BadRequest);

        var newPassword = NullIfBlank(input.Password);
        if (newPassword is not null && newPassword.Length < 6)
            return Problem(title: "Invalid user", detail: "Password must be at least 6 characters.", statusCode: StatusCodes.Status400BadRequest);

        user.Name = name;
        user.Email = email;
        user.Role = role;
        user.Type = role is "Provider" or "Client" ? "Mobile" : "Dashboard";
        user.Phone = NullIfBlank(input.Phone);
        user.Location = NullIfBlank(input.Location);
        user.Bio = NullIfBlank(input.Bio);
        if (newPassword is not null)
            user.PasswordHash = PasswordHasher.Hash(newPassword);

        db.SaveChanges();
        return Ok(ToProfile(user));
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Uploads (or replaces) the user's profile picture — the account owner or a dashboard admin.</summary>
    /// <remarks>The old image file is deleted. Stored under <c>wwwroot/images/users</c> and returned as an
    /// absolute URL prefixed with <c>BackendUrl</c> from appsettings.</remarks>
    /// <param name="id">The user identifier.</param>
    /// <param name="file">Multipart <c>file</c> field; png/jpg/webp/gif up to 5 MB.
    /// No <c>[FromForm]</c> attribute — ApiExplorer infers <c>BindingSource.FormFile</c> from
    /// <see cref="IFormFile"/> itself (the attribute would break Swashbuckle's form description).</param>
    /// <returns>The updated profile with the new image URL.</returns>
    [HttpPost("{id:int}/image")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserProfileDto>> UploadImage(int id, IFormFile? file)
    {
        var user = db.Users.FirstOrDefault(u => u.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        if (Access.UserId(User) != id && !Access.IsAdmin(User))
        {
            return Forbid();
        }

        if (file is null)
        {
            return Problem(
                title: "No image uploaded",
                detail: "Attach one file in the 'file' form field.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var error = ImageStore.Validate(file);
        if (error is not null)
        {
            return Problem(title: "Invalid image", detail: error, statusCode: StatusCodes.Status400BadRequest);
        }

        var url = await store.SaveAsync(file, "users");
        if (user.ImagePath is not null)
        {
            store.Delete(user.ImagePath);
        }

        user.ImagePath = url;
        db.SaveChanges();

        return Ok(ToProfile(user));
    }

    /// <summary>Removes the user's profile picture and deletes its file — the account owner or a dashboard admin.</summary>
    /// <param name="id">The user identifier.</param>
    /// <returns><c>204 No Content</c>, or <c>404</c> when no picture is set.</returns>
    [HttpDelete("{id:int}/image")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteImage(int id)
    {
        var user = db.Users.FirstOrDefault(u => u.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        if (Access.UserId(User) != id && !Access.IsAdmin(User))
        {
            return Forbid();
        }

        if (user.ImagePath is null)
        {
            return NotFound();
        }

        store.Delete(user.ImagePath);
        user.ImagePath = null;
        db.SaveChanges();
        return NoContent();
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
        new(user.Id, user.Email, user.Name, user.Role, user.Type, user.Phone, user.Location, user.Bio, user.ImagePath);
}
