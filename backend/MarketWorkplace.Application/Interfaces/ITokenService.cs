using MarketWorkplace.Domain.Entities;

namespace MarketWorkplace.Application.Interfaces;

/// <summary>
/// Mints the bearer tokens returned by sign-in. Implemented by the API tier (the signing settings
/// live in that project's configuration); the application layer only depends on this contract.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a bearer token for <paramref name="user"/> and reports when it expires.
    /// The user must have its <see cref="User.Role"/> loaded — the role name is a claim.
    /// </summary>
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}
