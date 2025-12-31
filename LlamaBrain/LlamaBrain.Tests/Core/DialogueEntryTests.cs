using System;
using NUnit.Framework;
using LlamaBrain.Core;

namespace LlamaBrain.Tests.Core
{
  /// <summary>
  /// Tests for DialogueEntry individual dialogue entries
  /// </summary>
  [TestFixture]
  public class DialogueEntryTests
  {
    #region Constructor Tests

    [Test]
    public void DialogueEntry_DefaultConstructor_InitializesWithDefaults()
    {
      // Arrange & Act
      var entry = new DialogueEntry();

      // Assert
      Assert.IsNotNull(entry);
      Assert.AreEqual(string.Empty, entry.Speaker);
      Assert.AreEqual(string.Empty, entry.Text);
      Assert.AreEqual(default(DateTime), entry.Timestamp);
    }

    #endregion

    #region Property Access Tests

    [Test]
    public void DialogueEntry_Properties_CanBeSetAndRetrieved()
    {
      // Arrange
      var entry = new DialogueEntry();
      var timestamp = DateTime.Now;

      // Act
      entry.Speaker = "Player";
      entry.Text = "Hello, world!";
      entry.Timestamp = timestamp;

      // Assert
      Assert.AreEqual("Player", entry.Speaker);
      Assert.AreEqual("Hello, world!", entry.Text);
      Assert.AreEqual(timestamp, entry.Timestamp);
    }

    [Test]
    public void DialogueEntry_Speaker_CanBeSetToAnyString()
    {
      // Arrange
      var entry = new DialogueEntry();

      // Act
      entry.Speaker = "NPC";
      Assert.AreEqual("NPC", entry.Speaker);

      entry.Speaker = "System";
      Assert.AreEqual("System", entry.Speaker);

      entry.Speaker = "CustomSpeaker123";
      Assert.AreEqual("CustomSpeaker123", entry.Speaker);
    }

    [Test]
    public void DialogueEntry_Text_CanBeSetToAnyString()
    {
      // Arrange
      var entry = new DialogueEntry();

      // Act
      entry.Text = "Short";
      Assert.AreEqual("Short", entry.Text);

      entry.Text = "This is a much longer dialogue text that contains multiple words and sentences.";
      Assert.AreEqual("This is a much longer dialogue text that contains multiple words and sentences.", entry.Text);

      entry.Text = "Text with\nnewlines\nand\tspecial\tcharacters!";
      Assert.AreEqual("Text with\nnewlines\nand\tspecial\tcharacters!", entry.Text);
    }

    [Test]
    public void DialogueEntry_Timestamp_CanBeSetToAnyDateTime()
    {
      // Arrange
      var entry = new DialogueEntry();
      var now = DateTime.Now;
      var past = new DateTime(2020, 1, 1, 12, 0, 0);
      var future = new DateTime(2030, 12, 31, 23, 59, 59);

      // Act & Assert
      entry.Timestamp = now;
      Assert.AreEqual(now, entry.Timestamp);

      entry.Timestamp = past;
      Assert.AreEqual(past, entry.Timestamp);

      entry.Timestamp = future;
      Assert.AreEqual(future, entry.Timestamp);
    }

    [Test]
    public void DialogueEntry_Timestamp_CanBeSetToUtcTime()
    {
      // Arrange
      var entry = new DialogueEntry();
      var utcNow = DateTime.UtcNow;

      // Act
      entry.Timestamp = utcNow;

      // Assert
      Assert.AreEqual(utcNow, entry.Timestamp);
      Assert.AreEqual(DateTimeKind.Utc, entry.Timestamp.Kind);
    }

    #endregion

    #region Object Initializer Tests

    [Test]
    public void DialogueEntry_ObjectInitializer_SetsAllProperties()
    {
      // Arrange
      var timestamp = DateTime.Now;

      // Act
      var entry = new DialogueEntry
      {
        Speaker = "Player",
        Text = "Hello, world!",
        Timestamp = timestamp
      };

      // Assert
      Assert.AreEqual("Player", entry.Speaker);
      Assert.AreEqual("Hello, world!", entry.Text);
      Assert.AreEqual(timestamp, entry.Timestamp);
    }

    #endregion

    #region Timestamp Handling Tests

    [Test]
    public void DialogueEntry_Timestamp_DefaultValueIsMinValue()
    {
      // Arrange & Act
      var entry = new DialogueEntry();

      // Assert
      Assert.AreEqual(DateTime.MinValue, entry.Timestamp);
    }

    [Test]
    public void DialogueEntry_Timestamp_CanHandleMinValue()
    {
      // Arrange
      var entry = new DialogueEntry();

      // Act
      entry.Timestamp = DateTime.MinValue;

      // Assert
      Assert.AreEqual(DateTime.MinValue, entry.Timestamp);
    }

    [Test]
    public void DialogueEntry_Timestamp_CanHandleMaxValue()
    {
      // Arrange
      var entry = new DialogueEntry();

      // Act
      entry.Timestamp = DateTime.MaxValue;

      // Assert
      Assert.AreEqual(DateTime.MaxValue, entry.Timestamp);
    }

    [Test]
    public void DialogueEntry_Timestamp_PreservesPrecision()
    {
      // Arrange
      var entry = new DialogueEntry();
      var timestamp = new DateTime(2024, 1, 15, 14, 30, 45, 123);

      // Act
      entry.Timestamp = timestamp;

      // Assert
      Assert.AreEqual(timestamp, entry.Timestamp);
      Assert.AreEqual(123, entry.Timestamp.Millisecond);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void DialogueEntry_EmptyStrings_AreValid()
    {
      // Arrange
      var entry = new DialogueEntry();

      // Act
      entry.Speaker = string.Empty;
      entry.Text = string.Empty;

      // Assert
      Assert.AreEqual(string.Empty, entry.Speaker);
      Assert.AreEqual(string.Empty, entry.Text);
    }

    [Test]
    public void DialogueEntry_WhitespaceStrings_AreValid()
    {
      // Arrange
      var entry = new DialogueEntry();

      // Act
      entry.Speaker = "   ";
      entry.Text = "\t\n\r";

      // Assert
      Assert.AreEqual("   ", entry.Speaker);
      Assert.AreEqual("\t\n\r", entry.Text);
    }

    [Test]
    public void DialogueEntry_UnicodeStrings_AreHandledCorrectly()
    {
      // Arrange
      var entry = new DialogueEntry();

      // Act
      entry.Speaker = "プレイヤー";
      entry.Text = "こんにちは、世界！";

      // Assert
      Assert.AreEqual("プレイヤー", entry.Speaker);
      Assert.AreEqual("こんにちは、世界！", entry.Text);
    }

    [Test]
    public void DialogueEntry_SpecialCharacters_AreHandledCorrectly()
    {
      // Arrange
      var entry = new DialogueEntry();

      // Act
      entry.Text = "Text with \"quotes\", 'apostrophes', and <tags>!";

      // Assert
      Assert.AreEqual("Text with \"quotes\", 'apostrophes', and <tags>!", entry.Text);
    }

    #endregion
  }
}

