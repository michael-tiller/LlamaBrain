using System;
using NUnit.Framework;
using LlamaBrain.Core.Audit;

namespace LlamaBrain.Tests.Audit
{
  /// <summary>
  /// Tests for AuditHasher SHA256 hashing utilities.
  /// Verifies deterministic hash computation for audit records.
  /// </summary>
  [TestFixture]
  [Category("Audit")]
  public class AuditHasherTests
  {
    #region ComputeSha256 Tests

    [Test]
    public void ComputeSha256_EmptyString_ReturnsConsistentHash()
    {
      // Act
      var hash1 = AuditHasher.ComputeSha256("");
      var hash2 = AuditHasher.ComputeSha256("");

      // Assert
      Assert.That(hash1, Is.EqualTo(hash2));
      Assert.That(hash1, Is.Not.Empty);
    }

    [Test]
    public void ComputeSha256_SameInput_ReturnsSameHash()
    {
      // Arrange
      const string input = "Hello, World!";

      // Act
      var hash1 = AuditHasher.ComputeSha256(input);
      var hash2 = AuditHasher.ComputeSha256(input);

      // Assert
      Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void ComputeSha256_DifferentInputs_ReturnsDifferentHashes()
    {
      // Arrange
      const string input1 = "Hello";
      const string input2 = "Hello!";

      // Act
      var hash1 = AuditHasher.ComputeSha256(input1);
      var hash2 = AuditHasher.ComputeSha256(input2);

      // Assert
      Assert.That(hash1, Is.Not.EqualTo(hash2));
    }

    [Test]
    public void ComputeSha256_NullInput_ThrowsArgumentNull()
    {
      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => AuditHasher.ComputeSha256(null!));
    }

    [Test]
    public void ComputeSha256_KnownValue_ReturnsExpectedHash()
    {
      // This test ensures the hash algorithm is SHA256 and encoding is UTF-8
      // Known SHA256 hash for "test" in base64
      const string input = "test";
      const string expectedBase64 = "n4bQgYhMfWWaL+qgxVrQFaO/TxsrC4Is0V1sFbDwCgg=";

      // Act
      var hash = AuditHasher.ComputeSha256(input);

      // Assert
      Assert.That(hash, Is.EqualTo(expectedBase64));
    }

    [Test]
    public void ComputeSha256_Unicode_HandlesCorrectly()
    {
      // Arrange
      const string input = "Hello, 世界! 🌍";

      // Act
      var hash1 = AuditHasher.ComputeSha256(input);
      var hash2 = AuditHasher.ComputeSha256(input);

      // Assert
      Assert.That(hash1, Is.EqualTo(hash2));
      Assert.That(hash1, Is.Not.Empty);
    }

    [Test]
    public void ComputeSha256_LongInput_HandlesCorrectly()
    {
      // Arrange
      var input = new string('x', 100000);

      // Act
      var hash = AuditHasher.ComputeSha256(input);

      // Assert
      Assert.That(hash, Is.Not.Empty);
      Assert.That(hash.Length, Is.EqualTo(44)); // Base64 of 32 bytes = 44 chars
    }

    #endregion

    #region ComputeSha256Hex Tests

    [Test]
    public void ComputeSha256Hex_EmptyString_ReturnsHexHash()
    {
      // Act
      var hash = AuditHasher.ComputeSha256Hex("");

      // Assert
      Assert.That(hash, Is.Not.Empty);
      Assert.That(hash.Length, Is.EqualTo(64)); // SHA256 = 32 bytes = 64 hex chars
      Assert.That(hash, Does.Match("^[a-f0-9]+$")); // Lowercase hex
    }

    [Test]
    public void ComputeSha256Hex_KnownValue_ReturnsExpectedHash()
    {
      // Known SHA256 hex for "test"
      const string input = "test";
      const string expectedHex = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";

      // Act
      var hash = AuditHasher.ComputeSha256Hex(input);

      // Assert
      Assert.That(hash, Is.EqualTo(expectedHex));
    }

    [Test]
    public void ComputeSha256Hex_NullInput_ThrowsArgumentNull()
    {
      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => AuditHasher.ComputeSha256Hex(null!));
    }

    #endregion

    #region ComputeSha256Prefix Tests

