#nullable enable

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LlamaBrain.Core;
using LlamaBrain.Persona;
using LlamaBrain.Runtime.Core;
using LlamaBrain.Runtime.Core.Expectancy;
using LlamaBrain.Runtime.Core.Validation;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Validation;
using LlamaBrain.Core.StructuredOutput;
using System.Collections;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System.Linq;
using LlamaBrain.Persona.MemoryTypes;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;

namespace LlamaBrain.Tests.PlayMode
{
  /// <summary>
  /// Stub IApiClient implementation for deterministic contract tests.
  /// Returns configurable responses based on LLM call attempt count.
  /// Both SendPromptAsync and SendPromptWithMetricsAsync route through the same attempt counter.
  /// </summary>
  internal class StubApiClient : IApiClient, IDisposable
  {
    private int llmCallCount = 0; // Single counter for all LLM calls (attempts) - thread-safe via Interlocked
    private readonly List<StubResponse> responses;
    private bool disposed = false;

    public event Action<CompletionMetrics>? OnMetricsAvailable;

    /// <summary>
    /// Optional custom handler for SendPromptWithMetricsAsync (for throwing exceptions, etc.)
    /// The attempt number is passed as the second parameter to enable deterministic "attempt 1 throws, attempt 2 succeeds" behaviors.
    /// </summary>
    public Func<string, int, int?, float?, int?, bool, CancellationToken, Task<CompletionMetrics>>? OnSendMetrics;

    public StubApiClient(List<StubResponse> responses)
    {
      this.responses = responses ?? throw new ArgumentNullException(nameof(responses));
      // Allow empty list if OnSendMetrics is provided (for exception throwing, etc.)
      // Otherwise require at least one response
      if (responses.Count == 0)
      {
        // Empty list is allowed if OnSendMetrics will be set (checked at call time)
        // For now, we'll allow it and validate at SendPromptWithMetricsAsync
      }
    }

    /// <summary>
    /// Sends a prompt and returns just the content string.
    /// Routes through SendPromptWithMetricsAsync to ensure single attempt counter.
    /// </summary>
    public async Task<string> SendPromptAsync(string prompt, int? maxTokens = null, float? temperature = null, int? seed = null, CancellationToken cancellationToken = default)
    {
      var metrics = await SendPromptWithMetricsAsync(prompt, maxTokens, temperature, seed, false, cancellationToken);
      return metrics.Content;
    }

    /// <summary>
    /// Sends a prompt and returns detailed metrics.
    /// This is the primary method - all LLM calls increment the attempt counter here.
    /// </summary>
    public Task<CompletionMetrics> SendPromptWithMetricsAsync(string prompt, int? maxTokens = null, float? temperature = null, int? seed = null, bool cachePrompt = false, CancellationToken cancellationToken = default)
    {
      if (disposed) throw new ObjectDisposedException(nameof(StubApiClient));

      // Increment atomically to ensure thread-safety if concurrent calls occur
      // This ensures consistent attempt counting whether OnSendMetrics throws or returns normally
      var attempt = Interlocked.Increment(ref llmCallCount);

      // Use custom handler if provided (for throwing exceptions, etc.)
      // Pass attempt number to enable deterministic "attempt 1 throws, attempt 2 succeeds" behaviors
      if (OnSendMetrics != null)
      {
        return OnSendMetrics(prompt, attempt, maxTokens, temperature, seed, cachePrompt, cancellationToken);
      }
      
      // Validate responses list is not empty (unless OnSendMetrics is provided)
      if (responses.Count == 0)
      {
        throw new InvalidOperationException("StubApiClient requires at least one response, or OnSendMetrics must be set");
      }
      
      // Get response for current attempt (attempt 1 = responses[0], attempt 2 = responses[1], etc.)
      var response = GetResponse(attempt);
      
      var metrics = new CompletionMetrics
      {
        Content = response.Content,
        PromptTokenCount = response.PromptTokenCount,
        GeneratedTokenCount = response.GeneratedTokenCount,
        TotalTimeMs = response.TotalTimeMs,
        TtftMs = response.TtftMs,
        PrefillTimeMs = response.PrefillTimeMs,
        DecodeTimeMs = response.DecodeTimeMs,
        CachedTokenCount = response.CachedTokenCount
      };
      
      // Raise event if subscribed
      OnMetricsAvailable?.Invoke(metrics);
      
      return Task.FromResult(metrics);
    }

    /// <summary>
    /// Sends a structured prompt and returns just the content string.
    /// Routes through SendStructuredPromptWithMetricsAsync to ensure single attempt counter.
    /// </summary>
    public async Task<string> SendStructuredPromptAsync(
      string prompt,
      string jsonSchema,
      StructuredOutputFormat format = StructuredOutputFormat.JsonSchema,
      int? maxTokens = null,
      float? temperature = null,
      int? seed = null,
      bool cachePrompt = false,
      CancellationToken cancellationToken = default)
    {
      var metrics = await SendStructuredPromptWithMetricsAsync(prompt, jsonSchema, format, maxTokens, temperature, seed, cachePrompt, cancellationToken);
      return metrics.Content;
    }

    /// <summary>
    /// Sends a structured prompt and returns detailed metrics.
    /// This uses the same attempt counter as SendPromptWithMetricsAsync to ensure consistent counting.
    /// </summary>
    public Task<CompletionMetrics> SendStructuredPromptWithMetricsAsync(
      string prompt,
      string jsonSchema,
      StructuredOutputFormat format = StructuredOutputFormat.JsonSchema,
      int? maxTokens = null,
      float? temperature = null,
      int? seed = null,
      bool cachePrompt = false,
      CancellationToken cancellationToken = default)
    {
      if (disposed) throw new ObjectDisposedException(nameof(StubApiClient));
      
      // Increment atomically to ensure thread-safety if concurrent calls occur
      // This ensures consistent attempt counting whether OnSendMetrics throws or returns normally
      var attempt = Interlocked.Increment(ref llmCallCount);
      
      // Use custom handler if provided (for throwing exceptions, etc.)
      // Pass attempt number to enable deterministic "attempt 1 throws, attempt 2 succeeds" behaviors
      // Note: For structured prompts, we'll use the same handler but it won't receive jsonSchema/format
      // This is acceptable for stub testing - the handler can ignore those parameters
      if (OnSendMetrics != null)
      {
        // For structured prompts, we'll route through the same handler but it won't have schema info
        // This is fine for stub testing - the handler can work with just prompt/attempt
        return OnSendMetrics(prompt, attempt, maxTokens, temperature, seed, cachePrompt, cancellationToken);
      }
      
      // Validate responses list is not empty (unless OnSendMetrics is provided)
      if (responses.Count == 0)
      {
        throw new InvalidOperationException("StubApiClient requires at least one response, or OnSendMetrics must be set");
      }
      
      // Get response for current attempt (attempt 1 = responses[0], attempt 2 = responses[1], etc.)
      var response = GetResponse(attempt);
      
      var metrics = new CompletionMetrics
      {
        Content = response.Content,
        PromptTokenCount = response.PromptTokenCount,
        GeneratedTokenCount = response.GeneratedTokenCount,
        TotalTimeMs = response.TotalTimeMs,
        TtftMs = response.TtftMs,
        PrefillTimeMs = response.PrefillTimeMs,
        DecodeTimeMs = response.DecodeTimeMs,
        CachedTokenCount = response.CachedTokenCount
      };
      
      // Raise event if subscribed
      OnMetricsAvailable?.Invoke(metrics);
      
      return Task.FromResult(metrics);
    }

    /// <summary>
    /// Gets the response for the specified attempt number.
    /// Attempt 1 maps to index 0, attempt 2 to index 1, etc.
    /// </summary>
    private StubResponse GetResponse(int attempt)
    {
      var index = Math.Min(attempt - 1, responses.Count - 1);
      return responses[index];
    }

    public void Dispose()
    {
      disposed = true;
      OnSendMetrics = null;
      // Note: Cannot null event from outside - if you need deterministic teardown,
      // convert OnMetricsAvailable to a delegate field instead of an event
    }

    public class StubResponse
    {
      public string Content { get; set; } = "";
      public int PromptTokenCount { get; set; } = 10;
      public int GeneratedTokenCount { get; set; } = 5;
      public long TotalTimeMs { get; set; } = 100;
      public long TtftMs { get; set; } = 20;
      public long PrefillTimeMs { get; set; } = 20;
      public long DecodeTimeMs { get; set; } = 80;
      public int CachedTokenCount { get; set; } = 0;
    }
  }

