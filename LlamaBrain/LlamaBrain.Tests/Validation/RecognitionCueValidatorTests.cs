using NUnit.Framework;
using LlamaBrain.Core.Retrieval;
using LlamaBrain.Core.Validation;

namespace LlamaBrain.Tests.Validation
{
    /// <summary>
    /// Tests for RecognitionCueValidator.
    /// </summary>
    [Category("RAG")]
    [Category("Validation")]
    public class RecognitionCueValidatorTests
    {
        #region Validate Tests

        [Test]
        public void Validate_NullRecognition_ReturnsNotRequired()
        {
            // Arrange
            var output = "Hello, welcome to my shop!";

            // Act
            var result = RecognitionCueValidator.Validate(output, recognition: null);

            // Assert
            Assert.That(result.CueExpected, Is.False);
            Assert.That(result.CueFound, Is.False);
            Assert.That(result.Warning, Is.Null);
        }

        [Test]
        public void Validate_NotRecognized_ReturnsNotRequired()
        {
            // Arrange
            var output = "Hello, welcome to my shop!";
            var recognition = RecognitionResult.NotRecognized();

            // Act
            var result = RecognitionCueValidator.Validate(output, recognition);

            // Assert
            Assert.That(result.CueExpected, Is.False);
            Assert.That(result.CueFound, Is.False);
        }

        [Test]
        public void Validate_LocationRecognized_FindsRememberCue()
        {
            // Arrange
            var output = "Ah, I remember you! You were here just yesterday.";
            var recognition = RecognitionResult.LocationRecognized(2, 12345, new[] { "mem-1" });

            // Act
            var result = RecognitionCueValidator.Validate(output, recognition);

            // Assert
            Assert.That(result.CueFound, Is.True);
            Assert.That(result.CueExpected, Is.True);
            Assert.That(result.MatchedCue, Is.EqualTo("remember"));
            Assert.That(result.Warning, Is.Null);
        }

        [Test]
        public void Validate_LocationRecognized_FindsBeenHereCue()
        {
            // Arrange
            var output = "You've been here before, haven't you?";
            var recognition = RecognitionResult.LocationRecognized(2, 12345, new[] { "mem-1" });

            // Act
            var result = RecognitionCueValidator.Validate(output, recognition);

            // Assert
            Assert.That(result.CueFound, Is.True);
            Assert.That(result.MatchedCue, Is.EqualTo("been here"));
        }

        [Test]
        public void Validate_LocationRecognized_NoCue_ReturnsWarning()
        {
            // Arrange
            var output = "Hello, welcome to my shop! What can I do for you today?";
            var recognition = RecognitionResult.LocationRecognized(2, 12345, new[] { "mem-1" });

            // Act
            var result = RecognitionCueValidator.Validate(output, recognition);

            // Assert
            Assert.That(result.CueFound, Is.False);
            Assert.That(result.CueExpected, Is.True);
            Assert.That(result.Warning, Is.Not.Null);
            Assert.That(result.Warning, Does.Contain("Location"));
        }

        [Test]
        public void Validate_TopicRecognized_FindsDiscussedCue()
        {
            // Arrange
            var output = "We've already discussed this topic at length.";
            var recognition = RecognitionResult.TopicRecognized(3, 12345, new[] { "mem-1" }, 0.8f, "dragons");

            // Act
            var result = RecognitionCueValidator.Validate(output, recognition);

            // Assert
            Assert.That(result.CueFound, Is.True);
            Assert.That(result.MatchedCue, Is.EqualTo("already discussed"));
        }

        [Test]
        public void Validate_TopicRecognized_FindsMentionedCue()
        {
            // Arrange
            var output = "As I mentioned before, dragons are dangerous creatures.";
            var recognition = RecognitionResult.TopicRecognized(2, 12345, new[] { "mem-1" }, 0.75f, "dragons");

            // Act
            var result = RecognitionCueValidator.Validate(output, recognition);

            // Assert
            Assert.That(result.CueFound, Is.True);
            Assert.That(result.MatchedCue, Is.EqualTo("mentioned before"));
        }

        [Test]
        public void Validate_CaseInsensitive()
        {
            // Arrange
            var output = "I REMEMBER seeing you here before!";
            var recognition = RecognitionResult.LocationRecognized(2, 12345, new[] { "mem-1" });

            // Act
            var result = RecognitionCueValidator.Validate(output, recognition);

            // Assert
            Assert.That(result.CueFound, Is.True);
            Assert.That(result.MatchedCue, Is.EqualTo("remember"));
        }

        [Test]
        public void Validate_EmptyOutput_ReturnsWarning()
        {
            // Arrange
            var output = "";
            var recognition = RecognitionResult.LocationRecognized(2, 12345, new[] { "mem-1" });

            // Act
            var result = RecognitionCueValidator.Validate(output, recognition);

            // Assert
            Assert.That(result.CueFound, Is.False);
            Assert.That(result.Warning, Is.Not.Null);
        }

        #endregion

        #region ContainsAnyCue Tests

        [Test]
        public void ContainsAnyCue_WithRecognitionPhrase_ReturnsTrue()
        {
            // Arrange
            var text = "I remember you from before.";

            // Act
            var result = RecognitionCueValidator.ContainsAnyCue(text);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ContainsAnyCue_WithNoRecognitionPhrase_ReturnsFalse()
        {
            // Arrange
            var text = "Hello, welcome to my shop!";

            // Act
            var result = RecognitionCueValidator.ContainsAnyCue(text);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ContainsAnyCue_EmptyString_ReturnsFalse()
        {
            // Act
            var result = RecognitionCueValidator.ContainsAnyCue("");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ContainsAnyCue_NullString_ReturnsFalse()
        {
            // Act
            var result = RecognitionCueValidator.ContainsAnyCue(null!);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion
    }
}
