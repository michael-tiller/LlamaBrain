using System;
using System.Collections.Generic;
using System.Linq;
using LlamaBrain.Core.Retrieval;

namespace LlamaBrain.Core.Validation
{
    /// <summary>
    /// Result of recognition cue validation.
    /// </summary>
    public sealed class RecognitionCueValidationResult
    {
        /// <summary>
        /// Whether a recognition cue was found in the output.
        /// </summary>
        public bool CueFound { get; }

        /// <summary>
        /// The specific cue phrase that was matched, if any.
        /// </summary>
        public string? MatchedCue { get; }

        /// <summary>
        /// The recognition type that was expected.
        /// </summary>
        public RecognitionType ExpectedType { get; }

        /// <summary>
        /// Whether the cue was expected (recognition was injected into prompt).
        /// </summary>
        public bool CueExpected { get; }

        /// <summary>
        /// Warning message if cue was expected but not found.
        /// </summary>
        public string? Warning { get; }

        private RecognitionCueValidationResult(
            bool cueFound,
            string? matchedCue,
            RecognitionType expectedType,
            bool cueExpected,
            string? warning)
        {
            CueFound = cueFound;
            MatchedCue = matchedCue;
            ExpectedType = expectedType;
            CueExpected = cueExpected;
            Warning = warning;
        }

        /// <summary>
        /// Creates a result indicating no cue was expected or needed.
        /// </summary>
        public static RecognitionCueValidationResult NotRequired()
        {
            return new RecognitionCueValidationResult(
                cueFound: false,
                matchedCue: null,
                expectedType: RecognitionType.None,
                cueExpected: false,
                warning: null);
        }

        /// <summary>
        /// Creates a result indicating cue was found as expected.
        /// </summary>
        public static RecognitionCueValidationResult CuePresent(string matchedCue, RecognitionType expectedType)
        {
            return new RecognitionCueValidationResult(
                cueFound: true,
                matchedCue: matchedCue,
                expectedType: expectedType,
                cueExpected: true,
                warning: null);
        }

        /// <summary>
        /// Creates a result indicating cue was expected but not found.
        /// </summary>
        public static RecognitionCueValidationResult CueMissing(RecognitionType expectedType)
        {
            return new RecognitionCueValidationResult(
                cueFound: false,
                matchedCue: null,
                expectedType: expectedType,
                cueExpected: true,
                warning: $"Expected {expectedType} recognition cue not found in output");
        }
    }

    /// <summary>
    /// Validates that generated output contains appropriate recognition cues
    /// when a RECOGNITION block was injected into the prompt.
    /// This is soft validation - it warns but does not block.
    /// </summary>
    public static class RecognitionCueValidator
    {
        /// <summary>
        /// Phrases that indicate location recognition.
        /// </summary>
        private static readonly string[] LocationCues = new[]
        {
            "remember", "been here", "recognize", "familiar", "before",
            "again", "back to", "returned", "last time", "previously",
            "know this place", "seen this", "recall", "already"
        };

        /// <summary>
        /// Phrases that indicate topic recognition.
        /// </summary>
        private static readonly string[] TopicCues = new[]
        {
            "already discussed", "talked about", "mentioned before",
            "keep asking", "again about", "you've said", "we discussed",
            "remember telling", "as I said", "like I said", "told you",
            "we talked", "mentioned", "brought up"
        };

        /// <summary>
        /// Phrases that indicate conversation pattern recognition.
        /// </summary>
        private static readonly string[] ConversationCues = new[]
        {
            "you keep", "always ask", "every time", "this again",
            "same thing", "again", "repeatedly", "pattern"
        };

        /// <summary>
        /// Validates whether the output contains appropriate recognition cues.
        /// </summary>
        /// <param name="outputText">The generated NPC output text.</param>
        /// <param name="recognition">The recognition result that was injected (or null if none).</param>
        /// <returns>Validation result indicating whether cue was found.</returns>
        public static RecognitionCueValidationResult Validate(string outputText, RecognitionResult? recognition)
        {
            // No recognition was injected - nothing to validate
            if (recognition == null || !recognition.Recognized)
            {
                return RecognitionCueValidationResult.NotRequired();
            }

            var normalizedOutput = outputText?.ToLowerInvariant() ?? "";
            var cues = GetCuesForType(recognition.RecognitionType);

            foreach (var cue in cues)
            {
                if (normalizedOutput.Contains(cue))
                {
                    return RecognitionCueValidationResult.CuePresent(cue, recognition.RecognitionType);
                }
            }

            // Cue was expected but not found
            return RecognitionCueValidationResult.CueMissing(recognition.RecognitionType);
        }

        /// <summary>
        /// Gets the appropriate cue phrases for a recognition type.
        /// </summary>
        private static IReadOnlyList<string> GetCuesForType(RecognitionType type)
        {
            switch (type)
            {
                case RecognitionType.Location:
                    return LocationCues;
                case RecognitionType.Topic:
                    return TopicCues;
                case RecognitionType.Conversation:
                    return ConversationCues;
                default:
                    // Fall back to all cues
                    return LocationCues.Concat(TopicCues).Concat(ConversationCues).ToArray();
            }
        }

        /// <summary>
        /// Checks if any recognition cue is present in the text.
        /// Useful for quick checks without full validation context.
        /// </summary>
        /// <param name="text">Text to check.</param>
        /// <returns>True if any recognition cue phrase is found.</returns>
        public static bool ContainsAnyCue(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            var normalized = text.ToLowerInvariant();
            return LocationCues.Any(c => normalized.Contains(c)) ||
                   TopicCues.Any(c => normalized.Contains(c)) ||
                   ConversationCues.Any(c => normalized.Contains(c));
        }
    }
}
