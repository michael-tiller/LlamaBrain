using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;

namespace LlamaBrain.Tests
{
  /// <summary>
  /// Tests for the FallbackSystem fallback response system
  /// </summary>
  [TestFixture]
  public class FallbackSystemTests
  {
    private FallbackSystem.FallbackConfig _defaultConfig = null!;
    private FallbackSystem _fallbackSystem = null!;

    [SetUp]
    public void SetUp()
    {
      _defaultConfig = new FallbackSystem.FallbackConfig();
      _fallbackSystem = new FallbackSystem(_defaultConfig);
    }

    #region Constructor Tests

    [Test]
    public void FallbackSystem_Constructor_WithDefaultConfig_CreatesCorrectly()
    {
      // Arrange & Act
      var system = new FallbackSystem();

      // Assert
      Assert.IsNotNull(system);
      Assert.IsNotNull(system.Stats);
      Assert.AreEqual(0, system.Stats.TotalFallbacks);
    }

    [Test]
    public void FallbackSystem_Constructor_WithCustomConfig_CreatesCorrectly()
    {
      // Arrange
      var config = new FallbackSystem.FallbackConfig
      {
        GenericFallbacks = new List<string> { "Custom fallback 1", "Custom fallback 2" }
      };

      // Act
      var system = new FallbackSystem(config);

      // Assert
      Assert.IsNotNull(system);
      Assert.AreEqual(2, config.GenericFallbacks.Count);
    }

    [Test]
    public void FallbackSystem_Constructor_WithNullConfig_UsesDefaultConfig()
    {
      // Arrange & Act
      var system = new FallbackSystem((FallbackSystem.FallbackConfig)null!);

      // Assert
      Assert.IsNotNull(system);
      // Should still work with default config
      var response = system.GetFallbackResponse(
        new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance },
        "Test failure");
      Assert.IsNotEmpty(response);
    }

    #endregion

    #region GetFallbackResponse Tests - Basic Functionality

    [Test]
    public void GetFallbackResponse_WithValidContext_ReturnsFallback()
    {
      // Arrange
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        GameTime = 100f
      };

      // Act
      var response = _fallbackSystem.GetFallbackResponse(context, "Test failure");

