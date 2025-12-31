using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Runtime.Core.Expectancy
{
  /// <summary>
  /// Unity wrapper for the Expectancy Engine.
  /// Wraps the core ExpectancyEvaluator and adds Unity-specific features
  /// (MonoBehaviour lifecycle, ScriptableObject rules, singleton pattern).
  /// </summary>
  public class ExpectancyEngine : MonoBehaviour
  {
    [Header("Global Rules")]
    [Tooltip("Rules that apply to all NPCs.")]
    [SerializeField] private List<ExpectancyRuleAsset> globalRules = new List<ExpectancyRuleAsset>();

    [Header("Settings")]
    [Tooltip("Enable debug logging for rule evaluation.")]
    [SerializeField] private bool debugLogging = false;

    /// <summary>
    /// The core evaluator that does the actual rule processing.
    /// Engine-agnostic and could be used by Unreal, Godot, etc.
    /// </summary>
    private ExpectancyEvaluator _coreEvaluator;

    /// <summary>
    /// Singleton instance for global access.
    /// </summary>
    public static ExpectancyEngine Instance { get; private set; }

    /// <summary>
    /// Access to the core evaluator for advanced usage.
    /// </summary>
    public ExpectancyEvaluator CoreEvaluator => _coreEvaluator;

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Debug.LogWarning("[ExpectancyEngine] Multiple instances detected. Destroying duplicate.");
        Destroy(gameObject);
        return;
      }
      Instance = this;

      // Initialize core evaluator
      _coreEvaluator = new ExpectancyEvaluator();
      if (debugLogging)
      {
        _coreEvaluator.OnLog = msg => Debug.Log(msg);
      }
    }

    private void OnDestroy()
    {
      if (Instance == this)
      {
        Instance = null;
      }
    }

    /// <summary>
    /// Registers a code-based rule.
    /// </summary>
    /// <param name="rule">The expectancy rule to register</param>
    public void RegisterRule(IExpectancyRule rule)
    {
      _coreEvaluator.RegisterRule(rule);
    }

    /// <summary>
    /// Unregisters a code-based rule.
    /// </summary>
    /// <param name="ruleId">The ID of the rule to unregister</param>
    public void UnregisterRule(string ruleId)
    {
      _coreEvaluator.UnregisterRule(ruleId);
    }

    /// <summary>
    /// Evaluates all rules against the given context and returns the resulting constraints.
    /// </summary>
    /// <param name="context">The interaction context to evaluate.</param>
    /// <param name="npcRules">Optional NPC-specific rules (from the NPC's configuration).</param>
    /// <returns>A ConstraintSet containing all applicable constraints.</returns>
    public ConstraintSet Evaluate(InteractionContext context, IEnumerable<ExpectancyRuleAsset> npcRules = null)
    {
      if (debugLogging)
      {
        Debug.Log($"[ExpectancyEngine] Evaluating context: {context}");
      }

      // Collect all rules
      var allRules = new List<IExpectancyRule>();

      // Add global ScriptableObject rules
      foreach (var rule in globalRules)
      {
        if (rule != null) allRules.Add(rule);
      }

      // Add NPC-specific rules
      if (npcRules != null)
      {
        foreach (var rule in npcRules)
        {
          if (rule != null) allRules.Add(rule);
        }
      }

      // Use core evaluator with combined rules
      var constraints = _coreEvaluator.Evaluate(context, allRules);

      if (debugLogging)
      {
        Debug.Log($"[ExpectancyEngine] Result: {constraints}");
      }

      return constraints;
    }

    /// <summary>
    /// Creates a default ExpectancyEngine if none exists.
    /// </summary>
    /// <returns>The singleton ExpectancyEngine instance, creating one if necessary</returns>
    public static ExpectancyEngine GetOrCreate()
    {
      if (Instance != null) return Instance;

      var go = new GameObject("ExpectancyEngine");
      return go.AddComponent<ExpectancyEngine>();
    }

    /// <summary>
    /// Static evaluation method for when no MonoBehaviour instance is available.
    /// Uses the singleton instance if available, otherwise evaluates without global rules.
    /// </summary>
    /// <param name="context">The interaction context to evaluate</param>
    /// <param name="npcRules">Optional NPC-specific rules to include in evaluation</param>
    /// <returns>A ConstraintSet containing all applicable constraints</returns>
    public static ConstraintSet EvaluateStatic(InteractionContext context, IEnumerable<ExpectancyRuleAsset> npcRules = null)
    {
      if (Instance != null)
      {
        return Instance.Evaluate(context, npcRules);
      }

      // No instance - create a temporary evaluator for NPC rules only
      var tempEvaluator = new ExpectancyEvaluator();
      var rulesList = npcRules?.Cast<IExpectancyRule>() ?? Enumerable.Empty<IExpectancyRule>();
      return tempEvaluator.Evaluate(context, rulesList);
    }
  }
}