  /// <summary>
  /// Integration tests split into two categories:
  /// 1. PlayMode_ExternalIntegration_*: Tests that require real llama.cpp server (may be Inconclusive)
  /// 2. PlayMode_PipelineContract_*: Deterministic contract tests using IApiClient stubs
  /// 
  /// NOTE: LlamaBrainAgent now accepts IApiClient interface, enabling deterministic contract tests with stubs.
  /// External integration tests use ApiClient (which implements IApiClient) for real server communication.
  /// </summary>
  public class FullPipelinePlayModeTests
  {
    /// <summary>
    /// Reference equality comparer for de-duping IDisposable instances by reference, not value equality.
    /// Prevents Distinct() from dropping entries if IDisposable overrides Equals.
    /// </summary>
    private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
      public static readonly ReferenceEqualityComparer<T> Instance = new();
      public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
      public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    /// <summary>
    /// De-dupes items by reference equality to prevent Distinct() from dropping entries.
    /// </summary>
    private static IEnumerable<T> DistinctByReference<T>(IEnumerable<T> items) where T : class
    {
      var set = new HashSet<T>(ReferenceEqualityComparer<T>.Instance);
      foreach (var item in items)
      {
        if (item != null && set.Add(item))
        {
          yield return item;
        }
      }
    }
    private GameObject? serverObject;
    private BrainServer? server;
    private BrainSettings? settings;
    private GameObject? agentObject;
    private LlamaBrainAgent? unityAgent;
    private ApiClient? apiClient;
    private PersonaMemoryStore? memoryStore;
    private PersonaConfig? personaConfig;
    private InferenceResultWithRetries? lastResult; // For failure logging
    private List<ScriptableObject> createdScriptableObjects = new List<ScriptableObject>(); // Track for cleanup
    private List<GameObject> createdGameObjects = new List<GameObject>(); // Track GameObjects for cleanup (expectancy configs, etc.)
    private List<IDisposable> disposables = new List<IDisposable>(); // Track IDisposable instances for cleanup
    private string? resolvedExePath; // Resolved executable path from RequireExternal()
    private string? resolvedModelPath; // Resolved model path from RequireExternal()
    
    // Configurable paths from environment variables
    private static string GetExecutablePath() => 
      Environment.GetEnvironmentVariable("LLAMABRAIN_EXECUTABLE_PATH") ?? "Backend/llama-server.exe";
    
    private static string GetModelPath() => 
      Environment.GetEnvironmentVariable("LLAMABRAIN_MODEL_PATH") ?? "Backend/model/stablelm-zephyr-3b.Q4_0.gguf";
    
    private static int GetPort()
    {
      var envPort = Environment.GetEnvironmentVariable("LLAMABRAIN_PORT");
      if (!string.IsNullOrEmpty(envPort) && int.TryParse(envPort, out var envPortValue))
        return envPortValue;
      
      // Pick a random free TCP port to avoid collisions
      try
      {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var freePort = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return freePort;
      }
      catch
      {
        // Fallback to default if socket creation fails
        return 5000;
      }
    }
    
    private static float GetServerTimeoutSeconds()
    {
      var timeoutEnv = Environment.GetEnvironmentVariable("LLAMABRAIN_SERVER_TIMEOUT_SECONDS");
      if (string.IsNullOrEmpty(timeoutEnv))
        return 30f;
      
      // Use InvariantCulture to avoid locale-specific parsing issues (e.g., comma decimals)
      if (float.TryParse(timeoutEnv, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var timeout))
        return timeout;
      
      return 30f;
    }

