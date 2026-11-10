using System.Security.Cryptography;
using System.Text;

namespace Ilevora.Domain.Utilities;

/// <summary>
/// One-time link tokens (invitations, password reset). The raw token only ever goes into the email; the database keeps
/// its SHA-256 hash, so a leaked table cannot be used to take over an account.
/// </summary>
public static class SecureToken
{
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    /// <summary>Fixed-time comparison of the hash of <paramref name="token"/> against a stored hash.</summary>
    public static bool Matches(string token, string storedHash)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(storedHash)) return false;
        var a = Encoding.UTF8.GetBytes(Hash(token));
        var b = Encoding.UTF8.GetBytes(storedHash);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}
