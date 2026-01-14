// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Michael Tiller and contributors

using System.Collections.Generic;
using LlamaBrain.Core;
using LlamaBrain.Persona;

namespace LlamaBrain.Config
{
  /// <summary>
  /// Validates PersonaProfile and LlmConfig before applying hot reload changes.
  /// Ensures no invalid configurations are applied to running agents.
  /// </summary>
  public static class ConfigValidator
  {
    /// <summary>
    /// Validates a PersonaProfile for hot reload.
    /// </summary>
    /// <param name="profile">The profile to validate</param>
    /// <returns>Array of validation error messages (empty if valid)</returns>
    public static string[] ValidatePersonaProfile(PersonaProfile? profile)
    {
      var errors = new List<string>();

      if (profile == null)
      {
        errors.Add("PersonaProfile cannot be null");
        return errors.ToArray();
      }

      if (string.IsNullOrWhiteSpace(profile.PersonaId))
      {
        errors.Add("PersonaProfile.PersonaId cannot be null or empty");
      }

      if (string.IsNullOrWhiteSpace(profile.Name))
      {
        errors.Add("PersonaProfile.Name cannot be null or empty");
      }

      if (string.IsNullOrWhiteSpace(profile.SystemPrompt))
      {
        errors.Add("PersonaProfile.SystemPrompt cannot be null or empty");
      }

      // Description is optional, so no validation needed

      return errors.ToArray();
    }

    /// <summary>
    /// Validates an LlmConfig for hot reload.
    /// Uses the existing LlmConfig.Validate() method to avoid code duplication.
    /// Note: LlmConfig has automatic property clamping, so invalid values are self-healing.
    /// This validation checks the clamped values to ensure they're in valid ranges.
    /// </summary>
    /// <param name="config">The LlmConfig to validate</param>
    /// <returns>Array of validation error messages (empty if valid)</returns>
    public static string[] ValidateLlmConfig(LlmConfig? config)
    {
      if (config == null)
      {
        return new[] { "LlmConfig cannot be null" };
      }

      // Use the existing Validate() method from LlmConfig
      // This ensures validation logic is centralized and consistent
      return config.Validate();
    }
  }
}
