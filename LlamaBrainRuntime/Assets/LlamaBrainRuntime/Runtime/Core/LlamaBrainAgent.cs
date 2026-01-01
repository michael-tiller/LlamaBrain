#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using LlamaBrain.Core;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Metrics;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;
using LlamaBrain.Runtime.Core.Expectancy;
using LlamaBrain.Runtime.Core.Inference;
using LlamaBrain.Runtime.Core.Validation;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Validation;
using System.Linq;
using System.Diagnostics;

namespace LlamaBrain.Runtime.Core
{
  /// <summary>
  /// A LlamaBrain agent that lives in Unity that can be used to interact with a LlamaBrain server.
  /// Implements IAgentMetrics for metrics collection.
  /// </summary>
  public class LlamaBrainAgent : MonoBehaviour, IAgentMetrics
  {
    /// <summary>
    /// The persona configuration for this agent (Unity ScriptableObject).
    /// </summary>
    [Header("Persona Configuration")]
    public PersonaConfig? PersonaConfig;

    /// <summary>
    /// The runtime persona profile (converted from config).
    /// </summary>
    [Header("Runtime Profile")]
    [SerializeField] private PersonaProfile? runtimeProfile;

    /// <summary>
    /// The prompt composer settings for this agent.
    /// </summary>
    [Header("Prompt Settings")]
    public PromptComposerSettings? PromptSettings;

    /// <summary>
    /// The memory category manager for this agent.
    /// </summary>
    [Header("Memory Settings")]
    public MemoryCategoryManager? MemoryCategoryManager;

    /// <summary>
    /// Optional expectancy configuration for constraint-based behavior.
    /// If set, rules will be evaluated and constraints injected into prompts.
    /// </summary>
    [Header("Expectancy (Determinism Layer)")]
    [Tooltip("Optional NPC-specific expectancy rules. If set, constraints will be evaluated and injected into prompts.")]
    public NpcExpectancyConfig? ExpectancyConfig;

    /// <summary>
    /// Optional fallback configuration for author-controlled fallback responses.
    /// If not set, uses default fallback configuration.
    /// </summary>
    [Header("Fallback System")]
    [Tooltip("Optional fallback configuration. Leave empty for defaults.")]
    [SerializeField] private FallbackConfigAsset? fallbackConfig;

    /// <summary>
    /// Optional NPC-specific validation rule set.
    /// These rules apply to this NPC in addition to global rules from ValidationPipeline.
    /// </summary>
    [Header("Validation Rules")]
    [Tooltip("Optional NPC-specific validation rules. These are added to global rules from ValidationPipeline.")]
    [SerializeField] private LlamaBrain.Runtime.Core.Validation.ValidationRuleSetAsset? npcValidationRules;

    /// <summary>
    /// The last evaluated constraint set (for debugging/metrics).
    /// </summary>
    public ConstraintSet? LastConstraints { get; private set; }

    /// <summary>
    /// The last state snapshot used for inference (for debugging/metrics).
    /// </summary>
    public StateSnapshot? LastSnapshot { get; private set; }

    /// <summary>
    /// The last inference result (for debugging/metrics).
    /// </summary>
    public InferenceResultWithRetries? LastInferenceResult { get; private set; }

    /// <summary>
    /// The memory provider for the persona.
    /// </summary>
    private PersonaMemoryStore? memoryProvider;
    /// <summary>
    /// The client for the LlamaBrain server.
    /// </summary>
    private IApiClient? client;
    /// <summary>
    /// The dialogue session for this agent.
    /// </summary>
    private DialogueSession? dialogueSession;

    /// <summary>
    /// Separate storage for conversation history (not mixed with memory)
    /// </summary>
    private List<DialogueEntry> conversationHistory = new List<DialogueEntry>();

    /// <summary>
    /// Maximum number of conversation history entries to keep
    /// </summary>
    [Header("Conversation Settings")]
    [SerializeField] private int maxConversationHistoryEntries = 20;

    /// <summary>
    /// Whether to store conversation history automatically
    /// </summary>
    [SerializeField] private bool storeConversationHistory = true;

    /// <summary>
    /// Maximum tokens for NPC responses (default: 24 for normal replies)
    /// Target ranges: barks 8-12, normal replies 20-24, dialogue 30-48
    /// Note: At 170 tps (GPU), 24 tokens ≈ 140ms decode time.
    /// </summary>
    [Header("Performance Settings")]
    [Tooltip("Maximum tokens for NPC responses. Normal replies: 20-24 tokens. At 170 tps: 24 tokens ≈ 140ms")]
    [SerializeField] private int maxResponseTokens = 24;

    /// <summary>
    /// Enable automatic memory decay (episodic memories fade over time)
    /// </summary>
    [Header("Memory Decay Settings")]
    [Tooltip("Enable automatic memory decay. Memories will fade over time.")]
    [SerializeField] private bool enableAutoDecay = false;

    /// <summary>
    /// Optional prompt assembler settings (ScriptableObject).
    /// If not set, uses default settings.
    /// </summary>
    [Header("Prompt Assembly")]
    [Tooltip("Optional prompt assembler settings. Leave empty for defaults.")]
    [SerializeField] private PromptAssemblerSettings? promptAssemblerSettings;

    /// <summary>
    /// The response validator for constraint checking.
    /// </summary>
    private ResponseValidator? responseValidator;

    /// <summary>
    /// The prompt assembler for building inference prompts.
    /// </summary>
    private PromptAssembler? promptAssembler;

    /// <summary>
    /// The output parser for structured output extraction.
    /// </summary>
    private OutputParser? outputParser;

    /// <summary>
    /// The validation gate for constraint and canonical fact checking.
    /// </summary>
    private ValidationGate? validationGate;

    /// <summary>
    /// The last assembled prompt (for debugging/metrics).
    /// </summary>
    public AssembledPrompt? LastAssembledPrompt { get; private set; }

    /// <summary>
    /// The last parsed output (for debugging/metrics).
    /// </summary>
    public ParsedOutput? LastParsedOutput { get; private set; }

    /// <summary>
    /// The last gate result (for debugging/metrics).
    /// </summary>
    public GateResult? LastGateResult { get; private set; }

    /// <summary>
    /// The last mutation batch result (for debugging/metrics).
    /// </summary>
    public MutationBatchResult? LastMutationBatchResult { get; private set; }

    /// <summary>
    /// The retry policy for inference.
    /// </summary>
    private RetryPolicy? retryPolicy;

    /// <summary>
    /// The fallback system for when inference fails after all retries.
    /// </summary>
    private AuthorControlledFallback? fallbackSystem;

    /// <summary>
    /// The mutation controller for executing validated memory mutations.
    /// </summary>
    private MemoryMutationController? mutationController;

    /// <summary>
    /// Interval in seconds between memory decay applications
    /// </summary>
    [Tooltip("Interval in seconds between memory decay applications (default: 300 = 5 minutes)")]
    [SerializeField] private float decayIntervalSeconds = 300f;

    private float lastDecayTime = 0f;

    /// <summary>
    /// Server configuration info for detailed logging
    /// </summary>
    private string modelPath = "";
    private string modelQuantization = "Unknown";
    private int gpuLayers = 0;
    private int batchSize = 0;
    private int uBatchSize = 0;
    private int parallelSlots = 0;
    private int actualGpuLayersOffloaded = -1; // -1 = unknown, will be parsed from server logs

    /// <summary>
    /// The current runtime profile
    /// </summary>
    public PersonaProfile? RuntimeProfile
    {
      get => runtimeProfile;
      set => runtimeProfile = value;
    }


    /// <summary>
    /// The memories of the persona.
    /// </summary>
    public string Memories
    {
      get
      {
        if (!Application.isPlaying || runtimeProfile == null || memoryProvider == null)
          return string.Empty;

        var memorySystem = memoryProvider.GetOrCreateSystem(runtimeProfile.PersonaId);
        return string.Join("\n", memorySystem.GetAllMemoriesForPrompt(maxEpisodic: 20));
      }
    }
    
    /// <summary>
    /// The last completion metrics from the most recent request (for metrics collection).
    /// </summary>
    public CompletionMetrics? LastMetrics { get; private set; }
    
    /// <summary>
    /// Gets the maximum response tokens setting (for metrics collection).
    /// </summary>
    public int MaxResponseTokens => maxResponseTokens;

