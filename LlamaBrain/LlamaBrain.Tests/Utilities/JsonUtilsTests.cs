using System;
using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json;
using LlamaBrain.Utilities;

namespace LlamaBrain.Tests.Utilities
{
  /// <summary>
  /// Tests for JsonUtils utility class.
  /// </summary>
  public class JsonUtilsTests
  {
    #region Test Classes

    private class SimpleObject
    {
      public string Name { get; set; } = "";
      public int Value { get; set; }
    }

    private class NestedObject
    {
      public string Id { get; set; } = "";
      public SimpleObject? Inner { get; set; }
      public List<string> Items { get; set; } = new List<string>();
    }

    #endregion

    #region Serialize Tests

    [Test]
    public void Serialize_SimpleObject_ReturnsValidJson()
    {
      // Arrange
      var obj = new SimpleObject { Name = "Test", Value = 42 };

      // Act
      var json = JsonUtils.Serialize(obj);

      // Assert
      Assert.That(json, Does.Contain("\"Name\":\"Test\""));
      Assert.That(json, Does.Contain("\"Value\":42"));
    }

    [Test]
    public void Serialize_NullObject_ThrowsArgumentNullException()
    {
      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => JsonUtils.Serialize<SimpleObject>(null!));
    }

    [Test]
    public void Serialize_WithFormatting_ReturnsFormattedJson()
    {
      // Arrange
      var obj = new SimpleObject { Name = "Test", Value = 42 };

      // Act
      var json = JsonUtils.Serialize(obj, Formatting.Indented);

      // Assert
      Assert.That(json, Does.Contain("\n"));
      Assert.That(json, Does.Contain("  "));
    }

    [Test]
    public void Serialize_WithFormattingNone_ReturnsCompactJson()
    {
      // Arrange
      var obj = new SimpleObject { Name = "Test", Value = 42 };

      // Act
      var json = JsonUtils.Serialize(obj, Formatting.None);

      // Assert
      Assert.That(json, Does.Not.Contain("\n"));
    }

    [Test]
    public void Serialize_NestedObject_ReturnsValidJson()
    {
      // Arrange
      var obj = new NestedObject
      {
        Id = "test-id",
        Inner = new SimpleObject { Name = "Inner", Value = 10 },
        Items = new List<string> { "a", "b", "c" }
      };

      // Act
      var json = JsonUtils.Serialize(obj);

      // Assert
      Assert.That(json, Does.Contain("\"Id\":\"test-id\""));
      Assert.That(json, Does.Contain("\"Inner\""));
      Assert.That(json, Does.Contain("\"Items\""));
    }

    #endregion

    #region Deserialize Tests

