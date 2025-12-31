using System.Linq;
using NUnit.Framework;
using LlamaBrain.Persona;
using System.Collections.Generic;

#nullable enable

namespace LlamaBrain.Tests
{
  /// <summary>
  /// Tests for the PersonaProfile system
  /// </summary>
  public class PersonaProfileTests
  {
    #region Default Values Tests

    [Test]
    public void PersonaProfile_DefaultValues_AreCorrect()
    {
      // Act
      var profile = new PersonaProfile();

      // Assert
      Assert.That(profile.PersonaId, Is.EqualTo(string.Empty));
      Assert.That(profile.Name, Is.EqualTo(string.Empty));
      Assert.That(profile.Description, Is.EqualTo(string.Empty));
      Assert.That(profile.SystemPrompt, Is.EqualTo(string.Empty));
      Assert.That(profile.Background, Is.EqualTo(string.Empty));
      Assert.That(profile.UseMemory, Is.True);
      Assert.That(profile.Traits, Is.Not.Null);
      Assert.That(profile.Traits.Count, Is.EqualTo(0));
      Assert.That(profile.Metadata, Is.Not.Null);
      Assert.That(profile.Metadata.Count, Is.EqualTo(0));
    }

    #endregion

    #region Create Factory Method Tests

    [Test]
    public void PersonaProfile_Create_ShouldCreateValidProfile()
    {
      // Arrange & Act
      var profile = PersonaProfile.Create("test-persona", "Test Persona");

      // Assert
      Assert.IsNotNull(profile);
      Assert.AreEqual("test-persona", profile.PersonaId);
      Assert.AreEqual("Test Persona", profile.Name);
      Assert.IsNotNull(profile.Metadata);
      Assert.AreEqual(0, profile.Metadata.Count);
    }

    [Test]
    public void PersonaProfile_Create_InitializesEmptyTraits()
    {
      // Act
      var profile = PersonaProfile.Create("id", "name");

      // Assert
      Assert.That(profile.Traits, Is.Not.Null);
      Assert.That(profile.Traits.Count, Is.EqualTo(0));
    }

    [Test]
    public void PersonaProfile_Create_InitializesEmptyMetadata()
    {
      // Act
      var profile = PersonaProfile.Create("id", "name");

      // Assert
      Assert.That(profile.Metadata, Is.Not.Null);
      Assert.That(profile.Metadata.Count, Is.EqualTo(0));
    }

    #endregion

    #region Trait Methods Tests

    [Test]
    public void SetTrait_AddsNewTrait()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      profile.SetTrait("Personality", "Friendly");

      // Assert
      Assert.That(profile.Traits.Count, Is.EqualTo(1));
      Assert.That(profile.Traits["Personality"], Is.EqualTo("Friendly"));
    }

    [Test]
    public void SetTrait_OverwritesExistingTrait()
    {
      // Arrange
      var profile = new PersonaProfile();
      profile.SetTrait("Personality", "Grumpy");

      // Act
      profile.SetTrait("Personality", "Friendly");

      // Assert
      Assert.That(profile.Traits.Count, Is.EqualTo(1));
      Assert.That(profile.Traits["Personality"], Is.EqualTo("Friendly"));
    }

    [Test]
    public void GetTrait_ReturnsValueWhenExists()
    {
      // Arrange
      var profile = new PersonaProfile();
      profile.SetTrait("Mood", "Happy");

      // Act
      var result = profile.GetTrait("Mood");

      // Assert
      Assert.That(result, Is.EqualTo("Happy"));
    }

    [Test]
    public void GetTrait_ReturnsNullWhenNotExists()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      var result = profile.GetTrait("NonExistent");