    /// <summary>
    /// Initializes the LlamaBrain agent.
    /// </summary>
    /// <param name="client">The API client to use for LLM communication</param>
    /// <param name="memoryProvider">The memory store provider for persona memories</param>
    public void Initialize(IApiClient client, PersonaMemoryStore memoryProvider)
    {
      initializationAttempted = true;
      UnityEngine.Debug.Log("[LlamaBrainAgent] Starting initialization...");

      if (client == null)
      {
        UnityEngine.Debug.LogError("[LlamaBrainAgent] Initialize failed: ApiClient is null");
        return;
      }

      if (memoryProvider == null)
      {
        UnityEngine.Debug.LogError("[LlamaBrainAgent] Initialize failed: PersonaMemoryStore is null");
        return;
      }

      try
      {
        this.client = client;
        this.memoryProvider = memoryProvider;

        // Subscribe to performance metrics events
        client.OnMetricsAvailable += OnPerformanceMetricsReceived;

        // Auto-configure with server settings if BrainServer is available
        TryAutoConfigureServerSettings();

        UnityEngine.Debug.Log("[LlamaBrainAgent] Client and memory provider set successfully");

        // Convert config to runtime profile if available
        if (PersonaConfig != null && runtimeProfile == null)
        {
          runtimeProfile = PersonaConfig.ToProfile();
          UnityEngine.Debug.Log($"[LlamaBrainAgent] Converted PersonaConfig to profile: {runtimeProfile?.Name ?? "Unknown"}");
        }

        // Initialize dialogue session with persona ID
        var personaId = runtimeProfile?.PersonaId ?? runtimeProfile?.Name ?? "Unknown";
        dialogueSession = new DialogueSession(personaId, memoryProvider);
        UnityEngine.Debug.Log($"[LlamaBrainAgent] Dialogue session initialized with persona ID: {personaId}");

        // Auto-detect ExpectancyConfig if not set
        if (ExpectancyConfig == null)
        {
          ExpectancyConfig = GetComponentInParent<NpcExpectancyConfig>();
          if (ExpectancyConfig != null)
          {
            UnityEngine.Debug.Log($"[LlamaBrainAgent] Auto-detected NpcExpectancyConfig on {ExpectancyConfig.gameObject.name}");
          }
        }

        // Initialize response validator and retry policy
        responseValidator = new ResponseValidator();
        retryPolicy = RetryPolicy.Default;
        UnityEngine.Debug.Log($"[LlamaBrainAgent] Response validator and retry policy initialized (max retries: {retryPolicy.MaxRetries})");

        // Initialize prompt assembler
        var assemblerConfig = promptAssemblerSettings != null
          ? promptAssemblerSettings.ToConfig()
          : PromptAssemblerConfig.Default;
        promptAssembler = new PromptAssembler(assemblerConfig);
        UnityEngine.Debug.Log($"[LlamaBrainAgent] Prompt assembler initialized (max tokens: {assemblerConfig.MaxPromptTokens})");

        // Initialize output parser and validation gate
        outputParser = new OutputParser();
        validationGate = new ValidationGate();
        mutationController = new MemoryMutationController();
        mutationController.OnLog = msg => UnityEngine.Debug.Log(msg);
        UnityEngine.Debug.Log($"[LlamaBrainAgent] Output parser, validation gate, and mutation controller initialized");

        // Load validation rules (global + NPC-specific)
        LoadValidationRules();

        // Initialize fallback system
        var fallbackConfigInstance = fallbackConfig != null 
          ? fallbackConfig.ToFallbackConfig() 
          : new AuthorControlledFallback.FallbackConfig();
        fallbackSystem = new AuthorControlledFallback(fallbackConfigInstance);
        UnityEngine.Debug.Log($"[LlamaBrainAgent] Fallback system initialized{(fallbackConfig != null ? " with custom config" : " with default config")}");

        // Initialize canonical facts if PersonaConfig has them
        InitializeCanonicalFacts();

        UnityEngine.Debug.Log($"[LlamaBrainAgent] Initialization complete. IsInitialized: {IsInitialized}");
        UnityEngine.Debug.Log($"[LlamaBrainAgent] Final state - Client: {(client != null ? "Set" : "Null")}, MemoryProvider: {(memoryProvider != null ? "Set" : "Null")}, RuntimeProfile: {(runtimeProfile != null ? $"Set ({runtimeProfile.Name})" : "Null")}");
      }
      catch (System.Exception ex)
      {
        UnityEngine.Debug.LogError($"[LlamaBrainAgent] Initialize failed with exception: {ex.Message}\nStackTrace: {ex.StackTrace}");
      }
    }

    /// <summary>
    /// Converts the current PersonaConfig to a runtime profile
    /// </summary>
    public void ConvertConfigToProfile()
    {
      if (PersonaConfig != null)
      {
        runtimeProfile = PersonaConfig.ToProfile();
      }
    }

    /// <summary>
    /// Updates the PersonaConfig from the current runtime profile
    /// </summary>
    public void UpdateConfigFromProfile()
    {
      if (PersonaConfig != null && runtimeProfile != null)
      {
        PersonaConfig.FromProfile(runtimeProfile);
      }
    }

    /// <summary>
    /// Sends a player input to the LlamaBrain server using the player name from settings.
    /// </summary>
    /// <param name="input">The input from the player.</param>
    /// <returns>The response from the LlamaBrain server.</returns>
    public async UniTask<string> SendPlayerInputAsync(string input)
    {
      return await SendPlayerInputAsync(null, input);
    }

    /// <summary>
    /// The current interaction context (set when using SendPlayerInputWithContextAsync).
    /// Used by the expectancy system to evaluate context-aware rules.
    /// </summary>
    private InteractionContext? currentInteractionContext;

    /// <summary>
    /// Temporary trigger rules for the current interaction (set by triggers).
    /// Cleared after evaluation.
    /// </summary>
    private System.Collections.Generic.IEnumerable<LlamaBrain.Runtime.Core.Expectancy.ExpectancyRuleAsset>? currentTriggerRules;

    /// <summary>
    /// Temporary trigger fallbacks for the current interaction (set by triggers).
    /// Cleared after evaluation.
    /// </summary>
    private System.Collections.Generic.IReadOnlyList<string>? currentTriggerFallbacks;

    /// <summary>
    /// Sends a player input with explicit interaction context.
    /// This allows triggers to pass context for expectancy rule evaluation.
    /// </summary>
    /// <param name="input">The input from the player/trigger.</param>
    /// <param name="context">The interaction context with trigger/scene information.</param>
    /// <returns>The response from the LlamaBrain server.</returns>
    public async UniTask<string> SendPlayerInputWithContextAsync(string input, InteractionContext context)
    {
      return await SendPlayerInputWithContextAsync(input, context, null, null);
    }

    /// <summary>
    /// Sends a player input with explicit interaction context and trigger rules.
    /// This allows triggers to pass both context and trigger-specific rules for expectancy rule evaluation.
    /// </summary>
    /// <param name="input">The input from the player/trigger.</param>
    /// <param name="context">The interaction context with trigger/scene information.</param>
    /// <param name="triggerRules">Trigger-specific rules to evaluate.</param>
    /// <returns>The response from the LlamaBrain server.</returns>
    public async UniTask<string> SendPlayerInputWithContextAsync(string input, InteractionContext context, System.Collections.Generic.IEnumerable<LlamaBrain.Runtime.Core.Expectancy.ExpectancyRuleAsset>? triggerRules)
    {
      return await SendPlayerInputWithContextAsync(input, context, triggerRules, null);
    }

    /// <summary>
    /// Sends a player input with explicit interaction context, trigger rules, and trigger fallbacks.
    /// This allows triggers to pass context, trigger-specific rules, and trigger-specific fallbacks.
    /// </summary>
    /// <param name="input">The input from the player/trigger.</param>
    /// <param name="context">The interaction context with trigger/scene information.</param>
    /// <param name="triggerRules">Trigger-specific rules to evaluate.</param>
    /// <param name="triggerFallbacks">Trigger-specific fallback responses to use if inference fails.</param>
    /// <returns>The response from the LlamaBrain server.</returns>
    public async UniTask<string> SendPlayerInputWithContextAsync(string input, InteractionContext context, System.Collections.Generic.IEnumerable<LlamaBrain.Runtime.Core.Expectancy.ExpectancyRuleAsset>? triggerRules, System.Collections.Generic.IReadOnlyList<string>? triggerFallbacks)
    {
      currentInteractionContext = context;
      currentTriggerRules = triggerRules;
      currentTriggerFallbacks = triggerFallbacks;
      try
      {
        return await SendPlayerInputAsync(null, input);
      }
      finally
      {
        currentInteractionContext = null;
        currentTriggerRules = null;
        currentTriggerFallbacks = null;
      }
    }

