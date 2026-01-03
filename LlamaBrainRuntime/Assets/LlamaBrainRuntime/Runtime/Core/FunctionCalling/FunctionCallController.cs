#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LlamaBrain.Core.FunctionCalling;
using LlamaBrain.Core.Validation;
using LlamaBrain.Core.Inference;
using LlamaBrain.Persona;

namespace LlamaBrain.Runtime.Core.FunctionCalling
{
  /// <summary>
  /// Unity wrapper for the Function Call system.
  /// Wraps the core FunctionCallDispatcher and adds Unity-specific features
  /// (MonoBehaviour lifecycle, ScriptableObject configs, singleton pattern, UnityEvents).
  /// 
  /// Similar to ExpectancyEngine and WorldIntentDispatcher patterns.
  /// </summary>
  [AddComponentMenu("LlamaBrain/Function Call Controller")]
  public class FunctionCallController : MonoBehaviour
  {
    [Header("Global Function Configs")]
    [Tooltip("Global function configurations that apply to all NPCs.")]
    [SerializeField] private List<FunctionCallConfigAsset> globalFunctionConfigs = new List<FunctionCallConfigAsset>();

    [Header("Settings")]
    [Tooltip("Enable debug logging for function call execution.")]
    [SerializeField] private bool debugLogging = false;

    /// <summary>
    /// The core dispatcher that does the actual function call processing.
    /// Engine-agnostic and could be used by Unreal, Godot, etc.
    /// </summary>
    private FunctionCallDispatcher? _coreDispatcher;

    /// <summary>
    /// Singleton instance for global access.
    /// </summary>
    public static FunctionCallController? Instance { get; private set; }

    /// <summary>
    /// Access to the core dispatcher for advanced usage.
    /// </summary>
    public FunctionCallDispatcher? CoreDispatcher => _coreDispatcher;

    [Header("General Events")]
    [Tooltip("Event fired for all function calls. Parameters: FunctionCall, FunctionCallResult")]
    [SerializeField] private FunctionCallEvent onAnyFunctionCall = new FunctionCallEvent();

    [Tooltip("Event fired when function calls are executed. Parameters: Dictionary of function call results")]
    [SerializeField] private FunctionCallResultsEvent onFunctionCallsExecuted = new FunctionCallResultsEvent();

    [Header("Function-Specific Handlers")]
    [Tooltip("Handlers for specific function types")]
    [SerializeField] private List<FunctionHandlerConfig> functionHandlers = new List<FunctionHandlerConfig>();

    // Runtime state
    private readonly Dictionary<string, List<Action<FunctionCall, FunctionCallResult>>> codeHandlers
      = new Dictionary<string, List<Action<FunctionCall, FunctionCallResult>>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Event fired for any function call execution.
    /// </summary>
    public FunctionCallEvent OnAnyFunctionCall => onAnyFunctionCall;

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Debug.LogWarning("[FunctionCallController] Multiple instances detected. Destroying duplicate.");
        Destroy(gameObject);
        return;
      }
      Instance = this;

      // Initialize core dispatcher
      _coreDispatcher = new FunctionCallDispatcher();
      if (debugLogging)
      {
        Debug.Log("[FunctionCallController] Core dispatcher initialized");
      }

