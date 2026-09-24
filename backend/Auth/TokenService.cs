using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MarketWorkplace.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace MarketWorkplace.Api.Auth;

/// <summary>
/// Mints the signed JWTs accepted by the API. Settings live under the <c>Jwt</c> section of
/// appsettings.json (issuer, audience, secret, expiry) — override the secret per environment.
/// </summary>
public class TokenService(IConfiguration configuration)
{
    /// <summary>Creates a bearer token for <paramref name="user"/> and reports when it expires.</summary>
    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        var section = configuration.GetSection("Jwt");
        var secret = section["Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is missing from configuration.");
        var issuer = section["Issuer"] ?? "MarketWorkplace.Api";
        var audience = section["Audience"] ?? "MarketWorkplace.Client";
        var expiryMinutes = int.TryParse(section["ExpiryMinutes"], out var minutes) ? minutes : 480;

        // Claims are written with their short JWT names and MapInboundClaims is disabled on the
        // validating side, so User.Identity reads `name` / `role` exactly as issued below.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim("role", user.Role),
            new Claim("type", user.Type),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