    /// <summary>
    /// Sends a player input using the StateSnapshot-based inference pipeline with retry logic.
    /// This is the new inference method that captures deterministic state and validates responses.
    /// </summary>
    /// <param name="input">The input from the player.</param>
    /// <returns>The inference result including response and retry information.</returns>
    public async UniTask<InferenceResultWithRetries> SendWithSnapshotAsync(string input)
    {
      if (client == null)
      {
        throw new InvalidOperationException("Cannot send prompt: Client is null. Make sure the agent is properly initialized.");
      }

      // Ensure runtime profile exists
      if (runtimeProfile == null && PersonaConfig != null)
      {
        runtimeProfile = PersonaConfig.ToProfile();
      }
      if (runtimeProfile == null)
      {
        runtimeProfile = PersonaProfile.Create(gameObject.name + "-persona", gameObject.name);
        runtimeProfile.SystemPrompt = "You are a helpful NPC.";
      }

      // Initialize validator if needed
      if (responseValidator == null)
      {
        responseValidator = new ResponseValidator();
      }
      if (retryPolicy == null)
      {
        retryPolicy = RetryPolicy.Default;
      }
      if (promptAssembler == null)
      {
        var assemblerConfig = promptAssemblerSettings != null
          ? promptAssemblerSettings.ToConfig()
          : PromptAssemblerConfig.Default;
        promptAssembler = new PromptAssembler(assemblerConfig);
      }

      // Build initial snapshot
      var snapshot = BuildStateSnapshot(input);
      LastSnapshot = snapshot;

      UnityEngine.Debug.Log($"[LlamaBrainAgent] Built state snapshot: {snapshot}");

      // Execute inference with retry loop
      var attempts = new List<InferenceResult>();
      var totalStopwatch = Stopwatch.StartNew();

      while (attempts.Count < retryPolicy.MaxAttempts)
      {
        var attemptStopwatch = Stopwatch.StartNew();

        try
        {
          // Build prompt using PromptAssembler with ephemeral working memory
          string? retryFeedback = null;
          if (snapshot.AttemptNumber > 0 && LastInferenceResult != null && LastInferenceResult.AllAttempts.Count > 0)
          {
            var lastAttempt = LastInferenceResult.AllAttempts[LastInferenceResult.AllAttempts.Count - 1];
            retryFeedback = retryPolicy.GenerateRetryFeedback(lastAttempt);
          }

          var wmConfig = promptAssemblerSettings != null
            ? promptAssemblerSettings.ToWorkingMemoryConfig()
            : WorkingMemoryConfig.Default;

          var assembledPrompt = promptAssembler.AssembleFromSnapshot(
            snapshot,
            npcName: runtimeProfile?.Name,
            retryFeedback: retryFeedback,
            workingMemoryConfig: wmConfig
          );

          LastAssembledPrompt = assembledPrompt;
          var prompt = assembledPrompt.Text;

          UnityEngine.Debug.Log($"[LlamaBrainAgent] {assembledPrompt}");

          // Determine max tokens
          var isSingleLine = (runtimeProfile?.SystemPrompt ?? "").Contains("one line");
          var effectiveMaxTokens = isSingleLine ? System.Math.Min(maxResponseTokens, 24) : maxResponseTokens;

          // Send to LLM
          var metrics = await client.SendPromptWithMetricsAsync(prompt, maxTokens: effectiveMaxTokens);
          attemptStopwatch.Stop();

          // Check if truncated
          var wasTruncated = metrics.GeneratedTokenCount >= effectiveMaxTokens;

          // Step 1: Parse the output using OutputParser
          if (outputParser == null) outputParser = new OutputParser();
          var parsedOutput = outputParser.Parse(metrics.Content, wasTruncated);
          LastParsedOutput = parsedOutput;

          // Step 2: Validate through ValidationGate and/or ValidationPipeline
          GateResult gateResult;
          var validationContext = new ValidationContext
          {
            Constraints = snapshot.Constraints,
            MemorySystem = memoryProvider?.GetOrCreateSystem(runtimeProfile?.PersonaId ?? ""),
            Snapshot = snapshot
          };

          // Use ValidationPipeline if available (it includes global rules)
          var globalPipeline = LlamaBrain.Runtime.Core.Validation.ValidationPipeline.Instance;
          if (globalPipeline != null)
          {
            // ValidationPipeline handles global rules automatically and does its own parsing
            // We need to pass the raw output, not the parsed output
            gateResult = globalPipeline.ProcessWithSnapshot(metrics.Content, snapshot, validationContext.MemorySystem, wasTruncated);
            
            // Update LastParsedOutput from pipeline if available
            if (globalPipeline.LastParsedOutput != null)
            {
              LastParsedOutput = globalPipeline.LastParsedOutput;
            }

            // Also check NPC-specific and trigger-specific rules in our local gate
            // (ValidationPipeline handles global rules, but we need to check NPC/trigger rules separately)
            if (npcValidationRules != null || validationGate != null)
            {
              if (validationGate == null) validationGate = new ValidationGate();
              var localGateResult = validationGate.Validate(parsedOutput, validationContext);
              
              // Merge results: both must pass
              if (!localGateResult.Passed)
              {
                // Combine failures from both gates
                var combinedFailures = new List<ValidationFailure>(gateResult.Failures);
                combinedFailures.AddRange(localGateResult.Failures);
                gateResult = GateResult.Fail(combinedFailures.ToArray());
              }
            }
          }
          else
          {
            // Fallback to local ValidationGate (includes NPC-specific rules, but no global rules)
            if (validationGate == null) validationGate = new ValidationGate();
            gateResult = validationGate.Validate(parsedOutput, validationContext);
          }
          
          LastGateResult = gateResult;

          // Also run the constraint validator for backwards compatibility
          var constraintResult = responseValidator.Validate(parsedOutput.DialogueText, snapshot);

          InferenceResult result;
          if (gateResult.Passed && constraintResult.IsValid)
          {
            result = InferenceResult.Succeeded(
              response: parsedOutput.DialogueText,
              snapshot: snapshot,
              elapsedMs: attemptStopwatch.ElapsedMilliseconds,
              tokenUsage: new TokenUsage
              {
                PromptTokens = metrics.PromptTokenCount,
                CompletionTokens = metrics.GeneratedTokenCount
              }
            );
            attempts.Add(result);

            // Step 3: Execute approved mutations from the validation gate
            if (mutationController != null && validationContext.MemorySystem != null)
            {
              var mutationResult = mutationController.ExecuteMutations(
                gateResult,
                validationContext.MemorySystem,
                runtimeProfile?.PersonaId
              );
              LastMutationBatchResult = mutationResult;

              if (mutationResult.TotalAttempted > 0)
              {
                UnityEngine.Debug.Log($"[LlamaBrainAgent] Executed mutations: {mutationResult}");
              }
            }

            UnityEngine.Debug.Log($"[LlamaBrainAgent] Inference succeeded on attempt {attempts.Count}: '{parsedOutput.DialogueText}'");
            break;
          }
          else
          {
            // Combine failures from both validation paths
            var outcome = constraintResult.IsValid ? ValidationOutcome.InvalidFormat : constraintResult.Outcome;
            if (gateResult.HasCriticalFailure)
            {
              outcome = ValidationOutcome.ProhibitionViolated;
            }

            result = InferenceResult.FailedValidation(
              response: parsedOutput.Success ? parsedOutput.DialogueText : metrics.Content,
              outcome: outcome,
              violations: constraintResult.Violations,
              snapshot: snapshot,
              elapsedMs: attemptStopwatch.ElapsedMilliseconds,
              tokenUsage: new TokenUsage
              {
                PromptTokens = metrics.PromptTokenCount,
                CompletionTokens = metrics.GeneratedTokenCount
              }
            );
            attempts.Add(result);

            var failureCount = gateResult.Failures.Count + constraintResult.Violations.Count;
            UnityEngine.Debug.LogWarning($"[LlamaBrainAgent] Inference failed validation on attempt {attempts.Count}: {failureCount} total failures (gate: {gateResult.Failures.Count}, constraints: {constraintResult.Violations.Count})");

            // Check if we should use fallback (critical failure) or retry
            if (gateResult.HasCriticalFailure)
            {
              UnityEngine.Debug.LogWarning($"[LlamaBrainAgent] Critical failure detected - will use fallback");
              // Don't retry on critical failures
            }
            else if (snapshot.CanRetry)
            {
              // Generate additional constraints for retry
              var additionalConstraints = retryPolicy.GenerateRetryConstraints(constraintResult.Violations, snapshot.AttemptNumber);
              snapshot = snapshot.ForRetry(additionalConstraints);
              LastSnapshot = snapshot;

              retryPolicy.OnRetry?.Invoke(attempts.Count, result);

              if (retryPolicy.RetryDelayMs > 0)
              {
                await UniTask.Delay(retryPolicy.RetryDelayMs);
              }
            }
          }
        }
        catch (System.Exception ex)
        {
          attemptStopwatch.Stop();
          var errorResult = InferenceResult.FailedError(
            errorMessage: ex.Message,
            snapshot: snapshot,
            elapsedMs: attemptStopwatch.ElapsedMilliseconds
          );
          attempts.Add(errorResult);

          UnityEngine.Debug.LogError($"[LlamaBrainAgent] Inference error on attempt {attempts.Count}: {ex.Message}");

          // For errors, check if we can retry
          if (snapshot.CanRetry)
          {
            snapshot = snapshot.ForRetry();
            LastSnapshot = snapshot;
          }
        }
        finally
        {
          // Dispose working memory after each attempt (ephemeral - not persisted)
          if (LastAssembledPrompt?.WorkingMemory != null)
          {
            LastAssembledPrompt.WorkingMemory.Dispose();
          }
        }

        // Check total time limit
        if (totalStopwatch.ElapsedMilliseconds > retryPolicy.MaxTotalTimeMs)
        {
          UnityEngine.Debug.LogWarning($"[LlamaBrainAgent] Exceeded max total time ({retryPolicy.MaxTotalTimeMs}ms), stopping retries");
          break;
        }
      }

      totalStopwatch.Stop();

      var finalResult = new InferenceResultWithRetries(
        finalResult: attempts[attempts.Count - 1],
        allAttempts: attempts,
        totalElapsedMs: totalStopwatch.ElapsedMilliseconds
      );

      LastInferenceResult = finalResult;

      // If all retries failed, use fallback system
      if (!finalResult.Success)
      {
        // Initialize fallback if needed
        if (fallbackSystem == null)
        {
          var fallbackConfigInstance = fallbackConfig != null 
            ? fallbackConfig.ToFallbackConfig() 
            : new AuthorControlledFallback.FallbackConfig();
          fallbackSystem = new AuthorControlledFallback(fallbackConfigInstance);
        }

        // Get interaction context from snapshot
        var context = snapshot.Context;
        var failureReason = BuildFailureReason(finalResult);
        
        // Get fallback response (use trigger fallbacks if available)
        var fallbackResponse = fallbackSystem.GetFallbackResponse(context, failureReason, finalResult, currentTriggerFallbacks);
        
        UnityEngine.Debug.LogWarning($"[LlamaBrainAgent] All retries exhausted ({finalResult.AttemptCount} attempts). Using fallback: '{fallbackResponse}'");

        // Create a successful result with the fallback response
        var fallbackResult = InferenceResult.Succeeded(
          response: fallbackResponse,
          snapshot: snapshot,
          elapsedMs: totalStopwatch.ElapsedMilliseconds,
          tokenUsage: finalResult.GetTotalTokenUsage()
        );

        // Replace final result with fallback
        finalResult = new InferenceResultWithRetries(
          finalResult: fallbackResult,
          allAttempts: finalResult.AllAttempts,
          totalElapsedMs: totalStopwatch.ElapsedMilliseconds
        );

        LastInferenceResult = finalResult;
      }

      // Store dialogue history if successful (including fallback responses)
      if (finalResult.Success && storeConversationHistory && memoryProvider != null && runtimeProfile != null)
      {
        var memorySystem = memoryProvider.GetOrCreateSystem(runtimeProfile.PersonaId);
        memorySystem.AddDialogue("Player", input, significance: 0.5f, MutationSource.ValidatedOutput);
        memorySystem.AddDialogue("NPC", finalResult.FinalResult.Response, significance: 0.7f, MutationSource.ValidatedOutput);

        dialogueSession?.AppendPlayer(input);
        dialogueSession?.AppendNpc(finalResult.FinalResult.Response);
      }

      UnityEngine.Debug.Log($"[LlamaBrainAgent] Inference complete: {finalResult}");

      return finalResult;
    }

