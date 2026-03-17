using System;
using System.Security.Cryptography;
using System.Text;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Provides deterministic SHA256 hashing utilities for audit records.
  /// All hash computations use UTF-8 encoding for consistent results.
  /// </summary>
  /// <remarks>
  /// Thread-safe: All methods are stateless and can be called concurrently.
  /// </remarks>
  public static class AuditHasher
  {
    /// <summary>
    /// Computes the SHA256 hash of the input string and returns it as a Base64 string.
    /// </summary>
    /// <param name="input">The string to hash. Must not be null.</param>
    /// <returns>Base64-encoded SHA256 hash (44 characters).</returns>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    public static string ComputeSha256(string input)
    {
      if (input == null)
        throw new ArgumentNullException(nameof(input));

      using var sha256 = SHA256.Create();
      var bytes = Encoding.UTF8.GetBytes(input);
      var hashBytes = sha256.ComputeHash(bytes);
      return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Computes the SHA256 hash of the input string and returns it as a lowercase hex string.
    /// </summary>
    /// <param name="input">The string to hash. Must not be null.</param>
    /// <returns>Lowercase hexadecimal SHA256 hash (64 characters).</returns>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    public static string ComputeSha256Hex(string input)
    {
      if (input == null)
        throw new ArgumentNullException(nameof(input));

      using var sha256 = SHA256.Create();
      var bytes = Encoding.UTF8.GetBytes(input);
      var hashBytes = sha256.ComputeHash(bytes);
      return BytesToHex(hashBytes);
    }

    /// <summary>
    /// Computes the SHA256 hash prefix (first N characters of the hex hash).
    /// Useful for compact identification where full hash is not needed.
    /// </summary>
    /// <param name="input">The string to hash. Must not be null.</param>
    /// <param name="length">Number of hex characters to return (0-64). Default is 8.</param>
    /// <returns>First <paramref name="length"/> characters of the lowercase hex hash.</returns>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when length is negative or greater than 64.</exception>
    public static string ComputeSha256Prefix(string input, int length = 8)
    {
      if (input == null)
        throw new ArgumentNullException(nameof(input));

      if (length < 0)
        throw new ArgumentOutOfRangeException(nameof(length), length, "Length cannot be negative.");

      if (length > 64)
        throw new ArgumentOutOfRangeException(nameof(length), length, "Length cannot exceed 64 (full SHA256 hex length).");

      if (length == 0)
        return string.Empty;

      var fullHash = ComputeSha256Hex(input);
      return fullHash.Substring(0, length);
    }

    /// <summary>
    /// Converts a byte array to a lowercase hexadecimal string.
    /// </summary>
    private static string BytesToHex(byte[] bytes)
    {
      var sb = new StringBuilder(bytes.Length * 2);
      foreach (var b in bytes)
      {
        sb.Append(b.ToString("x2")); // Lowercase hex, 2 chars per byte
      }
      return sb.ToString();
    }
  }
}