      // Register functions from ScriptableObject configs
      RegisterFunctionsFromConfigs();
    }

    private void OnDestroy()
    {
      if (Instance == this)
      {
        Instance = null;
      }
    }

    /// <summary>
    /// Registers functions from global FunctionCallConfigAsset list.
    /// </summary>
    private void RegisterFunctionsFromConfigs()
    {
      if (_coreDispatcher == null) return;

      // Sort by priority (higher priority first)
      var sortedConfigs = globalFunctionConfigs
        .Where(c => c != null && c.IsEnabled)
        .OrderByDescending(c => c.Priority)
        .ToList();

      foreach (var config in sortedConfigs)
      {
        try
        {
          // For now, we'll register a placeholder handler
          // In a full implementation, this would use reflection to find and call the handler method
          // or use UnityEvents configured in the inspector
          _coreDispatcher.RegisterFunction(
            config.FunctionName,
            call => FunctionCallResult.FailureResult($"Handler not implemented for {config.FunctionName}. Use RegisterFunction() to provide a handler."),
            config.Description,
            config.ParameterSchema
          );

          if (debugLogging)
          {
            Debug.Log($"[FunctionCallController] Registered function from config: {config.FunctionName}");
          }
        }
        catch (Exception ex)
        {
          Debug.LogError($"[FunctionCallController] Failed to register function {config.FunctionName}: {ex.Message}");
        }
      }
    }

    /// <summary>
    /// Registers a function handler programmatically.
    /// </summary>
    /// <param name="functionName">The name of the function</param>
    /// <param name="handler">The handler function that processes the call</param>
    /// <param name="description">Optional description of what the function does</param>
    /// <param name="parameterSchema">Optional JSON schema for function parameters</param>
    public void RegisterFunction(
      string functionName,
      Func<FunctionCall, FunctionCallResult> handler,
      string? description = null,
      string? parameterSchema = null)
    {
      if (_coreDispatcher == null)
      {
        Debug.LogError("[FunctionCallController] Cannot register function: Core dispatcher not initialized");
        return;
      }

      if (string.IsNullOrWhiteSpace(functionName))
      {
        Debug.LogError("[FunctionCallController] Cannot register function: Function name cannot be null or empty");
        return;
      }

      if (handler == null)
      {
        Debug.LogError("[FunctionCallController] Cannot register function: Handler cannot be null");
        return;
      }

      _coreDispatcher.RegisterFunction(functionName, handler, description, parameterSchema);

      if (debugLogging)
      {
        Debug.Log($"[FunctionCallController] Registered function: {functionName}");
      }
    }

    /// <summary>
    /// Unregisters a function handler.
    /// </summary>
    /// <param name="functionName">The name of the function to unregister</param>
    /// <returns>True if the function was found and removed</returns>
    public bool UnregisterFunction(string functionName)
    {
      if (_coreDispatcher == null)
      {
        Debug.LogError("[FunctionCallController] Cannot unregister function: Core dispatcher not initialized");
        return false;
      }

      var result = _coreDispatcher.UnregisterFunction(functionName);
      if (result && debugLogging)
      {
        Debug.Log($"[FunctionCallController] Unregistered function: {functionName}");
      }
      return result;
    }

    /// <summary>
    /// Executes function calls from a ParsedOutput.
    /// </summary>
    /// <param name="parsedOutput">The parsed output containing function calls</param>
    /// <param name="snapshot">The current state snapshot (for context)</param>
    /// <param name="memorySystem">Optional memory system (for memory queries)</param>
    /// <returns>A dictionary mapping call IDs (or function names) to results</returns>
    public Dictionary<string, FunctionCallResult> ExecuteFunctionCalls(
      ParsedOutput parsedOutput,
      StateSnapshot snapshot,
      AuthoritativeMemorySystem? memorySystem = null)
    {
      if (_coreDispatcher == null)
      {
        Debug.LogError("[FunctionCallController] Cannot execute function calls: Core dispatcher not initialized");
        return new Dictionary<string, FunctionCallResult>();
      }

      if (parsedOutput?.FunctionCalls == null || parsedOutput.FunctionCalls.Count == 0)
      {
        return new Dictionary<string, FunctionCallResult>();
      }

      var results = new Dictionary<string, FunctionCallResult>();

      for (int i = 0; i < parsedOutput.FunctionCalls.Count; i++)
      {
        var functionCall = parsedOutput.FunctionCalls[i];
        try
        {
          // Execute via core dispatcher
          var result = _coreDispatcher.DispatchCall(functionCall);
          
          // Build unique key: CallId > FunctionName_index > index
          var key = functionCall.CallId 
            ?? (!string.IsNullOrEmpty(functionCall.FunctionName) 
                ? $"{functionCall.FunctionName}_{i}" 
                : i.ToString());
          results[key] = result;

          // Fire general event
          onAnyFunctionCall?.Invoke(functionCall, result);

          // Fire function-specific Unity events
          foreach (var handler in functionHandlers)
          {
            if (string.Equals(handler.functionName, functionCall.FunctionName, StringComparison.OrdinalIgnoreCase))
            {
              handler.onFunctionCall?.Invoke(functionCall, result);
            }
          }

          // Fire code-registered handlers
          if (codeHandlers.TryGetValue(functionCall.FunctionName, out var handlers))
          {
            foreach (var handler in handlers)
            {
              try
              {
                handler(functionCall, result);
              }
              catch (Exception ex)
              {
                Debug.LogError($"[FunctionCallController] Code handler error for {functionCall.FunctionName}: {ex.Message}");
              }
            }
          }

          // Also check for wildcard handlers
          if (codeHandlers.TryGetValue("*", out var wildcardHandlers))
          {
            foreach (var handler in wildcardHandlers)
            {
              try
              {
                handler(functionCall, result);
              }
              catch (Exception ex)
              {
                Debug.LogError($"[FunctionCallController] Wildcard handler error: {ex.Message}");
              }
            }
          }

          if (debugLogging)
          {
            Debug.Log($"[FunctionCallController] Executed function call: {functionCall.FunctionName} -> {(result.Success ? "Success" : "Failed")}");
          }
        }
        catch (Exception ex)
        {
          var errorResult = FunctionCallResult.FailureResult($"Error executing function call: {ex.Message}", functionCall.CallId);
          var key = functionCall.CallId ?? functionCall.FunctionName;
          results[key] = errorResult;
          Debug.LogError($"[FunctionCallController] Error executing function call {functionCall.FunctionName}: {ex.Message}");
        }
      }

      // Fire batch event with all results
      if (results.Count > 0)
      {
        onFunctionCallsExecuted?.Invoke(results);
      }

      return results;
    }

    /// <summary>
    /// Registers a code-based handler for a specific function type.
    /// </summary>
    /// <param name="functionName">The function name to handle (case-insensitive). Use "*" for all functions.</param>
    /// <param name="handler">The handler action. Parameters: FunctionCall, FunctionCallResult.</param>
    public void RegisterHandler(string functionName, Action<FunctionCall, FunctionCallResult> handler)
    {
      var key = functionName.ToLowerInvariant();
      if (!codeHandlers.ContainsKey(key))
      {
        codeHandlers[key] = new List<Action<FunctionCall, FunctionCallResult>>();
      }
      codeHandlers[key].Add(handler);

      if (debugLogging)
      {
        Debug.Log($"[FunctionCallController] Registered code handler for: {functionName}");
      }
    }

    /// <summary>
    /// Unregisters a code-based handler.
    /// </summary>
    /// <param name="functionName">The function name.</param>
    /// <param name="handler">The handler to remove.</param>
    /// <returns>True if the handler was found and removed.</returns>
    public bool UnregisterHandler(string functionName, Action<FunctionCall, FunctionCallResult> handler)
    {
      var key = functionName.ToLowerInvariant();
      if (codeHandlers.TryGetValue(key, out var handlers))
      {
        return handlers.Remove(handler);
      }
      return false;
    }

    /// <summary>
    /// Gets the dispatcher instance (for advanced usage).
    /// </summary>
    /// <returns>The core FunctionCallDispatcher instance</returns>
    public FunctionCallDispatcher? GetDispatcher()
    {
      return _coreDispatcher;
    }

    /// <summary>
    /// Creates a default FunctionCallController if none exists.
    /// </summary>
    /// <returns>The singleton FunctionCallController instance, creating one if necessary</returns>
    public static FunctionCallController GetOrCreate()
    {
      if (Instance != null) return Instance;

      var go = new GameObject("FunctionCallController");
      return go.AddComponent<FunctionCallController>();
    }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
    /// <summary>
    /// Resets the singleton instance for testing purposes.
    /// Only available in editor and test builds.
    /// </summary>
    internal static void ResetForTests()
    {
      Instance = null;
    }
#endif
  }

  /// <summary>
  /// Configuration for a registered function handler.
  /// </summary>
  [Serializable]
  public class FunctionHandlerConfig
  {
    /// <summary>
    /// The function name to handle (e.g., "get_memories", "get_constraints").
    /// </summary>
    [Tooltip("The function name to handle (e.g., 'get_memories', 'get_constraints')")]
    public string functionName = "";

    /// <summary>
    /// Event fired when this function is called.
    /// Parameters: FunctionCall, FunctionCallResult
    /// </summary>
    [Tooltip("Event fired when this function is called")]
    public FunctionCallEvent onFunctionCall = new FunctionCallEvent();
  }
}