    /// <summary>
    /// Builds a StateSnapshot for the current interaction.
    /// </summary>
    private StateSnapshot BuildStateSnapshot(string playerInput)
    {
      // Get dialogue history
      var dialogueHistory = GetConversationHistory();

      // If we have ExpectancyConfig and memory provider, use the full builder
      if (ExpectancyConfig != null && memoryProvider != null && runtimeProfile != null)
      {
        var memorySystem = memoryProvider.GetOrCreateSystem(runtimeProfile.PersonaId);
        var interactionContext = ExpectancyConfig.CreatePlayerUtteranceContext(playerInput);
        var evaluatedConstraints = ExpectancyConfig.Evaluate(interactionContext, currentTriggerRules);
        LastConstraints = evaluatedConstraints; // Store constraints for debugging/metrics
        
        var snapshot = UnityStateSnapshotBuilder.BuildForNpcDialogue(
          npcConfig: ExpectancyConfig,
          memorySystem: memorySystem,
          playerInput: playerInput,
          systemPrompt: runtimeProfile.SystemPrompt ?? "",
          dialogueHistory: dialogueHistory
        );
        return snapshot;
      }

      // Fallback to minimal snapshot
      var context = currentInteractionContext ?? new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        PlayerInput = playerInput,
        GameTime = Time.time,
        NpcId = runtimeProfile?.PersonaId
      };

      // Evaluate constraints if we have ExpectancyConfig
      ConstraintSet constraints = ConstraintSet.Empty;
      if (ExpectancyConfig != null)
      {
        constraints = ExpectancyConfig.Evaluate(context, currentTriggerRules);
        LastConstraints = constraints;
      }

      return new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(runtimeProfile?.SystemPrompt ?? "")
        .WithPlayerInput(playerInput)
        .WithDialogueHistory(dialogueHistory)
        .WithMaxAttempts(retryPolicy?.MaxAttempts ?? 3)
        .WithMetadata("game_time", Time.time.ToString("F2"))
        .Build();
    }

    /// <summary>
    /// Builds a prompt string from a StateSnapshot.
    /// </summary>
    private string BuildPromptFromSnapshot(StateSnapshot snapshot)
    {
      var parts = new List<string>();

      // System prompt
      if (!string.IsNullOrEmpty(snapshot.SystemPrompt))
      {
        parts.Add($"System: {snapshot.SystemPrompt}");
      }

      // Memory context
      var memories = snapshot.GetAllMemoryForPrompt().ToList();
      if (memories.Count > 0)
      {
        parts.Add("\n[Context]");
        parts.AddRange(memories);
      }

      // Constraints
      if (snapshot.Constraints.HasConstraints)
      {
        parts.Add(snapshot.Constraints.ToPromptInjection());
      }

      // Retry feedback if this is a retry
      if (snapshot.AttemptNumber > 0 && LastInferenceResult != null)
      {
        var lastAttempt = LastInferenceResult.AllAttempts.LastOrDefault();
        if (lastAttempt != null && retryPolicy != null)
        {
          parts.Add(retryPolicy.GenerateRetryFeedback(lastAttempt));
        }
      }

      // Dialogue history
      if (snapshot.DialogueHistory.Count > 0)
      {
        parts.Add("\n[Conversation]");
        foreach (var line in snapshot.DialogueHistory)
        {
          parts.Add(line);
        }
      }

      // Player input
      parts.Add($"\nPlayer: {snapshot.PlayerInput}");

      // NPC turn
      var npcName = runtimeProfile?.Name ?? "NPC";
      parts.Add($"{npcName}:");

      return string.Join("\n", parts);
    }

    /// <summary>
    /// Sends a player input to the LlamaBrain server.
    /// </summary>
    /// <param name="playerName">The name of the player (optional, will use settings if not provided).</param>
    /// <param name="input">The input from the player.</param>
    /// <returns>The response from the LlamaBrain server.</returns>
    public async UniTask<string> SendPlayerInputAsync(string? playerName, string input)
    {
      var result = await SendWithSnapshotAsync(input);
      return result.FinalResult.Response;
    }

    /// <summary>
    /// Add important information to the NPC's memory
    /// </summary>
    /// <param name="memoryEntry">The memory entry to add</param>
    public void AddMemory(string memoryEntry)
    {
      if (runtimeProfile == null || memoryProvider == null)
      {
        UnityEngine.Debug.LogWarning("[LlamaBrainAgent] Cannot add memory: Agent not initialized or runtime profile is null");
        return;
      }

      UnityEngine.Debug.Log($"Adding memory: {memoryEntry}");
      
      // Use structured memory system - store as episodic memory with default significance
      var memorySystem = memoryProvider.GetOrCreateSystem(runtimeProfile.PersonaId);
      var entry = EpisodicMemoryEntry.FromLearnedInfo(memoryEntry, "Player", significance: 0.5f);
      memorySystem.AddEpisodicMemory(entry, MutationSource.ValidatedOutput);
    }

