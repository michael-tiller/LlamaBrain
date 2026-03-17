// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Michael Tiller and contributors

using NUnit.Framework;
using NSubstitute;
using LlamaBrain.Config;
using LlamaBrain.Persona;
using LlamaBrain.Core;
using System.Collections.Generic;

namespace LlamaBrain.Tests.Config
{
  [TestFixture]
  [Category("Domain")]
  public class ConfigHotReloadServiceTests
  {
    private IConfigWatcher? _mockWatcher;
    private ConfigHotReloadService? _service;
    private List<string> _personaConfigChanges = new List<string>();
    private List<string> _llmConfigChanges = new List<string>();

    [SetUp]
    public void SetUp()
    {
      _mockWatcher = Substitute.For<IConfigWatcher>();
      _service = new ConfigHotReloadService(_mockWatcher);
      _personaConfigChanges.Clear();
      _llmConfigChanges.Clear();

      _service.OnPersonaProfileChanged += (oldProfile, newProfile) =>
      {
        _personaConfigChanges.Add($"{oldProfile.Name} -> {newProfile.Name}");
      };

      _service.OnLlmConfigChanged += (config) =>
      {
        _llmConfigChanges.Add($"Temp: {config.Temperature}");
      };
    }

    [TearDown]
    public void TearDown()
    {
      _service?.Dispose();
      _personaConfigChanges.Clear();
      _llmConfigChanges.Clear();
    }

    [Test]
    public void Initialize_StartsWatcher()
    {
      // Act
      _service!.Initialize();

      // Assert
      _mockWatcher!.Received(1).StartWatching();
    }

    [Test]
    public void Dispose_StopsWatcher()
    {
      // Arrange
      _service!.Initialize();

      // Act
      _service.Dispose();

      // Assert
      _mockWatcher!.Received(1).StopWatching();
    }

    [Test]
    public void ValidatePersonaProfile_ValidProfile_ReturnsTrue()
    {
      // Arrange
      var profile = CreateValidPersonaProfile();

      // Act
      bool isValid = _service!.ValidatePersonaProfile(profile, out var errors);

      // Assert
      Assert.IsTrue(isValid, "Valid profile should pass validation");
      Assert.IsEmpty(errors, "Valid profile should have no errors");
    }

    [Test]
    public void ValidatePersonaProfile_InvalidProfile_ReturnsFalse()
    {
      // Arrange
      var profile = new PersonaProfile
      {
        PersonaId = "", // Invalid: empty PersonaId
        Name = "Test",
        SystemPrompt = "Test prompt"
      };

      // Act
      bool isValid = _service!.ValidatePersonaProfile(profile, out var errors);

      // Assert
      Assert.IsFalse(isValid, "Invalid profile should fail validation");
      Assert.IsNotEmpty(errors, "Invalid profile should have errors");
    }

    [Test]
    public void ValidateLlmConfig_ValidConfig_ReturnsTrue()
    {
      // Arrange
      var config = new LlmConfig
      {
        MaxTokens = 64,
        Temperature = 0.7f,
        TopP = 0.9f
      };

      // Act
      bool isValid = _service!.ValidateLlmConfig(config, out var errors);

      // Assert
      Assert.IsTrue(isValid, "Valid config should pass validation");
      Assert.IsEmpty(errors, "Valid config should have no errors");
    }

    [Test]
    public void ValidateLlmConfig_NullConfig_ReturnsFalse()
    {
      // Act
      bool isValid = _service!.ValidateLlmConfig(null, out var errors);

      // Assert
      Assert.IsFalse(isValid, "Null config should fail validation");
      Assert.IsNotEmpty(errors, "Null config should have errors");
    }

