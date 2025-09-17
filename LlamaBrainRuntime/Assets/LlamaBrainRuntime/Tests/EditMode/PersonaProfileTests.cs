using NUnit.Framework;
using LlamaBrain.Persona;
using System.Collections.Generic;

#nullable enable

namespace LlamaBrain.Tests.EditMode
{
  /// <summary>
  /// Tests for the PersonaProfile system
  /// </summary>
  public class PersonaProfileTests
  {
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
      Assert.AreEqual(2, memories.Count);
      Assert.Contains("Test memory 1", (System.Collections.ICollection)memories);
      Assert.Contains("Test memory 2", (System.Collections.ICollection)memories);
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
    public void PersonaMemoryStore_BackwardCompatibility_ShouldWork()
    {
      // Arrange
      var store = new PersonaMemoryStore();

      // Act
      store.AddMemory("test-id", "Test memory");
      var memories = store.GetMemory("test-id");

      // Assert
      Assert.AreEqual(1, memories.Count);
      Assert.Contains("Test memory", (System.Collections.ICollection)memories);
    }
  }
}