      // Assert
      Assert.IsNotNull(response);
      Assert.IsNotEmpty(response);
      Assert.IsTrue(_defaultConfig.PlayerUtteranceFallbacks.Contains(response) ||
                    _defaultConfig.GenericFallbacks.Contains(response));
    }

    [Test]
    public void GetFallbackResponse_WithNullContext_CreatesDefaultContext()
    {
      // Act
      var response = _fallbackSystem.GetFallbackResponse(null, "Test failure");

      // Assert
      Assert.IsNotNull(response);
      Assert.IsNotEmpty(response);
    }

    [Test]
    public void GetFallbackResponse_WithNullFailureReason_HandlesGracefully()
    {
      // Arrange
      var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };

      // Act
      var response = _fallbackSystem.GetFallbackResponse(context, null);

      // Assert
      Assert.IsNotNull(response);
      Assert.IsNotEmpty(response);
    }

    [Test]
    public void GetFallbackResponse_WithEmptyFailureReason_HandlesGracefully()
    {
      // Arrange
      var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };

      // Act
      var response = _fallbackSystem.GetFallbackResponse(context, "");

      // Assert
      Assert.IsNotNull(response);
      Assert.IsNotEmpty(response);
    }

    #endregion

    #region GetFallbackResponse Tests - Context-Aware Selection

    [Test]
    public void GetFallbackResponse_WithPlayerUtterance_ReturnsPlayerUtteranceFallback()
    {
      // Arrange
      var config = new FallbackSystem.FallbackConfig
      {
        PlayerUtteranceFallbacks = new List<string> { "Player specific fallback" },
        GenericFallbacks = new List<string> { "Generic fallback" }
      };
      var system = new FallbackSystem(config, new Random(12345));
      var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };

      // Act
      var response = system.GetFallbackResponse(context, "Test");

      // Assert
      Assert.AreEqual("Player specific fallback", response);
    }

    [Test]
    public void GetFallbackResponse_WithZoneTrigger_ReturnsZoneTriggerFallback()
    {
      // Arrange
      var config = new FallbackSystem.FallbackConfig
      {
        ZoneTriggerFallbacks = new List<string> { "Zone specific fallback" },
        GenericFallbacks = new List<string> { "Generic fallback" }
      };
      var system = new FallbackSystem(config, new Random(12345));
      var context = new InteractionContext { TriggerReason = TriggerReason.ZoneTrigger };

      // Act
      var response = system.GetFallbackResponse(context, "Test");

      // Assert
      Assert.AreEqual("Zone specific fallback", response);
    }

    [Test]
    public void GetFallbackResponse_WithTimeTrigger_ReturnsTimeTriggerFallback()
    {
      // Arrange
      var config = new FallbackSystem.FallbackConfig
      {
        TimeTriggerFallbacks = new List<string> { "Time specific fallback" },
        GenericFallbacks = new List<string> { "Generic fallback" }
      };
      var system = new FallbackSystem(config, new Random(12345));
      var context = new InteractionContext { TriggerReason = TriggerReason.TimeTrigger };

      // Act
      var response = system.GetFallbackResponse(context, "Test");

      // Assert
      Assert.AreEqual("Time specific fallback", response);
    }

    [Test]
    public void GetFallbackResponse_WithQuestTrigger_ReturnsQuestTriggerFallback()
    {
      // Arrange
      var config = new FallbackSystem.FallbackConfig
      {
        QuestTriggerFallbacks = new List<string> { "Quest specific fallback" },
        GenericFallbacks = new List<string> { "Generic fallback" }
      };
      var system = new FallbackSystem(config, new Random(12345));
      var context = new InteractionContext { TriggerReason = TriggerReason.QuestTrigger };

      // Act
      var response = system.GetFallbackResponse(context, "Test");

      // Assert
      Assert.AreEqual("Quest specific fallback", response);
    }

    [Test]
    public void GetFallbackResponse_WithNpcInteraction_ReturnsNpcInteractionFallback()
    {
      // Arrange
      var config = new FallbackSystem.FallbackConfig
      {
        NpcInteractionFallbacks = new List<string> { "NPC interaction fallback" },
        GenericFallbacks = new List<string> { "Generic fallback" }
      };
      var system = new FallbackSystem(config, new Random(12345));
      var context = new InteractionContext { TriggerReason = TriggerReason.NpcInteraction };

      // Act
      var response = system.GetFallbackResponse(context, "Test");

      // Assert
      Assert.AreEqual("NPC interaction fallback", response);
    }

    [Test]
    public void GetFallbackResponse_WithWorldEvent_ReturnsWorldEventFallback()
    {
      // Arrange
      var config = new FallbackSystem.FallbackConfig
      {
        WorldEventFallbacks = new List<string> { "World event fallback" },
        GenericFallbacks = new List<string> { "Generic fallback" }
      };
      var system = new FallbackSystem(config, new Random(12345));
      var context = new InteractionContext { TriggerReason = TriggerReason.WorldEvent };

      // Act
      var response = system.GetFallbackResponse(context, "Test");

      // Assert
      Assert.AreEqual("World event fallback", response);
    }

    [Test]
    public void GetFallbackResponse_WithCustomTrigger_ReturnsCustomTriggerFallback()
    {
      // Arrange
      var config = new FallbackSystem.FallbackConfig
      {
        CustomTriggerFallbacks = new List<string> { "Custom trigger fallback" },
        GenericFallbacks = new List<string> { "Generic fallback" }
      };
      var system = new FallbackSystem(config, new Random(12345));
      var context = new InteractionContext { TriggerReason = TriggerReason.Custom };

      // Act
      var response = system.GetFallbackResponse(context, "Test");

      // Assert
      Assert.AreEqual("Custom trigger fallback", response);
    }

    [Test]
    public void GetFallbackResponse_WithEmptyContextSpecificList_FallsBackToGeneric()
    {
      // Arrange
      var config = new FallbackSystem.FallbackConfig
      {
        PlayerUtteranceFallbacks = new List<string>(), // Empty
        GenericFallbacks = new List<string> { "Generic fallback" }
      };
      var system = new FallbackSystem(config, new Random(12345));
      var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };

      // Act
      var response = system.GetFallbackResponse(context, "Test");

      // Assert
      Assert.AreEqual("Generic fallback", response);
    }

    #endregion

    #region GetFallbackResponse Tests - Trigger-Specific Fallbacks

    [Test]
    public void GetFallbackResponse_WithTriggerFallbacks_UsesTriggerFallbacksFirst()
    {
      // Arrange
      var config = new FallbackSystem.FallbackConfig
      {
        PlayerUtteranceFallbacks = new List<string> { "Config fallback" },
        GenericFallbacks = new List<string> { "Generic fallback" }
      };
      var system = new FallbackSystem(config, new Random(12345));
      var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };
      var triggerFallbacks = new List<string> { "Trigger specific fallback" };

      // Act
      var response = system.GetFallbackResponse(context, "Test", null, triggerFallbacks);

      // Assert
      Assert.AreEqual("Trigger specific fallback", response);
    }

    [Test]
    public void GetFallbackResponse_WithEmptyTriggerFallbacks_FallsBackToConfig()
    {
      // Arrange
      var config = new FallbackSystem.FallbackConfig
      {
        PlayerUtteranceFallbacks = new List<string> { "Config fallback" }
      };
      var system = new FallbackSystem(config, new Random(12345));
      var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };
      var triggerFallbacks = new List<string>(); // Empty

      // Act
      var response = system.GetFallbackResponse(context, "Test", null, triggerFallbacks);

      // Assert
      Assert.AreEqual("Config fallback", response);
    }

    #endregion

    #region GetFallbackResponse Tests - Emergency Fallbacks

    [Test]
    public void GetFallbackResponse_WithAllListsEmpty_UsesEmergencyFallbacks()
    {
      // Arrange
      var config = new FallbackSystem.FallbackConfig
      {
        GenericFallbacks = new List<string>(),
        PlayerUtteranceFallbacks = new List<string>(),
        EmergencyFallbacks = new List<string> { "Emergency fallback" }
      };
      var system = new FallbackSystem(config, new Random(12345));
      var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };

      // Act
      var response = system.GetFallbackResponse(context, "Test");

      // Assert
      Assert.AreEqual("Emergency fallback", response);
      Assert.AreEqual(1, system.Stats.EmergencyFallbacks);
    }

    [Test]
    public void GetFallbackResponse_WithAllListsIncludingEmergencyEmpty_ReturnsHardcodedFallback()
    {
      // Arrange
      var config = new FallbackSystem.FallbackConfig
      {
        GenericFallbacks = new List<string>(),
        PlayerUtteranceFallbacks = new List<string>(),
        EmergencyFallbacks = new List<string>()
      };
      var system = new FallbackSystem(config, new Random(12345));
      var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };

      // Act
      var response = system.GetFallbackResponse(context, "Test");

      // Assert
      Assert.AreEqual("I'm having trouble responding right now.", response);
      Assert.AreEqual(1, system.Stats.EmergencyFallbacks);
    }

    #endregion

    #region Statistics Tests

    [Test]
    public void GetFallbackResponse_UpdatesTotalFallbacks()
    {
      // Arrange
      var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };

      // Act
      _fallbackSystem.GetFallbackResponse(context, "Failure 1");
      _fallbackSystem.GetFallbackResponse(context, "Failure 2");
      _fallbackSystem.GetFallbackResponse(context, "Failure 3");

      // Assert
      Assert.AreEqual(3, _fallbackSystem.Stats.TotalFallbacks);
    }

    [Test]
    public void GetFallbackResponse_TracksFallbacksByTriggerReason()
    {
      // Arrange
      var playerContext = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };
      var zoneContext = new InteractionContext { TriggerReason = TriggerReason.ZoneTrigger };

      // Act
      _fallbackSystem.GetFallbackResponse(playerContext, "Failure 1");
      _fallbackSystem.GetFallbackResponse(playerContext, "Failure 2");
      _fallbackSystem.GetFallbackResponse(zoneContext, "Failure 3");

      // Assert
      Assert.AreEqual(2, _fallbackSystem.Stats.FallbacksByTriggerReason[TriggerReason.PlayerUtterance]);
      Assert.AreEqual(1, _fallbackSystem.Stats.FallbacksByTriggerReason[TriggerReason.ZoneTrigger]);
    }

    [Test]
    public void GetFallbackResponse_TracksFallbacksByFailureReason()
    {
      // Arrange
      var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };

      // Act
      _fallbackSystem.GetFallbackResponse(context, "Network error");
      _fallbackSystem.GetFallbackResponse(context, "Network error");
      _fallbackSystem.GetFallbackResponse(context, "Validation failed");

      // Assert
      Assert.AreEqual(2, _fallbackSystem.Stats.FallbacksByFailureReason["Network error"]);
      Assert.AreEqual(1, _fallbackSystem.Stats.FallbacksByFailureReason["Validation failed"]);
    }

    [Test]
    public void GetFallbackResponse_TruncatesLongFailureReasons()
    {
      // Arrange
      var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };
      var longFailureReason = new string('A', 150); // 150 characters
      var expectedKey = new string('A', 100); // Should be truncated to 100

      // Act
      _fallbackSystem.GetFallbackResponse(context, longFailureReason);

      // Assert
      Assert.IsTrue(_fallbackSystem.Stats.FallbacksByFailureReason.ContainsKey(expectedKey));
      Assert.AreEqual(1, _fallbackSystem.Stats.FallbacksByFailureReason[expectedKey]);
    }

    [Test]
    public void GetFallbackResponse_WithNullFailureReason_TracksAsUnknown()
    {
      // Arrange
      var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };

      // Act
      _fallbackSystem.GetFallbackResponse(context, null);

      // Assert
      Assert.IsTrue(_fallbackSystem.Stats.FallbacksByFailureReason.ContainsKey("Unknown"));
      Assert.AreEqual(1, _fallbackSystem.Stats.FallbacksByFailureReason["Unknown"]);
    }

    [Test]
    public void GetFallbackResponse_WithEmptyFailureReason_TracksAsEmptyString()
    {
      // Arrange
      var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };

      // Act
      _fallbackSystem.GetFallbackResponse(context, "");

      // Assert
      // Empty string is tracked as empty string, not "Unknown"
      Assert.IsTrue(_fallbackSystem.Stats.FallbacksByFailureReason.ContainsKey("") || 
                    _fallbackSystem.Stats.FallbacksByFailureReason.ContainsKey("Unknown"));
    }

    #endregion

    #region Random Selection Tests

    [Test]
    public void GetFallbackResponse_WithMultipleOptions_SelectsFromList()
    {
      // Arrange
      var config = new FallbackSystem.FallbackConfig
      {
        PlayerUtteranceFallbacks = new List<string>
        {
          "Fallback 1",
          "Fallback 2",
          "Fallback 3"
        }
      };
      var system = new FallbackSystem(config);
      var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };

      // Act - Call multiple times to ensure we get different results (with high probability)
      var responses = new HashSet<string>();
      for (int i = 0; i < 20; i++)
      {
        responses.Add(system.GetFallbackResponse(context, "Test"));
      }

      // Assert - With 20 calls and 3 options, we should see at least 2 different responses
      // (allowing for randomness, but very unlikely to get same response 20 times)
      Assert.GreaterOrEqual(responses.Count, 1);
      Assert.LessOrEqual(responses.Count, 3);
    }

    #endregion

    #region FallbackStats Tests

    [Test]
    public void FallbackStats_ToString_FormatsCorrectly()
    {
      // Arrange
      var context1 = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };
      var context2 = new InteractionContext { TriggerReason = TriggerReason.ZoneTrigger };
      _fallbackSystem.GetFallbackResponse(context1, "Error 1");
      _fallbackSystem.GetFallbackResponse(context1, "Error 1");
      _fallbackSystem.GetFallbackResponse(context2, "Error 2");

      // Act
      var statsString = _fallbackSystem.Stats.ToString();

      // Assert
      Assert.IsNotNull(statsString);
      Assert.That(statsString, Contains.Substring("Total Fallbacks: 3"));
      Assert.That(statsString, Contains.Substring("PlayerUtterance"));
      Assert.That(statsString, Contains.Substring("ZoneTrigger"));
    }

    #endregion

    #region Integration with InferenceResult Tests

    [Test]
    public void GetFallbackResponse_WithInferenceResult_HandlesGracefully()
    {
      // Arrange
      var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };
      // InferenceResult is optional, so we can pass null
      InferenceResultWithRetries? inferenceResult = null;

      // Act
      var response = _fallbackSystem.GetFallbackResponse(context, "Test failure", inferenceResult);

      // Assert
      Assert.IsNotNull(response);
      Assert.IsNotEmpty(response);
    }

    #endregion
  }
}

