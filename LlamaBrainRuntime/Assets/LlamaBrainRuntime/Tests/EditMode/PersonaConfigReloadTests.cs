#nullable enable
#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using LlamaBrain.Runtime.Core;
using LlamaBrain.Persona;
using System.Collections.Generic;

namespace LlamaBrain.Tests.EditMode
{
  [TestFixture]
  [Category("Domain")]
  public class PersonaConfigReloadTests
  {
    private GameObject? _agentObj;
    private LlamaBrainAgent? _agent;
    private PersonaConfig? _config;

    [SetUp]
    public void SetUp()
    {
      // Create agent GameObject
      _agentObj = new GameObject("TestAgent");
      _agent = _agentObj.AddComponent<LlamaBrainAgent>();

      // Create PersonaConfig
      _config = ScriptableObject.CreateInstance<PersonaConfig>();
      _config.PersonaId = "wizard_001";
      _config.Name = "Test Wizard";
      _config.Description = "A test wizard";
      _config.SystemPrompt = "You are a wizard.";
      _config.Background = "Ancient lore";
      _config.UseMemory = true;

      _agent.PersonaConfig = _config;
    }

    [TearDown]
    public void TearDown()
    {
      if (_agentObj != null)
      {
        Object.DestroyImmediate(_agentObj);
      }

      if (_config != null)
      {
        Object.DestroyImmediate(_config);
      }
    }

    [Test]
    public void ReloadPersonaConfig_ValidChanges_UpdatesProfile()
    {
      // Arrange: Initialize agent
      _agent!.ConvertConfigToProfile();
      var originalName = _agent.RuntimeProfile?.Name;
      Assert.AreEqual("Test Wizard", originalName);

      // Act: Modify config and reload
      _config!.Name = "Updated Wizard";
      _config.SystemPrompt = "You are a powerful wizard.";
      bool success = _agent.ReloadPersonaConfig();

      // Assert
      Assert.IsTrue(success, "Reload should succeed");
      Assert.AreEqual("Updated Wizard", _agent.RuntimeProfile?.Name, "Profile name should be updated");
      Assert.AreEqual("You are a powerful wizard.", _agent.RuntimeProfile?.SystemPrompt, "System prompt should be updated");
      Assert.AreEqual("wizard_001", _agent.RuntimeProfile?.PersonaId, "PersonaId should remain unchanged");
    }

    [Test]
    public void ReloadPersonaConfig_InvalidConfig_RollsBack()
    {
      // Arrange: Initialize agent
      _agent!.ConvertConfigToProfile();
      var originalName = _agent.RuntimeProfile?.Name;

      // Act: Set invalid config (empty name) and attempt reload
      _config!.Name = ""; // Invalid
      bool success = _agent.ReloadPersonaConfig();

      // Assert
      Assert.IsFalse(success, "Reload should fail with invalid config");
      Assert.AreEqual(originalName, _agent.RuntimeProfile?.Name, "Profile should not be changed");
    }

    [Test]
    public void ReloadPersonaConfig_PreservesRuntimeState()
    {
      // Arrange: Initialize agent with interaction count
      _agent!.ConvertConfigToProfile();
      _agent.InteractionCount = 10;

      // Act: Reload config
      _config!.Description = "Modified description";
      _agent.ReloadPersonaConfig();

      // Assert: InteractionCount should be preserved
      Assert.AreEqual(10, _agent.InteractionCount, "InteractionCount should be preserved");
    }

    [Test]
    public void ReloadPersonaConfig_UpdatesSystemPrompt()
    {
      // Arrange
      _agent!.ConvertConfigToProfile();
      Assert.AreEqual("You are a wizard.", _agent.RuntimeProfile?.SystemPrompt);

      // Act: Change system prompt
      _config!.SystemPrompt = "You are an ancient and wise wizard.";
      _agent.ReloadPersonaConfig();

      // Assert
      Assert.AreEqual("You are an ancient and wise wizard.", _agent.RuntimeProfile?.SystemPrompt);
    }

    [Test]
    public void ReloadPersonaConfig_UpdatesTraits()
    {
      // Arrange
      _agent!.ConvertConfigToProfile();

      // Add a trait to config
      var trait = ScriptableObject.CreateInstance<PersonaTrait>();
      trait.TraitId = "wisdom";
      trait.DisplayName = "Wisdom";
      trait.DefaultValue = "High";

      var assignment = new PersonaTraitAssignment
      {
        Trait = trait,
        CustomValue = "Very High",
        IsEnabled = true
      };

      _config!.TraitAssignments = new List<PersonaTraitAssignment> { assignment };

      // Act: Reload config
      _agent.ReloadPersonaConfig();

      // Assert
      Assert.IsNotNull(_agent.RuntimeProfile?.Traits);
      Assert.IsTrue(_agent.RuntimeProfile?.Traits.ContainsKey("Wisdom"), "Profile should have Wisdom trait");
      Assert.AreEqual("Very High", _agent.RuntimeProfile?.Traits["Wisdom"], "Trait value should be updated");

      // Cleanup
      Object.DestroyImmediate(trait);
    }

    [Test]
    public void ReloadPersonaConfig_NullConfig_ReturnsFalse()
    {
      // Arrange
      _agent!.ConvertConfigToProfile();
      _agent.PersonaConfig = null;

      // Act
      bool success = _agent.ReloadPersonaConfig();

      // Assert
      Assert.IsFalse(success, "Reload should fail with null config");
    }

    [Test]
    public void ReloadPersonaConfig_NotInitialized_ReturnsFalse()
    {
      // Arrange: Don't initialize agent (no ConvertConfigToProfile call)

      // Act
      bool success = _agent!.ReloadPersonaConfig();

      // Assert
      Assert.IsFalse(success, "Reload should fail if agent not initialized");
    }

    [Test]
    public void ReloadPersonaConfig_FiresEvent()
    {
      // Arrange
      _agent!.ConvertConfigToProfile();
      bool eventFired = false;
      PersonaProfile? oldProfile = null;
      PersonaProfile? newProfile = null;

      _agent.OnPersonaConfigReloaded += (agent, old, @new) =>
      {
        eventFired = true;
        oldProfile = old;
        newProfile = @new;
      };

      // Act
      _config!.Name = "Modified Wizard";
      _agent.ReloadPersonaConfig();

      // Assert
      Assert.IsTrue(eventFired, "Event should fire on successful reload");
      Assert.IsNotNull(oldProfile, "Old profile should be passed to event");
      Assert.IsNotNull(newProfile, "New profile should be passed to event");
      Assert.AreEqual("Test Wizard", oldProfile?.Name, "Old profile should have original name");
      Assert.AreEqual("Modified Wizard", newProfile?.Name, "New profile should have new name");
    }
  }
}
#endif
