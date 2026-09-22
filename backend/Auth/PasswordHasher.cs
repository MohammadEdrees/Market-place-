using System.Security.Cryptography;

namespace MarketWorkplace.Api.Auth;

/// <summary>
/// Minimal PBKDF2 password hashing for the sample user store.
/// Format: <c>{iterations}.{salt}.{hash}</c> — the salt travels with the hash so it can be
/// verified without server-side state. In production use ASP.NET Core Identity (or similar)
/// instead of a hand-rolled helper.
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    /// <summary>Derives a new salted hash for the given plain-text password.</summary>
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return string.Join('.', Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    /// <summary>Constant-time verification of a password against a stored hash.</summary>
    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