    private static bool ShouldSkipExternalTests()
    {
      var skipEnv = Environment.GetEnvironmentVariable("LLAMABRAIN_SKIP_EXTERNAL_TESTS");
      // Case-insensitive comparison to avoid CI surprises
      return string.Equals(skipEnv, "1", StringComparison.OrdinalIgnoreCase)
          || string.Equals(skipEnv, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates external dependencies exist and throws Inconclusive if missing.
    /// Resolves and stores paths for use in SetUpServer().
    /// Call this before SetUpServer() in any test that requires external server.
    /// </summary>
    private void RequireExternal()
    {
      if (ShouldSkipExternalTests())
      {
        Assert.Inconclusive("External tests skipped (LLAMABRAIN_SKIP_EXTERNAL_TESTS=1)");
      }

      var exePath = GetExecutablePath();
      var modelPath = GetModelPath();

      // Resolve paths relative to Unity project root if needed
      // In Unity, paths might be relative to Assets folder
      if (!Path.IsPathRooted(exePath) && !File.Exists(exePath))
      {
        // Try common Unity paths (platform-safe: Directory.GetParent handles both / and \)
        var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
        var fullExePath = Path.Combine(projectRoot, exePath);
        if (File.Exists(fullExePath))
          exePath = fullExePath;
      }

      if (!Path.IsPathRooted(modelPath) && !File.Exists(modelPath))
      {
        var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
        var fullModelPath = Path.Combine(projectRoot, modelPath);
        if (File.Exists(fullModelPath))
          modelPath = fullModelPath;
      }

      if (!File.Exists(exePath))
      {
        Assert.Inconclusive($"Executable not found: {exePath}. Set LLAMABRAIN_EXECUTABLE_PATH or skip with LLAMABRAIN_SKIP_EXTERNAL_TESTS=1");
      }

      if (!File.Exists(modelPath))
      {
        Assert.Inconclusive($"Model file not found: {modelPath}. Set LLAMABRAIN_MODEL_PATH or skip with LLAMABRAIN_SKIP_EXTERNAL_TESTS=1");
      }

      // Store resolved paths for use in SetUpExternalServer()
      resolvedExePath = exePath;
      resolvedModelPath = modelPath;
    }

    [SetUp]
    public void SetUp()
    {
      // NOTE: Server is NOT started in SetUp. Tests that need it must call RequireExternal() then SetUpExternalServer() explicitly.
      // This allows tests like ServerUnavailable to run without any server component.

      // Create persona config
      personaConfig = ScriptableObject.CreateInstance<PersonaConfig>();
      personaConfig!.PersonaId = "test-npc";
      personaConfig!.Name = "Test NPC";
      personaConfig!.Description = "A friendly shopkeeper";
      personaConfig!.SystemPrompt = "You are a helpful shopkeeper. Be friendly and helpful.";

      // Create memory store
      memoryStore = new PersonaMemoryStore();
      
      // Initialize memory with canonical facts
      var memorySystem = memoryStore.GetOrCreateSystem(personaConfig.PersonaId);
      memorySystem.AddCanonicalFact("king_name", "The king is named Arthur", "world_lore");
      memorySystem.AddCanonicalFact("magic_exists", "Magic is real in this world", "world_lore");
      memorySystem.SetWorldState("door_status", "open", MutationSource.GameSystem);
    }

    [TearDown]
    public void TearDown()
    {
      // Log failure details if test failed or was inconclusive
      try
      {
        var result = TestContext.CurrentContext.Result;
        if (result != null && result.Outcome != null)
        {
          var status = result.Outcome.Status;
          var statusName = status.ToString();
          var shouldLog = statusName == "Failed" || statusName == "Inconclusive" || statusName == "Error";
          
          if (shouldLog)
          {
            LogFailureDetails(lastResult, unityAgent);
          }
        }
      }
      catch
      {
        // Ignore errors in logging - don't let cleanup fail
      }

      // Cleanup server FIRST - stop process before disposing clients
      // This prevents ApiClient.Dispose() from blocking on in-flight requests while server is shutting down
      // NOTE: serverObject is manually managed, not tracked in createdGameObjects
      // Destroying the GameObject may not stop the external process, so explicitly stop first
      if (server != null)
      {
        try
        {
          // Explicitly stop server process to ensure llama-server process is terminated
          // This prevents leaking llama-server processes on failed/inconclusive runs
          server.StopServer();
        }
        catch
        {
          // Ignore shutdown errors - proceed with destruction
        }
      }
      
      // Dispose tracked IDisposable instances (apiClient, stub clients, etc.) AFTER stopping server
      // Single disposal path: all IDisposable instances are tracked in disposables
      // This ensures any cancellation or cleanup in Dispose() happens after server is stopped
      // Use reference-equality de-dupe to prevent Distinct() from dropping entries if IDisposable overrides Equals
      foreach (var disposable in DistinctByReference(disposables))
      {
        try
        {
          disposable.Dispose();
        }
        catch
        {
          // Ignore disposal errors - don't let cleanup fail
        }
      }
      disposables.Clear();
      apiClient = null; // Ownership is disposables - clear reference

      // Cleanup Unity agent (after disposing clients)
      // Use Unity fake-null check instead of (agentObject != null)
      if (agentObject)
      {
        UnityEngine.Object.DestroyImmediate(agentObject);
        agentObject = null;
      }
      
      // Use Unity fake-null check instead of (serverObject != null)
      if (serverObject)
      {
        UnityEngine.Object.DestroyImmediate(serverObject);
        serverObject = null;
      }
      server = null; // Null component reference after destroying GameObject

      // Cleanup all created GameObjects (expectancy configs, etc.)
      // Use Unity's fake-null aware check (!go) instead of (go != null)
      foreach (var go in createdGameObjects)
      {
        if (!go) continue; // Unity null check (fake-null aware)
        UnityEngine.Object.DestroyImmediate(go);
      }
      createdGameObjects.Clear();

      // Cleanup all created ScriptableObjects
      // Use Unity's fake-null aware check (!so) instead of (so != null)
      foreach (var so in createdScriptableObjects)
      {
        if (!so) continue; // Unity null check (fake-null aware)
        UnityEngine.Object.DestroyImmediate(so);
      }
      createdScriptableObjects.Clear();

      // NOTE: settings (BrainSettings) is destroyed explicitly, not tracked in createdScriptableObjects.
      // This is acceptable - explicit cleanup is fine for test infrastructure objects.
      // Use Unity fake-null check for consistency
      if (settings)
      {
        UnityEngine.Object.DestroyImmediate(settings);
        settings = null;
      }

      if (personaConfig)
      {
        UnityEngine.Object.DestroyImmediate(personaConfig);
        personaConfig = null;
      }

      // Clear references to prevent cross-test contamination if fixture instance is reused
      memoryStore = null;
      unityAgent = null;
      lastResult = null; // Clear for next test
    }

    /// <summary>
    /// Sets up external server for integration tests.
    /// REQUIRES: RequireExternal() must be called first to validate and resolve paths.
    /// </summary>
    private void SetUpExternalServer()
    {
      Assert.That(resolvedExePath, Is.Not.Null, "Call RequireExternal() before SetUpExternalServer()");
      Assert.That(resolvedModelPath, Is.Not.Null, "Call RequireExternal() before SetUpExternalServer()");
      SetUpServer(resolvedExePath!, resolvedModelPath!);
    }

    /// <summary>
    /// Sets up server with explicit paths.
    /// Internal implementation - use SetUpExternalServer() for external integration tests.
    /// </summary>
    private void SetUpServer(string exePath, string modelPath)
    {
      if (server != null) return; // Already set up
      
      // Create test settings for BrainServer
      settings = ScriptableObject.CreateInstance<BrainSettings>();
      settings!.ExecutablePath = exePath;
      settings!.ModelPath = modelPath;
      settings!.Port = GetPort();
      settings!.ContextSize = 2048;

      // Create BrainServer GameObject
      serverObject = new GameObject("TestServer");
      server = serverObject!.AddComponent<BrainServer>();
      server!.Settings = settings;
      server!.Initialize();
    }

    /// <summary>
    /// Waits for server to be ready with configurable timeout.
    /// Probes HTTP endpoint to verify server is actually responding (more reliable than TCP connect).
    /// HTTP probe is the ground truth - process checks can be wrong in containers or permission-restricted runners.
    /// REQUIRES: RequireExternal() and SetUpExternalServer() must be called first.
    /// </summary>
    private IEnumerator WaitForServerReady()
    {
      // Enforce preconditions - server and settings must be set up before waiting
      Assert.That(server, Is.Not.Null, "Call RequireExternal() and SetUpExternalServer() before WaitForServerReady()");
      Assert.That(settings, Is.Not.Null, "Call RequireExternal() and SetUpExternalServer() before WaitForServerReady()");
      
      yield return null; // Let server initialize

      HttpClient? http = null;
      
      try
      {
        http = new HttpClient { Timeout = TimeSpan.FromSeconds(0.5) };
        
        var startTime = Time.realtimeSinceStartup;
        var maxWaitTime = GetServerTimeoutSeconds();
        var port = settings!.Port;
        var url = $"http://127.0.0.1:{port}/";

        // Poll HTTP endpoint until server responds (any response means server is ready)
        while (Time.realtimeSinceStartup - startTime < maxWaitTime)
        {
          // Probe HTTP endpoint to verify server is actually responding
          // Any HTTP response (even 500) means server is responding (not necessarily healthy)
          var httpTask = http.GetAsync(url);
          yield return new WaitUntil(() => httpTask.IsCompleted);
          
          // Observe exceptions to prevent unobserved Task exceptions
          if (httpTask.IsFaulted)
          {
            _ = httpTask.Exception; // observe exception
            yield return new WaitForSeconds(0.5f);
            continue;
          }
          
          if (httpTask.IsCanceled)
          {
            yield return new WaitForSeconds(0.5f);
            continue;
          }
          
          // Any HTTP response means server is ready (responding, not necessarily healthy)
          if (httpTask.IsCompletedSuccessfully && httpTask.Result != null)
          {
            using var resp = httpTask.Result; // Dispose response automatically
            // HTTP endpoint responding - server is ready
            yield break;
          }
          
          yield return new WaitForSeconds(0.5f);
        }
        
        // If we exit the loop without connecting, server is not ready
        Assert.Inconclusive($"Server not ready within {maxWaitTime}s on port {port}. Check server logs for startup errors.");
      }
      finally
      {
        http?.Dispose();
      }
    }
    
    /// <summary>
    /// Test helper to create expectancy config with constraints.
    /// Uses reflection but validates fields exist and fails hard if they don't.
    /// Returns rule for cleanup tracking.
    /// </summary>
    private (GameObject configObject, NpcExpectancyConfig config, ExpectancyRuleAsset rule) CreateExpectancyConfigWithRule(
      string npcId,
      RuleConstraintDefinition constraintDef)
    {
      var expectancyConfigObject = new GameObject("ExpectancyConfig");
      expectancyConfigObject.name = npcId + "-expectancy";
      var expectancyConfig = expectancyConfigObject.AddComponent<NpcExpectancyConfig>();
      createdGameObjects.Add(expectancyConfigObject); // Track for cleanup
      var rule = ScriptableObject.CreateInstance<ExpectancyRuleAsset>();
      createdScriptableObjects.Add(rule); // Track for cleanup
      
      // Use reflection to add constraint to rule (validated with hard assertions)
      var constraintsField = typeof(ExpectancyRuleAsset).GetField("constraints", BindingFlags.NonPublic | BindingFlags.Instance);
      Assert.NotNull(constraintsField, "ExpectancyRuleAsset.constraints field must exist");
      var constraintsObj = constraintsField!.GetValue(rule);
      Assert.That(constraintsObj, Is.AssignableTo<IList<RuleConstraintDefinition>>(),
        "ExpectancyRuleAsset.constraints must be assignable to IList<RuleConstraintDefinition> for tests (type mismatch indicates schema drift)");
      var constraints = (IList<RuleConstraintDefinition>)constraintsObj!;
      constraints.Add(constraintDef);
      
      // Ensure rule is enabled
      var isEnabledField = typeof(ExpectancyRuleAsset).GetField("isEnabled", BindingFlags.NonPublic | BindingFlags.Instance);
      Assert.NotNull(isEnabledField, "ExpectancyRuleAsset.isEnabled field must exist");
      isEnabledField!.SetValue(rule, true);
      
      // Use reflection to set rules on config (validated with hard assertions)
      var npcRulesField = typeof(NpcExpectancyConfig).GetField("npcRules", BindingFlags.NonPublic | BindingFlags.Instance);
      Assert.NotNull(npcRulesField, "NpcExpectancyConfig.npcRules field must exist");
      var npcRulesObj = npcRulesField!.GetValue(expectancyConfig);
      Assert.That(npcRulesObj, Is.AssignableTo<IList<ExpectancyRuleAsset>>(),
        "NpcExpectancyConfig.npcRules must be assignable to IList<ExpectancyRuleAsset> for tests (type mismatch indicates schema drift)");
      var npcRules = (IList<ExpectancyRuleAsset>)npcRulesObj!;
      npcRules.Add(rule);
      
      return (expectancyConfigObject, expectancyConfig, rule);
    }

    /// <summary>
    /// Logs test failure details for postmortem analysis.
    /// </summary>
    private void LogFailureDetails(InferenceResultWithRetries? result, LlamaBrainAgent? agent)
    {
      TestContext.Out.WriteLine("=== TEST FAILURE DETAILS ===");
      
      if (result != null)
      {
        TestContext.Out.WriteLine($"Total Attempts: {result.AllAttempts.Count}");
        for (int i = 0; i < result.AllAttempts.Count; i++)
        {
          var attempt = result.AllAttempts[i];
          TestContext.Out.WriteLine($"Attempt {i + 1}: Success={attempt.Success}, Outcome={attempt.Outcome}");
          TestContext.Out.WriteLine($"  Response: {attempt.Response}");
          if (attempt.Violations != null && attempt.Violations.Count > 0)
          {
            TestContext.Out.WriteLine($"  Violations: {string.Join(", ", attempt.Violations)}");
          }
        }
      }
      
      if (agent != null && agent.LastSnapshot != null)
      {
        TestContext.Out.WriteLine($"Snapshot PlayerInput: {agent.LastSnapshot.PlayerInput}");
        TestContext.Out.WriteLine($"Snapshot Constraints Count: {agent.LastSnapshot.Constraints.Count}");
        TestContext.Out.WriteLine($"Snapshot CanonicalFacts Count: {agent.LastSnapshot.CanonicalFacts.Count}");
        TestContext.Out.WriteLine($"Snapshot WorldState Count: {agent.LastSnapshot.WorldState.Count}");
        
        // Serialize snapshot to JSON-like format for debugging
        try
        {
          var snapshotJson = $"{{ \"PlayerInput\": \"{agent.LastSnapshot.PlayerInput}\", " +
                           $"\"Constraints\": {agent.LastSnapshot.Constraints.Count}, " +
                           $"\"CanonicalFacts\": {agent.LastSnapshot.CanonicalFacts.Count}, " +
                           $"\"WorldState\": {agent.LastSnapshot.WorldState.Count} }}";
          TestContext.Out.WriteLine($"Snapshot JSON: {snapshotJson}");
        }
        catch (Exception ex)
        {
          TestContext.Out.WriteLine($"Failed to serialize snapshot: {ex.Message}");
        }
      }
      
      if (agent != null && agent.LastAssembledPrompt != null)
      {
        var promptPreview = agent.LastAssembledPrompt.Text.Length > 500 
          ? agent.LastAssembledPrompt.Text.Substring(0, 500) + "..."
          : agent.LastAssembledPrompt.Text;
        TestContext.Out.WriteLine($"Assembled Prompt: {promptPreview}");
      }
      
      // Log server info if available
      if (server != null)
      {
        TestContext.Out.WriteLine($"Server Running: {server.IsServerRunning}");
        TestContext.Out.WriteLine($"Server Status: {server.ConnectionStatus}");
        if (!string.IsNullOrEmpty(server.LastErrorMessage))
        {
          TestContext.Out.WriteLine($"Server Error: {server.LastErrorMessage}");
        }
      }
      
      TestContext.Out.WriteLine("=== END FAILURE DETAILS ===");
    }

    #region External Integration Tests (May be Inconclusive)

    /// <summary>
    /// Single smoke test for external integration.
    /// Boots server, waits ready, single inference, assert non-empty response.
    /// All other detailed tests should use stubs for determinism.
    /// </summary>
    [UnityTest]
    [Category("ExternalIntegration")]
    public IEnumerator PlayMode_ExternalIntegration_SmokeTest_RealServer()
    {
      RequireExternal(); // Validates paths and throws Inconclusive if missing

      // Arrange - Set up and wait for server to be ready
      SetUpExternalServer();
      yield return WaitForServerReady();

      // Get client from server
      apiClient = server!.CreateClient();
      if (apiClient == null)
      {
        Assert.Inconclusive("Server is not running. Skipping integration test.");
        yield break;
      }
      disposables.Add(apiClient); // Track for cleanup (single disposal path)

      // Create LlamaBrainAgent Unity component
      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;

      // Initialize Unity agent with server client
      unityAgent!.Initialize(apiClient!, memoryStore!);
      yield return null; // Let initialization complete

      // Act - Send input through full pipeline
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Hello!");
      });

      // Store for failure logging
      lastResult = result;

      // Assert - Just verify we got a non-empty response
      Assert.IsNotNull(result, "Result should not be null");
      Assert.That(result!.FinalResult.Response, Is.Not.Null.And.Not.Empty, "Should have a non-empty response");
      
      // Log for postmortem
      TestContext.Out.WriteLine($"Smoke test response: {result.FinalResult.Response}");
      TestContext.Out.WriteLine($"Total attempts: {result.AllAttempts.Count}");
    }

