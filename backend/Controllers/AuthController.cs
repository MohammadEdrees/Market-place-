using MarketWorkplace.Api.Auth;
using MarketWorkplace.Api.Data;
using MarketWorkplace.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketWorkplace.Api.Controllers;

/// <summary>Issues and validates the bearer tokens used by the dashboard.</summary>
[ApiController]
[Route("api/auth")]
[Tags("Auth")]
[Produces("application/json")]
public class AuthController(MarketDbContext db, TokenService tokenService) : ControllerBase
{
    /// <summary>Exchanges email + password for a bearer token.</summary>
    /// <param name="request">Account email and plain-text password.</param>
    /// <returns>The token, its expiry and the signed-in user's profile.</returns>
    /// <response code="200">Credentials accepted.</response>
    /// <response code="400">Missing or malformed fields.</response>
    /// <response code="401">Email or password is incorrect.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = FindUser(request.Email);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Problem(
                title: "Invalid email or password",
                detail: "Check the credentials and try again.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var (token, expiresAt) = tokenService.CreateToken(user);
        return Ok(new LoginResponse(token, expiresAt, ToProfile(user)));
    }

    /// <summary>Creates a mobile account (Client or Provider) and returns a bearer token.</summary>
    /// <remarks>Dashboard accounts are provisioned by an admin — only mobile roles are accepted here.</remarks>
    /// <param name="request">Email, password, display name and the mobile role (Client or Provider).</param>
    /// <returns>The token, its expiry and the new user's profile.</returns>
    /// <response code="200">Account created and signed in.</response>
    /// <response code="400">Missing or malformed fields, or an unsupported role.</response>
    /// <response code="409">The email is already registered.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<LoginResponse> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var email = request.Email.Trim();
        if (db.Users.AsEnumerable().Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            return Problem(
                title: "Email already registered",
                detail: "Sign in instead or choose another email address.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var requestedRole = request.Role?.Trim();
        var role = string.IsNullOrEmpty(requestedRole)
            ? "Client"
            : requestedRole.ToLowerInvariant() switch
            {
                "client" => "Client",
                "provider" => "Provider",
                _ => string.Empty,
            };

        if (role.Length == 0)
        {
            return Problem(
                title: "Invalid role",
                detail: "Role must be Client or Provider.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var user = new User
        {
            Id = db.Users.Any() ? db.Users.Max(u => u.Id) + 1 : 1,
            Email = email,
            Name = request.Name.Trim(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = role,
            Type = "Mobile",
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
        };

        db.Users.Add(user);
        db.SaveChanges();

        var (token, expiresAt) = tokenService.CreateToken(user);
        return Ok(new LoginResponse(token, expiresAt, ToProfile(user)));
    }

    /// <summary>Describes the caller behind the current bearer token.</summary>
    /// <returns>The profile of the authenticated user.</returns>
    /// <response code="200">Valid token.</response>
    /// <response code="401">Missing, expired or invalid token.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserProfileDto> Me()
    {
        // MapInboundClaims is disabled, so claim types are the short JWT names issued by TokenService.
        var user = FindUser(User.FindFirst("email")?.Value ?? string.Empty);
        return user is null ? Unauthorized() : Ok(ToProfile(user));
    }

    /// <summary>Finds a user by email (case-insensitive), or <c>null</c> when unknown.</summary>
    private User? FindUser(string email) =>
        db.Users
            .AsNoTracking()
            .AsEnumerable()
            .FirstOrDefault(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));

    private static UserProfileDto ToProfile(User user) =>
        new(user.Id, user.Email, user.Name, user.Role, user.Type, user.Phone, user.Location, user.Bio, user.ImagePath);
}
