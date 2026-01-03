using System;
using System.Collections.Generic;
using System.Linq;

namespace LlamaBrain.Core.FunctionCalling
{
  /// <summary>
  /// Dispatches function calls from LLM output to registered handlers.
  /// Similar to WorldIntentDispatcher but for function calls (LLM requesting context/data).
  /// 
  /// Pattern: command dispatch
  /// - Command table: Dictionary mapping function names to handlers
  /// - Command invoker: DispatchCall() method
  /// - Concrete commands: Registered function handlers
  /// - Receiver: Context providers, memory systems, etc.
  /// </summary>
  public class FunctionCallDispatcher
  {
    private readonly Dictionary<string, Func<FunctionCall, FunctionCallResult>> _functionTable;
    private readonly Dictionary<string, FunctionCallMetadata> _functionMetadata;

    /// <summary>
    /// Creates a new function call dispatcher.
    /// </summary>
    public FunctionCallDispatcher()
    {
      _functionTable = new Dictionary<string, Func<FunctionCall, FunctionCallResult>>(StringComparer.OrdinalIgnoreCase);
      _functionMetadata = new Dictionary<string, FunctionCallMetadata>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Registers a function handler (like command table entry).
    /// </summary>
    /// <param name="functionName">The name of the function (case-insensitive)</param>
    /// <param name="handler">The handler function that processes the call</param>
    /// <param name="description">Optional description of what the function does</param>
    /// <param name="parameterSchema">Optional JSON schema for function parameters</param>
    public void RegisterFunction(
        string functionName,
        Func<FunctionCall, FunctionCallResult> handler,
        string? description = null,
        string? parameterSchema = null)
    {
      if (string.IsNullOrWhiteSpace(functionName))
        throw new ArgumentException("Function name cannot be null or empty", nameof(functionName));
      if (handler == null)
        throw new ArgumentNullException(nameof(handler));

      var key = functionName.ToLowerInvariant();
      _functionTable[key] = handler;
      _functionMetadata[key] = new FunctionCallMetadata
      {
        FunctionName = functionName,
        Description = description ?? "",
        ParameterSchema = parameterSchema
      };
    }

    /// <summary>
    /// Unregisters a function handler.
    /// </summary>
    /// <param name="functionName">The name of the function to unregister</param>
    /// <returns>True if the function was found and removed</returns>
    public bool UnregisterFunction(string functionName)
    {
      if (string.IsNullOrWhiteSpace(functionName))
        return false;

      var key = functionName.ToLowerInvariant();
      var removed = _functionTable.Remove(key);
      _functionMetadata.Remove(key);
      return removed;
    }

    /// <summary>
    /// Dispatches a function call to the appropriate handler (command dispatch pattern).
    /// </summary>
    /// <param name="functionCall">The function call to dispatch</param>
    /// <returns>The result of executing the function</returns>
    public FunctionCallResult DispatchCall(FunctionCall functionCall)
    {
      if (functionCall == null)
        return FunctionCallResult.FailureResult("Function call cannot be null");

      if (string.IsNullOrWhiteSpace(functionCall.FunctionName))
        return FunctionCallResult.FailureResult("Function name cannot be null or empty", functionCall.CallId);

      var key = functionCall.FunctionName.ToLowerInvariant();
      if (!_functionTable.TryGetValue(key, out var handler))
      {
        return FunctionCallResult.FailureResult(
            $"Function '{functionCall.FunctionName}' is not registered. Available functions: {string.Join(", ", GetRegisteredFunctionNames())}",
            functionCall.CallId);
      }

      try
      {
        return handler(functionCall);
      }
      catch (Exception ex)
      {
        return FunctionCallResult.FailureResult(
            $"Error executing function '{functionCall.FunctionName}': {ex.Message}",
            functionCall.CallId);
      }
    }

    /// <summary>
    /// Dispatches multiple function calls and returns all results.
    /// </summary>
    /// <param name="functionCalls">The function calls to dispatch</param>
    /// <returns>A list of results, one per function call</returns>
    public List<FunctionCallResult> DispatchCalls(IEnumerable<FunctionCall> functionCalls)
    {
      if (functionCalls == null)
        throw new ArgumentNullException(nameof(functionCalls));

      var results = new List<FunctionCallResult>();
      foreach (var call in functionCalls)
      {
        results.Add(DispatchCall(call));
      }
      return results;
    }

    /// <summary>
    /// Gets metadata for a registered function.
    /// </summary>
    /// <param name="functionName">The function name</param>
    /// <returns>Function metadata, or null if not found</returns>
    public FunctionCallMetadata? GetFunctionMetadata(string functionName)
    {
      if (string.IsNullOrWhiteSpace(functionName))
        return null;

      var key = functionName.ToLowerInvariant();
      return _functionMetadata.TryGetValue(key, out var metadata) ? metadata : null;
    }

    /// <summary>
    /// Gets all registered function names.
    /// </summary>
    /// <returns>A list of all registered function names</returns>
    public List<string> GetRegisteredFunctionNames()
    {
      return _functionMetadata.Values.Select(m => m.FunctionName).OrderBy(n => n).ToList();
    }

    /// <summary>
    /// Gets metadata for all registered functions.
    /// </summary>
    /// <returns>A list of all function metadata</returns>
    public List<FunctionCallMetadata> GetAllFunctionMetadata()
    {
      return _functionMetadata.Values.OrderBy(m => m.FunctionName).ToList();
    }

    /// <summary>
    /// Checks if a function is registered.
    /// </summary>
    /// <param name="functionName">The function name to check</param>
    /// <returns>True if the function is registered</returns>
    public bool IsFunctionRegistered(string functionName)
    {
      if (string.IsNullOrWhiteSpace(functionName))
        return false;

      var key = functionName.ToLowerInvariant();
      return _functionTable.ContainsKey(key);
    }

    /// <summary>
    /// Clears all registered functions.
    /// </summary>
    public void Clear()
    {
      _functionTable.Clear();
      _functionMetadata.Clear();
    }
  }

  /// <summary>
  /// Metadata about a registered function (for schema generation, documentation, etc.).
  /// </summary>
  public class FunctionCallMetadata
  {
    /// <summary>
    /// The function name.
    /// </summary>
    public string FunctionName { get; set; } = "";

    /// <summary>
    /// Description of what the function does.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// JSON schema for function parameters (optional).
    /// </summary>
    public string? ParameterSchema { get; set; }
  }
}