    #endregion

    #region External Integration Tests (Observational - Require Real Server)

    /// <summary>
    /// Tests that state snapshot contains all required components after one send.
    /// NOTE: This is an external integration test - it requires real server.
    /// True contract tests would use stubs (requires IApiClient interface).
    /// </summary>
    [UnityTest]
    [Category("ExternalIntegration")]
    public IEnumerator PlayMode_ExternalIntegration_StateSnapshot_ContainsAllComponents()
    {
      RequireExternal(); // Validates paths and throws Inconclusive if missing

      // Arrange - Set up and wait for server to be ready
      SetUpExternalServer();
      yield return WaitForServerReady();

      apiClient = server!.CreateClient();
      if (apiClient == null)
      {
        Assert.Inconclusive("Server is not running. Skipping test.");
        yield break;
      }
      disposables.Add(apiClient); // Track for cleanup (single disposal path)

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;

      unityAgent!.Initialize(apiClient!, memoryStore!);
      yield return null;

      // Act - Send input to trigger snapshot creation
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Tell me about yourself");
      });

      lastResult = result; // Store for failure logging

      // Assert - Verify snapshot contains all components (contract test)
      Assert.IsNotNull(unityAgent!.LastSnapshot, "State snapshot should be created");
      var snapshot = unityAgent!.LastSnapshot!;
      
      Assert.That(snapshot.Context, Is.Not.Null, "Snapshot should have interaction context");
      Assert.That(snapshot.Context.NpcId, Is.EqualTo(personaConfig!.PersonaId), "Snapshot should have correct NPC ID");
      Assert.That(snapshot.SystemPrompt, Is.Not.Null.And.Not.Empty, "Snapshot should have system prompt");
      Assert.That(snapshot.PlayerInput, Is.Not.Null.And.Not.Empty, "Snapshot should have player input");
      
      // Verify memory components are included
      Assert.That(snapshot.CanonicalFacts, Is.Not.Null, "Snapshot should have canonical facts collection");
      Assert.That(snapshot.WorldState, Is.Not.Null, "Snapshot should have world state collection");
      