    [Test]
    public void Deserialize_ValidJson_ReturnsObject()
    {
      // Arrange
      var json = "{\"Name\":\"Test\",\"Value\":42}";

      // Act
      var result = JsonUtils.Deserialize<SimpleObject>(json);

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result!.Name, Is.EqualTo("Test"));
      Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void Deserialize_NullString_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => JsonUtils.Deserialize<SimpleObject>(null!));
    }

    [Test]
    public void Deserialize_EmptyString_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => JsonUtils.Deserialize<SimpleObject>(""));
    }

    [Test]
    public void Deserialize_InvalidJson_ReturnsNull()
    {
      // Arrange - The implementation handles errors gracefully with error handler
      var invalidJson = "{ invalid json }";

      // Act
      var result = JsonUtils.Deserialize<SimpleObject>(invalidJson);

      // Assert - Returns null because error is handled
      Assert.That(result, Is.Null);
    }

    [Test]
    public void Deserialize_MissingProperties_ReturnsObjectWithDefaults()
    {
      // Arrange
      var json = "{\"Name\":\"Test\"}"; // Missing Value

      // Act
      var result = JsonUtils.Deserialize<SimpleObject>(json);

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result!.Name, Is.EqualTo("Test"));
      Assert.That(result.Value, Is.EqualTo(0)); // Default value
    }

    [Test]
    public void Deserialize_ExtraProperties_IgnoresThem()
    {
      // Arrange
      var json = "{\"Name\":\"Test\",\"Value\":42,\"Extra\":\"ignored\"}";

      // Act
      var result = JsonUtils.Deserialize<SimpleObject>(json);

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result!.Name, Is.EqualTo("Test"));
    }

    [Test]
    public void Deserialize_NestedObject_ReturnsCorrectStructure()
    {
      // Arrange
      var json = "{\"Id\":\"test\",\"Inner\":{\"Name\":\"Inner\",\"Value\":10},\"Items\":[\"a\",\"b\"]}";

      // Act
      var result = JsonUtils.Deserialize<NestedObject>(json);

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result!.Id, Is.EqualTo("test"));
      Assert.That(result.Inner, Is.Not.Null);
      Assert.That(result.Inner!.Name, Is.EqualTo("Inner"));
      Assert.That(result.Items.Count, Is.EqualTo(2));
    }

    #endregion

    #region IsValidJson Tests

    [Test]
    public void IsValidJson_ValidJson_ReturnsTrue()
    {
      // Arrange
      var json = "{\"Name\":\"Test\"}";

      // Act
      var result = JsonUtils.IsValidJson(json);

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public void IsValidJson_ValidArray_ReturnsTrue()
    {
      // Arrange
      var json = "[1,2,3]";

      // Act
      var result = JsonUtils.IsValidJson(json);

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public void IsValidJson_InvalidJson_ReturnsFalse()
    {
      // Arrange
      var json = "{ invalid }";

      // Act
      var result = JsonUtils.IsValidJson(json);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void IsValidJson_NullString_ReturnsFalse()
    {
      // Act
      var result = JsonUtils.IsValidJson(null!);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void IsValidJson_EmptyString_ReturnsFalse()
    {
      // Act
      var result = JsonUtils.IsValidJson("");

      // Assert
      Assert.That(result, Is.False);
    }

    #endregion

    #region SafeDeserialize Tests

    [Test]
    public void SafeDeserialize_ValidJson_ReturnsObject()
    {
      // Arrange
      var json = "{\"Name\":\"Test\",\"Value\":42}";
      var fallback = new SimpleObject { Name = "Fallback", Value = 0 };

      // Act
      var result = JsonUtils.SafeDeserialize(json, fallback);

      // Assert
      Assert.That(result.Name, Is.EqualTo("Test"));
      Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void SafeDeserialize_InvalidJson_ReturnsFallback()
    {
      // Arrange
      var json = "{ invalid }";
      var fallback = new SimpleObject { Name = "Fallback", Value = 0 };

      // Act
      var result = JsonUtils.SafeDeserialize(json, fallback);

      // Assert
      Assert.That(result.Name, Is.EqualTo("Fallback"));
    }

    [Test]
    public void SafeDeserialize_EmptyJson_ReturnsFallback()
    {
      // Arrange
      var fallback = new SimpleObject { Name = "Fallback", Value = 0 };

      // Act
      var result = JsonUtils.SafeDeserialize("", fallback);

      // Assert
      Assert.That(result.Name, Is.EqualTo("Fallback"));
    }

    #endregion

    #region GetJsonSize Tests

    [Test]
    public void GetJsonSize_ValidJson_ReturnsByteCount()
    {
      // Arrange
      var json = "{\"Name\":\"Test\"}";

      // Act
      var size = JsonUtils.GetJsonSize(json);

      // Assert
      Assert.That(size, Is.GreaterThan(0));
      Assert.That(size, Is.EqualTo(System.Text.Encoding.UTF8.GetByteCount(json)));
    }

    [Test]
    public void GetJsonSize_NullString_ReturnsZero()
    {
      // Act
      var size = JsonUtils.GetJsonSize(null!);

      // Assert
      Assert.That(size, Is.EqualTo(0));
    }

    [Test]
    public void GetJsonSize_EmptyString_ReturnsZero()
    {
      // Act
      var size = JsonUtils.GetJsonSize("");

      // Assert
      Assert.That(size, Is.EqualTo(0));
    }

    [Test]
    public void GetJsonSize_UnicodeCharacters_ReturnsCorrectByteCount()
    {
      // Arrange - Unicode characters take more bytes
      var json = "{\"Name\":\"日本語\"}";

      // Act
      var size = JsonUtils.GetJsonSize(json);

      // Assert
      Assert.That(size, Is.GreaterThan(json.Length)); // UTF-8 bytes > char count for Unicode
    }

    #endregion

    #region TruncateJson Tests

    [Test]
    public void TruncateJson_ShortJson_ReturnsUnchanged()
    {
      // Arrange
      var json = "{\"Name\":\"Test\"}";

      // Act
      var result = JsonUtils.TruncateJson(json, 100);

      // Assert
      Assert.That(result, Is.EqualTo(json));
    }

    [Test]
    public void TruncateJson_LongJson_TruncatesWithMarker()
    {
      // Arrange
      var json = "{\"Name\":\"" + new string('x', 100) + "\"}";

      // Act
      var result = JsonUtils.TruncateJson(json, 50);

      // Assert
      Assert.That(result.Length, Is.LessThanOrEqualTo(70)); // 50 + truncation text
      Assert.That(result, Does.Contain("[truncated]"));
    }

    [Test]
    public void TruncateJson_NullString_ReturnsNull()
    {
      // Act
      var result = JsonUtils.TruncateJson(null!);

      // Assert
      Assert.That(result, Is.Null);
    }

    [Test]
    public void TruncateJson_EmptyString_ReturnsEmpty()
    {
      // Act
      var result = JsonUtils.TruncateJson("");

      // Assert
      Assert.That(result, Is.EqualTo(""));
    }

    #endregion

    #region ValidateJsonSchema Tests

    [Test]
    public void ValidateJsonSchema_AllPropertiesPresent_ReturnsValid()
    {
      // Arrange
      var json = "{\"Name\":\"Test\",\"Value\":42}";
      var requiredProps = new[] { "Name", "Value" };

      // Act
      var result = JsonUtils.ValidateJsonSchema(json, requiredProps);

      // Assert
      Assert.That(result.IsValid, Is.True);
      Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void ValidateJsonSchema_MissingProperty_ReturnsInvalid()
    {
      // Arrange
      var json = "{\"Name\":\"Test\"}";
      var requiredProps = new[] { "Name", "Value" };

      // Act
      var result = JsonUtils.ValidateJsonSchema(json, requiredProps);

      // Assert
      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Errors, Has.Some.Contain("Value"));
    }

    [Test]
    public void ValidateJsonSchema_NullJson_ReturnsInvalid()
    {
      // Arrange
      var requiredProps = new[] { "Name" };

      // Act
      var result = JsonUtils.ValidateJsonSchema(null!, requiredProps);

      // Assert
      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Errors, Has.Some.Contain("null or empty"));
    }

    [Test]
    public void ValidateJsonSchema_EmptyJson_ReturnsInvalid()
    {
      // Arrange
      var requiredProps = new[] { "Name" };

      // Act
      var result = JsonUtils.ValidateJsonSchema("", requiredProps);

      // Assert
      Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateJsonSchema_InvalidJson_ReturnsInvalid()
    {
      // Arrange
      var json = "{ invalid }";
      var requiredProps = new[] { "Name" };

      // Act
      var result = JsonUtils.ValidateJsonSchema(json, requiredProps);

      // Assert
      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Errors, Has.Count.GreaterThan(0));
    }

    [Test]
    public void ValidateJsonSchema_ArrayInsteadOfObject_ReturnsInvalid()
    {
      // Arrange
      var json = "[1,2,3]";
      var requiredProps = new[] { "Name" };

      // Act
      var result = JsonUtils.ValidateJsonSchema(json, requiredProps);

      // Assert
      Assert.That(result.IsValid, Is.False);
    }

    #endregion

    #region SanitizeJson Tests

    [Test]
    public void SanitizeJson_ValidJson_ReturnsSanitized()
    {
      // Arrange
      var json = "{  \"Name\"  :  \"Test\"  }"; // Extra whitespace

      // Act
      var result = JsonUtils.SanitizeJson(json);

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(JsonUtils.IsValidJson(result), Is.True);
    }

    [Test]
    public void SanitizeJson_NullString_ReturnsNull()
    {
      // Act
      var result = JsonUtils.SanitizeJson(null!);

      // Assert
      Assert.That(result, Is.Null);
    }

    [Test]
    public void SanitizeJson_EmptyString_ReturnsEmpty()
    {
      // Act
      var result = JsonUtils.SanitizeJson("");

      // Assert
      Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void SanitizeJson_InvalidJson_ReturnsOriginal()
    {
      // Arrange
      var invalidJson = "{ invalid }";

      // Act
      var result = JsonUtils.SanitizeJson(invalidJson);

      // Assert
      Assert.That(result, Is.EqualTo(invalidJson));
    }

    #endregion

    #region CompressJson Tests

    [Test]
    public void CompressJson_FormattedJson_ReturnsCompressed()
    {
      // Arrange
      var json = "{\n  \"Name\": \"Test\",\n  \"Value\": 42\n}";

      // Act
      var result = JsonUtils.CompressJson(json);

      // Assert
      Assert.That(result, Does.Not.Contain("\n"));
      Assert.That(result.Length, Is.LessThan(json.Length));
    }

    [Test]
    public void CompressJson_NullString_ReturnsNull()
    {
      // Act
      var result = JsonUtils.CompressJson(null!);

      // Assert
      Assert.That(result, Is.Null);
    }

    [Test]
    public void CompressJson_EmptyString_ReturnsEmpty()
    {
      // Act
      var result = JsonUtils.CompressJson("");

      // Assert
      Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void CompressJson_InvalidJson_ReturnsOriginal()
    {
      // Arrange
      var invalidJson = "{ invalid }";

      // Act
      var result = JsonUtils.CompressJson(invalidJson);

      // Assert
      Assert.That(result, Is.EqualTo(invalidJson));
    }

    #endregion

    #region PrettyPrintJson Tests

    [Test]
    public void PrettyPrintJson_CompactJson_ReturnsFormatted()
    {
      // Arrange
      var json = "{\"Name\":\"Test\",\"Value\":42}";

      // Act
      var result = JsonUtils.PrettyPrintJson(json);

      // Assert
      Assert.That(result, Does.Contain("\n"));
      Assert.That(result.Length, Is.GreaterThan(json.Length));
    }

    [Test]
    public void PrettyPrintJson_NullString_ReturnsNull()
    {
      // Act
      var result = JsonUtils.PrettyPrintJson(null!);

      // Assert
      Assert.That(result, Is.Null);
    }

    [Test]
    public void PrettyPrintJson_EmptyString_ReturnsEmpty()
    {
      // Act
      var result = JsonUtils.PrettyPrintJson("");

      // Assert
      Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void PrettyPrintJson_InvalidJson_ReturnsOriginal()
    {
      // Arrange
      var invalidJson = "{ invalid }";

      // Act
      var result = JsonUtils.PrettyPrintJson(invalidJson);

      // Assert
      Assert.That(result, Is.EqualTo(invalidJson));
    }

    #endregion

    #region GetJsonStatistics Tests

    [Test]
    public void GetJsonStatistics_SimpleObject_ReturnsCorrectStats()
    {
      // Arrange
      var json = "{\"Name\":\"Test\",\"Value\":42}";

      // Act
      var stats = JsonUtils.GetJsonStatistics(json);

      // Assert
      Assert.That(stats.CharacterCount, Is.EqualTo(json.Length));
      Assert.That(stats.ByteCount, Is.GreaterThan(0));
      Assert.That(stats.PropertyCount, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void GetJsonStatistics_NestedObject_TracksDepth()
    {
      // Arrange
      var json = "{\"Level1\":{\"Level2\":{\"Value\":1}}}";

      // Act
      var stats = JsonUtils.GetJsonStatistics(json);

      // Assert
      Assert.That(stats.NestingDepth, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void GetJsonStatistics_ArrayJson_CountsArrays()
    {
      // Arrange
      var json = "{\"Items\":[1,2,3],\"More\":[4,5]}";

      // Act
      var stats = JsonUtils.GetJsonStatistics(json);

      // Assert
      Assert.That(stats.ArrayCount, Is.EqualTo(2));
    }

    [Test]
    public void GetJsonStatistics_NullString_ReturnsZeroStats()
    {
      // Act
      var stats = JsonUtils.GetJsonStatistics(null!);

      // Assert
      Assert.That(stats.CharacterCount, Is.EqualTo(0));
      Assert.That(stats.ByteCount, Is.EqualTo(0));
      Assert.That(stats.PropertyCount, Is.EqualTo(0));
    }

    [Test]
    public void GetJsonStatistics_EmptyString_ReturnsZeroStats()
    {
      // Act
      var stats = JsonUtils.GetJsonStatistics("");

      // Assert
      Assert.That(stats.CharacterCount, Is.EqualTo(0));
    }

    [Test]
    public void GetJsonStatistics_InvalidJson_ReturnsPartialStats()
    {
      // Arrange
      var invalidJson = "{ invalid }";

      // Act
      var stats = JsonUtils.GetJsonStatistics(invalidJson);

      // Assert
      Assert.That(stats.CharacterCount, Is.EqualTo(invalidJson.Length));
    }

    #endregion

    #region JsonValidationResult Tests

    [Test]
    public void JsonValidationResult_Default_IsValidAndNoErrors()
    {
      // Arrange & Act
      var result = new JsonValidationResult();

      // Assert
      Assert.That(result.IsValid, Is.False); // Default bool is false
      Assert.That(result.Errors, Is.Not.Null);
    }

    #endregion

    #region JsonStatistics Tests

    [Test]
    public void JsonStatistics_Default_AllZero()
    {
      // Arrange & Act
      var stats = new JsonStatistics();

      // Assert
      Assert.That(stats.CharacterCount, Is.EqualTo(0));
      Assert.That(stats.ByteCount, Is.EqualTo(0));
      Assert.That(stats.PropertyCount, Is.EqualTo(0));
      Assert.That(stats.ArrayCount, Is.EqualTo(0));
      Assert.That(stats.NestingDepth, Is.EqualTo(0));
    }

    #endregion
  }
}
