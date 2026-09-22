using MarketWorkplace.Api.Auth;
using MarketWorkplace.Api.Data;
using MarketWorkplace.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Api.Controllers;

/// <summary>Issues and validates the bearer tokens used by the dashboard.</summary>
[ApiController]
[Route("api/auth")]
[Tags("Auth")]
[Produces("application/json")]
public class AuthController(InMemoryStore store, TokenService tokenService) : ControllerBase
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

        var user = store.FindUser(request.Email);
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
        var user = store.FindUser(User.FindFirst("email")?.Value ?? string.Empty);
        return user is null ? Unauthorized() : Ok(ToProfile(user));
    }

    private static UserProfileDto ToProfile(User user) => new(user.Id, user.Email, user.Name, user.Role);
}
