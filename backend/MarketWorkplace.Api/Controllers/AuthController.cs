using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Api.Controllers;

/// <summary>Issues and validates the bearer tokens used by the dashboard.</summary>
[ApiController]
[Route("api/auth")]
[Tags("Auth")]
[Produces("application/json")]
public class AuthController(AuthService auth) : ControllerBase
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

        return auth.Login(request);
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

        return auth.Register(request);
    }

    /// <summary>Describes the caller behind the current bearer token.</summary>
    /// <returns>The profile of the authenticated user.</returns>
    /// <response code="200">Valid token.</response>
    /// <response code="401">Missing, expired or invalid token.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserProfileDto> Me() => auth.Me(User);
}
