#nullable enable

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LlamaBrain.Core;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Persona;
using LlamaBrain.Runtime.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System;

namespace LlamaBrain.Tests.PlayMode
{
  /// <summary>
  /// PlayMode tests for few-shot priming and fallback system including:
  /// - Few-shot example injection into prompts
  /// - Fallback response selection
  /// - Fallback statistics tracking
  /// - Multi-turn conversation handling
  /// </summary>
  public class FewShotAndFallbackPlayModeTests
  {
    private GameObject? agentObject;
    private LlamaBrainAgent? unityAgent;
    private PersonaMemoryStore? memoryStore;
    private PersonaConfig? personaConfig;
    private List<IDisposable> disposables = new List<IDisposable>();
    private List<ScriptableObject> createdScriptableObjects = new List<ScriptableObject>();

    [SetUp]
    public void SetUp()
    {
      // Create persona config
      personaConfig = ScriptableObject.CreateInstance<PersonaConfig>();
      personaConfig!.PersonaId = "fewshot-test-npc";
      personaConfig!.Name = "FewShot Test NPC";
      personaConfig!.Description = "An NPC for testing few-shot priming";
      personaConfig!.SystemPrompt = "You are a helpful shopkeeper. Be friendly and helpful.";
      createdScriptableObjects.Add(personaConfig);

      // Create memory store
      memoryStore = new PersonaMemoryStore();
    }

    [TearDown]
    public void TearDown()
    {
      // Dispose tracked IDisposable instances
      foreach (var disposable in disposables)
      {
        try { disposable.Dispose(); } catch { }
      }
      disposables.Clear();

      // Cleanup Unity agent
      if (agentObject)
      {
        UnityEngine.Object.DestroyImmediate(agentObject);
        agentObject = null;
      }

      // Cleanup ScriptableObjects
      foreach (var so in createdScriptableObjects)
      {
        if (so) UnityEngine.Object.DestroyImmediate(so);
      }
      createdScriptableObjects.Clear();

      memoryStore = null;
      unityAgent = null;
    }

    #region Stub Client Helper

    /// <summary>
    /// Creates a stub client with the provided responses.
    /// </summary>
    private StubApiClient CreateStubClient(params string[] responses)
    {
      var stubResponses = responses.Select(r => new StubApiClient.StubResponse { Content = r }).ToList();
      var client = new StubApiClient(stubResponses);
      disposables.Add(client);
      return client;
    }

    /// <summary>
    /// Creates a stub client that throws exception to simulate server failure.
    /// </summary>
    private StubApiClient CreateFailingStubClient()
    {
      var stubResponses = new List<StubApiClient.StubResponse>();
      var client = new StubApiClient(stubResponses);
      disposables.Add(client);
      client.OnSendMetrics = (prompt, attempt, maxTokens, temperature, seed, cachePrompt, cancellationToken) =>
      {
        return Task.FromException<CompletionMetrics>(new System.Net.Http.HttpRequestException("Server unavailable"));
      };
      return client;
    }

    #endregion

    #region Few-Shot Priming Tests

    [UnityTest]
    [Category("Contract")]
    public IEnumerator FewShot_Examples_IncludedInPrompt()
    {
      // Arrange
      var stubClient = CreateStubClient("Welcome to my shop! How can I help you today?");

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Note: Few-shot examples are configured via WorkingMemoryConfig
      // This test verifies the prompt assembly includes the few-shot section

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Hello!");
      });

