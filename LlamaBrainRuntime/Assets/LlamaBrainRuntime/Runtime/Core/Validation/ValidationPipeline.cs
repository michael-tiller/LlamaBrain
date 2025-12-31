#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Validation;
using LlamaBrain.Core.Inference;
using LlamaBrain.Persona;

namespace LlamaBrain.Runtime.Core.Validation
{
  /// <summary>
  /// Configuration for the validation pipeline (Unity inspector-friendly).
  /// </summary>
  [Serializable]
  public class ValidationPipelineSettings
  {
    [Header("Output Parsing")]
    /// <summary>
    /// Enforce single-line dialogue output.
    /// </summary>
    [Tooltip("Enforce single-line dialogue output")]
    public bool enforceSingleLine = true;

    /// <summary>
    /// Remove stage directions (text in asterisks).
    /// </summary>
    [Tooltip("Remove stage directions (text in asterisks)")]
    public bool removeStageDirections = true;

    /// <summary>
    /// Remove script directions (text in brackets).
    /// </summary>
    [Tooltip("Remove script directions (text in brackets)")]
    public bool removeScriptDirections = true;

    /// <summary>
    /// Attempt to extract structured data from output.
    /// </summary>
    [Tooltip("Attempt to extract structured data from output")]
    public bool extractStructuredData = true;

    [Header("Validation")]
    /// <summary>
    /// Check constraints from the expectancy engine.
    /// </summary>
    [Tooltip("Check constraints from the expectancy engine")]
    public bool checkConstraints = true;

    /// <summary>
    /// Check for canonical fact contradictions.
    /// </summary>
    [Tooltip("Check for canonical fact contradictions")]
    public bool checkCanonicalFacts = true;

    /// <summary>
    /// Check knowledge boundaries.
    /// </summary>
    [Tooltip("Check knowledge boundaries")]
    public bool checkKnowledgeBoundaries = true;

    /// <summary>
    /// Validate proposed memory mutations.
    /// </summary>
    [Tooltip("Validate proposed memory mutations")]
    public bool validateMutations = true;

    [Header("Debug")]
    /// <summary>
    /// Enable verbose logging.
    /// </summary>
    [Tooltip("Enable verbose logging")]
    public bool verboseLogging = false;

    /// <summary>
    /// Converts to core OutputParserConfig.
    /// </summary>
    /// <returns>An OutputParserConfig instance with the values from these settings</returns>
    public OutputParserConfig ToParserConfig()
    {
      return new OutputParserConfig
      {
        EnforceSingleLine = enforceSingleLine,
        RemoveStageDirections = removeStageDirections,
        RemoveScriptDirections = removeScriptDirections,
        ExtractStructuredData = extractStructuredData
      };
    }

    /// <summary>
    /// Converts to core ValidationGateConfig.
    /// </summary>
    /// <returns>A ValidationGateConfig instance with the values from these settings</returns>
    public ValidationGateConfig ToGateConfig()
    {
      return new ValidationGateConfig
      {
        CheckConstraints = checkConstraints,
        CheckCanonicalFacts = checkCanonicalFacts,
        CheckKnowledgeBoundaries = checkKnowledgeBoundaries,
        ValidateMutations = validateMutations
      };
    }
  }

  /// <summary>
  /// Unity component that provides a complete validation pipeline.
  /// Combines OutputParser and ValidationGate with Unity-friendly configuration.
  /// </summary>
  public class ValidationPipeline : MonoBehaviour
  {
    [Header("Configuration")]
    [SerializeField] private ValidationPipelineSettings settings = new ValidationPipelineSettings();

    [Header("Global Rules")]
    [Tooltip("Global validation rule sets applied to all NPCs")]
    [SerializeField] private List<ValidationRuleSetAsset> globalRuleSets = new List<ValidationRuleSetAsset>();

    [Header("Knowledge Boundaries")]
    [Tooltip("Topics that NPCs should not reveal knowledge about")]
    [SerializeField] private List<string> forbiddenKnowledge = new List<string>();

    // Core components
    private OutputParser? parser;
    private ValidationGate? gate;

    // Singleton pattern for global access
    private static ValidationPipeline? instance;
    
    /// <summary>
    /// Gets the singleton instance of the ValidationPipeline.
    /// </summary>
    public static ValidationPipeline? Instance => instance;

    /// <summary>
    /// The last parsed output (for debugging).
    /// </summary>
    public ParsedOutput? LastParsedOutput { get; private set; }

    /// <summary>
    /// The last gate result (for debugging).
    /// </summary>
    public GateResult? LastGateResult { get; private set; }

    /// <summary>
    /// Event fired when validation fails.
    /// </summary>
    public event Action<GateResult>? OnValidationFailed;

    /// <summary>
    /// Event fired when validation passes.
    /// </summary>
    public event Action<GateResult>? OnValidationPassed;

    private void Awake()
    {
      if (instance == null)
      {
        instance = this;
      }
      else if (instance != this)
      {
        Debug.LogWarning("[ValidationPipeline] Multiple instances detected. Using existing instance.");
      }

      Initialize();
    }