    [Test]
    public void ComputeSha256Prefix_DefaultLength_Returns8Chars()
    {
      // Arrange
      const string input = "test";

      // Act
      var prefix = AuditHasher.ComputeSha256Prefix(input);

      // Assert
      Assert.That(prefix.Length, Is.EqualTo(8));
      Assert.That(prefix, Does.Match("^[a-f0-9]+$"));
    }

    [Test]
    public void ComputeSha256Prefix_CustomLength_ReturnsRequestedLength()
    {
      // Arrange
      const string input = "test";

      // Act
      var prefix16 = AuditHasher.ComputeSha256Prefix(input, 16);
      var prefix4 = AuditHasher.ComputeSha256Prefix(input, 4);

      // Assert
      Assert.That(prefix16.Length, Is.EqualTo(16));
      Assert.That(prefix4.Length, Is.EqualTo(4));
    }

    [Test]
    public void ComputeSha256Prefix_IsPrefix_OfFullHash()
    {
      // Arrange
      const string input = "test";

      // Act
      var fullHash = AuditHasher.ComputeSha256Hex(input);
      var prefix = AuditHasher.ComputeSha256Prefix(input, 16);

      // Assert
      Assert.That(fullHash.StartsWith(prefix), Is.True);
    }

    [Test]
    public void ComputeSha256Prefix_ZeroLength_ReturnsEmpty()
    {
      // Act
      var prefix = AuditHasher.ComputeSha256Prefix("test", 0);

      // Assert
      Assert.That(prefix, Is.Empty);
    }

    [Test]
    public void ComputeSha256Prefix_NegativeLength_ThrowsArgumentOutOfRange()
    {
      // Act & Assert
      Assert.Throws<ArgumentOutOfRangeException>(() => AuditHasher.ComputeSha256Prefix("test", -1));
    }

    [Test]
    public void ComputeSha256Prefix_LengthExceeds64_ThrowsArgumentOutOfRange()
    {
      // Act & Assert
      Assert.Throws<ArgumentOutOfRangeException>(() => AuditHasher.ComputeSha256Prefix("test", 65));
    }

    #endregion

    #region Determinism Tests

    [Test]
    [Category("Determinism")]
    public void ComputeSha256_Deterministic_AcrossMultipleCalls()
    {
      // Arrange
      const string input = "Deterministic test input with special chars: <>&\"'";
      var hashes = new string[100];

      // Act
      for (int i = 0; i < 100; i++)
        hashes[i] = AuditHasher.ComputeSha256(input);

      // Assert
      var firstHash = hashes[0];
      Assert.That(hashes, Has.All.EqualTo(firstHash));
    }

    [Test]
    [Category("Determinism")]
    public void ComputeSha256_ByteIdentical_ForIdenticalStrings()
    {
      // This test ensures that string creation doesn't affect hash
      var str1 = "test" + "input";
      var str2 = string.Concat("test", "input");
      var str3 = new string(new[] { 't', 'e', 's', 't', 'i', 'n', 'p', 'u', 't' });

      // Act
      var hash1 = AuditHasher.ComputeSha256(str1);
      var hash2 = AuditHasher.ComputeSha256(str2);
      var hash3 = AuditHasher.ComputeSha256(str3);

      // Assert
      Assert.That(hash1, Is.EqualTo(hash2));
      Assert.That(hash2, Is.EqualTo(hash3));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void ComputeSha256_WhitespaceOnly_ReturnsValidHash()
    {
      // Act
      var hashSpace = AuditHasher.ComputeSha256(" ");
      var hashTab = AuditHasher.ComputeSha256("\t");
      var hashNewline = AuditHasher.ComputeSha256("\n");

      // Assert - Each should be different
      Assert.That(hashSpace, Is.Not.EqualTo(hashTab));
      Assert.That(hashTab, Is.Not.EqualTo(hashNewline));
      Assert.That(hashSpace, Is.Not.EqualTo(hashNewline));
    }

    [Test]
    public void ComputeSha256_CaseSensitive()
    {
      // Act
      var hashLower = AuditHasher.ComputeSha256("test");
      var hashUpper = AuditHasher.ComputeSha256("TEST");

      // Assert
      Assert.That(hashLower, Is.Not.EqualTo(hashUpper));
    }

    #endregion
  }
}
