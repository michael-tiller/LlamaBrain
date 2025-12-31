using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Expectancy
{
  /// <summary>
  /// Tests for Constraint and ConstraintSet classes.
  /// </summary>
  public class ConstraintTests
  {
    #region Constraint Tests

    [Test]
    public void Constraint_Prohibition_CreatesCorrectType()
    {
      // Act
      var constraint = Constraint.Prohibition(
        "no-secrets",
        "Cannot reveal secrets",
        "Do not reveal any secret information",
        "secret", "hidden", "confidential"
      );

      // Assert
      Assert.That(constraint.Id, Is.EqualTo("no-secrets"));
      Assert.That(constraint.Type, Is.EqualTo(ConstraintType.Prohibition));
      Assert.That(constraint.Description, Is.EqualTo("Cannot reveal secrets"));
      Assert.That(constraint.PromptInjection, Is.EqualTo("Do not reveal any secret information"));
      Assert.That(constraint.ValidationPatterns, Contains.Item("secret"));
      Assert.That(constraint.ValidationPatterns, Contains.Item("hidden"));
      Assert.That(constraint.ValidationPatterns, Contains.Item("confidential"));
      Assert.That(constraint.Severity, Is.EqualTo(ConstraintSeverity.Hard)); // Default
    }

    [Test]
    public void Constraint_Requirement_CreatesCorrectType()
    {
      // Act
      var constraint = Constraint.Requirement(
        "stay-in-character",
        "Must stay in character",
        "Always respond as your character would"
      );

      // Assert
      Assert.That(constraint.Id, Is.EqualTo("stay-in-character"));
      Assert.That(constraint.Type, Is.EqualTo(ConstraintType.Requirement));
      Assert.That(constraint.Description, Is.EqualTo("Must stay in character"));
      Assert.That(constraint.PromptInjection, Is.EqualTo("Always respond as your character would"));
    }

    [Test]
    public void Constraint_Permission_CreatesCorrectType()
    {
      // Act
      var constraint = Constraint.Permission(
        "can-joke",
        "May tell jokes",
        "You are allowed to make jokes and be humorous"
      );

      // Assert
      Assert.That(constraint.Id, Is.EqualTo("can-joke"));
      Assert.That(constraint.Type, Is.EqualTo(ConstraintType.Permission));
      Assert.That(constraint.Description, Is.EqualTo("May tell jokes"));
      Assert.That(constraint.PromptInjection, Is.EqualTo("You are allowed to make jokes and be humorous"));
    }

    [Test]
    public void Constraint_ToString_ReturnsFormattedString()
    {
      // Arrange
      var constraint = new Constraint
      {
        Id = "test",
        Type = ConstraintType.Prohibition,
        Severity = ConstraintSeverity.Critical,
        Description = "Test constraint"
      };

      // Act
      var result = constraint.ToString();

      // Assert
      Assert.That(result, Is.EqualTo("[Prohibition:Critical] Test constraint"));
    }

    #endregion

    #region ConstraintSet Tests

    [Test]
    public void ConstraintSet_Add_AddsConstraint()
    {
      // Arrange
      var set = new ConstraintSet();
      var constraint = Constraint.Prohibition("test", "Test", "Test injection");

      // Act
      set.Add(constraint);

      // Assert
      Assert.That(set.Count, Is.EqualTo(1));
      Assert.That(set.HasConstraints, Is.True);
      Assert.That(set.Contains("test"), Is.True);
    }

    [Test]
    public void ConstraintSet_Add_IgnoresDuplicateIds()
    {
      // Arrange
      var set = new ConstraintSet();
      var constraint1 = Constraint.Prohibition("test", "Test 1", "Injection 1");
      var constraint2 = Constraint.Prohibition("test", "Test 2", "Injection 2");

      // Act
      set.Add(constraint1);
      set.Add(constraint2);

      // Assert
      Assert.That(set.Count, Is.EqualTo(1));
      Assert.That(set.Get("test").Description, Is.EqualTo("Test 1")); // First one wins
    }

    [Test]
    public void ConstraintSet_Add_IgnoresNull()
    {
      // Arrange
      var set = new ConstraintSet();

      // Act
      set.Add(null!);

      // Assert
      Assert.That(set.Count, Is.EqualTo(0));
    }

    [Test]
    public void ConstraintSet_AddRange_AddsMultipleConstraints()
    {
      // Arrange
      var set = new ConstraintSet();
      var constraints = new List<Constraint>
      {
        Constraint.Prohibition("p1", "Prohibition 1", "No X"),
        Constraint.Requirement("r1", "Requirement 1", "Must Y"),
        Constraint.Permission("perm1", "Permission 1", "May Z")
      };

      // Act
      set.AddRange(constraints);

      // Assert
      Assert.That(set.Count, Is.EqualTo(3));
    }

    [Test]
    public void ConstraintSet_Remove_RemovesConstraint()
    {
      // Arrange
      var set = new ConstraintSet();
      set.Add(Constraint.Prohibition("test", "Test", "Injection"));

      // Act
      var removed = set.Remove("test");

      // Assert
      Assert.That(removed, Is.True);
      Assert.That(set.Count, Is.EqualTo(0));
      Assert.That(set.Contains("test"), Is.False);
    }

    [Test]
    public void ConstraintSet_Remove_ReturnsFalseForMissingId()
    {
      // Arrange
      var set = new ConstraintSet();

      // Act
      var removed = set.Remove("nonexistent");

      // Assert
      Assert.That(removed, Is.False);
    }

    [Test]
    public void ConstraintSet_Get_ReturnsConstraintById()
    {
      // Arrange
      var set = new ConstraintSet();
      var constraint = Constraint.Prohibition("test", "Test", "Injection");
      set.Add(constraint);

      // Act
      var retrieved = set.Get("test");

      // Assert
      Assert.That(retrieved, Is.SameAs(constraint));
    }

    [Test]
    public void ConstraintSet_Get_ReturnsNullForMissingId()
    {
      // Arrange
      var set = new ConstraintSet();

      // Act
      var retrieved = set.Get("nonexistent");

      // Assert
      Assert.That(retrieved, Is.Null);
    }

    [Test]
    public void ConstraintSet_Clear_RemovesAllConstraints()
    {
      // Arrange
      var set = new ConstraintSet();
      set.Add(Constraint.Prohibition("p1", "P1", "I1"));
      set.Add(Constraint.Requirement("r1", "R1", "I2"));

      // Act
      set.Clear();

      // Assert
      Assert.That(set.Count, Is.EqualTo(0));
      Assert.That(set.HasConstraints, Is.False);
    }

    [Test]
    public void ConstraintSet_Prohibitions_FiltersCorrectly()
    {
      // Arrange
      var set = new ConstraintSet();
      set.Add(Constraint.Prohibition("p1", "Prohibition", "No X"));
      set.Add(Constraint.Requirement("r1", "Requirement", "Must Y"));
      set.Add(Constraint.Permission("perm1", "Permission", "May Z"));

      // Act
      var prohibitions = new List<Constraint>(set.Prohibitions);

      // Assert
      Assert.That(prohibitions.Count, Is.EqualTo(1));
      Assert.That(prohibitions[0].Id, Is.EqualTo("p1"));
    }

    [Test]
    public void ConstraintSet_Requirements_FiltersCorrectly()
    {
      // Arrange
      var set = new ConstraintSet();
      set.Add(Constraint.Prohibition("p1", "Prohibition", "No X"));
      set.Add(Constraint.Requirement("r1", "Requirement", "Must Y"));
      set.Add(Constraint.Permission("perm1", "Permission", "May Z"));

      // Act
      var requirements = new List<Constraint>(set.Requirements);

      // Assert
      Assert.That(requirements.Count, Is.EqualTo(1));
      Assert.That(requirements[0].Id, Is.EqualTo("r1"));
    }

    [Test]
    public void ConstraintSet_Permissions_FiltersCorrectly()
    {
      // Arrange
      var set = new ConstraintSet();
      set.Add(Constraint.Prohibition("p1", "Prohibition", "No X"));
      set.Add(Constraint.Requirement("r1", "Requirement", "Must Y"));
      set.Add(Constraint.Permission("perm1", "Permission", "May Z"));

      // Act
      var permissions = new List<Constraint>(set.Permissions);

      // Assert
      Assert.That(permissions.Count, Is.EqualTo(1));
      Assert.That(permissions[0].Id, Is.EqualTo("perm1"));
    }

    [Test]
    public void ConstraintSet_Empty_ReturnsEmptySet()
    {
      // Act
      var set = ConstraintSet.Empty;

      // Assert
      Assert.That(set.Count, Is.EqualTo(0));
      Assert.That(set.HasConstraints, Is.False);
    }

    [Test]
    public void ConstraintSet_ToString_ReturnsFormattedSummary()
    {
      // Arrange
      var set = new ConstraintSet();
      set.Add(Constraint.Prohibition("p1", "P1", "I1"));
      set.Add(Constraint.Prohibition("p2", "P2", "I2"));
      set.Add(Constraint.Requirement("r1", "R1", "I3"));
      set.Add(Constraint.Permission("perm1", "Perm1", "I4"));

      // Act
      var result = set.ToString();

      // Assert
      Assert.That(result, Is.EqualTo("ConstraintSet[2 prohibitions, 1 requirements, 1 permissions]"));
    }

    #endregion

    #region ToPromptInjection Tests

    [Test]
    public void ToPromptInjection_EmptySet_ReturnsEmptyString()
    {
      // Arrange
      var set = new ConstraintSet();

      // Act
      var result = set.ToPromptInjection();

      // Assert
      Assert.That(result, Is.Empty);
    }

    [Test]
    public void ToPromptInjection_WithProhibitions_FormatsCorrectly()
    {
      // Arrange
      var set = new ConstraintSet();
      set.Add(Constraint.Prohibition("p1", "P1", "Do not reveal secrets"));
      set.Add(Constraint.Prohibition("p2", "P2", "Never break character"));

      // Act
      var result = set.ToPromptInjection();

      // Assert
      Assert.That(result, Contains.Substring("[CONSTRAINTS]"));
      Assert.That(result, Contains.Substring("You must NOT:"));
      Assert.That(result, Contains.Substring("- Do not reveal secrets"));
      Assert.That(result, Contains.Substring("- Never break character"));
      Assert.That(result, Contains.Substring("[/CONSTRAINTS]"));
    }

    [Test]
    public void ToPromptInjection_WithRequirements_FormatsCorrectly()
    {
      // Arrange
      var set = new ConstraintSet();
      set.Add(Constraint.Requirement("r1", "R1", "Stay in character"));
      set.Add(Constraint.Requirement("r2", "R2", "Be helpful"));

      // Act
      var result = set.ToPromptInjection();

      // Assert
      Assert.That(result, Contains.Substring("[CONSTRAINTS]"));
      Assert.That(result, Contains.Substring("You MUST:"));
      Assert.That(result, Contains.Substring("- Stay in character"));
      Assert.That(result, Contains.Substring("- Be helpful"));
      Assert.That(result, Contains.Substring("[/CONSTRAINTS]"));
    }

    [Test]
    public void ToPromptInjection_WithPermissions_FormatsCorrectly()
    {
      // Arrange
      var set = new ConstraintSet();
      set.Add(Constraint.Permission("perm1", "Perm1", "Make jokes"));
      set.Add(Constraint.Permission("perm2", "Perm2", "Use humor"));

      // Act
      var result = set.ToPromptInjection();

      // Assert
      Assert.That(result, Contains.Substring("[CONSTRAINTS]"));
      Assert.That(result, Contains.Substring("You MAY:"));
      Assert.That(result, Contains.Substring("- Make jokes"));
      Assert.That(result, Contains.Substring("- Use humor"));
      Assert.That(result, Contains.Substring("[/CONSTRAINTS]"));
    }

    [Test]
    public void ToPromptInjection_WithMixedConstraints_FormatsAllSections()
    {
      // Arrange
      var set = new ConstraintSet();
      set.Add(Constraint.Prohibition("p1", "P1", "No secrets"));
      set.Add(Constraint.Requirement("r1", "R1", "Be helpful"));
      set.Add(Constraint.Permission("perm1", "Perm1", "Make jokes"));

      // Act
      var result = set.ToPromptInjection();

      // Assert
      Assert.That(result, Contains.Substring("[CONSTRAINTS]"));
      Assert.That(result, Contains.Substring("You must NOT:"));
      Assert.That(result, Contains.Substring("- No secrets"));
      Assert.That(result, Contains.Substring("You MUST:"));
      Assert.That(result, Contains.Substring("- Be helpful"));
      Assert.That(result, Contains.Substring("You MAY:"));
      Assert.That(result, Contains.Substring("- Make jokes"));
      Assert.That(result, Contains.Substring("[/CONSTRAINTS]"));
    }

    #endregion
  }
}
