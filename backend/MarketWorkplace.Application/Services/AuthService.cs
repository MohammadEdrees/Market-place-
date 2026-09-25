using System.Security.Claims;
using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Application.Security;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static MarketWorkplace.Application.Common.ServiceResults;

namespace MarketWorkplace.Application.Services;

/// <summary>Sign-in, registration and caller-profile operations (backs <c>AuthController</c>).</summary>
public class AuthService(IUserRepository users, IRoleRepository roles, ITokenService tokens)
{
    /// <summary>Exchanges email + password for a token, or a 401 problem response.</summary>
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        var user = FindUser(request.Email);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Problem(
                title: "Invalid email or password",
                detail: "Check the credentials and try again.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var (token, expiresAt) = tokens.CreateToken(user);
        return Ok(new LoginResponse(token, expiresAt, ToProfile(user)));
    }

    /// <summary>Creates a mobile account (Client or Provider) and returns a token for it.</summary>
    public ActionResult<LoginResponse> Register(RegisterRequest request)
    {
        var email = request.Email.Trim();
        if (users.QueryReadOnly().AsEnumerable().Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            return Problem(
                title: "Email already registered",
                detail: "Sign in instead or choose another email address.",
                statusCode: StatusCodes.Status409Conflict);
        }

        // Only the two mobile roles may self-register; the roles table decides which exist.
        var requestedRole = request.Role?.Trim();
        var role = roles.FindByName(string.IsNullOrEmpty(requestedRole) ? "Client" : requestedRole);
        if (role is null || (role.Name != "Client" && role.Name != "Provider"))
        {
            return Problem(
                title: "Invalid role",
                detail: "Role must be Client or Provider.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var user = new User
        {
            Id = users.QueryReadOnly().Any() ? users.QueryReadOnly().Max(u => u.Id) + 1 : 1,
            Email = email,
            Name = request.Name.Trim(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = role,
            Type = "Mobile",
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
        };

        users.Add(user);
        users.SaveChanges();

        var (token, expiresAt) = tokens.CreateToken(user);
        return Ok(new LoginResponse(token, expiresAt, ToProfile(user)));
    }

    /// <summary>The profile behind the caller's bearer token.</summary>
    public ActionResult<UserProfileDto> Me(ClaimsPrincipal caller)
    {
        // MapInboundClaims is disabled, so claim types are the short JWT names issued by the token.
        var user = FindUser(caller.FindFirst("email")?.Value ?? string.Empty);
        return user is null ? Unauthorized() : Ok(ToProfile(user));
    }

    /// <summary>Finds a user by email (case-insensitive) with its role, or <c>null</c> when unknown.</summary>
    private User? FindUser(string email) =>
        users.Profiles()
            .AsEnumerable()
            .FirstOrDefault(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));

    private static UserProfileDto ToProfile(User user) =>
        new(user.Id, user.Email, user.Name, user.Role.Name, user.Type, user.Phone, user.Location, user.Bio, user.ImagePath);
}
