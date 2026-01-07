using System;
using NUnit.Framework;
using Newtonsoft.Json;
using LlamaBrain.Persistence.Dtos;

namespace LlamaBrain.Tests.Persistence
{
  /// <summary>
  /// Tests for DialogueEntryDto serialization and initialization.
  /// </summary>
  [TestFixture]
  public class DialogueEntryDtoTests
  {
    #region Default Value Tests

    [Test]
    public void Constructor_DefaultValues_SpeakerIsEmptyString()
    {
      // Act
      var dto = new DialogueEntryDto();

      // Assert
      Assert.That(dto.Speaker, Is.EqualTo(""));
    }

    [Test]
    public void Constructor_DefaultValues_TextIsEmptyString()
    {
      // Act
      var dto = new DialogueEntryDto();

      // Assert
      Assert.That(dto.Text, Is.EqualTo(""));
    }

    [Test]
    public void Constructor_DefaultValues_TimestampTicksIsZero()
    {
      // Act
      var dto = new DialogueEntryDto();

      // Assert
      Assert.That(dto.TimestampTicks, Is.EqualTo(0L));
    }

    #endregion

    #region Property Tests

    [Test]
    public void Speaker_SetAndGet_ReturnsSetValue()
    {
      // Arrange
      var dto = new DialogueEntryDto();

      // Act
      dto.Speaker = "Player";

      // Assert
      Assert.That(dto.Speaker, Is.EqualTo("Player"));
    }

    [Test]
    public void Text_SetAndGet_ReturnsSetValue()
    {
      // Arrange
      var dto = new DialogueEntryDto();

      // Act
      dto.Text = "Hello, world!";

      // Assert
      Assert.That(dto.Text, Is.EqualTo("Hello, world!"));
    }

    [Test]
    public void TimestampTicks_SetAndGet_ReturnsSetValue()
    {
      // Arrange
      var dto = new DialogueEntryDto();
      var expectedTicks = DateTime.UtcNow.Ticks;

      // Act
      dto.TimestampTicks = expectedTicks;

      // Assert
      Assert.That(dto.TimestampTicks, Is.EqualTo(expectedTicks));
    }

    #endregion

    #region Serialization Tests

    [Test]
    public void JsonRoundTrip_AllFieldsPopulated_PreservesValues()
    {
      // Arrange
      var original = new DialogueEntryDto
      {
        Speaker = "NPC",
        Text = "Greetings, traveler!",
        TimestampTicks = 638000000000000000L
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<DialogueEntryDto>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Speaker, Is.EqualTo(original.Speaker));
      Assert.That(restored.Text, Is.EqualTo(original.Text));
      Assert.That(restored.TimestampTicks, Is.EqualTo(original.TimestampTicks));
    }

    [Test]
    public void JsonRoundTrip_EmptyDto_PreservesDefaults()
    {
      // Arrange
      var original = new DialogueEntryDto();

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<DialogueEntryDto>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Speaker, Is.EqualTo(""));
      Assert.That(restored.Text, Is.EqualTo(""));
      Assert.That(restored.TimestampTicks, Is.EqualTo(0L));
    }

    [Test]
    public void JsonRoundTrip_SpecialCharactersInText_PreservesValues()
    {
      // Arrange
      var original = new DialogueEntryDto
      {
        Speaker = "Player",
        Text = "He said: \"Hello!\"\nNew line here.\tTab too.",
        TimestampTicks = 123456789L
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<DialogueEntryDto>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Text, Is.EqualTo(original.Text));
    }

    [Test]
    public void JsonRoundTrip_UnicodeCharacters_PreservesValues()
    {
      // Arrange
      var original = new DialogueEntryDto
      {
        Speaker = "NPC",
        Text = "Welcome to the kingdom! \u2764 \u2605",
        TimestampTicks = 987654321L
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<DialogueEntryDto>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Text, Is.EqualTo(original.Text));
    }

    [Test]
    public void JsonDeserialization_MissingFields_UsesDefaults()
    {
      // Arrange
      var json = "{}";

      // Act
      var restored = JsonConvert.DeserializeObject<DialogueEntryDto>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Speaker, Is.Null.Or.EqualTo(""));
      Assert.That(restored.Text, Is.Null.Or.EqualTo(""));
      Assert.That(restored.TimestampTicks, Is.EqualTo(0L));
    }

    [Test]
    public void JsonDeserialization_PartialFields_PreservesProvidedValues()
    {
      // Arrange
      var json = "{\"Speaker\":\"Guard\",\"TimestampTicks\":555}";

      // Act
      var restored = JsonConvert.DeserializeObject<DialogueEntryDto>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Speaker, Is.EqualTo("Guard"));
      Assert.That(restored.TimestampTicks, Is.EqualTo(555L));
    }

    #endregion

    #region Edge Case Tests

    [Test]
    public void TimestampTicks_MaxValue_HandlesCorrectly()
    {
      // Arrange
      var dto = new DialogueEntryDto
      {
        TimestampTicks = long.MaxValue
      };

      // Act
      var json = JsonConvert.SerializeObject(dto);
      var restored = JsonConvert.DeserializeObject<DialogueEntryDto>(json);

      // Assert
      Assert.That(restored!.TimestampTicks, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void TimestampTicks_MinValue_HandlesCorrectly()
    {
      // Arrange
      var dto = new DialogueEntryDto
      {
        TimestampTicks = long.MinValue
      };

      // Act
      var json = JsonConvert.SerializeObject(dto);
      var restored = JsonConvert.DeserializeObject<DialogueEntryDto>(json);

      // Assert
      Assert.That(restored!.TimestampTicks, Is.EqualTo(long.MinValue));
    }

    [Test]
    public void Text_VeryLongString_HandlesCorrectly()
    {
      // Arrange
      var longText = new string('x', 10000);
      var dto = new DialogueEntryDto
      {
        Text = longText
      };

      // Act
      var json = JsonConvert.SerializeObject(dto);
      var restored = JsonConvert.DeserializeObject<DialogueEntryDto>(json);

      // Assert
      Assert.That(restored!.Text, Is.EqualTo(longText));
      Assert.That(restored.Text.Length, Is.EqualTo(10000));
    }

    #endregion
  }
}