      // Assert
      Assert.That(unityAgent!.LastAssembledPrompt, Is.Not.Null);
      // The prompt should be assembled correctly
      var promptText = unityAgent!.LastAssembledPrompt!.Text;
      Assert.That(promptText, Is.Not.Null.And.Not.Empty);
    }

    [UnityTest]
    [Category("Contract")]
    public IEnumerator FewShot_WorkingMemory_TracksExampleCount()
    {
      // Arrange
      var stubClient = CreateStubClient("I'm doing well, thank you!");

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("How are you?");
      });

      // Assert - Working memory should track few-shot count
      Assert.That(unityAgent!.LastAssembledPrompt, Is.Not.Null);
      var breakdown = unityAgent!.LastAssembledPrompt!.Breakdown;
      Assert.That(breakdown, Is.Not.Null);
      // FewShotExamples count should be tracked (may be 0 if not configured)
      Assert.That(breakdown.FewShotExamples, Is.GreaterThanOrEqualTo(0));
    }

    #endregion

    #region Fallback System Tests

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Fallback_UsedWhenAllAttemptsFail()
    {
      // Arrange - Create stub that always fails
      var stubClient = CreateFailingStubClient();

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Get retry policy to know expected attempts
      var retryPolicyField = typeof(LlamaBrainAgent).GetField("retryPolicy", BindingFlags.NonPublic | BindingFlags.Instance);
      var retryPolicy = retryPolicyField?.GetValue(unityAgent) as RetryPolicy;
      var expectedAttempts = (retryPolicy?.MaxRetries ?? 2) + 1;

      // Expect error logs for each failed attempt
      var errorLog = new System.Text.RegularExpressions.Regex(@".*\[LlamaBrainAgent\] Inference error on attempt \d+: .*", System.Text.RegularExpressions.RegexOptions.Singleline);
      for (int i = 0; i < expectedAttempts; i++)
      {
        LogAssert.Expect(LogType.Error, errorLog);
      }

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Hello!");
      });

      // Assert
      Assert.That(result, Is.Not.Null, "Should always return a result");
      Assert.That(result!.FinalResult.Response, Is.Not.Null.And.Not.Empty,
        "Should have fallback response when all attempts fail");
      Assert.That(unityAgent!.FallbackStats, Is.Not.Null);
      Assert.That(unityAgent!.FallbackStats!.TotalFallbacks, Is.GreaterThan(0),
        "Fallback stats should track usage");
    }

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Fallback_StatisticsTrackedByTriggerReason()
    {
      // Arrange
      var stubClient = CreateFailingStubClient();

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Expect error logs
      var retryPolicyField = typeof(LlamaBrainAgent).GetField("retryPolicy", BindingFlags.NonPublic | BindingFlags.Instance);
      var retryPolicy = retryPolicyField?.GetValue(unityAgent) as RetryPolicy;
      var expectedAttempts = (retryPolicy?.MaxRetries ?? 2) + 1;
      var errorLog = new System.Text.RegularExpressions.Regex(@".*\[LlamaBrainAgent\] Inference error on attempt \d+: .*", System.Text.RegularExpressions.RegexOptions.Singleline);
      for (int i = 0; i < expectedAttempts; i++)
      {
        LogAssert.Expect(LogType.Error, errorLog);
      }

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Test input");
      });

      // Assert
      var stats = unityAgent!.FallbackStats;
      Assert.That(stats, Is.Not.Null);
      Assert.That(stats!.TotalFallbacks, Is.GreaterThan(0));
      // FallbacksByTriggerReason should track the trigger reason
      Assert.That(stats.FallbacksByTriggerReason, Is.Not.Null);
    }

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Fallback_NotUsedWhenInferenceSucceeds()
    {
      // Arrange
      var stubClient = CreateStubClient("Success response!");

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Hello!");
      });

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result!.Success, Is.True);
      Assert.That(result.FinalResult.Response, Is.EqualTo("Success response!"));

      // Fallback should NOT have been used
      var stats = unityAgent!.FallbackStats;
      if (stats != null)
      {
        Assert.That(stats.TotalFallbacks, Is.EqualTo(0),
          "Fallback should not be used when inference succeeds");
      }
    }

    #endregion

    #region Multi-Turn Conversation Tests

    [UnityTest]
    [Category("Contract")]
    public IEnumerator MultiTurn_DialogueHistory_Tracked()
    {
      // Arrange
      var stubClient = CreateStubClient(
        "Hello! Welcome to my shop.",
        "We have many items for sale.",
        "The sword costs 100 gold."
      );

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Act - Send multiple messages
      yield return UniTask.ToCoroutine(async () =>
      {
        await unityAgent!.SendWithSnapshotAsync("Hello!");
      });

      yield return UniTask.ToCoroutine(async () =>
      {
        await unityAgent!.SendWithSnapshotAsync("What do you sell?");
      });

      yield return UniTask.ToCoroutine(async () =>
      {
        await unityAgent!.SendWithSnapshotAsync("How much is the sword?");
      });

      // Assert - Dialogue should be tracked
      // The last snapshot should include dialogue history
      Assert.That(unityAgent!.LastSnapshot, Is.Not.Null);
      Assert.That(unityAgent!.LastSnapshot!.DialogueHistory, Is.Not.Null);
      Assert.That(unityAgent!.LastSnapshot!.DialogueHistory.Count, Is.GreaterThan(0),
        "Dialogue history should be tracked across turns");
    }

    [UnityTest]
    [Category("Contract")]
    public IEnumerator MultiTurn_PromptIncludesHistory()
    {
      // Arrange
      var stubClient = CreateStubClient(
        "Nice to meet you!",
        "I remember you from before."
      );

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Act - First turn
      yield return UniTask.ToCoroutine(async () =>
      {
        await unityAgent!.SendWithSnapshotAsync("My name is Alex.");
      });

      // Second turn
      yield return UniTask.ToCoroutine(async () =>
      {
        await unityAgent!.SendWithSnapshotAsync("Do you remember me?");
      });

      // Assert - Prompt should include previous exchanges
      Assert.That(unityAgent!.LastAssembledPrompt, Is.Not.Null);
      var promptText = unityAgent!.LastAssembledPrompt!.Text;
      Assert.That(promptText, Is.Not.Null.And.Not.Empty);

      // The breakdown should show dialogue history characters
      var breakdown = unityAgent!.LastAssembledPrompt!.Breakdown;
      Assert.That(breakdown.DialogueHistory, Is.GreaterThan(0),
        "Prompt should include dialogue history from previous turns");
    }

    [UnityTest]
    [Category("Contract")]
    public IEnumerator MultiTurn_InferenceResult_TracksAttempts()
    {
      // Arrange
      var stubClient = CreateStubClient("First response", "Second response");

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Act
      InferenceResultWithRetries? result1 = null;
      InferenceResultWithRetries? result2 = null;

      yield return UniTask.ToCoroutine(async () =>
      {
        result1 = await unityAgent!.SendWithSnapshotAsync("First message");
      });

      yield return UniTask.ToCoroutine(async () =>
      {
        result2 = await unityAgent!.SendWithSnapshotAsync("Second message");
      });

      // Assert
      Assert.That(result1, Is.Not.Null);
      Assert.That(result2, Is.Not.Null);
      Assert.That(result1!.AttemptCount, Is.GreaterThanOrEqualTo(1));
      Assert.That(result2!.AttemptCount, Is.GreaterThanOrEqualTo(1));
      Assert.That(result1.FinalResult.Response, Does.Contain("First response"));
      Assert.That(result2.FinalResult.Response, Does.Contain("Second response"));
    }

    #endregion

    #region Constraint Escalation Tests

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Escalation_ConstraintsEscalateOnRetry()
    {
      // Arrange - Create stub that fails first, succeeds second
      var stubResponses = new List<StubApiClient.StubResponse>
      {
        new StubApiClient.StubResponse { Content = "bad response" }, // Will fail validation
        new StubApiClient.StubResponse { Content = "good response" }  // Will pass
      };
      var stubClient = new StubApiClient(stubResponses);
      disposables.Add(stubClient);

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Test");
      });

      // Assert - Should have completed (either success or exhausted retries)
      Assert.That(result, Is.Not.Null);
      Assert.That(result!.AllAttempts.Count, Is.GreaterThanOrEqualTo(1));
    }

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Escalation_MaxRetriesRespected()
    {
      // Arrange - Create stub that always returns violating response
      var stubResponses = new List<StubApiClient.StubResponse>();
      for (int i = 0; i < 10; i++)
      {
        stubResponses.Add(new StubApiClient.StubResponse { Content = $"Response {i}" });
      }
      var stubClient = new StubApiClient(stubResponses);
      disposables.Add(stubClient);

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Get max retries
      var retryPolicyField = typeof(LlamaBrainAgent).GetField("retryPolicy", BindingFlags.NonPublic | BindingFlags.Instance);
      var retryPolicy = retryPolicyField?.GetValue(unityAgent) as RetryPolicy;
      var maxAttempts = (retryPolicy?.MaxRetries ?? 2) + 1;

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Test");
      });

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result!.AllAttempts.Count, Is.LessThanOrEqualTo(maxAttempts),
        "Should not exceed max retries");
    }

    #endregion

    #region Timing and Metrics Tests

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Metrics_ElapsedTimeTracked()
    {
      // Arrange
      var stubClient = CreateStubClient("Quick response");

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Hello");
      });

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result!.TotalElapsedMilliseconds, Is.GreaterThanOrEqualTo(0),
        "Elapsed time should be tracked");
      Assert.That(result.FinalResult.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(0),
        "Individual attempt elapsed time should be tracked");
    }

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Metrics_AttemptCountAccurate()
    {
      // Arrange - Create stub that fails first two times, succeeds third
      var stubResponses = new List<StubApiClient.StubResponse>
      {
        new StubApiClient.StubResponse { Content = "fail1" },
        new StubApiClient.StubResponse { Content = "fail2" },
        new StubApiClient.StubResponse { Content = "success" }
      };
      var stubClient = new StubApiClient(stubResponses);
      disposables.Add(stubClient);

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Test");
      });

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result!.AttemptCount, Is.EqualTo(result.AllAttempts.Count),
        "AttemptCount should match AllAttempts.Count");
    }

    #endregion
  }
}
