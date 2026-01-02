#nullable enable

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LlamaBrain.Core.Validation;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Persona;
using LlamaBrainRuntime.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LlamaBrain.Tests.PlayMode
{
  /// <summary>
  /// PlayMode tests for WorldIntentDispatcher (Feature 10.6).
  /// Tests pure dispatcher behavior including singleton lifecycle (Requirement #5).
  /// </summary>
  public class WorldIntentDispatcherTests
  {
    private GameObject? dispatcherObject1;
    private GameObject? dispatcherObject2;
    private WorldIntentDispatcher? dispatcher1;
    private WorldIntentDispatcher? dispatcher2;

    [TearDown]
    public void TearDown()
    {
      // Clean up any remaining instances
      if (dispatcherObject1 != null)
      {
        Object.DestroyImmediate(dispatcherObject1);
      }
      if (dispatcherObject2 != null)
      {
        Object.DestroyImmediate(dispatcherObject2);
      }
      // Reset singleton instance for test isolation
      // OnDestroy may not run in all test scenarios (domain reload, interruption)
      WorldIntentDispatcher.ResetForTests();
    }

    #region Singleton Pattern Tests (Requirement #5)

    [UnityTest]
    public IEnumerator Singleton_MultipleInstances_FirstBecomesInstance()
    {
      // Arrange - Create first dispatcher
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();

      // Act - Awake is called automatically when component is added
      yield return null; // Let Awake() execute

      // Assert - First instance should be the singleton
      Assert.That(WorldIntentDispatcher.Instance, Is.Not.Null, "Instance should be set");
      Assert.That(WorldIntentDispatcher.Instance, Is.EqualTo(dispatcher1), "First instance should be the singleton");
      Assert.That(dispatcher1!.IsDuplicateInstance, Is.False, "First instance should not be marked as duplicate");
    }

    [UnityTest]
    public IEnumerator Singleton_MultipleInstances_DuplicateDestroyed()
    {
      // Arrange - Create first dispatcher
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null; // Let first Awake() execute

      // Act - Create second dispatcher (duplicate)
      dispatcherObject2 = new GameObject("Dispatcher2");
      dispatcher2 = dispatcherObject2.AddComponent<WorldIntentDispatcher>();
      yield return null; // Let second Awake() execute (Destroy() is deferred)

      // Assert - Duplicate should be marked immediately
      Assert.That(dispatcher2, Is.Not.Null, "Dispatcher2 should exist before destruction");
      Assert.That(dispatcher2!.IsDuplicateInstance, Is.True, "Second instance should be marked as duplicate");
      Assert.That(WorldIntentDispatcher.Instance, Is.EqualTo(dispatcher1), "First instance should remain the singleton");
      Assert.That(WorldIntentDispatcher.Instance, Is.Not.EqualTo(dispatcher2), "Second instance should not be the singleton");
      
      // Verify duplicate is disabled - check via GameObject to avoid MissingReferenceException
      // The component is disabled in Awake() before Destroy() is called
      if (dispatcherObject2 != null)
      {
        var component = dispatcherObject2.GetComponent<WorldIntentDispatcher>();
        if (component != null)
        {
          Assert.That(component.enabled, Is.False, "Duplicate should be disabled");
        }
      }
      
      // Verify duplicate GameObject is destroyed (Unity lifecycle - may take a frame)
      yield return null; // Wait for Destroy() to process
      // Check GameObject destruction unambiguously (Unity fake-null weirdness)
      bool objectDestroyed = dispatcherObject2 == null || dispatcherObject2.Equals(null);
      bool componentDestroyed = dispatcherObject2 != null && dispatcherObject2.GetComponent<WorldIntentDispatcher>() == null;
      Assert.That(objectDestroyed || componentDestroyed, Is.True, 
        "Duplicate GameObject should be destroyed or component should be removed");
      Assert.That(WorldIntentDispatcher.Instance, Is.EqualTo(dispatcher1), 
        "First instance should remain the singleton after duplicate destruction");
    }

    [UnityTest]
    public IEnumerator Singleton_InstanceProperty_Accessible()
    {
      // Arrange - Create dispatcher
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null; // Let Awake() execute

      // Act & Assert
      Assert.That(WorldIntentDispatcher.Instance, Is.Not.Null, "Instance should be accessible");
      Assert.That(WorldIntentDispatcher.Instance, Is.EqualTo(dispatcher1), "Instance should match created dispatcher");
    }

    [UnityTest]
    public IEnumerator Singleton_OnDestroy_ClearsInstanceOnlyIfThis()
    {
      // Arrange - Create first dispatcher
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null; // Let Awake() execute
      
      var instanceBefore = WorldIntentDispatcher.Instance;
      Assert.That(instanceBefore, Is.EqualTo(dispatcher1), "First instance should be singleton");

      // Act - Create and destroy duplicate
      dispatcherObject2 = new GameObject("Dispatcher2");
      dispatcher2 = dispatcherObject2.AddComponent<WorldIntentDispatcher>();
      yield return null; // Let Awake() execute
      
      // Destroy duplicate (should NOT clear Instance)
      Object.DestroyImmediate(dispatcherObject2);
      yield return null; // Let OnDestroy() execute

      // Assert - Instance should still be first dispatcher (not cleared by duplicate)
      Assert.That(WorldIntentDispatcher.Instance, Is.EqualTo(dispatcher1), "Instance should not be cleared by duplicate destruction");
      Assert.That(WorldIntentDispatcher.Instance, Is.Not.Null, "Instance should still exist");

      // Act - Destroy the actual singleton
      Object.DestroyImmediate(dispatcherObject1);
      yield return null; // Let OnDestroy() execute

      // Assert - Instance should now be cleared
      Assert.That(WorldIntentDispatcher.Instance, Is.Null, "Instance should be cleared when singleton is destroyed");
    }

    [UnityTest]
    public IEnumerator Singleton_LifetimeModel_SceneLocal_NoDontDestroyOnLoad()
    {
      // Arrange - Create dispatcher
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null; // Let Awake() execute

      // Assert - Should NOT have DontDestroyOnLoad (scene-local model)
      // DontDestroyOnLoad objects live in a special scene named "DontDestroyOnLoad"
      Assert.That(dispatcherObject1.scene.IsValid(), Is.True, "Dispatcher should be in a scene (scene-local)");
      Assert.That(dispatcherObject1.scene.name, Is.Not.EqualTo("DontDestroyOnLoad"), 
        "Dispatcher should NOT be in DontDestroyOnLoad scene (scene-local lifetime model)");
      
      // Verify lifetime model: scene-local means it will be destroyed on scene unload
      // This is verified by the fact that we can destroy it normally
      Object.DestroyImmediate(dispatcherObject1);
      yield return null;
      Assert.That(WorldIntentDispatcher.Instance, Is.Null, "Instance should be cleared when destroyed");
    }

    #endregion

    #region Intent Dispatch Execution

    [UnityTest]
    public IEnumerator DispatchIntent_FiresOnAnyIntent_WithCorrectParameters()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var intent = WorldIntent.Create("follow_player", "player_001");
      intent.Parameters["distance"] = "5";
      var npcId = "npc_001";
      var eventFired = false;
      string? receivedIntentType = null;
      string? receivedTarget = null;
      Dictionary<string, string>? receivedParams = null;

      dispatcher1.OnAnyIntent.AddListener((type, target, parameters) =>
      {
        eventFired = true;
        receivedIntentType = type;
        receivedTarget = target;
        receivedParams = parameters;
      });

      // Act
      dispatcher1.DispatchIntent(intent, npcId);
      yield return null;

      // Assert
      Assert.That(eventFired, Is.True, "OnAnyIntent event should fire");
      Assert.That(receivedIntentType, Is.EqualTo("follow_player"), "Intent type should match");
      Assert.That(receivedTarget, Is.EqualTo("player_001"), "Target should match");
      Assert.That(receivedParams, Is.Not.Null, "Parameters should not be null");
      Assert.That(receivedParams!["distance"], Is.EqualTo("5"), "Parameter should match");
    }

    [UnityTest]
    public IEnumerator DispatchBatch_DispatchesAllIntents_FromEmittedIntents()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var intents = new List<WorldIntent>
      {
        WorldIntent.Create("follow_player", "player_001"),
        WorldIntent.Create("give_item", "player_001"),
        WorldIntent.Create("start_quest", "quest_001")
      };
      intents[1].Parameters["item"] = "sword";

      var batchResult = new MutationBatchResult
      {
        EmittedIntents = intents
      };

      var dispatchedCount = 0;
      dispatcher1.OnAnyIntent.AddListener((type, target, parameters) => dispatchedCount++);

      // Act
      dispatcher1.DispatchBatch(batchResult, "npc_001");
      yield return null;

      // Assert
      Assert.That(dispatchedCount, Is.EqualTo(3), "All intents should be dispatched");
    }

    [UnityTest]
    public IEnumerator DispatchBatch_DispatchesIntents_InOrder()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var intents = new List<WorldIntent>
      {
        WorldIntent.Create("intent1", "target1"),
        WorldIntent.Create("intent2", "target2"),
        WorldIntent.Create("intent3", "target3")
      };

      var batchResult = new MutationBatchResult
      {
        EmittedIntents = intents
      };

      var dispatchedOrder = new List<string>();
      dispatcher1.OnAnyIntent.AddListener((type, target, parameters) => dispatchedOrder.Add(type));

      // Act
      dispatcher1.DispatchBatch(batchResult, "npc_001");
      yield return null;

      // Assert
      Assert.That(dispatchedOrder, Has.Count.EqualTo(3), "All intents should be dispatched");
      Assert.That(dispatchedOrder[0], Is.EqualTo("intent1"), "First intent should be dispatched first");
      Assert.That(dispatchedOrder[1], Is.EqualTo("intent2"), "Second intent should be dispatched second");
      Assert.That(dispatchedOrder[2], Is.EqualTo("intent3"), "Third intent should be dispatched third");
    }

    [UnityTest]
    public IEnumerator DispatchIntent_NullIntent_HandledGracefully()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var eventFired = false;
      dispatcher1.OnAnyIntent.AddListener((type, target, parameters) => eventFired = true);

      // Act - Should not crash
      dispatcher1.DispatchIntent(null!, "npc_001");
      yield return null;

      // Assert - Event should not fire for null intent
      Assert.That(eventFired, Is.False, "Event should not fire for null intent");
      Assert.That(dispatcher1.TotalIntentsDispatched, Is.EqualTo(0), "Counter should not increment for null intent");
    }

    #endregion

    #region Intent Handler Execution

    [UnityTest]
    public IEnumerator IntentHandlers_OnAnyIntent_FiresForAllIntents()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var eventCount = 0;
      dispatcher1.OnAnyIntent.AddListener((type, target, parameters) => eventCount++);

      // Act
      dispatcher1.DispatchIntent(WorldIntent.Create("type1", "target1"), "npc1");
      dispatcher1.DispatchIntent(WorldIntent.Create("type2", "target2"), "npc2");
      dispatcher1.DispatchIntent(WorldIntent.Create("type3", "target3"), "npc3");
      yield return null;

      // Assert
      Assert.That(eventCount, Is.EqualTo(3), "OnAnyIntent should fire for all intents");
    }

    [UnityTest]
    public IEnumerator IntentHandlers_IntentSpecificHandler_FiresForMatchingType()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      // Use reflection to access private intentHandlers field and add a handler
      // Note: intentHandlers is typically configured via Unity Inspector, but we test via reflection
      var handlerConfig = new IntentHandlerConfig
      {
        intentType = "follow_player"
      };

      var handlerFired = false;
      WorldIntent? receivedIntent = null;
      string? receivedNpcId = null;

      handlerConfig.onIntentReceived.AddListener((intent, npcId) =>
      {
        handlerFired = true;
        receivedIntent = intent;
        receivedNpcId = npcId;
      });

      // Add handler via reflection (since intentHandlers is private SerializeField)
      var field = typeof(WorldIntentDispatcher).GetField("intentHandlers", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      if (field != null)
      {
        var handlers = (List<IntentHandlerConfig>)field.GetValue(dispatcher1)!;
        handlers.Add(handlerConfig);
      }

      // Act
      dispatcher1.DispatchIntent(WorldIntent.Create("follow_player", "player_001"), "npc_001");
      yield return null;

      // Assert
      Assert.That(handlerFired, Is.True, "Intent-specific handler should fire");
      Assert.That(receivedIntent, Is.Not.Null, "Intent should be received");
      Assert.That(receivedIntent!.IntentType, Is.EqualTo("follow_player"), "Intent type should match");
      Assert.That(receivedNpcId, Is.EqualTo("npc_001"), "NPC ID should match");
    }

    [UnityTest]
    public IEnumerator IntentHandlers_CodeRegisteredHandler_FiresForMatchingType()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var handlerFired = false;
      WorldIntent? receivedIntent = null;
      string? receivedNpcId = null;

      dispatcher1.RegisterHandler("give_item", (intent, npcId) =>
      {
        handlerFired = true;
        receivedIntent = intent;
        receivedNpcId = npcId;
      });

      // Act
      dispatcher1.DispatchIntent(WorldIntent.Create("give_item", "player_001"), "npc_001");
      yield return null;

      // Assert
      Assert.That(handlerFired, Is.True, "Code-registered handler should fire");
      Assert.That(receivedIntent, Is.Not.Null, "Intent should be received");
      Assert.That(receivedIntent!.IntentType, Is.EqualTo("give_item"), "Intent type should match");
      Assert.That(receivedNpcId, Is.EqualTo("npc_001"), "NPC ID should match");
    }

    [UnityTest]
    public IEnumerator IntentHandlers_WildcardHandler_FiresForAllIntents()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var handlerCount = 0;
      dispatcher1.RegisterHandler("*", (intent, npcId) => handlerCount++);

      // Act
      dispatcher1.DispatchIntent(WorldIntent.Create("type1", "target1"), "npc1");
      dispatcher1.DispatchIntent(WorldIntent.Create("type2", "target2"), "npc2");
      dispatcher1.DispatchIntent(WorldIntent.Create("type3", "target3"), "npc3");
      yield return null;

      // Assert
      Assert.That(handlerCount, Is.EqualTo(3), "Wildcard handler should fire for all intents");
    }

    [UnityTest]
    public IEnumerator IntentHandlers_HandlerException_DoesNotPreventOtherHandlers()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var handler1Fired = false;
      var handler2Fired = false;

      dispatcher1.RegisterHandler("test_intent", (intent, npcId) =>
      {
        handler1Fired = true;
        throw new System.Exception("Handler 1 exception");
      });

      dispatcher1.RegisterHandler("test_intent", (intent, npcId) =>
      {
        handler2Fired = true;
      });

      // Expect the error log from the exception handler (use regex for robustness)
      LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[WorldIntentDispatcher\] Handler error for test_intent:.*Handler 1 exception"));

      // Act - Should not crash even if first handler throws
      dispatcher1.DispatchIntent(WorldIntent.Create("test_intent", "target1"), "npc1");
      yield return null;

      // Assert
      Assert.That(handler1Fired, Is.True, "First handler should have been called");
      Assert.That(handler2Fired, Is.True, "Second handler should still fire despite first handler exception");
    }

    #endregion

    #region Intent History Tracking

    [UnityTest]
    public IEnumerator IntentHistory_DispatchedIntents_RecordedInHistory()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var intent1 = WorldIntent.Create("type1", "target1");
      var intent2 = WorldIntent.Create("type2", "target2");

      // Act
      dispatcher1.DispatchIntent(intent1, "npc1");
      dispatcher1.DispatchIntent(intent2, "npc2");
      yield return null;

      // Assert
      var history = dispatcher1.IntentHistory.ToList();
      Assert.That(history, Has.Count.EqualTo(2), "History should contain 2 intents");
      Assert.That(history[0].Intent.IntentType, Is.EqualTo("type1"), "First intent should be in history");
      Assert.That(history[1].Intent.IntentType, Is.EqualTo("type2"), "Second intent should be in history");
    }

    [UnityTest]
    public IEnumerator IntentHistory_MaxHistorySize_Respected()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      
      // Set maxHistorySize to 3 using test helper
      dispatcher1.SetMaxHistorySizeForTests(3);
      
      yield return null;

      // Act - Dispatch 5 intents
      for (int i = 0; i < 5; i++)
      {
        dispatcher1.DispatchIntent(WorldIntent.Create($"type{i}", "target"), "npc");
      }
      yield return null;

      // Assert - History should be capped at 3
      var history = dispatcher1.IntentHistory.ToList();
      Assert.That(history, Has.Count.EqualTo(3), "History should be capped at maxHistorySize");
      // Most recent 3 should be kept (type2, type3, type4)
      Assert.That(history[0].Intent.IntentType, Is.EqualTo("type2"), "Oldest should be type2");
      Assert.That(history[2].Intent.IntentType, Is.EqualTo("type4"), "Newest should be type4");
    }

    [UnityTest]
    public IEnumerator IntentHistory_GetIntentsFromNpc_FiltersCorrectly()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      dispatcher1.DispatchIntent(WorldIntent.Create("type1", "target1"), "npc1");
      dispatcher1.DispatchIntent(WorldIntent.Create("type2", "target2"), "npc2");
      dispatcher1.DispatchIntent(WorldIntent.Create("type3", "target3"), "npc1");
      yield return null;

      // Act
      var npc1Intents = dispatcher1.GetIntentsFromNpc("npc1").ToList();

      // Assert
      Assert.That(npc1Intents, Has.Count.EqualTo(2), "Should return 2 intents from npc1");
      Assert.That(npc1Intents.All(r => r.NpcId == "npc1"), Is.True, "All returned intents should be from npc1");
    }

    [UnityTest]
    public IEnumerator IntentHistory_GetIntentsByType_FiltersCorrectly()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      dispatcher1.DispatchIntent(WorldIntent.Create("follow_player", "target1"), "npc1");
      dispatcher1.DispatchIntent(WorldIntent.Create("give_item", "target2"), "npc2");
      dispatcher1.DispatchIntent(WorldIntent.Create("follow_player", "target3"), "npc3");
      yield return null;

      // Act
      var followIntents = dispatcher1.GetIntentsByType("follow_player").ToList();

      // Assert
      Assert.That(followIntents, Has.Count.EqualTo(2), "Should return 2 follow_player intents");
      Assert.That(followIntents.All(r => r.Intent.IntentType == "follow_player"), Is.True, "All returned intents should be follow_player type");
    }

    [UnityTest]
    public IEnumerator IntentHistory_RecordsIncludeGameTime()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var beforeTime = Time.time;
      yield return new WaitForSeconds(0.1f); // Wait a bit

      // Act
      dispatcher1.DispatchIntent(WorldIntent.Create("type1", "target1"), "npc1");
      yield return null;

      var afterTime = Time.time;

      // Assert
      var history = dispatcher1.IntentHistory.ToList();
      Assert.That(history, Has.Count.EqualTo(1), "History should contain 1 intent");
      Assert.That(history[0].GameTime, Is.GreaterThanOrEqualTo(beforeTime), "GameTime should be >= before dispatch");
      Assert.That(history[0].GameTime, Is.LessThanOrEqualTo(afterTime), "GameTime should be <= after dispatch");
    }

    #endregion

    #region Statistics

    [UnityTest]
    public IEnumerator Statistics_TotalIntentsDispatched_Increments()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      Assert.That(dispatcher1.TotalIntentsDispatched, Is.EqualTo(0), "Initial count should be 0");

      // Act
      dispatcher1.DispatchIntent(WorldIntent.Create("type1", "target1"), "npc1");
      yield return null;
      Assert.That(dispatcher1.TotalIntentsDispatched, Is.EqualTo(1), "Count should be 1 after first dispatch");

      dispatcher1.DispatchIntent(WorldIntent.Create("type2", "target2"), "npc2");
      yield return null;
      Assert.That(dispatcher1.TotalIntentsDispatched, Is.EqualTo(2), "Count should be 2 after second dispatch");
    }

    [UnityTest]
    public IEnumerator Statistics_ResetStatistics_ClearsHistoryAndCounter()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      dispatcher1.DispatchIntent(WorldIntent.Create("type1", "target1"), "npc1");
      dispatcher1.DispatchIntent(WorldIntent.Create("type2", "target2"), "npc2");
      yield return null;

      Assert.That(dispatcher1.TotalIntentsDispatched, Is.EqualTo(2), "Count should be 2");
      Assert.That(dispatcher1.IntentHistory.Count(), Is.EqualTo(2), "History should have 2 items");

      // Act
      dispatcher1.ResetStatistics();
      yield return null;

      // Assert
      Assert.That(dispatcher1.TotalIntentsDispatched, Is.EqualTo(0), "Counter should be reset to 0");
      Assert.That(dispatcher1.IntentHistory.Count(), Is.EqualTo(0), "History should be cleared");
    }

    [UnityTest]
    public IEnumerator Statistics_AccurateAcrossMultipleDispatchCalls()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      // Act - Dispatch multiple times
      for (int i = 0; i < 5; i++)
      {
        dispatcher1.DispatchIntent(WorldIntent.Create($"type{i}", "target"), "npc");
      }
      yield return null;

      // Assert
      Assert.That(dispatcher1.TotalIntentsDispatched, Is.EqualTo(5), "Counter should be 5");
      Assert.That(dispatcher1.IntentHistory.Count(), Is.EqualTo(5), "History should have 5 items");
    }

    #endregion

    #region Integration with MemoryMutationController

    [UnityTest]
    public IEnumerator Integration_HookToController_SubscribesToEvent()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var controller = new MemoryMutationController();

      var intentReceived = false;
      dispatcher1.OnAnyIntent.AddListener((type, target, parameters) => intentReceived = true);

      // Act
      dispatcher1.HookToController(controller);
      
      // Emit an intent from the controller using internal test method
      var intent = WorldIntent.Create("test_intent", "target");
      controller.RaiseWorldIntentEmittedForTests(intent, "npc1");
      yield return null;

      // Assert
      Assert.That(intentReceived, Is.True, "Intent should be received via event subscription");
    }

    [UnityTest]
    public IEnumerator Integration_UnhookFromController_Unsubscribes()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var controller = new MemoryMutationController();

      var intentCount = 0;
      dispatcher1.OnAnyIntent.AddListener((type, target, parameters) => intentCount++);

      dispatcher1.HookToController(controller);
      
      // Emit intent (should be received) using internal test method
      controller.RaiseWorldIntentEmittedForTests(WorldIntent.Create("intent1", "target"), "npc1");
      yield return null;
      Assert.That(intentCount, Is.EqualTo(1), "First intent should be received");

      // Act - Unhook
      dispatcher1.UnhookFromController(controller);
      
      // Emit another intent (should NOT be received) using internal test method
      controller.RaiseWorldIntentEmittedForTests(WorldIntent.Create("intent2", "target"), "npc1");
      yield return null;

      // Assert
      Assert.That(intentCount, Is.EqualTo(1), "Second intent should not be received after unhook");
    }

    [UnityTest]
    public IEnumerator Integration_AutomaticDispatch_WorksWhenHooked()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var controller = new MemoryMutationController();

      var dispatchedCount = 0;
      dispatcher1.OnAnyIntent.AddListener((type, target, parameters) => dispatchedCount++);

      dispatcher1.HookToController(controller);

      // Act - Emit intents from controller using internal test method
      controller.RaiseWorldIntentEmittedForTests(WorldIntent.Create("intent1", "target1"), "npc1");
      controller.RaiseWorldIntentEmittedForTests(WorldIntent.Create("intent2", "target2"), "npc1");
      yield return null;

      // Assert
      Assert.That(dispatchedCount, Is.EqualTo(2), "Both intents should be automatically dispatched");
    }

    [UnityTest]
    public IEnumerator Integration_ManualDispatch_StillWorksWhenHooked()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var controller = new MemoryMutationController();

      var dispatchedCount = 0;
      dispatcher1.OnAnyIntent.AddListener((type, target, parameters) => dispatchedCount++);

      dispatcher1.HookToController(controller);

      // Act - Manual dispatch should still work
      dispatcher1.DispatchIntent(WorldIntent.Create("manual_intent", "target"), "npc1");
      yield return null;

      // Assert
      Assert.That(dispatchedCount, Is.EqualTo(1), "Manual dispatch should still work when hooked");
    }

    [UnityTest]
    public IEnumerator Integration_HookToController_DoubleHook_DoesNotDoubleSubscribe()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var controller = new MemoryMutationController();

      var intentCount = 0;
      dispatcher1.OnAnyIntent.AddListener((type, target, parameters) => intentCount++);

      // Act - Hook twice
      dispatcher1.HookToController(controller);
      dispatcher1.HookToController(controller); // Second hook should be idempotent

      // Emit an intent - should only be received once, not twice
      controller.RaiseWorldIntentEmittedForTests(WorldIntent.Create("test_intent", "target"), "npc1");
      yield return null;

      // Assert - Should only receive once, not double-subscribed
      Assert.That(intentCount, Is.EqualTo(1), "Intent should be received exactly once, not double-subscribed");
    }

    [UnityTest]
    public IEnumerator Integration_PolicyBoundary_PassingValidation_ProducesDispatches()
    {
      // Positive control: passing validation must produce EmittedIntents and dispatches.

      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var controller = new MemoryMutationController();
      dispatcher1.HookToController(controller);

      var dispatchedCount = 0;
      dispatcher1.OnAnyIntent.AddListener((type, target, parameters) => dispatchedCount++);

      // Create a passing gate result (validation passes)
      var passingOutput = ParsedOutput.Dialogue("Follow me!", "raw")
        .WithIntent(WorldIntent.Create("follow", "player"));

      var validationGate = new ValidationGate();
      var validationContext = new ValidationContext { Constraints = new ConstraintSet() };

      // Act - Validate (should pass)
      var gateResult = validationGate.Validate(passingOutput, validationContext);
      Assert.That(gateResult.Passed, Is.True, "Gate should pass");
      Assert.That(gateResult.ApprovedIntents.Count, Is.EqualTo(1), "ApprovedIntents should contain the intent");

      // Execute mutations (should produce EmittedIntents because gate passed)
      var memorySystem = new AuthoritativeMemorySystem();
      var mutationResult = controller.ExecuteMutations(gateResult, memorySystem, "npc1");
      yield return null; // Allow event propagation

      // Assert - Intent emitted, dispatcher received it
      Assert.That(mutationResult.EmittedIntents.Count, Is.EqualTo(1), 
        "EmittedIntents should contain 1 intent when validation passes");
      Assert.That(mutationResult.EmittedIntents[0].IntentType, Is.EqualTo("follow"),
        "Emitted intent should have correct IntentType");
      Assert.That(mutationResult.EmittedIntents[0].Target, Is.EqualTo("player"),
        "Emitted intent should have correct Target");
      Assert.That(controller.WorldIntentEmitsForTests, Is.EqualTo(1), 
        "Controller should have emitted exactly 1 intent event");
      Assert.That(dispatchedCount, Is.EqualTo(1), 
        "Dispatcher should have received exactly 1 intent");
      Assert.That(dispatcher1.TotalIntentsDispatched, Is.EqualTo(1), 
        "TotalIntentsDispatched should be 1");
    }

    [UnityTest]
    public IEnumerator Integration_PolicyBoundary_FailingValidation_ProducesZeroDispatches()
    {
      // Policy boundary proof: a failing validation must produce zero EmittedIntents,
      // and therefore zero dispatches when hooked (integration test across controller + validator/mutator output).

      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var controller = new MemoryMutationController();
      dispatcher1.HookToController(controller);

      var dispatchedCount = 0;
      dispatcher1.OnAnyIntent.AddListener((type, target, parameters) => dispatchedCount++);

      // Create a failing gate result (validation failed)
      // This simulates a scenario where ParsedOutput contains intents, but validation fails
      var failingOutput = ParsedOutput.Dialogue("Secret: follow me!", "raw")
        .WithIntent(WorldIntent.Create("follow", "player"));

      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "No secrets", "Don't say secret", "secret"));

      var validationContext = new ValidationContext { Constraints = constraints };
      var validationGate = new ValidationGate();

      // Act - Validate (should fail)
      var gateResult = validationGate.Validate(failingOutput, validationContext);
      Assert.That(gateResult.Passed, Is.False, "Gate should fail due to constraint violation");
      Assert.That(gateResult.ApprovedIntents.Count, Is.EqualTo(0), "ApprovedIntents must be empty when gate fails");

      // Execute mutations (should produce zero EmittedIntents because gate failed)
      var memorySystem = new AuthoritativeMemorySystem();
      var mutationResult = controller.ExecuteMutations(gateResult, memorySystem, "npc1");
      yield return null; // Allow event propagation (should be none)

      // Assert - Zero intents emitted, zero controller emissions, zero dispatches
      Assert.That(mutationResult.EmittedIntents.Count, Is.EqualTo(0), 
        "EmittedIntents must be empty when validation fails");
      Assert.That(controller.WorldIntentEmitsForTests, Is.EqualTo(0), 
        "Controller should have emitted zero intent events (policy boundary proof)");
      Assert.That(dispatchedCount, Is.EqualTo(0), 
        "Zero dispatches should occur when validation fails (policy boundary proof)");
      Assert.That(dispatcher1.TotalIntentsDispatched, Is.EqualTo(0), 
        "TotalIntentsDispatched should remain zero");
    }

    #endregion

    #region Edge Cases

    [UnityTest]
    public IEnumerator EdgeCases_NullNpcId_HandledGracefully()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var eventFired = false;
      dispatcher1.OnAnyIntent.AddListener((type, target, parameters) => eventFired = true);

      // Act - Should not crash
      dispatcher1.DispatchIntent(WorldIntent.Create("type1", "target1"), null);
      yield return null;

      // Assert
      Assert.That(eventFired, Is.True, "Event should still fire with null npcId");
      Assert.That(dispatcher1.TotalIntentsDispatched, Is.EqualTo(1), "Counter should increment");
    }

    [UnityTest]
    public IEnumerator EdgeCases_EmptyIntentType_StillDispatches()
    {
      // Arrange
      dispatcherObject1 = new GameObject("Dispatcher1");
      dispatcher1 = dispatcherObject1.AddComponent<WorldIntentDispatcher>();
      yield return null;

      var eventFired = false;
      dispatcher1.OnAnyIntent.AddListener((type, target, parameters) => eventFired = true);

      // Act
      dispatcher1.DispatchIntent(WorldIntent.Create("", "target1"), "npc1");
      yield return null;

      // Assert
      Assert.That(eventFired, Is.True, "Event should fire even with empty intent type");
      Assert.That(dispatcher1.TotalIntentsDispatched, Is.EqualTo(1), "Counter should increment");
    }

    #endregion
  }
}
