using System;
using System.Security.Cryptography;

namespace ByteForge.Toolkit.Security.Rbac;

/// <summary>Creates and verifies PBKDF2-SHA256 password hashes for shared TELCO identities.</summary>
public class TelcoPasswordHashService
{
    /// <summary>The default PBKDF2 iteration count.</summary>
    public const int DefaultIterations = 100000;

    private const int SaltSize = 16;
    private const int HashSize = 32;

    /// <summary>Creates salted password material suitable for TELCO identity storage.</summary>
    public TelcoPasswordHashResult CreateHash(string password, int iterations = DefaultIterations)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required.", nameof(password));
        if (iterations <= 0)
            throw new ArgumentOutOfRangeException(nameof(iterations));

        var salt = new byte[SaltSize];
        using (var random = RandomNumberGenerator.Create())
            random.GetBytes(salt);

        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return new TelcoPasswordHashResult
        {
            Salt = Convert.ToBase64String(salt),
            Hash = Convert.ToBase64String(deriveBytes.GetBytes(HashSize)),
            Iterations = iterations
        };
    }

    /// <summary>Verifies a password against stored TELCO PBKDF2-SHA256 material.</summary>
    public bool VerifyPassword(string password, string salt, string hash, int iterations)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(salt) ||
            string.IsNullOrWhiteSpace(hash) ||
            iterations <= 0)
        {
            return false;
        }

        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var expectedHash = Convert.FromBase64String(hash);
            using var deriveBytes = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256);
            return FixedTimeEquals(deriveBytes.GetBytes(expectedHash.Length), expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        if (left == null || right == null || left.Length != right.Length)
            return false;

        var difference = 0;
        for (var index = 0; index < left.Length; index++)
            difference |= left[index] ^ right[index];
        return difference == 0;
    }
}

/// <summary>Represents generated TELCO password hash material.</summary>
public class TelcoPasswordHashResult
{
    /// <summary>Gets or sets the Base64 salt.</summary>
    public string Salt { get; set; }

    /// <summary>Gets or sets the Base64 hash.</summary>
    public string Hash { get; set; }

    /// <summary>Gets or sets the PBKDF2 iteration count.</summary>
    public int Iterations { get; set; }
}
