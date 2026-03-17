// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Michael Tiller and contributors

using NUnit.Framework;
using LlamaBrain.Config;
using LlamaBrain.Persona;
using LlamaBrain.Core;
using System.Collections.Generic;

namespace LlamaBrain.Tests.Config
{
  [TestFixture]
  [Category("Domain")]
  public class ConfigValidatorTests
  {
    [Test]
    public void ValidatePersonaProfile_ValidProfile_ReturnsEmpty()
    {
      // Arrange
      var profile = new PersonaProfile
      {
        PersonaId = "test_001",
        Name = "Test Wizard",
        Description = "A test character",
        SystemPrompt = "You are a wizard.",
        Background = "Ancient lore keeper",
        UseMemory = true,
        Traits = new Dictionary<string, string> { { "Personality", "Wise" } },
        Metadata = new Dictionary<string, string> { { "Role", "Guide" } }
      };

      // Act
      var errors = ConfigValidator.ValidatePersonaProfile(profile);

      // Assert
      Assert.IsEmpty(errors, "Valid profile should produce no errors");
    }

    [Test]
    public void ValidatePersonaProfile_NullProfile_ReturnsError()
    {
      // Act
      var errors = ConfigValidator.ValidatePersonaProfile(null);

      // Assert
      Assert.IsNotEmpty(errors, "Null profile should produce error");
      Assert.That(errors[0], Does.Contain("null"));
    }

    [Test]
    public void ValidatePersonaProfile_MissingName_ReturnsError()
    {
      // Arrange
      var profile = new PersonaProfile
      {
        PersonaId = "test_001",
        Name = "",
        Description = "A test character",
        SystemPrompt = "You are a wizard."
      };

      // Act
      var errors = ConfigValidator.ValidatePersonaProfile(profile);

      // Assert
      Assert.IsNotEmpty(errors, "Profile with empty name should produce error");
      Assert.That(errors[0], Does.Contain("Name"));
    }

    [Test]
    public void ValidatePersonaProfile_MissingPersonaId_ReturnsError()
    {
      // Arrange
      var profile = new PersonaProfile
      {
        PersonaId = "",
        Name = "Test Wizard",
        Description = "A test character",
        SystemPrompt = "You are a wizard."
      };

      // Act
      var errors = ConfigValidator.ValidatePersonaProfile(profile);

      // Assert
      Assert.IsNotEmpty(errors, "Profile with empty PersonaId should produce error");
      Assert.That(errors[0], Does.Contain("PersonaId"));
    }

    [Test]
    public void ValidatePersonaProfile_MissingSystemPrompt_ReturnsError()
    {
      // Arrange
      var profile = new PersonaProfile
      {
        PersonaId = "test_001",
        Name = "Test Wizard",
        Description = "A test character",
        SystemPrompt = ""
      };

      // Act
      var errors = ConfigValidator.ValidatePersonaProfile(profile);

      // Assert
      Assert.IsNotEmpty(errors, "Profile with empty SystemPrompt should produce error");
      Assert.That(errors[0], Does.Contain("SystemPrompt"));
    }

    [Test]
    public void ValidateLlmConfig_ValidConfig_ReturnsEmpty()
    {
      // Arrange
      var config = new LlmConfig
      {
        MaxTokens = 64,
        Temperature = 0.7f,
        TopP = 0.9f,
        TopK = 40,
        RepeatPenalty = 1.1f
      };

      // Act
      var errors = ConfigValidator.ValidateLlmConfig(config);

      // Assert
      Assert.IsEmpty(errors, "Valid LlmConfig should produce no errors");
    }

    [Test]
    public void ValidateLlmConfig_NullConfig_ReturnsError()
    {
      // Act
      var errors = ConfigValidator.ValidateLlmConfig(null);

      // Assert
      Assert.IsNotEmpty(errors, "Null LlmConfig should produce error");
      Assert.That(errors[0], Does.Contain("null"));
    }

    [Test]
    public void ValidateLlmConfig_NegativeMaxTokens_ClampedToValid()
    {
      // Arrange: LlmConfig has automatic clamping, so -1 becomes 1
      var config = new LlmConfig
      {
        MaxTokens = -1,
        Temperature = 0.7f
      };

      // Act
      var errors = ConfigValidator.ValidateLlmConfig(config);

      // Assert: After clamping, the config is valid
      Assert.IsEmpty(errors, "LlmConfig automatically clamps MaxTokens to valid range (1-2048)");
      Assert.AreEqual(1, config.MaxTokens, "MaxTokens should be clamped to 1");
    }

    [Test]
    public void ValidateLlmConfig_NegativeTemperature_ClampedToValid()
    {
      // Arrange: LlmConfig has automatic clamping, so -0.1 becomes 0.0
      var config = new LlmConfig
      {
        MaxTokens = 64,
        Temperature = -0.1f
      };

      // Act
      var errors = ConfigValidator.ValidateLlmConfig(config);

      // Assert: After clamping, the config is valid
      Assert.IsEmpty(errors, "LlmConfig automatically clamps Temperature to valid range (0.0-2.0)");
      Assert.AreEqual(0.0f, config.Temperature, "Temperature should be clamped to 0.0");
    }

    [Test]
    public void ValidateLlmConfig_TemperatureTooHigh_ClampedToValid()
    {
      // Arrange: LlmConfig has automatic clamping, so 2.5 becomes 2.0
      var config = new LlmConfig
      {
        MaxTokens = 64,
        Temperature = 2.5f
      };

      // Act
      var errors = ConfigValidator.ValidateLlmConfig(config);

      // Assert: After clamping, the config is valid
      Assert.IsEmpty(errors, "LlmConfig automatically clamps Temperature to valid range (0.0-2.0)");
      Assert.AreEqual(2.0f, config.Temperature, "Temperature should be clamped to 2.0");
    }

    [Test]
    public void ValidateLlmConfig_TopPOutOfRange_ClampedToValid()
    {
      // Arrange: LlmConfig has automatic clamping, so 1.5 becomes 1.0
      var config = new LlmConfig
      {
        MaxTokens = 64,
        Temperature = 0.7f,
        TopP = 1.5f
      };

      // Act
      var errors = ConfigValidator.ValidateLlmConfig(config);

      // Assert: After clamping, the config is valid
      Assert.IsEmpty(errors, "LlmConfig automatically clamps TopP to valid range (0.0-1.0)");
      Assert.AreEqual(1.0f, config.TopP, "TopP should be clamped to 1.0");
    }

    [Test]
    public void ValidateLlmConfig_AutomaticClamping_ProducesValidConfig()
    {
      // Arrange: Test that automatic clamping works for multiple properties
      var config = new LlmConfig
      {
        MaxTokens = -1,      // Will be clamped to 1
        Temperature = -0.1f, // Will be clamped to 0.0
        TopP = 1.5f          // Will be clamped to 1.0
      };

      // Act
      var errors = ConfigValidator.ValidateLlmConfig(config);

      // Assert: All values are clamped, config is valid
      Assert.IsEmpty(errors, "LlmConfig automatic clamping should produce valid config");
      Assert.AreEqual(1, config.MaxTokens);
      Assert.AreEqual(0.0f, config.Temperature);
      Assert.AreEqual(1.0f, config.TopP);
    }
  }
}
