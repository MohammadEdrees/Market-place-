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
    /// <summary>Lists user profiles (never includes password hashes).</summary>
    /// <returns>
    /// Dashboard admins see every user; other callers get the public directory of
    /// mobile providers/sellers only.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserProfileDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<UserProfileDto>> GetAll()
    {
        IEnumerable<User> users = db.Users.AsNoTracking().AsEnumerable();

        if (!Access.IsAdmin(User))
        {
            users = users.Where(u => u.Type == "Mobile" && u.Role == "Provider");
        }

        return Ok(users.OrderBy(u => u.Id).Select(ToProfile).ToList());
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