    private void OnDestroy()
    {
      if (instance == this)
      {
        instance = null;
      }
    }

    /// <summary>
    /// Initializes the pipeline components.
    /// </summary>
    public void Initialize()
    {
      parser = new OutputParser(settings.ToParserConfig());
      gate = new ValidationGate(settings.ToGateConfig());

      if (settings.verboseLogging)
      {
        parser.OnLog = msg => Debug.Log(msg);
        gate.OnLog = msg => Debug.Log(msg);
      }

      // Add global rules
      LoadGlobalRules();

      Debug.Log("[ValidationPipeline] Initialized");
    }

    /// <summary>
    /// Loads global validation rules into the gate.
    /// </summary>
    private void LoadGlobalRules()
    {
      if (gate == null) return;

      foreach (var ruleSet in globalRuleSets)
      {
        if (ruleSet != null && ruleSet.Enabled)
        {
          foreach (var rule in ruleSet.ToValidationRules())
          {
            gate.AddRule(rule);
          }
        }
      }
    }

    /// <summary>
    /// Processes raw LLM output through the full validation pipeline.
    /// </summary>
    /// <param name="rawOutput">The raw LLM output</param>
    /// <param name="wasTruncated">Whether the output was truncated</param>
    /// <param name="context">Optional validation context</param>
    /// <returns>The gate result with validated output or failures</returns>
    public GateResult Process(string rawOutput, bool wasTruncated = false, ValidationContext? context = null)
    {
      if (parser == null || gate == null)
      {
        Initialize();
      }

      // Step 1: Parse
      var parsed = parser!.Parse(rawOutput, wasTruncated);
      LastParsedOutput = parsed;

      if (!parsed.Success)
      {
        var failResult = GateResult.Fail(new ValidationFailure
        {
          Reason = ValidationFailureReason.InvalidFormat,
          Description = parsed.ErrorMessage ?? "Parsing failed"
        });
        LastGateResult = failResult;
        OnValidationFailed?.Invoke(failResult);
        return failResult;
      }

      // Step 2: Build context with forbidden knowledge
      context ??= new ValidationContext();
      if (forbiddenKnowledge.Count > 0)
      {
        context.ForbiddenKnowledge = new List<string>(forbiddenKnowledge);
      }

      // Step 3: Validate
      var result = gate!.Validate(parsed, context);
      LastGateResult = result;

      if (result.Passed)
      {
        OnValidationPassed?.Invoke(result);
      }
      else
      {
        OnValidationFailed?.Invoke(result);
      }

      return result;
    }

    /// <summary>
    /// Processes output with context from a state snapshot.
    /// </summary>
    /// <param name="rawOutput">The raw LLM output to process</param>
    /// <param name="snapshot">The state snapshot containing constraints and context</param>
    /// <param name="memorySystem">Optional memory system for canonical fact checking</param>
    /// <param name="wasTruncated">Whether the output was truncated</param>
    /// <returns>The gate result with validated output or failures</returns>
    public GateResult ProcessWithSnapshot(string rawOutput, StateSnapshot snapshot, AuthoritativeMemorySystem? memorySystem = null, bool wasTruncated = false)
    {
      var context = new ValidationContext
      {
        Constraints = snapshot.Constraints,
        MemorySystem = memorySystem,
        Snapshot = snapshot,
        ForbiddenKnowledge = new List<string>(forbiddenKnowledge)
      };

      return Process(rawOutput, wasTruncated, context);
    }

    /// <summary>
    /// Adds a custom validation rule.
    /// </summary>
    /// <param name="rule">The validation rule to add</param>
    public void AddRule(ValidationRule rule)
    {
      if (gate == null) Initialize();
      gate!.AddRule(rule);
    }

    /// <summary>
    /// Removes a validation rule by ID.
    /// </summary>
    /// <param name="ruleId">The ID of the rule to remove</param>
    /// <returns>True if the rule was found and removed, false otherwise</returns>
    public bool RemoveRule(string ruleId)
    {
      return gate?.RemoveRule(ruleId) ?? false;
    }

    /// <summary>
    /// Clears all custom validation rules (global rules remain).
    /// </summary>
    public void ClearCustomRules()
    {
      gate?.ClearRules();
      LoadGlobalRules();
    }

    /// <summary>
    /// Adds a forbidden knowledge topic at runtime.
    /// </summary>
    /// <param name="topic">The topic that NPCs should not reveal knowledge about</param>
    public void AddForbiddenKnowledge(string topic)
    {
      if (!forbiddenKnowledge.Contains(topic))
      {
        forbiddenKnowledge.Add(topic);
      }
    }

    /// <summary>
    /// Removes a forbidden knowledge topic at runtime.
    /// </summary>
    /// <param name="topic">The topic to remove from the forbidden knowledge list</param>
    /// <returns>True if the topic was found and removed, false otherwise</returns>
    public bool RemoveForbiddenKnowledge(string topic)
    {
      return forbiddenKnowledge.Remove(topic);
    }

    /// <summary>
    /// Gets current settings (for debugging).
    /// </summary>
    public ValidationPipelineSettings Settings => settings;
  }
}