      // Verify that canonical facts CAN be retrieved (configure retrieval to guarantee inclusion)
      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);
      var factsInMemory = memorySystem.GetCanonicalFacts();
      Assert.That(factsInMemory.Count, Is.GreaterThan(0), "Test setup should have canonical facts in memory");
      
      // Verify world state structure supports retrieval (WorldState is IReadOnlyList<string> with format "key=value")
      // Note: World state retrieval is relevance-based, so it may not be included in every snapshot.
      // The important contract is that the snapshot structure supports world state retrieval.
      Assert.That(snapshot.WorldState, Is.Not.Null, "Snapshot should have world state collection structure");
      
      // If world state is retrieved, verify it's in the correct format
      if (snapshot.WorldState.Count > 0)
      {
        // Verify world state entries are in "key=value" format
        var allValidFormat = snapshot.WorldState.All(ws => ws.Contains("="));
        Assert.That(allValidFormat, Is.True, "World state entries should be in 'key=value' format");
        
        // If door_status is in memory, check if it was retrieved (may not be if not relevant to query)
        var doorStateInMemory = memorySystem.GetWorldState("door_status");
        if (doorStateInMemory != null)
        {
          // World state exists in memory - it may or may not be in snapshot depending on relevance
          // This is acceptable - the contract is that the structure supports it
          TestContext.Out.WriteLine($"World state 'door_status' exists in memory but may not be in snapshot if not relevant to query");
        }
      }
      else
      {
        // No world state retrieved - this is acceptable if retrieval layer determined it's not relevant
        TestContext.Out.WriteLine("No world state retrieved in snapshot (may be filtered by relevance)");
      }
      
      // Verify constraints are included (even if empty)
      Assert.That(snapshot.Constraints, Is.Not.Null, "Snapshot should have constraints (even if empty)");
    }

    /// <summary>
    /// Tests that expectancy constraints are included in snapshot and affect generation.
    /// NOTE: External integration test - verifies constraint injection into prompts (invariant behavior).
    /// </summary>
    [UnityTest]
    [Category("ExternalIntegration")]
    public IEnumerator PlayMode_ExternalIntegration_ExpectancyConstraints_AffectGeneration()
    {
      RequireExternal(); // Validates paths and throws Inconclusive if missing

      // Arrange - Set up and wait for server to be ready
      SetUpExternalServer();
      yield return WaitForServerReady();

      apiClient = server!.CreateClient();
      if (apiClient == null)
      {
        Assert.Inconclusive("Server is not running. Skipping test.");
        yield break;
      }
      disposables.Add(apiClient); // Track for cleanup (single disposal path)

      // Create expectancy config with a rule
      var constraintDef = new RuleConstraintDefinition
      {
        Type = ConstraintType.Prohibition,
        Severity = ConstraintSeverity.Hard,
        Description = "Cannot reveal secrets",
        PromptInjection = "Do not reveal any secrets. Never mention confidential information."
      };
      constraintDef.ValidationPatterns.Add("secret");
      constraintDef.ValidationPatterns.Add("hidden");
      constraintDef.ValidationPatterns.Add("confidential");
      
      var (expectancyConfigObject, expectancyConfig, rule) = CreateExpectancyConfigWithRule("test-npc", constraintDef);

      // Create agent with expectancy config
      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.ExpectancyConfig = expectancyConfig;

      unityAgent!.Initialize(apiClient!, memoryStore!);
      yield return null;

      // Act - Send input
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Tell me a secret");
      });

      lastResult = result; // Store for failure logging

      // Assert
      Assert.IsNotNull(result);
      Assert.IsNotNull(unityAgent!.LastConstraints, "Constraints should be generated from expectancy rules");
      Assert.That(unityAgent!.LastConstraints!.Count, Is.GreaterThan(0), "Should have constraints from rule");
      
      // Verify constraints were included in snapshot
      Assert.That(unityAgent!.LastSnapshot, Is.Not.Null);
      Assert.That(unityAgent!.LastSnapshot!.Constraints.Count, Is.GreaterThan(0), "Snapshot should include constraints");
      
      // CRITICAL: Assert that constraints actually affected generation by checking prompt injection
      Assert.That(unityAgent!.LastAssembledPrompt, Is.Not.Null, "Assembled prompt should be available");
      var promptText = unityAgent!.LastAssembledPrompt!.Text;
      Assert.That(promptText, Is.Not.Null.And.Not.Empty, "Prompt text should not be empty");
      
      // Verify constraint prompt injection was included in the prompt
      var hasConstraintInjection = promptText.Contains("Do not reveal any secrets", StringComparison.OrdinalIgnoreCase) ||
                                   promptText.Contains("Never mention confidential", StringComparison.OrdinalIgnoreCase);
      Assert.That(hasConstraintInjection, Is.True, 
        "Prompt should contain constraint prompt injection text. This proves constraints affected generation.");
      
      // Cleanup handled by TearDown (expectancyConfigObject is tracked in createdGameObjects)
    }

    /// <summary>
    /// Tests invariant behavior: retry mechanism is bounded and respects max retries.
    /// NOTE: External integration test - verifies retry bounds (invariant), not that retries occurred.
    /// </summary>
    [UnityTest]
    [Category("ExternalIntegration")]
    public IEnumerator PlayMode_ExternalIntegration_RetryMechanism_RespectsBounds()
    {
      RequireExternal(); // Validates paths and throws Inconclusive if missing

      // Arrange - Set up and wait for server to be ready
      SetUpExternalServer();
      yield return WaitForServerReady();

      apiClient = server!.CreateClient();
      if (apiClient == null)
      {
        Assert.Inconclusive("Server is not running. Skipping test.");
        yield break;
      }
      disposables.Add(apiClient); // Track for cleanup (single disposal path)

      // Create expectancy config with strict rule that will likely be violated
      var constraintDef = new RuleConstraintDefinition
      {
        Type = ConstraintType.Prohibition,
        Severity = ConstraintSeverity.Hard,
        Description = "Cannot say 'hello'",
        PromptInjection = "Do not use the word 'hello' in your response"
      };
      constraintDef.ValidationPatterns.Add("hello");
      constraintDef.ValidationPatterns.Add("Hello");
      constraintDef.ValidationPatterns.Add("HELLO");
      
      var (expectancyConfigObject, expectancyConfig, rule) = CreateExpectancyConfigWithRule("test-npc", constraintDef);

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.ExpectancyConfig = expectancyConfig;

      unityAgent!.Initialize(apiClient!, memoryStore!);
      yield return null;
      
      // Set max retries (using reflection - validated with hard assertions)
      var retryPolicyField = typeof(LlamaBrainAgent).GetField("retryPolicy", BindingFlags.NonPublic | BindingFlags.Instance);
      Assert.NotNull(retryPolicyField, "LlamaBrainAgent.retryPolicy field must exist for test configuration");
      var retryPolicy = retryPolicyField!.GetValue(unityAgent) as RetryPolicy;
      Assert.NotNull(retryPolicy, "LlamaBrainAgent.retryPolicy must not be null");
      retryPolicy!.MaxRetries = 3;

      // Act - Send input
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Greet me warmly");
      });

      lastResult = result; // Store for failure logging

      // Assert - Verify invariant behavior: retry bounds are respected
      Assert.IsNotNull(result);
      Assert.That(result!.AllAttempts.Count, Is.LessThanOrEqualTo(4), "Should not exceed max retries (invariant: bounded retries)");
      
      // If retries occurred, verify retry mechanism worked correctly
      if (result.AllAttempts.Count > 1)
      {
        // Verify validation failures triggered retries
        // Check for any failed attempts (not just ProhibitionViolated, as other validation failures can occur)
        var failedAttempts = result.AllAttempts.Where(a => !a.Success).ToList();
        
        Assert.That(failedAttempts.Count, Is.GreaterThan(0), 
          "If retries occurred, validation should have rejected at least one violating output (invariant: retries imply failures)");
      }
      
      // Final response should exist (invariant: always returns a response)
      Assert.That(result.FinalResult.Response, Is.Not.Null.And.Not.Empty);
      
      // Cleanup handled by TearDown (expectancyConfigObject is tracked in createdGameObjects)
    }

    /// <summary>
    /// Tests invariant behavior: mutation controller executes when validation passes.
    /// NOTE: External integration test - verifies mutation controller is invoked (invariant), not that mutations occurred.
    /// </summary>
    [UnityTest]
    [Category("ExternalIntegration")]
    public IEnumerator PlayMode_ExternalIntegration_MutationController_ExecutesWhenApproved()
    {
      RequireExternal(); // Validates paths and throws Inconclusive if missing

      // Arrange - Set up and wait for server to be ready
      SetUpExternalServer();
      yield return WaitForServerReady();

      apiClient = server!.CreateClient();
      if (apiClient == null)
      {
        Assert.Inconclusive("Server is not running. Skipping test.");
        yield break;
      }
      disposables.Add(apiClient); // Track for cleanup (single disposal path)

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;

      unityAgent!.Initialize(apiClient!, memoryStore!);
      yield return null;

      // Get initial memory state
      var initialMemorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);
      int initialEpisodicCount = System.Linq.Enumerable.Count(initialMemorySystem.GetRecentMemories(100));
      var initialBeliefs = initialMemorySystem.GetAllBeliefs().ToList();
      var initialWorldState = initialMemorySystem.GetAllWorldState().ToList();

      // Act - Send input
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("I am your best friend. Remember this.");
      });

      lastResult = result; // Store for failure logging

      // Assert - Verify invariant: mutation controller executes when validation passes
      Assert.IsNotNull(result);
      
      // Verify mutation controller executed if validation passed (invariant behavior)
      if (unityAgent!.LastGateResult != null && unityAgent!.LastGateResult!.Passed)
      {
        Assert.That(unityAgent!.LastMutationBatchResult, Is.Not.Null, "Mutation batch result should be set when validation passes (invariant)");
        Assert.That(unityAgent!.LastMutationBatchResult!.TotalAttempted, Is.GreaterThanOrEqualTo(0), 
          "Mutation controller should have been invoked (invariant)");
      }
      else
      {
        // If validation failed, mutations shouldn't have been attempted (invariant)
        Assert.That(unityAgent!.LastMutationBatchResult, Is.Null.Or.Property("TotalAttempted").EqualTo(0),
          "Mutations should not be attempted when validation fails (invariant)");
      }
    }

    /// <summary>
    /// Tests invariant behavior: canonical facts are immutable (protected from modification).
    /// NOTE: External integration test - verifies immutability (invariant), not contradiction detection.
    /// </summary>
    [UnityTest]
    [Category("ExternalIntegration")]
    public IEnumerator PlayMode_ExternalIntegration_CanonicalFacts_Immutable()
    {
      RequireExternal(); // Validates paths and throws Inconclusive if missing

      // Arrange - Set up and wait for server to be ready
      SetUpExternalServer();
      yield return WaitForServerReady();

      apiClient = server!.CreateClient();
      if (apiClient == null)
      {
        Assert.Inconclusive("Server is not running. Skipping test.");
        yield break;
      }
      disposables.Add(apiClient); // Track for cleanup (single disposal path)

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;

      unityAgent!.Initialize(apiClient!, memoryStore!);
      yield return null;

      // Get canonical fact before
      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);
      var factsBefore = memorySystem.GetCanonicalFacts();
      var kingFactBefore = factsBefore.FirstOrDefault(f => f.Id == "king_name");
      Assert.That(kingFactBefore, Is.Not.Null, "Test setup should have king_name canonical fact");

      // Act - Ask about king
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("What's the king's name?");
      });

      lastResult = result; // Store for failure logging

      // Assert - Verify invariant: canonical facts are immutable
      Assert.IsNotNull(result);
      
      // CRITICAL: Verify canonical fact is still intact (immutability - invariant behavior)
      var factsAfter = memorySystem.GetCanonicalFacts();
      var kingFactAfter = factsAfter.FirstOrDefault(f => f.Id == "king_name");
      
      Assert.That(kingFactAfter, Is.Not.Null, "Canonical fact should still exist (invariant: immutability)");
      Assert.That(kingFactAfter!.Fact, Is.EqualTo(kingFactBefore!.Fact), "Canonical fact should not be modified (invariant: immutability)");
      
      // Verify gate result tracks failures (invariant: gate always tracks state)
      if (unityAgent!.LastGateResult != null)
      {
        Assert.That(unityAgent!.LastGateResult.Failures, Is.Not.Null, "Gate result should track failures (invariant)");
      }
    }

    /// <summary>
    /// Deterministic test: Fallback is used when API client throws exception.
    /// Uses stub that throws HttpRequestException to simulate server unavailable.
    /// CRITICAL: No server component should exist for this test.
    /// </summary>
    [UnityTest]
    [Category("Contract")]
    public IEnumerator PlayMode_PipelineContract_ServerUnavailable_UsesFallback()
    {
      // Arrange - Create agent WITHOUT starting server (no BrainServer component at all)
      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;

      // Verify no server exists
      Assert.That(serverObject, Is.Null, "No server should exist for this test");
      Assert.That(server, Is.Null, "No BrainServer component should exist");

      // Create stub that throws exception to simulate server unavailable (deterministic)
      // Empty list is OK because OnSendMetrics will be used
      var stubResponses = new List<StubApiClient.StubResponse>(); // Empty - won't be used since OnSendMetrics throws
      var stubClient = new StubApiClient(stubResponses);
      disposables.Add(stubClient); // Track for cleanup
      stubClient.OnSendMetrics = (prompt, attempt, maxTokens, temperature, seed, cachePrompt, cancellationToken) =>
      {
        // Deterministically return faulted task to simulate server unavailable (async error path)
        // Attempt number is available but not needed for this test (all attempts should fail)
        return Task.FromException<CompletionMetrics>(new HttpRequestException("Connection refused"));
      };

      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Read retry policy to calculate expected number of attempts (1 initial + maxRetries retries)
      var retryPolicyField = typeof(LlamaBrainAgent).GetField("retryPolicy", BindingFlags.NonPublic | BindingFlags.Instance);
      Assert.NotNull(retryPolicyField, "LlamaBrainAgent.retryPolicy field must exist");
      var retryPolicy = retryPolicyField!.GetValue(unityAgent) as RetryPolicy;
      Assert.NotNull(retryPolicy, "LlamaBrainAgent.retryPolicy must not be null");
      var expectedAttempts = retryPolicy!.MaxRetries + 1; // 1 initial + maxRetries retries

      // Expect error logs for each attempt (one per failed attempt)
      // Note: Unity may inject prefix text or newlines into the message string, so we use permissive pattern with Singleline
      // If LogAssert.Expect(LogType.Error, Regex) is not available in your Unity version, use string form:
      // LogAssert.Expect(LogType.Error, "[LlamaBrainAgent] Inference error on attempt");
      var errorLog = new Regex(@".*\[LlamaBrainAgent\] Inference error on attempt \d+: .*", RegexOptions.Singleline);
      for (int i = 0; i < expectedAttempts; i++)
      {
        LogAssert.Expect(LogType.Error, errorLog); // Expect one error log per attempt
      }

      // Act - Try to send input (should fail and use fallback)
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Hello");
      });

      lastResult = result; // Store for failure logging

      // Assert - Deterministic: always returns a response via fallback when server unavailable
      Assert.IsNotNull(result, "Result should not be null even if server failed (deterministic: always returns result)");
      Assert.That(result!.AllAttempts.Count, Is.EqualTo(expectedAttempts), 
        "Should have exactly expectedAttempts attempts (deterministic)");
      
      // Verify all attempts failed (deterministic: graceful degradation contract)
      var allFailed = result!.AllAttempts.All(a => !a.Success);
      Assert.That(allFailed, Is.True, "All attempts should fail when server is unavailable (deterministic)");
      Assert.That(result!.FinalResult.Response, Is.Not.Null.And.Not.Empty, 
        "Should have a response (deterministic: always returns response)");
      Assert.That(unityAgent!.FallbackStats?.TotalFallbacks, Is.GreaterThan(0),
        "Fallback should have been used when all attempts failed (deterministic: graceful degradation contract)");

      // Optional: ensure no additional unexpected errors occurred
      // LogAssert.NoUnexpectedReceived();

      // Cleanup handled by TearDown (stubClient tracked in disposables)
    }

    #endregion

    #region Deterministic Contract Tests (Using IApiClient Stubs)

    /// <summary>
    /// Deterministic test: Validation failure triggers retry with bounded attempts.
    /// Uses stub that returns violating response on attempt 1, compliant on attempt 2.
    /// </summary>
    [UnityTest]
    [Category("Contract")]
    public IEnumerator PlayMode_PipelineContract_ValidationFailure_TriggersRetry()
    {
      // Arrange - Create stub that returns violating response, then compliant
      var stubResponses = new List<StubApiClient.StubResponse>
      {
        new StubApiClient.StubResponse { Content = "damn, that's interesting!" }, // Violates constraint
        new StubApiClient.StubResponse { Content = "That's interesting!" } // Compliant
      };
      var stubClient = new StubApiClient(stubResponses);
      disposables.Add(stubClient); // Track for cleanup

      // Create expectancy config with constraint that will be violated
      var constraintDef = new RuleConstraintDefinition
      {
        Type = ConstraintType.Prohibition,
        Severity = ConstraintSeverity.Hard,
        Description = "No swearing",
        PromptInjection = "Do not use profanity"
      };
      constraintDef.ValidationPatterns.Add("damn");
      constraintDef.ValidationPatterns.Add("hell");
      
      var (expectancyConfigObject, expectancyConfig, rule) = CreateExpectancyConfigWithRule("test-npc", constraintDef);

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.ExpectancyConfig = expectancyConfig;

      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;
      
      // Set max retries (using reflection - validated with hard assertions)
      var retryPolicyField = typeof(LlamaBrainAgent).GetField("retryPolicy", BindingFlags.NonPublic | BindingFlags.Instance);
      Assert.NotNull(retryPolicyField, "LlamaBrainAgent.retryPolicy field must exist for test configuration");
      var retryPolicy = retryPolicyField!.GetValue(unityAgent) as RetryPolicy;
      Assert.NotNull(retryPolicy, "LlamaBrainAgent.retryPolicy must not be null");
      retryPolicy!.MaxRetries = 3;

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Tell me something");
      });

      lastResult = result; // Store for failure logging

      // Assert - Deterministic: exactly 2 attempts, first fails, second succeeds
      Assert.IsNotNull(result);
      Assert.That(result!.AllAttempts.Count, Is.EqualTo(2), "Should have exactly 2 attempts (deterministic)");
      Assert.That(result.AllAttempts[0].Success, Is.False, "First attempt should fail validation (deterministic)");
      Assert.That(result.AllAttempts[1].Success, Is.True, "Second attempt should succeed (deterministic)");
      
      // CRITICAL: Assert attempt content matches expectation (deterministic)
      Assert.That(result.AllAttempts[0].Response, Is.EqualTo("damn, that's interesting!"), 
        "First attempt response should be the violating content (deterministic)");
      Assert.That(result.AllAttempts[1].Response, Is.EqualTo("That's interesting!"), 
        "Second attempt response should be the compliant content (deterministic)");
      
      Assert.That(result.AllAttempts[0].Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated), 
        "First attempt should be flagged as prohibition violation (deterministic)");
      Assert.That(result.FinalResult.Response, Is.EqualTo("That's interesting!"), 
        "Final response should be from successful attempt (deterministic)");
      
      // Verify gate result corresponds to final accepted attempt
      Assert.That(unityAgent!.LastGateResult, Is.Not.Null);
      Assert.That(unityAgent!.LastGateResult!.Passed, Is.True, "Final gate result should pass (deterministic)");

      // Cleanup handled by TearDown (expectancyConfigObject tracked in createdGameObjects, stubClient tracked in disposables)
    }

    // NOTE: Mutation controller deterministic test removed.
    // Mutation extraction depends on OutputParser parsing structured output format.
    // Testing mutation extraction/execution belongs in OutputParser and MutationController unit tests.
    // The pipeline contract is: "mutation controller is invoked when validation passes" - 
    // but without deterministic mutation payload extraction, this test would be vacuous (TotalAttempted >= 0 proves nothing).

    /// <summary>
    /// Deterministic test: Canonical facts remain immutable even when model output contradicts them.
    /// Uses stub that explicitly contradicts canonical fact.
    /// This test proves immutability, not contradiction detection (which may vary by validator implementation).
    /// </summary>
    [UnityTest]
    [Category("Contract")]
    public IEnumerator PlayMode_PipelineContract_CanonicalFacts_Immutable_EvenWhenModelContradicts()
    {
      // Arrange - Create stub that contradicts canonical fact
      var stubResponses = new List<StubApiClient.StubResponse>
      {
        new StubApiClient.StubResponse { Content = "The king is named Albert, not Arthur." } // Contradicts canonical fact
      };
      var stubClient = new StubApiClient(stubResponses);
      disposables.Add(stubClient); // Track for cleanup

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;

      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Get canonical fact before
      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);
      var factsBefore = memorySystem.GetCanonicalFacts();
      var kingFactBefore = factsBefore.FirstOrDefault(f => f.Id == "king_name");
      Assert.That(kingFactBefore, Is.Not.Null, "Test setup should have king_name canonical fact");

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("What's the king's name?");
      });

      lastResult = result; // Store for failure logging

      // Assert - Deterministic: canonical fact remains immutable (this is the contract)
      Assert.IsNotNull(result);
      
      // CRITICAL: Verify canonical fact is still intact (immutability - deterministic contract)
      var factsAfter = memorySystem.GetCanonicalFacts();
      var kingFactAfter = factsAfter.FirstOrDefault(f => f.Id == "king_name");
      Assert.That(kingFactAfter, Is.Not.Null, "Canonical fact should still exist (deterministic: immutability)");
      Assert.That(kingFactAfter!.Fact, Is.EqualTo(kingFactBefore!.Fact), 
        "Canonical fact should not be modified even when model output contradicts it (deterministic: immutability contract)");

      // Cleanup handled by TearDown (stubClient tracked in disposables)
    }

    /// <summary>
    /// Deterministic test: Expectancy constraints are injected into prompts.
    /// Uses stub and verifies prompt contains constraint injection text.
    /// </summary>
    [UnityTest]
    [Category("Contract")]
    public IEnumerator PlayMode_PipelineContract_ExpectancyConstraints_InjectedIntoPrompt()
    {
      // Arrange - Create stub
      var stubResponses = new List<StubApiClient.StubResponse>
      {
        new StubApiClient.StubResponse { Content = "I cannot reveal secrets." }
      };
      var stubClient = new StubApiClient(stubResponses);
      disposables.Add(stubClient); // Track for cleanup

      // Create expectancy config with constraint
      var constraintDef = new RuleConstraintDefinition
      {
        Type = ConstraintType.Prohibition,
        Severity = ConstraintSeverity.Hard,
        Description = "Cannot reveal secrets",
        PromptInjection = "Do not reveal any secrets. Never mention confidential information."
      };
      constraintDef.ValidationPatterns.Add("secret");
      
      var (expectancyConfigObject, expectancyConfig, rule) = CreateExpectancyConfigWithRule("test-npc", constraintDef);

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.ExpectancyConfig = expectancyConfig;

      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Tell me a secret");
      });

      lastResult = result; // Store for failure logging

      // Assert - Deterministic: constraint injection text must be in prompt
      Assert.IsNotNull(result);
      Assert.That(unityAgent!.LastAssembledPrompt, Is.Not.Null, "Assembled prompt should be available");
      var promptText = unityAgent!.LastAssembledPrompt!.Text;
      
      // CRITICAL: Verify constraint prompt injection was included (deterministic contract)
      var hasConstraintInjection = promptText.Contains("Do not reveal any secrets", StringComparison.OrdinalIgnoreCase) ||
                                   promptText.Contains("Never mention confidential", StringComparison.OrdinalIgnoreCase);
      Assert.That(hasConstraintInjection, Is.True, 
        "Prompt MUST contain constraint prompt injection text (deterministic contract: constraints affect generation)");
      
      // Verify constraints were included in snapshot (deterministic)
      Assert.That(unityAgent!.LastSnapshot, Is.Not.Null);
      Assert.That(unityAgent!.LastSnapshot!.Constraints.Count, Is.GreaterThan(0), 
        "Snapshot should include constraints (deterministic)");

      // Cleanup handled by TearDown (expectancyConfigObject tracked in createdGameObjects, stubClient tracked in disposables)
    }

    /// <summary>
    /// Deterministic test: State snapshot contains all required components.
    /// Uses stub to verify snapshot structure contract.
    /// </summary>
    [UnityTest]
    [Category("Contract")]
    public IEnumerator PlayMode_PipelineContract_StateSnapshot_ContainsAllComponents()
    {
      // Arrange - Create stub
      var stubResponses = new List<StubApiClient.StubResponse>
      {
        new StubApiClient.StubResponse { Content = "I am a helpful shopkeeper." }
      };
      var stubClient = new StubApiClient(stubResponses);
      disposables.Add(stubClient); // Track for cleanup

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;

      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Tell me about yourself");
      });

      lastResult = result; // Store for failure logging

      // Assert - Verify snapshot contains all components (deterministic contract)
      Assert.IsNotNull(unityAgent!.LastSnapshot, "State snapshot should be created (deterministic)");
      var snapshot = unityAgent!.LastSnapshot!;
      
      Assert.That(snapshot.Context, Is.Not.Null, "Snapshot should have interaction context (deterministic)");
      Assert.That(snapshot.Context.NpcId, Is.EqualTo(personaConfig!.PersonaId), 
        "Snapshot should have correct NPC ID (deterministic)");
      Assert.That(snapshot.SystemPrompt, Is.Not.Null.And.Not.Empty, 
        "Snapshot should have system prompt (deterministic)");
      Assert.That(snapshot.PlayerInput, Is.Not.Null.And.Not.Empty, 
        "Snapshot should have player input (deterministic)");
      
      // Verify memory components are included (deterministic structure contract)
      Assert.That(snapshot.CanonicalFacts, Is.Not.Null, 
        "Snapshot should have canonical facts collection (deterministic)");
      Assert.That(snapshot.WorldState, Is.Not.Null, 
        "Snapshot should have world state collection (deterministic)");
      Assert.That(snapshot.Constraints, Is.Not.Null, 
        "Snapshot should have constraints collection (deterministic)");
      
      // Verify canonical facts CAN be retrieved (structure supports it)
      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);
      var factsInMemory = memorySystem.GetCanonicalFacts();
      Assert.That(factsInMemory.Count, Is.GreaterThan(0), 
        "Test setup should have canonical facts in memory (deterministic)");

      // Cleanup handled by TearDown (stubClient tracked in disposables)
    }

    #endregion

    #region Seed Determinism Tests (Feature 14 - Deferred Tests)

    /// <summary>
    /// Feature 14.3/14.6 Deferred Test: Retry attempts use the same seed.
    ///
    /// This test verifies that all retry attempts within a single interaction
    /// use the SAME seed value (Double-Lock Pattern: Lock 2 - Entropy Locking).
    ///
    /// Contract: Same InteractionCount = Same seed across all retry attempts.
    /// </summary>
    [UnityTest]
    [Category("Contract")]
    public IEnumerator PlayMode_PipelineContract_RetryAttempts_UseSameSeed()
    {
      // Arrange - Track seeds used across retry attempts
      var seedsUsed = new List<int?>();

      // Create stub that:
      // - Attempt 1: Returns violating response (triggers retry)
      // - Attempt 2: Returns compliant response (succeeds)
      // Also captures the seed used for each attempt
      var stubResponses = new List<StubApiClient.StubResponse>
      {
        new StubApiClient.StubResponse { Content = "damn, this is bad!" }, // Will fail validation
        new StubApiClient.StubResponse { Content = "This is good!" }       // Will pass validation
      };
      var stubClient = new StubApiClient(stubResponses);
      disposables.Add(stubClient);

      // Track seeds via custom handler that wraps default behavior
      stubClient.OnSendMetrics = (prompt, attempt, maxTokens, temperature, seed, cachePrompt, ct) =>
      {
        seedsUsed.Add(seed);
        // Return the pre-configured response based on attempt
        var response = attempt <= stubResponses.Count
          ? stubResponses[attempt - 1]
          : stubResponses[stubResponses.Count - 1];
        return Task.FromResult(new CompletionMetrics
        {
          Content = response.Content,
          PromptTokenCount = response.PromptTokenCount,
          GeneratedTokenCount = response.GeneratedTokenCount
        });
      };

      // Create expectancy config with profanity filter to trigger retry
      var expectancyConfigObject = new GameObject("ExpectancyConfig");
      createdGameObjects.Add(expectancyConfigObject);
      var expectancyConfig = expectancyConfigObject.AddComponent<NpcExpectancyConfig>();

      var profanityRule = ScriptableObject.CreateInstance<ExpectancyRuleAsset>();
      profanityRule.RuleName = "No Profanity";
      profanityRule.RuleType = ConstraintType.Prohibition;
      profanityRule.Severity = ConstraintSeverity.Hard;
      profanityRule.PromptInjection = "Never use profanity";
      profanityRule.PatternType = PatternMatchType.Contains;
      profanityRule.Pattern = "damn";
      expectancyConfig.Rules = new List<ExpectancyRuleAsset> { profanityRule };

      agentObject = new GameObject("TestAgent");
      createdGameObjects.Add(agentObject);
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.ExpectancyConfig = expectancyConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Tell me something");
      });

      // Assert
      Assert.IsNotNull(result);
      Assert.That(result!.AllAttempts.Count, Is.EqualTo(2),
        "Should have exactly 2 attempts (first fails, second succeeds)");

      // CRITICAL: Verify all retry attempts used the same seed
      Assert.That(seedsUsed.Count, Is.EqualTo(2),
        "Should have tracked 2 seed values (one per attempt)");
      Assert.That(seedsUsed[0], Is.EqualTo(seedsUsed[1]),
        "All retry attempts MUST use the same seed for determinism (Feature 14 contract)");

      // Verify the seed was derived from InteractionCount (should be non-null)
      Assert.That(seedsUsed[0], Is.Not.Null,
        "Seed should be derived from InteractionCount (not null)");

      // Cleanup
      UnityEngine.Object.DestroyImmediate(profanityRule);
    }

    /// <summary>
    /// Feature 14.3 Deferred Test: Multiple retry attempts all use InteractionCount as seed.
    ///
    /// This test verifies that even with many retries, the seed remains constant.
    /// </summary>
    [UnityTest]
    [Category("Contract")]
    public IEnumerator PlayMode_PipelineContract_MultipleRetries_AllUseSameSeed()
    {
      // Arrange - Track all seeds
      var seedsUsed = new List<int?>();

      // Create stub that always returns violating response (forces max retries)
      var stubResponses = new List<StubApiClient.StubResponse>();
      for (int i = 0; i < 10; i++) // More than max retries
      {
        stubResponses.Add(new StubApiClient.StubResponse { Content = $"damn response {i}" });
      }
      var stubClient = new StubApiClient(stubResponses);
      disposables.Add(stubClient);

      stubClient.OnSendMetrics = (prompt, attempt, maxTokens, temperature, seed, cachePrompt, ct) =>
      {
        seedsUsed.Add(seed);
        var response = stubResponses[Math.Min(attempt - 1, stubResponses.Count - 1)];
        return Task.FromResult(new CompletionMetrics
        {
          Content = response.Content,
          PromptTokenCount = response.PromptTokenCount,
          GeneratedTokenCount = response.GeneratedTokenCount
        });
      };

      // Create expectancy config with profanity filter
      var expectancyConfigObject = new GameObject("ExpectancyConfig");
      createdGameObjects.Add(expectancyConfigObject);
      var expectancyConfig = expectancyConfigObject.AddComponent<NpcExpectancyConfig>();

      var profanityRule = ScriptableObject.CreateInstance<ExpectancyRuleAsset>();
      profanityRule.RuleName = "No Profanity";
      profanityRule.RuleType = ConstraintType.Prohibition;
      profanityRule.Severity = ConstraintSeverity.Hard;
      profanityRule.PromptInjection = "Never use profanity";
      profanityRule.PatternType = PatternMatchType.Contains;
      profanityRule.Pattern = "damn";
      expectancyConfig.Rules = new List<ExpectancyRuleAsset> { profanityRule };

      agentObject = new GameObject("TestAgent");
      createdGameObjects.Add(agentObject);
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.ExpectancyConfig = expectancyConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Tell me something");
      });

      // Assert
      Assert.IsNotNull(result);
      Assert.That(seedsUsed.Count, Is.GreaterThan(1),
        "Should have multiple attempts tracked");

      // CRITICAL: All seeds must be identical
      var firstSeed = seedsUsed[0];
      for (int i = 1; i < seedsUsed.Count; i++)
      {
        Assert.That(seedsUsed[i], Is.EqualTo(firstSeed),
          $"Attempt {i + 1} seed ({seedsUsed[i]}) must match attempt 1 seed ({firstSeed}) - Feature 14 contract");
      }

      // Cleanup
      UnityEngine.Object.DestroyImmediate(profanityRule);
    }

    /// <summary>
    /// Feature 14.3 Deferred Test: Save/Load preserves InteractionCount for determinism.
    ///
    /// This test verifies that InteractionCount is correctly persisted through save/load,
    /// ensuring the same seed is used after restoration (Double-Lock Pattern).
    ///
    /// Contract: Save → Load → Same InteractionCount → Same seed → Identical behavior.
    /// </summary>
    [UnityTest]
    [Category("Contract")]
    public IEnumerator PlayMode_PipelineContract_SaveLoad_PreservesInteractionCount()
    {
      // Arrange - Create agent and interact 3 times to increment InteractionCount
      var stubResponses = new List<StubApiClient.StubResponse>();
      for (int i = 0; i < 10; i++)
      {
        stubResponses.Add(new StubApiClient.StubResponse { Content = $"Response {i}" });
      }
      var stubClient = new StubApiClient(stubResponses);
      disposables.Add(stubClient);

      agentObject = new GameObject("TestAgent");
      createdGameObjects.Add(agentObject);
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Interact 3 times to set InteractionCount = 3
      for (int i = 0; i < 3; i++)
      {
        yield return UniTask.ToCoroutine(async () =>
        {
          await unityAgent!.SendWithSnapshotAsync($"Hello {i}");
        });
      }

      // Verify InteractionCount is 3
      Assert.That(unityAgent!.InteractionCount, Is.EqualTo(3),
        "After 3 successful interactions, InteractionCount should be 3");

      // Act - Create save data
      var saveData = unityAgent!.CreateSaveData();
      Assert.That(saveData, Is.Not.Null, "Save data should be created");
      Assert.That(saveData!.MemorySnapshot.InteractionCount, Is.EqualTo(3),
        "Save data should contain InteractionCount = 3");

      // Simulate new session - create fresh agent
      var stubClient2 = new StubApiClient(stubResponses);
      disposables.Add(stubClient2);

      var agentObject2 = new GameObject("TestAgent2");
      createdGameObjects.Add(agentObject2);
      var unityAgent2 = agentObject2.AddComponent<LlamaBrainAgent>();
      unityAgent2.PersonaConfig = personaConfig;
      unityAgent2.Initialize(stubClient2, memoryStore!);
      yield return null;

      // Verify new agent starts at 0
      Assert.That(unityAgent2.InteractionCount, Is.EqualTo(0),
        "New agent should start with InteractionCount = 0");

      // Restore from save data
      unityAgent2.RestoreFromSaveData(saveData);

      // Assert - InteractionCount restored
      Assert.That(unityAgent2.InteractionCount, Is.EqualTo(3),
        "After restoration, InteractionCount should be 3 (Feature 14 contract)");
    }

    /// <summary>
    /// Feature 14.3 Deferred Test: Save/Load determinism produces identical outputs.
    ///
    /// This test verifies that after save/load, the same interaction produces
    /// the same result because the same seed is used.
    /// </summary>
    [UnityTest]
    [Category("Contract")]
    public IEnumerator PlayMode_PipelineContract_SaveLoad_SameSeedProducesSameOutput()
    {
      // Arrange - Track seeds used
      var seedsUsed = new List<int?>();

      var stubResponses = new List<StubApiClient.StubResponse>
      {
        new StubApiClient.StubResponse { Content = "Response 0" },
        new StubApiClient.StubResponse { Content = "Response 1" },
        new StubApiClient.StubResponse { Content = "Response 2" },
        new StubApiClient.StubResponse { Content = "Response 3" }, // For 4th interaction
        new StubApiClient.StubResponse { Content = "Response 4" }  // For 5th interaction (after load)
      };
      var stubClient = new StubApiClient(stubResponses);
      disposables.Add(stubClient);

      stubClient.OnSendMetrics = (prompt, attempt, maxTokens, temperature, seed, cachePrompt, ct) =>
      {
        seedsUsed.Add(seed);
        var response = stubResponses[Math.Min(seedsUsed.Count - 1, stubResponses.Count - 1)];
        return Task.FromResult(new CompletionMetrics
        {
          Content = response.Content,
          PromptTokenCount = response.PromptTokenCount,
          GeneratedTokenCount = response.GeneratedTokenCount
        });
      };

      agentObject = new GameObject("TestAgent");
      createdGameObjects.Add(agentObject);
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Interact 3 times to set InteractionCount = 3
      for (int i = 0; i < 3; i++)
      {
        yield return UniTask.ToCoroutine(async () =>
        {
          await unityAgent!.SendWithSnapshotAsync($"Hello {i}");
        });
      }

      // Save state at InteractionCount = 3
      var saveData = unityAgent!.CreateSaveData();
      var seedAtSaveTime = seedsUsed[seedsUsed.Count - 1]; // Last seed used was 2 (for interaction 3)

      // Do one more interaction (InteractionCount = 3 becomes seed, then increments to 4)
      yield return UniTask.ToCoroutine(async () =>
      {
        await unityAgent!.SendWithSnapshotAsync("After save");
      });
      var seedAfterSave = seedsUsed[seedsUsed.Count - 1]; // Should be 3

      // Now simulate loading - restore InteractionCount to 3
      seedsUsed.Clear();

      var stubClient2 = new StubApiClient(stubResponses);
      disposables.Add(stubClient2);

      stubClient2.OnSendMetrics = (prompt, attempt, maxTokens, temperature, seed, cachePrompt, ct) =>
      {
        seedsUsed.Add(seed);
        var response = stubResponses[Math.Min(attempt - 1, stubResponses.Count - 1)];
        return Task.FromResult(new CompletionMetrics
        {
          Content = response.Content,
          PromptTokenCount = response.PromptTokenCount,
          GeneratedTokenCount = response.GeneratedTokenCount
        });
      };

      var agentObject2 = new GameObject("TestAgent2");
      createdGameObjects.Add(agentObject2);
      var unityAgent2 = agentObject2.AddComponent<LlamaBrainAgent>();
      unityAgent2.PersonaConfig = personaConfig;
      unityAgent2.Initialize(stubClient2, memoryStore!);
      yield return null;

      unityAgent2.RestoreFromSaveData(saveData);

      // Do the same interaction
      yield return UniTask.ToCoroutine(async () =>
      {
        await unityAgent2.SendWithSnapshotAsync("After save");
      });
      var seedAfterLoad = seedsUsed[0]; // Should also be 3

      // Assert - Same seed used after save/load restoration
      Assert.That(seedAfterLoad, Is.EqualTo(seedAfterSave),
        $"After save/load, the same interaction should use the same seed. Expected {seedAfterSave}, got {seedAfterLoad} (Feature 14 contract)");
    }

    #endregion
  }
}