      // Assert
      Assert.That(result, Is.Null);
    }

    [Test]
    public void RemoveTrait_ReturnsTrueWhenExists()
    {
      // Arrange
      var profile = new PersonaProfile();
      profile.SetTrait("ToRemove", "Value");

      // Act
      var result = profile.RemoveTrait("ToRemove");

      // Assert
      Assert.That(result, Is.True);
      Assert.That(profile.Traits.ContainsKey("ToRemove"), Is.False);
    }

    [Test]
    public void RemoveTrait_ReturnsFalseWhenNotExists()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      var result = profile.RemoveTrait("NonExistent");

      // Assert
      Assert.That(result, Is.False);
    }

    #endregion

    #region GetTraitsAsString Tests

    [Test]
    public void GetTraitsAsString_EmptyTraits_ReturnsEmptyString()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      var result = profile.GetTraitsAsString();

      // Assert
      Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetTraitsAsString_SingleTrait_ReturnsFormatted()
    {
      // Arrange
      var profile = new PersonaProfile();
      profile.SetTrait("Personality", "Friendly");

      // Act
      var result = profile.GetTraitsAsString();

      // Assert
      Assert.That(result, Is.EqualTo("Personality: Friendly"));
    }

    [Test]
    public void GetTraitsAsString_MultipleTraits_ReturnsSemicolonSeparated()
    {
      // Arrange
      var profile = new PersonaProfile();
      profile.SetTrait("Personality", "Friendly");
      profile.SetTrait("Mood", "Happy");

      // Act
      var result = profile.GetTraitsAsString();

      // Assert
      Assert.That(result, Does.Contain("Personality: Friendly"));
      Assert.That(result, Does.Contain("Mood: Happy"));
      Assert.That(result, Does.Contain("; "));
    }

    #endregion

    #region UseMemory Property Tests

    [Test]
    public void UseMemory_DefaultsToTrue()
    {
      // Act
      var profile = new PersonaProfile();

      // Assert
      Assert.That(profile.UseMemory, Is.True);
    }

    [Test]
    public void UseMemory_CanBeSetToFalse()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      profile.UseMemory = false;

      // Assert
      Assert.That(profile.UseMemory, Is.False);
    }

    #endregion

    #region Original Tests

    [Test]
    public void PersonaProfile_Properties_ShouldBeSettable()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      profile.PersonaId = "test-id";
      profile.Name = "Test Name";
      profile.Description = "Test Description";
      profile.SystemPrompt = "Test System Prompt";
      profile.SetTrait("Personality", "Friendly, Helpful");
      profile.Background = "Test Background";
      profile.Metadata = new Dictionary<string, string> { { "key", "value" } };

      // Assert
      Assert.AreEqual("test-id", profile.PersonaId);
      Assert.AreEqual("Test Name", profile.Name);
      Assert.AreEqual("Test Description", profile.Description);
      Assert.AreEqual("Test System Prompt", profile.SystemPrompt);
      Assert.AreEqual("Friendly, Helpful", profile.GetTrait("Personality"));
      Assert.AreEqual("Test Background", profile.Background);
      Assert.IsNotNull(profile.Metadata);
      Assert.AreEqual(1, profile.Metadata.Count);
      Assert.AreEqual("value", profile.Metadata["key"]);
    }

    [Test]
    public void PersonaProfile_MetadataMethods_ShouldWorkCorrectly()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act & Assert
      // Test SetMetadata
      profile.SetMetadata("test-key", "test-value");
      Assert.AreEqual("test-value", profile.GetMetadata("test-key"));
      Assert.IsNull(profile.GetMetadata("non-existent"));

      // Test RemoveMetadata
      Assert.IsTrue(profile.RemoveMetadata("test-key"));
      Assert.IsNull(profile.GetMetadata("test-key"));
      Assert.IsFalse(profile.RemoveMetadata("non-existent"));
    }

    [Test]
    public void PersonaMemoryStore_WithProfile_ShouldWorkCorrectly()
    {
      // Arrange
      var store = new PersonaMemoryStore();
      var profile = PersonaProfile.Create("test-persona", "Test Persona");

      // Act
      store.AddMemory(profile, "Test memory 1");
      store.AddMemory(profile, "Test memory 2");

      // Assert
      var memories = store.GetMemory(profile);
      Assert.GreaterOrEqual(memories.Count, 2);
      // Memories are now formatted, so check for content
      Assert.IsTrue(memories.Any(m => m.Contains("Test memory 1")));
      Assert.IsTrue(memories.Any(m => m.Contains("Test memory 2")));
    }

    [Test]
    public void PersonaMemoryStore_WithNullProfile_ShouldThrowException()
    {
      // Arrange
      var store = new PersonaMemoryStore();
      PersonaProfile? nullProfile = null;

      // Act & Assert
      Assert.Throws<System.ArgumentException>(() => store.AddMemory(nullProfile!, "test"));
      Assert.Throws<System.ArgumentException>(() => store.GetMemory(nullProfile!));
      Assert.Throws<System.ArgumentException>(() => store.ClearMemory(nullProfile!));
    }

    [Test]
    public void PersonaMemoryStore_WithProfileWithoutId_ShouldThrowException()
    {
      // Arrange
      var store = new PersonaMemoryStore();
      var profile = new PersonaProfile { Name = "Test" }; // No PersonaId

      // Act & Assert
      Assert.Throws<System.ArgumentException>(() => store.AddMemory(profile, "test"));
      Assert.Throws<System.ArgumentException>(() => store.GetMemory(profile));
      Assert.Throws<System.ArgumentException>(() => store.ClearMemory(profile));
    }

    [Test]
    public void PersonaMemoryStore_ConvenienceAPI_ShouldWork()
    {
      // Arrange
      var store = new PersonaMemoryStore();

      // Act
      store.AddMemory("test-id", "Test memory");
      var memories = store.GetMemory("test-id");

      // Assert
      Assert.GreaterOrEqual(memories.Count, 1);
      // Memories are now formatted, so check for content
      Assert.IsTrue(memories.Any(m => m.Contains("Test memory")));
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public void SetTrait_WithEmptyKey_AddsTrait()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      profile.SetTrait("", "Value");

      // Assert
      Assert.That(profile.Traits.ContainsKey(""), Is.True);
      Assert.That(profile.Traits[""], Is.EqualTo("Value"));
    }

    [Test]
    public void SetTrait_WithNullKey_ThrowsException()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act & Assert
      Assert.Throws<System.ArgumentNullException>(() => profile.SetTrait(null!, "Value"));
    }

    [Test]
    public void SetTrait_WithEmptyValue_AddsTrait()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      profile.SetTrait("Key", "");

      // Assert
      Assert.That(profile.Traits["Key"], Is.EqualTo(""));
    }

    [Test]
    public void SetTrait_WithNullValue_AddsTrait()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      profile.SetTrait("Key", null!);

      // Assert
      Assert.That(profile.Traits["Key"], Is.Null);
    }

    [Test]
    public void SetMetadata_WithEmptyKey_AddsMetadata()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      profile.SetMetadata("", "Value");

      // Assert
      Assert.That(profile.Metadata.ContainsKey(""), Is.True);
      Assert.That(profile.Metadata[""], Is.EqualTo("Value"));
    }

    [Test]
    public void SetMetadata_WithNullKey_ThrowsException()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act & Assert
      Assert.Throws<System.ArgumentNullException>(() => profile.SetMetadata(null!, "Value"));
    }

    [Test]
    public void SetMetadata_WithEmptyValue_AddsMetadata()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      profile.SetMetadata("Key", "");

      // Assert
      Assert.That(profile.Metadata["Key"], Is.EqualTo(""));
    }

    [Test]
    public void SetMetadata_WithNullValue_AddsMetadata()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      profile.SetMetadata("Key", null!);

      // Assert
      Assert.That(profile.Metadata["Key"], Is.Null);
    }

    [Test]
    public void GetTrait_WithEmptyKey_ReturnsNull()
    {
      // Arrange
      var profile = new PersonaProfile();
      profile.SetTrait("", "Value");

      // Act
      var result = profile.GetTrait("");

      // Assert
      Assert.That(result, Is.EqualTo("Value"));
    }

    [Test]
    public void GetTrait_WithNullKey_ThrowsException()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act & Assert
      Assert.Throws<System.ArgumentNullException>(() => profile.GetTrait(null!));
    }

    [Test]
    public void GetMetadata_WithEmptyKey_ReturnsValue()
    {
      // Arrange
      var profile = new PersonaProfile();
      profile.SetMetadata("", "Value");

      // Act
      var result = profile.GetMetadata("");

      // Assert
      Assert.That(result, Is.EqualTo("Value"));
    }

    [Test]
    public void GetMetadata_WithNullKey_ThrowsException()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act & Assert
      Assert.Throws<System.ArgumentNullException>(() => profile.GetMetadata(null!));
    }

    [Test]
    public void RemoveTrait_WithEmptyKey_ReturnsFalse()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      var result = profile.RemoveTrait("");

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void RemoveTrait_WithNullKey_ThrowsException()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act & Assert
      Assert.Throws<System.ArgumentNullException>(() => profile.RemoveTrait(null!));
    }

    [Test]
    public void RemoveMetadata_WithEmptyKey_ReturnsFalse()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      var result = profile.RemoveMetadata("");

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void RemoveMetadata_WithNullKey_ThrowsException()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act & Assert
      Assert.Throws<System.ArgumentNullException>(() => profile.RemoveMetadata(null!));
    }

    #endregion

    #region GetTraitsAsString Edge Cases

    [Test]
    public void GetTraitsAsString_WithSpecialCharacters_FormatsCorrectly()
    {
      // Arrange
      var profile = new PersonaProfile();
      profile.SetTrait("Personality", "Friendly; Helpful");
      profile.SetTrait("Quote", "He said \"Hello\"");

      // Act
      var result = profile.GetTraitsAsString();

      // Assert
      Assert.That(result, Does.Contain("Personality: Friendly; Helpful"));
      Assert.That(result, Does.Contain("Quote: He said \"Hello\""));
      Assert.That(result, Does.Contain("; "));
    }

    [Test]
    public void GetTraitsAsString_WithEmptyTraitValues_FormatsCorrectly()
    {
      // Arrange
      var profile = new PersonaProfile();
      profile.SetTrait("Key1", "");
      profile.SetTrait("Key2", "Value");

      // Act
      var result = profile.GetTraitsAsString();

      // Assert
      Assert.That(result, Does.Contain("Key1: "));
      Assert.That(result, Does.Contain("Key2: Value"));
    }

    [Test]
    public void GetTraitsAsString_WithNullTraitValues_FormatsCorrectly()
    {
      // Arrange
      var profile = new PersonaProfile();
      profile.SetTrait("Key1", null!);
      profile.SetTrait("Key2", "Value");

      // Act
      var result = profile.GetTraitsAsString();

      // Assert
      Assert.That(result, Does.Contain("Key1: "));
      Assert.That(result, Does.Contain("Key2: Value"));
    }

    [Test]
    public void GetTraitsAsString_WithManyTraits_FormatsAll()
    {
      // Arrange
      var profile = new PersonaProfile();
      for (int i = 0; i < 10; i++)
      {
        profile.SetTrait($"Trait{i}", $"Value{i}");
      }

      // Act
      var result = profile.GetTraitsAsString();

      // Assert
      Assert.That(result, Does.Contain("Trait0: Value0"));
      Assert.That(result, Does.Contain("Trait9: Value9"));
      // Count semicolons to verify all traits are included (9 semicolons for 10 traits)
      var semicolonCount = result.Split(';').Length - 1;
      Assert.That(semicolonCount, Is.EqualTo(9));
    }

    [Test]
    public void GetTraitsAsString_WithUnicodeCharacters_FormatsCorrectly()
    {
      // Arrange
      var profile = new PersonaProfile();
      profile.SetTrait("Name", "José");
      profile.SetTrait("Emoji", "😊");

      // Act
      var result = profile.GetTraitsAsString();

      // Assert
      Assert.That(result, Does.Contain("Name: José"));
      Assert.That(result, Does.Contain("Emoji: 😊"));
    }

    #endregion

    #region Property Edge Cases

    [Test]
    public void PersonaProfile_AllPropertiesCanBeSetToEmpty()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      profile.PersonaId = "";
      profile.Name = "";
      profile.Description = "";
      profile.SystemPrompt = "";
      profile.Background = "";

      // Assert
      Assert.That(profile.PersonaId, Is.EqualTo(""));
      Assert.That(profile.Name, Is.EqualTo(""));
      Assert.That(profile.Description, Is.EqualTo(""));
      Assert.That(profile.SystemPrompt, Is.EqualTo(""));
      Assert.That(profile.Background, Is.EqualTo(""));
    }

    [Test]
    public void PersonaProfile_PropertiesCanBeSetToNull()
    {
      // Arrange
      var profile = new PersonaProfile();

      // Act
      profile.PersonaId = null!;
      profile.Name = null!;
      profile.Description = null!;
      profile.SystemPrompt = null!;
      profile.Background = null!;

      // Assert
      Assert.That(profile.PersonaId, Is.Null);
      Assert.That(profile.Name, Is.Null);
      Assert.That(profile.Description, Is.Null);
      Assert.That(profile.SystemPrompt, Is.Null);
      Assert.That(profile.Background, Is.Null);
    }

    [Test]
    public void PersonaProfile_TraitsDictionaryCanBeReplaced()
    {
      // Arrange
      var profile = new PersonaProfile();
      profile.SetTrait("Old", "Value");
      var newTraits = new Dictionary<string, string> { { "New", "Value" } };

      // Act
      profile.Traits = newTraits;

      // Assert
      Assert.That(profile.Traits.Count, Is.EqualTo(1));
      Assert.That(profile.Traits.ContainsKey("New"), Is.True);
      Assert.That(profile.Traits.ContainsKey("Old"), Is.False);
    }

    [Test]
    public void PersonaProfile_MetadataDictionaryCanBeReplaced()
    {
      // Arrange
      var profile = new PersonaProfile();
      profile.SetMetadata("Old", "Value");
      var newMetadata = new Dictionary<string, string> { { "New", "Value" } };

      // Act
      profile.Metadata = newMetadata;

      // Assert
      Assert.That(profile.Metadata.Count, Is.EqualTo(1));
      Assert.That(profile.Metadata.ContainsKey("New"), Is.True);
      Assert.That(profile.Metadata.ContainsKey("Old"), Is.False);
    }

    #endregion

    #region Create Factory Method Edge Cases

    [Test]
    public void PersonaProfile_Create_WithEmptyId_CreatesProfile()
    {
      // Act
      var profile = PersonaProfile.Create("", "Name");

      // Assert
      Assert.That(profile.PersonaId, Is.EqualTo(""));
      Assert.That(profile.Name, Is.EqualTo("Name"));
    }

    [Test]
    public void PersonaProfile_Create_WithEmptyName_CreatesProfile()
    {
      // Act
      var profile = PersonaProfile.Create("id", "");

      // Assert
      Assert.That(profile.PersonaId, Is.EqualTo("id"));
      Assert.That(profile.Name, Is.EqualTo(""));
    }

    [Test]
    public void PersonaProfile_Create_WithNullId_CreatesProfile()
    {
      // Act
      var profile = PersonaProfile.Create(null!, "Name");

      // Assert
      Assert.That(profile.PersonaId, Is.Null);
      Assert.That(profile.Name, Is.EqualTo("Name"));
    }

    [Test]
    public void PersonaProfile_Create_WithNullName_CreatesProfile()
    {
      // Act
      var profile = PersonaProfile.Create("id", null!);

      // Assert
      Assert.That(profile.PersonaId, Is.EqualTo("id"));
      Assert.That(profile.Name, Is.Null);
    }

    #endregion
  }
}