    /// <summary>
    /// Add important information to the NPC's memory with a specific category
    /// </summary>
    /// <param name="category">The category of memory (e.g., "PlayerInfo", "WorldFacts", "Preferences")</param>
    /// <param name="memoryEntry">The memory entry to add</param>
    public void AddMemory(string category, string memoryEntry)
    {
      if (runtimeProfile == null || memoryProvider == null)
      {
        UnityEngine.Debug.LogWarning("[LlamaBrainAgent] Cannot add memory: Agent not initialized or runtime profile is null");
        return;
      }

      // Use structured memory system
      var memorySystem = memoryProvider.GetOrCreateSystem(runtimeProfile.PersonaId);
      var categoryLower = category.ToLower();
      
      // Categorize based on memory type
      if (categoryLower.Contains("belief") || categoryLower.Contains("opinion") || categoryLower.Contains("relationship"))
      {
        // Store as Belief
        var belief = new BeliefMemoryEntry("Player", memoryEntry);
        var beliefId = $"player_{category}_{Guid.NewGuid().ToString("N")[..8]}";
        memorySystem.SetBelief(beliefId, belief, MutationSource.ValidatedOutput);
        UnityEngine.Debug.Log($"[LlamaBrainAgent] Stored categorized belief '{category}': {memoryEntry}");
      }
      else if (categoryLower.Contains("fact") || categoryLower.Contains("world"))
      {
        // Store as World State (not canonical - those are designer-only)
        var stateKey = $"player_{category}";
        memorySystem.SetWorldState(stateKey, memoryEntry, MutationSource.ValidatedOutput);
        UnityEngine.Debug.Log($"[LlamaBrainAgent] Stored categorized world state '{category}': {memoryEntry}");
      }
      else
      {
        // Store as Episodic Memory
        var entry = EpisodicMemoryEntry.FromLearnedInfo(memoryEntry, "Player", 0.5f);
        memorySystem.AddEpisodicMemory(entry, MutationSource.ValidatedOutput);
        UnityEngine.Debug.Log($"[LlamaBrainAgent] Stored categorized episodic memory '{category}': {memoryEntry}");
      }
    }

    /// <summary>
    /// Extract and store important information from a conversation exchange
    /// </summary>
    /// <param name="playerInput">The player's input</param>
    /// <param name="npcResponse">The NPC's response</param>
    private void ExtractAndStoreImportantInformation(string playerInput, string npcResponse)
    {
      if (!(runtimeProfile?.UseMemory ?? false))
        return;

      // Only extract memory if the input seems meaningful (not just greetings, etc.)
      if (!IsMeaningfulInput(playerInput))
        return;

      // Use ScriptableObject-based extraction if available
      if (MemoryCategoryManager != null)
      {
        ExtractUsingScriptableObjectCategories(playerInput, npcResponse);
      }
      else
      {
        // Fallback to hardcoded extraction
        ExtractUsingHardcodedRules(playerInput, npcResponse);
      }
    }

    /// <summary>
    /// Check if the input contains meaningful information worth storing as memory
    /// </summary>
    /// <param name="input">The player input to check</param>
    /// <returns>True if the input contains meaningful information</returns>
    private bool IsMeaningfulInput(string input)
    {
      if (string.IsNullOrEmpty(input))
        return false;

      var lowerInput = input.ToLower().Trim();

      // Skip very short inputs
      if (lowerInput.Length < 10)
        return false;

      // Skip common greetings and casual responses
      var casualPhrases = new[]
      {
                "hello", "hi", "hey", "goodbye", "bye", "thanks", "thank you",
                "yes", "no", "ok", "okay", "sure", "maybe", "i don't know",
                "what", "how", "why", "when", "where", "who",
                "please", "excuse me", "sorry", "pardon",
                "um", "uh", "ah", "oh", "hmm", "well",
                "you know", "i mean", "like", "sort of", "kind of",
                "good morning", "good afternoon", "good evening", "good night"
            };

      // If the input is just a casual phrase, don't extract memory
      if (casualPhrases.Any(phrase => lowerInput.Contains(phrase) && lowerInput.Length < 20))
        return false;

      // Look for indicators of meaningful information
      var meaningfulIndicators = new[]
      {
                "my name is", "i'm", "i am", "call me",
                "i like", "i love", "i prefer", "i hate", "i don't like",
                "i am a", "i'm a", "i work as", "i do",
                "remember", "don't forget", "important", "note",
                "quest", "mission", "task", "help", "save", "rescue",
                "family", "friend", "home", "town", "city", "village"
            };

      return meaningfulIndicators.Any(indicator => lowerInput.Contains(indicator));
    }

