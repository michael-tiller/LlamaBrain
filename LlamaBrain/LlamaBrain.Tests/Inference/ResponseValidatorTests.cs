using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Inference
{
  /// <summary>
  /// Tests for ResponseValidator.
  /// </summary>
  public class ResponseValidatorTests
  {
    private ResponseValidator _validator = null!;
    private StateSnapshot _defaultSnapshot = null!;

    [SetUp]
    public void SetUp()
    {
      _validator = new ResponseValidator();
      
      var context = new InteractionContext { NpcId = "npc-001" };
      var constraints = new ConstraintSet();
      _defaultSnapshot = new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .Build();
    }

    #region Basic Validation Tests

    [Test]
    public void Validate_EmptyResponse_ReturnsInvalidFormat()
    {
      // Act
      var result = _validator.Validate("", _defaultSnapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.InvalidFormat));
      Assert.That(result.IsValid, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("empty"));
    }

    [Test]
    public void Validate_WhitespaceOnly_ReturnsInvalidFormat()
    {
      // Act
      var result = _validator.Validate("   \n\t  ", _defaultSnapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.InvalidFormat));
      Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_ValidResponse_ReturnsValid()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("Hello, traveler!", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.Valid));
      Assert.That(result.IsValid, Is.True);
      Assert.That(result.Violations, Is.Empty);
    }

    #endregion

    #region Prohibition Validation Tests

    [Test]
    public void Validate_ProhibitionViolated_ReturnsProhibitionViolated()
    {
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "Cannot reveal secrets", "Do not reveal secrets", "secret"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("I know a secret about the king", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Violations.Count, Is.EqualTo(1));
      Assert.That(result.Violations[0].Constraint.Type, Is.EqualTo(ConstraintType.Prohibition));
    }

    [Test]
    public void Validate_ProhibitionWithMultiplePatterns_DetectsAnyMatch()
    {
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-bad-words", "Cannot use bad words", "Do not use bad words", "secret", "hidden", "confidential"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("This is confidential information", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
      Assert.That(result.Violations.Count, Is.EqualTo(1));
    }

    [Test]
    public void Validate_ProhibitionCaseInsensitive_DetectsViolation()
    {
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "Cannot reveal secrets", "Do not reveal secrets", "secret"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("This is a SECRET message", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    [Test]
    public void Validate_ProhibitionWithRegex_DetectsViolation()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var prohibition = Constraint.Prohibition("no-numbers", "Cannot use numbers", "Do not use numbers");
      prohibition.ValidationPatterns.Add("/\\d+/");
      var constraintSet = new ConstraintSet();
      constraintSet.Add(prohibition);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraintSet)
        .Build();

      // Act
      var result = _validator.Validate("I have 5 apples", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    [Test]
    public void Validate_ProhibitionNotViolated_ReturnsValid()
    {
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "Cannot reveal secrets", "Do not reveal secrets", "secret"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("Hello, how are you today?", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.Valid));
      Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_ProhibitionViolation_IncludesViolatingText()
    {
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "Cannot reveal secrets", "Do not reveal secrets", "secret"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("I know a secret about the king", snapshot);

      // Assert
      Assert.That(result.Violations[0].ViolatingText, Is.Not.Null);
      Assert.That(result.Violations[0].ViolatingText, Does.Contain("secret"));
    }

    #endregion

    #region Requirement Validation Tests

    [Test]
    public void Validate_RequirementMet_ReturnsValid()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var requirement = Constraint.Requirement("greet", "Must greet player", "Greet the player");
      requirement.ValidationPatterns.AddRange(new[] { "hello", "greetings" });
      constraints.Add(requirement);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("Hello, traveler! How may I help you?", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.Valid));
      Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_RequirementNotMet_ReturnsRequirementNotMet()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var requirement = Constraint.Requirement("greet", "Must greet player", "Greet the player");
      requirement.ValidationPatterns.AddRange(new[] { "hello", "greetings" });
      constraints.Add(requirement);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("What do you want?", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.RequirementNotMet));
      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Violations.Count, Is.EqualTo(1));
      Assert.That(result.Violations[0].Constraint.Type, Is.EqualTo(ConstraintType.Requirement));
    }

    [Test]
    public void Validate_RequirementWithMultiplePatterns_AnyMatchSatisfies()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var requirement = Constraint.Requirement("greet", "Must greet", "Greet");
      requirement.ValidationPatterns.AddRange(new[] { "hello", "hi", "greetings" });
      constraints.Add(requirement);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("Hi there!", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.Valid));
    }

    [Test]
    public void Validate_RequirementCaseInsensitive_DetectsMatch()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var requirement = Constraint.Requirement("greet", "Must greet", "Greet");
      requirement.ValidationPatterns.Add("hello");
      constraints.Add(requirement);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("HELLO, traveler!", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.Valid));
    }

    [Test]
    public void Validate_RequirementWithRegex_DetectsMatch()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var requirement = Constraint.Requirement("has-number", "Must include number", "Include a number");
      requirement.ValidationPatterns.Add("/\\d+/");
      constraints.Add(requirement);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("I have 5 apples", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.Valid));
    }

    [Test]
    public void Validate_RequirementWithoutPatterns_ReturnsValid()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var requirement = Constraint.Requirement("descriptive", "Must be descriptive", "Be descriptive");
      // No patterns added - descriptive only
      constraints.Add(requirement);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("Any response", snapshot);

      // Assert
      // Descriptive requirements without patterns cannot be validated automatically
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.Valid));
    }

    #endregion

    #region Mixed Constraint Tests

    [Test]
    public void Validate_MultipleProhibitions_DetectsAllViolations()
    {
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "No secrets", "Do not reveal secrets", "secret"));
      constraints.Add(Constraint.Prohibition("no-lies", "No lies", "Do not lie", "lie"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("I will lie about the secret", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
      Assert.That(result.Violations.Count, Is.EqualTo(2));
    }

    [Test]
    public void Validate_ProhibitionAndRequirement_ProhibitionTakesPrecedence()
    {
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "No secrets", "Do not reveal secrets", "secret"));
      var requirement = Constraint.Requirement("greet", "Must greet", "Greet");
      requirement.ValidationPatterns.Add("hello");
      constraints.Add(requirement);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("Hello, I know a secret", snapshot);

      // Assert
      // Prohibition violation takes precedence
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    [Test]
    public void Validate_RequirementMetButProhibitionViolated_ReturnsProhibitionViolated()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var requirement = Constraint.Requirement("greet", "Must greet", "Greet");
      requirement.ValidationPatterns.Add("hello");
      constraints.Add(requirement);
      constraints.Add(Constraint.Prohibition("no-secrets", "No secrets", "Do not reveal secrets", "secret"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("Hello, I know a secret", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
      Assert.That(result.Violations.Count, Is.EqualTo(1));
    }

    #endregion

    #region Pattern Extraction Tests

    [Test]
    public void Validate_ExtractsQuotedPatterns_FromDescription()
    {
      // Arrange
      var constraints = new ConstraintSet();
      // Description with quoted text should extract patterns
      var prohibition = Constraint.Prohibition("test", "Cannot say \"secret\" or 'hidden'", "Do not say secret or hidden");
      prohibition.ValidationPatterns.Clear(); // Clear explicit patterns to test extraction
      constraints.Add(prohibition);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("I know a secret", snapshot);

      // Assert
      // Should extract "secret" from description
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    #endregion

    #region ValidationResult Tests

    [Test]
    public void ValidationResult_ToString_Valid_ReturnsFormattedString()
    {
      // Arrange
      var result = new ValidationResult(ValidationOutcome.Valid, new List<ConstraintViolation>());

      // Act
      var str = result.ToString();

      // Assert
      Assert.That(str, Does.Contain("ValidationResult[Valid]"));
    }

    [Test]
    public void ValidationResult_ToString_WithViolations_ReturnsFormattedString()
    {
      // Arrange
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibition("test", "Test", "Do not"),
          "Violation"
        )
      };
      var result = new ValidationResult(ValidationOutcome.ProhibitionViolated, violations);

      // Act
      var str = result.ToString();

      // Assert
      Assert.That(str, Does.Contain("ValidationResult[ProhibitionViolated]"));
      Assert.That(str, Does.Contain("1 violations"));
    }

    #endregion

    #region Logging Tests

    [Test]
    public void Validate_WithLogging_CallsOnLog()
    {
      // Arrange
      var logMessages = new List<string>();
      _validator.OnLog = msg => logMessages.Add(msg);
      
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "No secrets", "Do not reveal secrets", "secret"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      _validator.Validate("I know a secret", snapshot);

      // Assert
      Assert.That(logMessages.Count, Is.GreaterThan(0));
      Assert.That(logMessages, Has.Some.Contain("Prohibition violated"));
    }

    #endregion

    #region Edge Case Pattern Matching Tests

    [Test]
    public void Validate_PatternInMiddleOfWord_DetectsViolation()
    {
      // This is current behavior - substring match
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secret", "No secret", "Do not reveal secrets", "secret"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act - "secretary" contains "secret"
      var result = _validator.Validate("I spoke to the secretary", snapshot);

      // Assert - Current behavior: substring matching will flag this
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    [Test]
    public void Validate_WordBoundaryRegex_MatchesOnlyWholeWords()
    {
      // Arrange - Use word boundary regex
      var constraints = new ConstraintSet();
      var prohibition = Constraint.Prohibition("no-secret-word", "No secret word", "Do not say secret");
      prohibition.ValidationPatterns.Add(@"/\bsecret\b/");
      constraints.Add(prohibition);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act - "secretary" should NOT match word-boundary regex
      var result = _validator.Validate("I spoke to the secretary", snapshot);

      // Assert - Word boundary regex should not match
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.Valid));
    }

    [Test]
    public void Validate_SpecialCharactersInPattern_MatchesLiteral()
    {
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-special", "No special chars", "Do not use special chars", "$100"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("That costs $100 exactly", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    [Test]
    public void Validate_UnicodeCharacters_MatchesCorrectly()
    {
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-emoji", "No emoji", "Do not use emoji", "🔥"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("This is fire 🔥!", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    [Test]
    public void Validate_VeryLongPattern_MatchesCorrectly()
    {
      // Arrange
      var longPattern = "this is a very long pattern that should still be matched correctly in the response";
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-long", "No long pattern", "Do not say this", longPattern));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate($"I will say that {longPattern} now", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    [Test]
    public void Validate_EmptyPatternList_ReturnsValid()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var prohibition = Constraint.Prohibition("empty-patterns", "Empty", "");
      prohibition.ValidationPatterns.Clear();
      constraints.Add(prohibition);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("Any response here", snapshot);

      // Assert - No patterns to match
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.Valid));
    }

    [Test]
    public void Validate_InvalidRegexPattern_FallsThroughToKeyword()
    {
      // Arrange - Invalid regex that will fail to compile
      var constraints = new ConstraintSet();
      var prohibition = Constraint.Prohibition("bad-regex", "Bad regex", "Do not");
      prohibition.ValidationPatterns.Add("/[invalid/"); // Unclosed bracket
      prohibition.ValidationPatterns.Add("secret"); // Fallback keyword
      constraints.Add(prohibition);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("I know a secret", snapshot);

      // Assert - Should fall through to keyword check
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    [Test]
    public void Validate_NewlineInResponse_HandledCorrectly()
    {
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secret", "No secret", "Do not reveal", "secret"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("Line one\nI know a secret\nLine three", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    [Test]
    public void Validate_VeryLongResponse_HandlesPerformance()
    {
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secret", "No secret", "Do not reveal", "secret"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Build a very long response (100KB)
      var longResponse = new string('x', 100000) + " secret " + new string('y', 100000);

      // Act
      var result = _validator.Validate(longResponse, snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    #endregion

    #region Complex Rule Combination Tests

    [Test]
    public void Validate_MultipleRequirements_AllMustBeMet()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var req1 = Constraint.Requirement("greet", "Must greet", "Greet");
      req1.ValidationPatterns.Add("hello");
      var req2 = Constraint.Requirement("name", "Must use name", "Use name");
      req2.ValidationPatterns.Add("traveler");
      constraints.Add(req1);
      constraints.Add(req2);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act - Only one requirement met
      var result = _validator.Validate("Hello there!", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.RequirementNotMet));
      Assert.That(result.Violations.Count, Is.EqualTo(1));
      Assert.That(result.Violations[0].Constraint.Id, Is.EqualTo("name"));
    }

    [Test]
    public void Validate_MultipleRequirements_BothMet_ReturnsValid()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var req1 = Constraint.Requirement("greet", "Must greet", "Greet");
      req1.ValidationPatterns.Add("hello");
      var req2 = Constraint.Requirement("name", "Must use name", "Use name");
      req2.ValidationPatterns.Add("traveler");
      constraints.Add(req1);
      constraints.Add(req2);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("Hello, traveler!", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.Valid));
    }

    [Test]
    public void Validate_ManyConstraints_ProcessesAll()
    {
      // Arrange - 10 prohibitions
      var constraints = new ConstraintSet();
      for (int i = 0; i < 10; i++)
      {
        constraints.Add(Constraint.Prohibition($"no-word{i}", $"No word{i}", $"Do not say word{i}", $"forbidden{i}"));
      }
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act - Violate the 7th one
      var result = _validator.Validate("I will say forbidden6 now", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
      Assert.That(result.Violations.Count, Is.EqualTo(1));
      Assert.That(result.Violations[0].Constraint.Id, Is.EqualTo("no-word6"));
    }

    [Test]
    public void Validate_OverlappingPatterns_ReportsAll()
    {
      // Arrange - Two separate words, both prohibited
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secret", "No secret", "Do not say secret", "secret"));
      constraints.Add(Constraint.Prohibition("no-hidden", "No hidden", "Do not say hidden", "hidden"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act - Response contains both prohibited words
      var result = _validator.Validate("The secret is hidden in the cave", snapshot);

      // Assert - Both should be detected
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
      Assert.That(result.Violations.Count, Is.EqualTo(2));
    }

    [Test]
    public void Validate_RequirementNotMet_NoProhibition_ReturnsRequirementNotMet()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var requirement = Constraint.Requirement("greet", "Must greet", "Greet");
      requirement.ValidationPatterns.Add("hello");
      constraints.Add(requirement);
      constraints.Add(Constraint.Prohibition("no-secret", "No secret", "Do not", "secret"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act - No greeting, no secret mention
      var result = _validator.Validate("What do you want?", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.RequirementNotMet));
    }

    #endregion

    #region Error Message Accuracy Tests

    [Test]
    public void Validate_ProhibitionViolation_DescriptionContainsConstraintInfo()
    {
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("secret-id", "Cannot reveal secrets", "Do not reveal secrets", "secret"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("I know a secret", snapshot);

      // Assert
      Assert.That(result.Violations[0].Description, Does.Contain("Cannot reveal secrets"));
    }

    [Test]
    public void Validate_RequirementNotMet_DescriptionContainsConstraintInfo()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var requirement = Constraint.Requirement("greeting-req", "Must greet the player warmly", "Greet warmly");
      requirement.ValidationPatterns.Add("hello");
      constraints.Add(requirement);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("What do you want?", snapshot);

      // Assert
      Assert.That(result.Violations[0].Description, Does.Contain("Must greet the player warmly"));
    }

    [Test]
    public void Validate_InvalidFormat_ErrorMessageDescriptive()
    {
      // Act
      var result = _validator.Validate("", _defaultSnapshot);

      // Assert
      Assert.That(result.ErrorMessage, Is.Not.Null);
      Assert.That(result.ErrorMessage, Does.Contain("empty"));
    }

    [Test]
    public void Validate_ViolatingText_TruncatesLongMatches()
    {
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secret", "No secret", "Do not", "secret"));
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act - The word appears early but response continues long after
      var result = _validator.Validate("I have a secret that is very long and continues for many more words", snapshot);

      // Assert - ViolatingText should be captured but reasonable length
      Assert.That(result.Violations[0].ViolatingText, Is.Not.Null);
      Assert.That(result.Violations[0].ViolatingText!.Length, Is.LessThanOrEqualTo(50));
    }

    #endregion

    #region Pattern Extraction Edge Cases

    [Test]
    public void ExtractPatterns_DoubleQuotes_ExtractsCorrectly()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var prohibition = Constraint.Prohibition("test", "Cannot say \"forbidden\" in response", "Do not say forbidden");
      prohibition.ValidationPatterns.Clear();
      constraints.Add(prohibition);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("That word is forbidden here", snapshot);

      // Assert - Should extract "forbidden" from description
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    [Test]
    public void ExtractPatterns_SingleQuotes_ExtractsCorrectly()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var prohibition = Constraint.Prohibition("test", "Cannot say 'banned' in response", "Do not say banned");
      prohibition.ValidationPatterns.Clear();
      constraints.Add(prohibition);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("That word is banned here", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    [Test]
    public void ExtractPatterns_MixedQuotes_ExtractsAll()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var prohibition = Constraint.Prohibition("test", "Cannot say \"word1\" or 'word2' in response", "Do not");
      prohibition.ValidationPatterns.Clear();
      constraints.Add(prohibition);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act - Test word2
      var result = _validator.Validate("I will say word2 now", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    [Test]
    public void ExtractPatterns_KeywordAfterAbout_Extracts()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var prohibition = Constraint.Prohibition("test", "Do not talk about dragons", "Do not mention dragons");
      prohibition.ValidationPatterns.Clear();
      constraints.Add(prohibition);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("Let me tell you about the dragons", snapshot);

      // Assert - Should extract "dragons" from "about dragons"
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    [Test]
    public void ExtractPatterns_KeywordAfterMention_Extracts()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var prohibition = Constraint.Prohibition("test", "Do not mention treasure", "");
      prohibition.ValidationPatterns.Clear();
      constraints.Add(prohibition);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("There is treasure in the cave", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    [Test]
    public void ExtractPatterns_ShortWord_Ignored()
    {
      // Arrange - "of" is too short (2 chars)
      var constraints = new ConstraintSet();
      var prohibition = Constraint.Prohibition("test", "Do not speak of it", "");
      prohibition.ValidationPatterns.Clear();
      constraints.Add(prohibition);
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("Any response here", snapshot);

      // Assert - "of" should be ignored due to length
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.Valid));
    }

    #endregion

    #region ConstraintViolation Tests

    [Test]
    public void ConstraintViolation_ToString_ReturnsFormattedString()
    {
      // Arrange
      var constraint = Constraint.Prohibition("test-id", "Test prohibition", "Do not");
      var violation = new ConstraintViolation(constraint, "Test description", "violating text");

      // Act
      var str = violation.ToString();

      // Assert
      Assert.That(str, Does.Contain("[Prohibition]"));
      Assert.That(str, Does.Contain("Test description"));
    }

    [Test]
    public void ConstraintViolation_WithoutViolatingText_Works()
    {
      // Arrange
      var constraint = Constraint.Requirement("test-id", "Test requirement", "Must do");
      var violation = new ConstraintViolation(constraint, "Requirement not met");

      // Assert
      Assert.That(violation.ViolatingText, Is.Null);
      Assert.That(violation.Description, Is.EqualTo("Requirement not met"));
    }

    #endregion

    #region Null/Empty Constraint Set Tests

    [Test]
    public void Validate_EmptyConstraintSet_ReturnsValid()
    {
      // Arrange
      var constraints = new ConstraintSet();
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      // Act
      var result = _validator.Validate("Any response", snapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.Valid));
    }

    [Test]
    public void Validate_NullResponse_ReturnsInvalidFormat()
    {
      // Act
      var result = _validator.Validate(null!, _defaultSnapshot);

      // Assert
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.InvalidFormat));
    }

    #endregion
  }
}