    [Test]
    public void ApplyPersonaProfileChange_ValidChange_FiresEvent()
    {
      // Arrange
      var oldProfile = CreateValidPersonaProfile();
      var newProfile = CreateValidPersonaProfile();
      newProfile.Name = "Updated Wizard";

      // Act
      bool success = _service!.ApplyPersonaProfileChange(oldProfile, newProfile);

      // Assert
      Assert.IsTrue(success, "Valid profile change should succeed");
      Assert.That(_personaConfigChanges.Count, Is.EqualTo(1),
        "Profile change event should fire once");
      Assert.That(_personaConfigChanges[0], Does.Contain("Updated Wizard"),
        "Event should contain new profile name");
    }

    [Test]
    public void ApplyPersonaProfileChange_InvalidChange_DoesNotFireEvent()
    {
      // Arrange
      var oldProfile = CreateValidPersonaProfile();
      var newProfile = new PersonaProfile
      {
        PersonaId = "", // Invalid
        Name = "Invalid",
        SystemPrompt = "Test"
      };

      // Act
      bool success = _service!.ApplyPersonaProfileChange(oldProfile, newProfile);

      // Assert
      Assert.IsFalse(success, "Invalid profile change should fail");
      Assert.That(_personaConfigChanges.Count, Is.EqualTo(0),
        "No event should fire for invalid profile");
    }

    [Test]
    public void ApplyLlmConfigChange_ValidChange_FiresEvent()
    {
      // Arrange
      var config = new LlmConfig
      {
        Temperature = 0.5f,
        MaxTokens = 32
      };

      // Act
      bool success = _service!.ApplyLlmConfigChange(config);

      // Assert
      Assert.IsTrue(success, "Valid LlmConfig change should succeed");
      Assert.That(_llmConfigChanges.Count, Is.EqualTo(1),
        "LlmConfig change event should fire once");
      Assert.That(_llmConfigChanges[0], Does.Contain("0.5"),
        "Event should contain new temperature");
    }

    [Test]
    public void ApplyLlmConfigChange_NullConfig_DoesNotFireEvent()
    {
      // Act
      bool success = _service!.ApplyLlmConfigChange(null);

      // Assert
      Assert.IsFalse(success, "Null config change should fail");
      Assert.That(_llmConfigChanges.Count, Is.EqualTo(0),
        "No event should fire for null config");
    }

    [Test]
    public void ConcurrentChanges_ProcessedSequentially()
    {
      // Arrange
      var profiles = new List<PersonaProfile>();
      for (int i = 0; i < 5; i++)
      {
        var profile = CreateValidPersonaProfile();
        profile.Name = $"Wizard {i}";
        profiles.Add(profile);
      }

      // Act: Apply multiple changes
      foreach (var profile in profiles)
      {
        _service!.ApplyPersonaProfileChange(
          CreateValidPersonaProfile(),
          profile
        );
      }

      // Assert: All changes should be processed
      Assert.That(_personaConfigChanges.Count, Is.EqualTo(5),
        "All 5 profile changes should be processed");
    }

    [Test]
    public void ExceptionInEventHandler_DoesNotCrashService()
    {
      // Arrange
      _service!.OnPersonaProfileChanged += (old, @new) =>
      {
        throw new System.Exception("Test exception");
      };

      var oldProfile = CreateValidPersonaProfile();
      var newProfile = CreateValidPersonaProfile();
      newProfile.Name = "Updated";

      // Act & Assert: Should not throw
      Assert.DoesNotThrow(() =>
      {
        _service.ApplyPersonaProfileChange(oldProfile, newProfile);
      }, "Exception in event handler should be caught");
    }

    private PersonaProfile CreateValidPersonaProfile()
    {
      return new PersonaProfile
      {
        PersonaId = "wizard_001",
        Name = "Test Wizard",
        Description = "A test wizard",
        SystemPrompt = "You are a wizard.",
        Background = "Ancient lore",
        UseMemory = true,
        Traits = new Dictionary<string, string> { { "Wisdom", "High" } },
        Metadata = new Dictionary<string, string> { { "Role", "Guide" } }
      };
    }
  }
}