    /// <summary>
    /// Extract information using ScriptableObject-based memory categories
    /// </summary>
    /// <param name="playerInput">The player's input</param>
    /// <param name="npcResponse">The NPC's response</param>
    private void ExtractUsingScriptableObjectCategories(string playerInput, string npcResponse)
    {
      // Only extract from player input, not NPC responses (to avoid junk)
      if (MemoryCategoryManager == null)
        return;
      
      var playerExtractions = MemoryCategoryManager.ExtractInformationFromText(playerInput);

      // Use structured memory system
      if (memoryProvider == null || runtimeProfile == null)
        return;

      var memorySystem = memoryProvider.GetOrCreateSystem(runtimeProfile.PersonaId);
      
      foreach (var kvp in playerExtractions)
      {
        var category = MemoryCategoryManager.GetCategory(kvp.Key);
        if (category != null && ShouldStoreMemory(category, kvp.Value))
        {
          foreach (var extractedInfo in kvp.Value)
          {
            // Check if this memory already exists to avoid duplicates
            if (!MemoryExists(category, extractedInfo))
            {
              // Categorize based on memory type
              var categoryName = category.CategoryName.ToLower();
              var significance = category.Importance;
              
              if (categoryName.Contains("belief") || categoryName.Contains("opinion") || categoryName.Contains("relationship"))
              {
                // Store as Belief
                var belief = new BeliefMemoryEntry("Player", extractedInfo);
                var beliefId = $"player_{category.CategoryName}_{Guid.NewGuid().ToString("N")[..8]}";
                memorySystem.SetBelief(beliefId, belief, MutationSource.ValidatedOutput);
                UnityEngine.Debug.Log($"[LlamaBrainAgent] Stored belief using category '{category.DisplayName}': {extractedInfo}");
              }
              else if (categoryName.Contains("fact") || categoryName.Contains("world"))
              {
                // Store as World State (not canonical - those are designer-only)
                var stateKey = $"player_{category.CategoryName}";
                memorySystem.SetWorldState(stateKey, extractedInfo, MutationSource.ValidatedOutput);
                UnityEngine.Debug.Log($"[LlamaBrainAgent] Stored world state using category '{category.DisplayName}': {extractedInfo}");
              }
              else
              {
                // Store as Episodic Memory
                var entry = EpisodicMemoryEntry.FromLearnedInfo(extractedInfo, "Player", significance);
                memorySystem.AddEpisodicMemory(entry, MutationSource.ValidatedOutput);
                UnityEngine.Debug.Log($"[LlamaBrainAgent] Stored episodic memory using category '{category.DisplayName}': {extractedInfo}");
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Determine if a memory should be stored based on category importance and content quality
    /// </summary>
    /// <param name="category">The memory category</param>
    /// <param name="extractedInfo">The extracted information</param>
    /// <returns>True if the memory should be stored</returns>
    private bool ShouldStoreMemory(MemoryCategory category, List<string> extractedInfo)
    {
      // Only store if category has high importance
      if (category.Importance < 0.5f)
        return false;

      // Check if we're at max entries for this category
      var existingMemories = GetMemoriesByCategory(category.CategoryName);
      if (existingMemories.Count >= category.MaxEntries)
        return false;

      // Filter out common junk phrases
      foreach (var info in extractedInfo)
      {
        var lowerInfo = info.ToLower();
        if (IsJunkPhrase(lowerInfo))
          return false;
      }

      return true;
    }

    /// <summary>
    /// Check if a phrase is junk that shouldn't be stored as memory
    /// </summary>
    /// <param name="phrase">The phrase to check</param>
    /// <returns>True if it's junk</returns>
    private bool IsJunkPhrase(string phrase)
    {
      var junkPhrases = new[]
      {
                "hello", "hi", "hey", "goodbye", "bye", "thanks", "thank you",
                "yes", "no", "ok", "okay", "sure", "maybe", "i don't know",
                "what", "how", "why", "when", "where", "who",
                "please", "excuse me", "sorry", "pardon",
                "um", "uh", "ah", "oh", "hmm", "well",
                "you know", "i mean", "like", "sort of", "kind of"
            };

      return junkPhrases.Any(junk => phrase.Contains(junk));
    }

    /// <summary>
    /// Check if a memory already exists for this category and content
    /// </summary>
    /// <param name="category">The memory category</param>
    /// <param name="content">The content to check</param>
    /// <returns>True if the memory already exists</returns>
    private bool MemoryExists(MemoryCategory category, string content)
    {
      var existingMemories = GetMemoriesByCategory(category.CategoryName);
      var normalizedContent = content.ToLower().Trim();

      foreach (var memory in existingMemories)
      {
        var parsedContent = category.ParseMemoryEntry(memory);
        if (parsedContent != null && parsedContent.ToLower().Trim() == normalizedContent)
          return true;
      }

      return false;
    }

    /// <summary>
    /// Extract information using hardcoded rules (fallback)
    /// </summary>
    /// <param name="playerInput">The player's input</param>
    /// <param name="npcResponse">The NPC's response</param>
    private void ExtractUsingHardcodedRules(string playerInput, string npcResponse)
    {
      var extractedFacts = new List<string>();

      // Extract player name if mentioned
      ExtractPlayerName(playerInput, npcResponse, extractedFacts);

      // Extract preferences and important facts
      ExtractPreferences(playerInput, npcResponse, extractedFacts);

      // Extract world knowledge and important events
      ExtractWorldKnowledge(playerInput, npcResponse, extractedFacts);

      // Store extracted facts as memory
      foreach (var fact in extractedFacts)
      {
        AddMemory(fact);
      }
    }

    /// <summary>
    /// Extract player name from conversation
    /// </summary>
    private void ExtractPlayerName(string playerInput, string npcResponse, List<string> extractedFacts)
    {
      // Look for patterns like "My name is X" or "I'm X" or "Call me X"
      var namePatterns = new[]
      {
                @"(?:my name is|i'm|i am|call me)\s+([a-zA-Z]+)",
                @"(?:name's|name is)\s+([a-zA-Z]+)"
            };

      foreach (var pattern in namePatterns)
      {
        var match = System.Text.RegularExpressions.Regex.Match(playerInput.ToLower(), pattern);
        if (match.Success)
        {
          var name = match.Groups[1].Value;
          if (name.Length > 1) // Avoid single letters
          {
            extractedFacts.Add($"PlayerInfo: Player's name is {char.ToUpper(name[0]) + name.Substring(1)}");
            return;
          }
        }
      }
    }

    /// <summary>
    /// Extract preferences and important facts from conversation
    /// </summary>
    private void ExtractPreferences(string playerInput, string npcResponse, List<string> extractedFacts)
    {
      var input = playerInput.ToLower();

      // Extract preferences
      if (input.Contains("like") || input.Contains("love") || input.Contains("prefer"))
      {
        if (input.Contains("magic") || input.Contains("spell"))
          extractedFacts.Add("Preferences: Player prefers magic/spells");
        if (input.Contains("weapon") || input.Contains("sword") || input.Contains("combat"))
          extractedFacts.Add("Preferences: Player prefers weapons/combat");
        if (input.Contains("stealth") || input.Contains("sneak"))
          extractedFacts.Add("Preferences: Player prefers stealth approach");
      }

      // Extract important facts about the player
      if (input.Contains("i am") || input.Contains("i'm"))
      {
        if (input.Contains("wizard") || input.Contains("mage"))
          extractedFacts.Add("PlayerInfo: Player is a wizard/mage");
        if (input.Contains("warrior") || input.Contains("fighter"))
          extractedFacts.Add("PlayerInfo: Player is a warrior/fighter");
        if (input.Contains("rogue") || input.Contains("thief"))
          extractedFacts.Add("PlayerInfo: Player is a rogue/thief");
      }
    }

    /// <summary>
    /// Extract world knowledge and important events
    /// </summary>
    private void ExtractWorldKnowledge(string playerInput, string npcResponse, List<string> extractedFacts)
    {
      var input = playerInput.ToLower();
      var response = npcResponse.ToLower();

      // Look for important world events or knowledge
      if (input.Contains("quest") || input.Contains("mission"))
      {
        if (input.Contains("complete") || input.Contains("finished"))
          extractedFacts.Add("WorldEvents: Player completed a quest");
        if (input.Contains("start") || input.Contains("begin"))
          extractedFacts.Add("WorldEvents: Player started a quest");
      }

      // Extract location information
      if (input.Contains("town") || input.Contains("city") || input.Contains("village"))
      {
        var locationMatch = System.Text.RegularExpressions.Regex.Match(input, @"(?:in|at|to)\s+([a-zA-Z\s]+)(?:town|city|village)");
        if (locationMatch.Success)
        {
          var location = locationMatch.Groups[1].Value.Trim();
          if (!string.IsNullOrEmpty(location))
            extractedFacts.Add($"WorldInfo: Player mentioned location {location}");
        }
      }
    }

    /// <summary>
    /// Manually add a specific memory entry with category
    /// </summary>
    /// <param name="category">The category of the memory</param>
    /// <param name="content">The memory content</param>
    public void AddSpecificMemory(string category, string content)
    {
      AddMemory(category, content);
    }

    /// <summary>
    /// Get memories by category
    /// </summary>
    /// <param name="category">The category to filter by</param>
    /// <returns>List of memories in the specified category</returns>
    public IReadOnlyList<string> GetMemoriesByCategory(string category)
    {
      var allMemories = GetMemories();
      var filteredMemories = new List<string>();

      foreach (var memory in allMemories)
      {
        if (memory.StartsWith($"[{category}]"))
        {
          filteredMemories.Add(memory);
        }
      }

      return filteredMemories;
    }

    /// <summary>
    /// Add an entry to the conversation history
    /// </summary>
    /// <param name="speaker">The speaker (Player or NPC)</param>
    /// <param name="text">The text spoken</param>
    private void AddToConversationHistory(string speaker, string text)
    {
      var entry = new DialogueEntry
      {
        Speaker = speaker,
        Text = text,
        Timestamp = System.DateTime.Now
      };

      conversationHistory.Add(entry);

      // Keep only the most recent entries
      while (conversationHistory.Count > maxConversationHistoryEntries)
      {
        conversationHistory.RemoveAt(0);
      }
    }

    /// <summary>
    /// Clear the conversation history
    /// </summary>
    public void ClearDialogueHistory()
    {
      conversationHistory.Clear();
      dialogueSession?.Clear();
    }

    /// <summary>
    /// Get all memories for the persona as a list
    /// </summary>
    /// <returns>List of all memories</returns>
    public IReadOnlyList<string> GetMemories()
    {
      if (runtimeProfile == null || memoryProvider == null)
        return new List<string>();

      var memorySystem = memoryProvider.GetOrCreateSystem(runtimeProfile.PersonaId);
      return memorySystem.GetAllMemoriesForPrompt(maxEpisodic: 20).ToList();
    }

    /// <summary>
    /// Clear all memories for the persona
    /// </summary>
    public void ClearMemories()
    {
      if (runtimeProfile == null || memoryProvider == null)
        return;

      UnityEngine.Debug.Log($"[LlamaBrainAgent] Clearing all memories for {runtimeProfile.Name}");
      memoryProvider.ClearMemory(runtimeProfile);
    }

    /// <summary>
    /// Get the conversation history as a list of strings
    /// </summary>
    /// <returns>List of conversation history entries</returns>
    public IReadOnlyList<string> GetConversationHistory()
    {
      var history = new List<string>();
      foreach (var entry in conversationHistory)
      {
        history.Add($"{entry.Speaker}: {entry.Text}");
      }
      return history;
    }

    /// <summary>
    /// Get recent conversation history entries
    /// </summary>
    /// <param name="count">Number of recent entries to retrieve</param>
    /// <returns>List of recent dialogue entries</returns>
    public IReadOnlyList<DialogueEntry> GetRecentHistory(int count)
    {
      var recentCount = Mathf.Min(count, conversationHistory.Count);
      var startIndex = conversationHistory.Count - recentCount;
      return conversationHistory.GetRange(startIndex, recentCount);
    }

    /// <summary>
    /// Get the number of memories stored
    /// </summary>
    public int MemoryCount => GetMemories().Count;

    /// <summary>
    /// Get the number of conversation history entries
    /// </summary>
    public int ConversationHistoryCount => conversationHistory.Count;

    /// <summary>
    /// Check if the persona has any memories
    /// </summary>
    public bool HasMemories => MemoryCount > 0;

    /// <summary>
    /// Check if the persona has any conversation history
    /// </summary>
    public bool HasConversationHistory => conversationHistory.Count > 0;

    /// <summary>
    /// Check if the agent is properly initialized
    /// </summary>
    public bool IsInitialized => client != null && memoryProvider != null && runtimeProfile != null;

    /// <summary>
    /// Check if the agent is connected to the server
    /// </summary>
    public bool IsConnected => client != null;

    /// <summary>
    /// Check if initialization was attempted (regardless of success)
    /// </summary>
    private bool initializationAttempted = false;
    public bool WasInitializationAttempted => initializationAttempted;

    /// <summary>
    /// Get the connection status message
    /// </summary>
    public string ConnectionStatus
    {
      get
      {
        if (!IsInitialized)
          return "Agent not initialized";
        if (!IsConnected)
          return "Not connected to server";
        return "Connected";
      }
    }

    /// <summary>
    /// Get setup instructions for the current state
    /// </summary>
    public string GetSetupInstructions()
    {
      if (!WasInitializationAttempted)
      {
        return "Initialization Issue:\n" +
               "1. Initialize() method has never been called\n" +
               "2. Check if NpcAgentExample.Start() is running\n" +
               "3. Verify that the agent component is properly referenced\n" +
               "4. Look for initialization errors in the console";
      }

      if (!IsInitialized)
      {
        return "Setup Instructions:\n" +
               "1. Add a BrainServer component to your scene\n" +
               "2. Configure BrainSettings with valid ExecutablePath and ModelPath\n" +
               "3. Or use NpcAgentExample component which handles setup automatically\n" +
               "4. Check console for initialization error messages";
      }

      if (!IsConnected)
      {
        return "Connection Issue:\n" +
               "1. Check if BrainServer is running\n" +
               "2. Verify BrainSettings configuration\n" +
               "3. Ensure llama.cpp executable and model files exist\n" +
               "4. Check server logs for startup errors";
      }

      return "Agent is ready to use!";
    }


    /// <summary>
    /// Builds a failure reason string from the inference result.
    /// </summary>
    private string BuildFailureReason(InferenceResultWithRetries result)
    {
      var reasons = new List<string>();

      if (!result.Success)
      {
        var final = result.FinalResult;
        
        if (!string.IsNullOrEmpty(final.ErrorMessage))
        {
          reasons.Add($"API Error: {final.ErrorMessage}");
        }
        else if (final.Violations.Count > 0)
        {
          reasons.Add($"Validation Failed: {final.Outcome} ({final.Violations.Count} violations)");
        }
        else
        {
          reasons.Add($"Validation Failed: {final.Outcome}");
        }

        reasons.Add($"After {result.AttemptCount} attempts");
      }

      return string.Join("; ", reasons);
    }

    /// <summary>
    /// Generate a context-appropriate fallback response when validation fails
    /// (Legacy method - now uses AuthorControlledFallback internally)
    /// </summary>
    private string GenerateFallbackResponse(string playerInput)
    {
      // Initialize fallback if needed
      if (fallbackSystem == null)
      {
        var fallbackConfigInstance = fallbackConfig != null 
          ? fallbackConfig.ToFallbackConfig() 
          : new AuthorControlledFallback.FallbackConfig();
        fallbackSystem = new AuthorControlledFallback(fallbackConfigInstance);
      }

      // Use context from current interaction if available
      var context = currentInteractionContext ?? new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        PlayerInput = playerInput,
        GameTime = Time.time
      };

      return fallbackSystem.GetFallbackResponse(context, "Legacy fallback triggered", null, currentTriggerFallbacks);
    }

    /// <summary>
    /// Gets the fallback system statistics (for metrics/debugging).
    /// </summary>
    IFallbackStats? IAgentMetrics.FallbackStats => fallbackSystem?.Stats;

    /// <summary>
    /// Gets the fallback system statistics (for direct access).
    /// </summary>
    public AuthorControlledFallback.FallbackStats? FallbackStats => fallbackSystem?.Stats;

    /// <summary>
    /// Gets the mutation controller statistics (for metrics/debugging).
    /// </summary>
    public MutationStatistics? MutationStats => mutationController?.Statistics;

    /// <summary>
    /// Hooks a WorldIntentDispatcher to receive world intents from this agent.
    /// </summary>
    /// <param name="dispatcher">The dispatcher to hook.</param>
    public void HookIntentDispatcher(LlamaBrainRuntime.Core.WorldIntentDispatcher dispatcher)
    {
      if (mutationController != null && dispatcher != null)
      {
        dispatcher.HookToController(mutationController);
      }
    }

    /// <summary>
    /// Unhooks a WorldIntentDispatcher from this agent.
    /// </summary>
    /// <param name="dispatcher">The dispatcher to unhook.</param>
    public void UnhookIntentDispatcher(LlamaBrainRuntime.Core.WorldIntentDispatcher dispatcher)
    {
      if (mutationController != null && dispatcher != null)
      {
        dispatcher.UnhookFromController(mutationController);
      }
    }

    /// <summary>
    /// Set server configuration info for detailed logging
    /// </summary>
    public void SetServerConfig(string modelPath, int gpuLayers, int batchSize, int uBatchSize, int parallelSlots)
    {
      this.modelPath = modelPath ?? "";
      this.gpuLayers = gpuLayers;
      this.batchSize = batchSize;
      this.uBatchSize = uBatchSize;
      this.parallelSlots = parallelSlots;

      // Extract quantization from model path (e.g., "model.Q4_0.gguf" -> "Q4_0")
      if (!string.IsNullOrEmpty(modelPath))
      {
        var fileName = System.IO.Path.GetFileNameWithoutExtension(modelPath);
        var parts = fileName.Split('.');
        foreach (var part in parts)
        {
          if (part.StartsWith("Q") && (part.Contains("_") || part.Length >= 2))
          {
            modelQuantization = part;
            break;
          }
        }
        if (modelQuantization == "Unknown" && fileName.Contains("F16"))
        {
          modelQuantization = "F16";
        }
        else if (modelQuantization == "Unknown" && fileName.Contains("F32"))
        {
          modelQuantization = "F32";
        }
      }
    }

    /// <summary>
    /// Update actual GPU layers offloaded from server log parsing
    /// </summary>
    public void UpdateGpuLayersOffloaded(int layersOffloaded, int totalLayers)
    {
      actualGpuLayersOffloaded = layersOffloaded;
    }

    /// <summary>
    /// Try to auto-configure server settings by finding BrainServer in the scene
    /// </summary>
    private void TryAutoConfigureServerSettings()
    {
      try
      {
        var brainServer = UnityEngine.Object.FindObjectOfType<BrainServer>();
        if (brainServer != null)
        {
          brainServer.ConfigureAgent(this);
          UnityEngine.Debug.Log("[LlamaBrainAgent] Auto-configured with BrainServer settings");
        }
      }
      catch (System.Exception ex)
      {
        // Silently fail - configuration is optional
        UnityEngine.Debug.LogWarning($"[LlamaBrainAgent] Could not auto-configure server settings: {ex.Message}");
      }
    }

    /// <summary>
    /// Handler for performance metrics events from ApiClient
    /// </summary>
    /// <param name="metrics">The performance metrics</param>
    private void OnPerformanceMetricsReceived(CompletionMetrics metrics)
    {
      // Store last metrics for external access
      LastMetrics = metrics;
      // Extract model name from path
      var modelName = !string.IsNullOrEmpty(modelPath)
        ? System.IO.Path.GetFileName(modelPath)
        : "Unknown";

      // Build comprehensive request log
      var logMessage = $"[LlamaBrainAgent] Request Metrics:\n" +
        $"  Model: {modelName} ({modelQuantization})\n" +
        $"  GPU Layers: {gpuLayers} (offloaded: {(actualGpuLayersOffloaded >= 0 ? actualGpuLayersOffloaded.ToString() : "unknown")})\n" +
        $"  Batch/UBatch: {batchSize}/{uBatchSize}\n" +
        $"  Parallel Slots: {parallelSlots}\n" +
        $"  Tokens: Prompt={metrics.PromptTokenCount} | Generated={metrics.GeneratedTokenCount} | Cached={metrics.CachedTokenCount}\n" +
        $"  Timing: TTFT={metrics.TtftMs}ms | Prefill={metrics.PrefillTimeMs}ms | Decode={metrics.DecodeTimeMs}ms | Wall={metrics.TotalTimeMs}ms\n" +
        $"  Speed: {metrics.TokensPerSecond:F1} tokens/sec";

      UnityEngine.Debug.Log(logMessage);

      // VALIDATION: Fail loudly if GPU layers drop to 0 (CPU-only mode)
      if (actualGpuLayersOffloaded == 0 && gpuLayers > 0)
      {
        UnityEngine.Debug.LogError($"[LlamaBrainAgent] CRITICAL: GPU layers offloaded = 0! " +
          $"Expected {gpuLayers} layers. Model is running on CPU only - performance will be severely degraded. " +
          $"Check: CUDA-enabled llama.cpp build, GPU drivers, VRAM availability.");
      }
      // Only warn about partial offload if performance is actually regressed
      // llama.cpp may not offload all layers (embeddings/output may stay on CPU by design)
      // Warn only on observed performance regression, not layer-count trivia
      else if (gpuLayers > 0 && actualGpuLayersOffloaded > 0)
      {
        var offloadRatio = (double)actualGpuLayersOffloaded / gpuLayers;
        var isLowOffloadRatio = offloadRatio < 0.9; // Less than 90% offloaded

        // Performance regression indicators
        var isLowTps = metrics.TokensPerSecond > 0 && metrics.TokensPerSecond < 50;
        var isHighLatency = metrics.TotalTimeMs > 500 && metrics.GeneratedTokenCount < 30; // >500ms for short replies

        // Only warn if performance is actually regressed AND offload ratio is low
        if (isLowOffloadRatio && (isLowTps || isHighLatency))
        {
          UnityEngine.Debug.LogWarning($"[LlamaBrainAgent] WARNING: Performance regression detected. " +
            $"Only {actualGpuLayersOffloaded}/{gpuLayers} GPU layers offloaded ({offloadRatio:P0}), " +
            $"decode speed: {metrics.TokensPerSecond:F1} tps, wall time: {metrics.TotalTimeMs}ms. " +
            $"Some layers running on CPU - performance may be degraded.");
        }
        // If performance is good (>100 tps and reasonable latency), don't warn
        // llama.cpp's layer allocation is often optimal even if not all layers are offloaded
      }

      // VALIDATION: Fail loudly if decode speed is too low
      const double MIN_DECODE_TPS = 50.0;
      if (metrics.TokensPerSecond > 0 && metrics.TokensPerSecond < MIN_DECODE_TPS)
      {
        UnityEngine.Debug.LogError($"[LlamaBrainAgent] CRITICAL: Decode speed {metrics.TokensPerSecond:F1} tps is below threshold ({MIN_DECODE_TPS} tps)! " +
          $"Expected 100-200 tps with GPU. Possible causes: CPU-only inference, GPU not enabled, model too large, or system throttling. " +
          $"Check: GPU layers offloaded, CUDA build, VRAM usage, GPU temperature.");
      }
      else if (metrics.TokensPerSecond > 0 && metrics.TokensPerSecond < 100 && gpuLayers > 0)
      {
        UnityEngine.Debug.LogWarning($"[LlamaBrainAgent] WARNING: Decode speed {metrics.TokensPerSecond:F1} tps is lower than expected. " +
          $"With GPU, expect 100-200 tps. Current performance may indicate partial CPU fallback or optimization issues.");
      }
    }

    /// <summary>
    /// Debug method to log the current state of the agent
    /// </summary>
    [ContextMenu("Debug Agent State")]
    public void DebugAgentState()
    {
      UnityEngine.Debug.Log($"[LlamaBrainAgent] Debug State:");
      UnityEngine.Debug.Log($"  - WasInitializationAttempted: {WasInitializationAttempted}");
      UnityEngine.Debug.Log($"  - IsInitialized: {IsInitialized}");
      UnityEngine.Debug.Log($"  - IsConnected: {IsConnected}");
      UnityEngine.Debug.Log($"  - Client: {(client != null ? "Set" : "Null")}");
      UnityEngine.Debug.Log($"  - MemoryProvider: {(memoryProvider != null ? "Set" : "Null")}");
      UnityEngine.Debug.Log($"  - RuntimeProfile: {(runtimeProfile != null ? $"Set ({runtimeProfile.Name})" : "Null")}");
      UnityEngine.Debug.Log($"  - PersonaConfig: {(PersonaConfig != null ? $"Set ({PersonaConfig.Name})" : "Null")}");
      UnityEngine.Debug.Log($"  - DialogueSession: {(dialogueSession != null ? "Set" : "Null")}");
      UnityEngine.Debug.Log($"  - ConnectionStatus: {ConnectionStatus}");
    }

    /// <summary>
    /// Update method for automatic memory decay
    /// </summary>
    private void Update()
    {
      if (enableAutoDecay && Application.isPlaying && IsInitialized && memoryProvider != null && runtimeProfile != null)
      {
        if (Time.time - lastDecayTime >= decayIntervalSeconds)
        {
          memoryProvider.ApplyDecay(runtimeProfile.PersonaId);
          lastDecayTime = Time.time;
          UnityEngine.Debug.Log($"[LlamaBrainAgent] Applied memory decay for {runtimeProfile.PersonaId}");
        }
      }
    }

    /// <summary>
    /// Initialize canonical facts from PersonaConfig if available
    /// </summary>
    private void InitializeCanonicalFacts()
    {
      if (!IsInitialized || runtimeProfile == null || memoryProvider == null)
        return;

      // For now, this is a placeholder - canonical facts can be added via public method
      // In the future, PersonaConfig could have a CanonicalFacts list
      // Example usage:
      // agent.InitializeCanonicalFact("world_rule_1", "Magic is real", "world_lore");
    }

    /// <summary>
    /// Initialize a canonical fact for this NPC (designer-defined immutable truth)
    /// </summary>
    /// <param name="factId">Unique identifier for the fact</param>
    /// <param name="fact">The immutable fact content</param>
    /// <param name="domain">Optional domain/category (e.g., "world", "character", "lore")</param>
    public void InitializeCanonicalFact(string factId, string fact, string? domain = null)
    {
      if (!IsInitialized || runtimeProfile == null || memoryProvider == null)
      {
        UnityEngine.Debug.LogWarning("[LlamaBrainAgent] Cannot initialize canonical fact: Agent not initialized");
        return;
      }

      var result = memoryProvider.AddCanonicalFact(runtimeProfile.PersonaId, factId, fact, domain);
      if (result.Success)
      {
        UnityEngine.Debug.Log($"[LlamaBrainAgent] Initialized canonical fact: {factId} = '{fact}'");
      }
      else
      {
        UnityEngine.Debug.LogWarning($"[LlamaBrainAgent] Failed to initialize canonical fact {factId}: {result.FailureReason}");
      }
    }

    /// <summary>
    /// Loads validation rules from ValidationPipeline (global) and NPC-specific rules.
    /// Called during initialization and can be called again to reload rules.
    /// </summary>
    private void LoadValidationRules()
    {
      if (validationGate == null)
      {
        UnityEngine.Debug.LogWarning("[LlamaBrainAgent] Cannot load validation rules: ValidationGate not initialized");
        return;
      }

      int globalRuleCount = 0;
      int npcRuleCount = 0;

      // Load global rules from ValidationPipeline singleton
      // Note: We access the internal gate to get rules, but ValidationPipeline doesn't expose this
      // So we'll use a reflection-based approach or add a public method to ValidationPipeline
      // For now, we'll note that ValidationPipeline applies rules during its Process method
      var globalPipeline = LlamaBrain.Runtime.Core.Validation.ValidationPipeline.Instance;
      if (globalPipeline != null)
      {
        // Try to get rules via reflection (temporary until ValidationPipeline exposes GetRules)
        // For now, we'll use ValidationPipeline's Process method which includes global rules
        UnityEngine.Debug.Log($"[LlamaBrainAgent] ValidationPipeline found - global rules will be applied via pipeline");
      }
      else
      {
        UnityEngine.Debug.Log($"[LlamaBrainAgent] No ValidationPipeline found in scene - no global validation rules will be applied");
      }

      // Load NPC-specific rules into our ValidationGate
      if (npcValidationRules != null && npcValidationRules.Enabled)
      {
        foreach (var rule in npcValidationRules.ToValidationRules())
        {
          validationGate.AddRule(rule);
          npcRuleCount++;
        }
        UnityEngine.Debug.Log($"[LlamaBrainAgent] Loaded {npcRuleCount} NPC-specific validation rules from '{npcValidationRules.name}'");
      }

      if (globalRuleCount == 0 && npcRuleCount == 0)
      {
        UnityEngine.Debug.Log($"[LlamaBrainAgent] No validation rules loaded (will use ValidationPipeline if available)");
      }
    }

    /// <summary>
    /// Loads trigger-specific validation rules into the validation gate.
    /// These rules are temporary and should be cleared after the interaction.
    /// </summary>
    /// <param name="triggerRuleSet">The validation rule set from the trigger</param>
    /// <returns>The number of rules loaded</returns>
    public int LoadTriggerValidationRules(LlamaBrain.Runtime.Core.Validation.ValidationRuleSetAsset? triggerRuleSet)
    {
      if (validationGate == null)
      {
        UnityEngine.Debug.LogWarning("[LlamaBrainAgent] Cannot load trigger validation rules: ValidationGate not initialized");
        return 0;
      }

      if (triggerRuleSet == null || !triggerRuleSet.Enabled)
      {
        return 0;
      }

      int count = 0;
      foreach (var rule in triggerRuleSet.ToValidationRules())
      {
        validationGate.AddRule(rule);
        count++;
      }

      if (count > 0)
      {
        UnityEngine.Debug.Log($"[LlamaBrainAgent] Loaded {count} trigger-specific validation rules from '{triggerRuleSet.name}'");
      }

      return count;
    }

    /// <summary>
    /// Clears trigger-specific validation rules by rule set name.
    /// This should be called after an interaction completes to remove temporary rules.
    /// </summary>
    /// <param name="ruleSetName">The name of the rule set to remove (optional - if null, clears all custom rules)</param>
    public void ClearTriggerValidationRules(string? ruleSetName = null)
    {
      if (validationGate == null) return;

      // Note: The core ValidationGate doesn't track rule sources, so we can't selectively remove by set name
      // For now, we'll just clear all custom rules and reload NPC-specific rules
      // In a future enhancement, we could track rule sources in the gate
      validationGate.ClearRules();
      LoadValidationRules(); // Reload global + NPC rules
      UnityEngine.Debug.Log($"[LlamaBrainAgent] Cleared trigger validation rules{(ruleSetName != null ? $" for '{ruleSetName}'" : "")}");
    }
  }
}


