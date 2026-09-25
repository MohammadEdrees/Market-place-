using System.Security.Claims;
using MarketWorkplace.Application.Common;
using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Application.Security;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static MarketWorkplace.Application.Common.ServiceResults;

namespace MarketWorkplace.Application.Services;

/// <summary>User profile reads and account administration (backs <c>UsersController</c>).</summary>
public class UsersService(IUserRepository users, IRoleRepository roles, IImageStore images)
{
    /// <summary>One page of profiles — admins see everyone, other callers get the provider directory.</summary>
    public ActionResult<PagedResponse<UserProfileDto>> GetAll(
        ClaimsPrincipal caller,
        string? search,
        string? role,
        string? type,
        int page = 1,
        int pageSize = Paging.DefaultPageSize,
        string? sortBy = null,
        string sortDir = "asc")
    {
        page = Paging.NormalizePage(page);
        pageSize = Paging.NormalizePageSize(pageSize);

        IEnumerable<User> profiles = users.Profiles().AsEnumerable();

        if (!Access.IsAdmin(caller))
        {
            profiles = profiles.Where(u => u.Type == "Mobile" && u.Role.Name == "Provider");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            profiles = profiles.Where(u =>
                u.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (u.Location ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            profiles = profiles.Where(u => u.Role.Name.Equals(role, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            profiles = profiles.Where(u => u.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        var total = profiles.Count();
        var items = ApplySort(profiles, sortBy, sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToProfile)
            .ToList();

        return Ok(new PagedResponse<UserProfileDto>(items, total, page, pageSize));
    }

    /// <summary>Gets one user's profile, including contact info (phone / location / bio).</summary>
    public ActionResult<UserProfileDto> GetById(int id)
    {
        var user = users.Profiles().FirstOrDefault(u => u.Id == id);
        return user is null ? NotFound() : Ok(ToProfile(user));
    }

    /// <summary>Creates a dashboard or mobile account (admin callers only).</summary>
    public ActionResult<UserProfileDto> Create(ClaimsPrincipal caller, UserCreateInput input)
    {
        if (!Access.IsAdmin(caller))
        {
            return Forbid();
        }

        var email = input.Email.Trim();
        if (users.QueryReadOnly().AsEnumerable().Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            return Problem(
                title: "Email already registered",
                detail: "An account with this email already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        // The roles table decides which names are valid (seeded defaults plus anything an admin adds).
        var role = roles.FindByName(input.Role.Trim());
        if (role is null)
        {
            return Problem(
                title: "Invalid role",
                detail: "Role must be one of SuperAdmin, Admin, Manager, Viewer, Provider or Client.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var created = new User
        {
            Id = users.QueryReadOnly().Any() ? users.QueryReadOnly().Max(u => u.Id) + 1 : 1,
            Email = email,
            Name = input.Name.Trim(),
            PasswordHash = PasswordHasher.Hash(input.Password),
            Role = role,
            // Provider/Client are mobile accounts; dashboard roles run the web dashboard.
            Type = role.Name is "Provider" or "Client" ? "Mobile" : "Dashboard",
            Phone = NullIfBlank(input.Phone),
            Location = NullIfBlank(input.Location),
            Bio = NullIfBlank(input.Bio),
        };

        users.Add(created);
        users.SaveChanges();

        return CreatedAtAction("GetById", new { id = created.Id }, ToProfile(created));
    }

    /// <summary>Edits any user — profile fields, email, role and an optional password reset (admin callers only).</summary>
    public ActionResult<UserProfileDto> Update(ClaimsPrincipal caller, int id, UserEditInput input)
    {
        if (!Access.IsAdmin(caller)) return Forbid();

        var user = users.WithRole().FirstOrDefault(u => u.Id == id);
        if (user is null) return NotFound();

        var name = NullIfBlank(input.Name);
        if (name is null)
            return Problem(title: "Invalid user", detail: "Name is required.", statusCode: StatusCodes.Status400BadRequest);

        var email = NullIfBlank(input.Email);
        if (email is null)
            return Problem(title: "Invalid user", detail: "Email is required.", statusCode: StatusCodes.Status400BadRequest);
        if (users.QueryReadOnly().Any(u => u.Id != id && u.Email.ToLower() == email.ToLower()))
            return Problem(title: "Email in use", detail: "Another account already uses this email.", statusCode: StatusCodes.Status409Conflict);

        var role = NullIfBlank(input.Role);
        if (role is null)
            return Problem(title: "Invalid user", detail: "Role is required.", statusCode: StatusCodes.Status400BadRequest);
        var roleEntity = roles.FindByName(role);
        if (roleEntity is null)
            return Problem(title: "Invalid user", detail: "Role must be SuperAdmin, Admin, Manager, Viewer, Provider or Client.", statusCode: StatusCodes.Status400BadRequest);

        var newPassword = NullIfBlank(input.Password);
        if (newPassword is not null && newPassword.Length < 6)
            return Problem(title: "Invalid user", detail: "Password must be at least 6 characters.", statusCode: StatusCodes.Status400BadRequest);

        user.Name = name;
        user.Email = email;
        user.Role = roleEntity;
        user.Type = roleEntity.Name is "Provider" or "Client" ? "Mobile" : "Dashboard";
        user.Phone = NullIfBlank(input.Phone);
        user.Location = NullIfBlank(input.Location);
        user.Bio = NullIfBlank(input.Bio);
        if (newPassword is not null)
            user.PasswordHash = PasswordHasher.Hash(newPassword);

        users.SaveChanges();
        return Ok(ToProfile(user));
    }

    /// <summary>Uploads (or replaces) the user's profile picture — the account owner or an admin.</summary>
    public async Task<ActionResult<UserProfileDto>> UploadImage(ClaimsPrincipal caller, int id, IFormFile? file)
    {
        var user = users.WithRole().FirstOrDefault(u => u.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        if (Access.UserId(caller) != id && !Access.IsAdmin(caller))
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

        var error = ImageUpload.Validate(file);
        if (error is not null)
        {
            return Problem(title: "Invalid image", detail: error, statusCode: StatusCodes.Status400BadRequest);
        }

        var url = await images.SaveAsync(file, "users");
        if (user.ImagePath is not null)
        {
            images.Delete(user.ImagePath);
        }

        user.ImagePath = url;
        users.SaveChanges();

        return Ok(ToProfile(user));
    }

    /// <summary>Removes the user's profile picture and deletes its file — the owner or an admin.</summary>
    public IActionResult DeleteImage(ClaimsPrincipal caller, int id)
    {
        var user = users.WithRole().FirstOrDefault(u => u.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        if (Access.UserId(caller) != id && !Access.IsAdmin(caller))
        {
            return Forbid();
        }

        if (user.ImagePath is null)
        {
            return NotFound();
        }

        images.Delete(user.ImagePath);
        user.ImagePath = null;
        users.SaveChanges();
        return NoContent();
    }

    /// <summary>Updates the caller's own display name and contact fields.</summary>
    public ActionResult<UserProfileDto> UpdateMe(ClaimsPrincipal caller, UserUpdateInput input)
    {
        var user = users.WithRole().FirstOrDefault(u => u.Id == Access.UserId(caller));
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

        users.SaveChanges();
        return Ok(ToProfile(user));
    }

    /// <summary>Whitelisted sort keys for the user list; unknown values fall back to the id.</summary>
    private static IOrderedEnumerable<User> ApplySort(IEnumerable<User> profiles, string? sortBy, bool desc)
    {
        IComparer<User> comparer = (sortBy ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "name" => Comparer<User>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)),
            "email" => Comparer<User>.Create((a, b) => string.Compare(a.Email, b.Email, StringComparison.OrdinalIgnoreCase)),
            "role" => Comparer<User>.Create((a, b) => string.Compare(a.Role.Name, b.Role.Name, StringComparison.OrdinalIgnoreCase)),
            "type" => Comparer<User>.Create((a, b) => string.Compare(a.Type, b.Type, StringComparison.OrdinalIgnoreCase)),
            "location" => Comparer<User>.Create((a, b) => string.Compare(a.Location ?? string.Empty, b.Location ?? string.Empty, StringComparison.OrdinalIgnoreCase)),
            _ => Comparer<User>.Create((a, b) => a.Id.CompareTo(b.Id)),
        };

        return desc ? profiles.OrderByDescending(u => u, comparer) : profiles.OrderBy(u => u, comparer);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static UserProfileDto ToProfile(User user) =>
        new(user.Id, user.Email, user.Name, user.Role.Name, user.Type, user.Phone, user.Location, user.Bio, user.ImagePath);
}
