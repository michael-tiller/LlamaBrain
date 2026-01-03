using System;
using System.Collections.Generic;
using System.Linq;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Persona;

namespace LlamaBrain.Core.FunctionCalling
{
  /// <summary>
  /// Executes function calls from parsed output and returns results.
  /// Integrates with the pipeline to handle function calls from LLM responses.
  /// </summary>
  public class FunctionCallExecutor
  {
    private readonly FunctionCallDispatcher _dispatcher;

    /// <summary>
    /// Creates a new function call executor.
    /// </summary>
    /// <param name="dispatcher">The function call dispatcher with registered functions</param>
    public FunctionCallExecutor(FunctionCallDispatcher dispatcher)
    {
      _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <summary>
    /// Executes all function calls from parsed output and returns results.
    /// </summary>
    /// <param name="parsedOutput">The parsed output containing function calls</param>
    /// <returns>A dictionary mapping call IDs (or function names) to results</returns>
    public Dictionary<string, FunctionCallResult> ExecuteAll(Validation.ParsedOutput parsedOutput)
    {
      var results = new Dictionary<string, FunctionCallResult>();

      if (parsedOutput?.FunctionCalls == null || parsedOutput.FunctionCalls.Count == 0)
      {
        return results;
      }

      for (var i = 0; i < parsedOutput.FunctionCalls.Count; i++)
      {
        var functionCall = parsedOutput.FunctionCalls[i];
        var result = _dispatcher.DispatchCall(functionCall);

        var key = !string.IsNullOrEmpty(functionCall.CallId)
            ? functionCall.CallId
            : !string.IsNullOrEmpty(functionCall.FunctionName)
                ? functionCall.FunctionName
                : throw new ArgumentException(
                    $"Function call at index {i} has both null/empty CallId and FunctionName.",
                    nameof(parsedOutput));

        results[key] = result;
      }

      return results;
    }

    /// <summary>
    /// Executes a single function call.
    /// </summary>
    /// <param name="functionCall">The function call to execute</param>
    /// <returns>The result of executing the function</returns>
    public FunctionCallResult Execute(FunctionCall functionCall)
    {
      return _dispatcher.DispatchCall(functionCall);
    }

    /// <summary>
    /// Creates a function call executor with built-in context functions registered.
    /// </summary>
    /// <param name="snapshot">The current state snapshot</param>
    /// <param name="memorySystem">Optional memory system</param>
    /// <returns>A configured FunctionCallExecutor with built-in functions registered</returns>
    public static FunctionCallExecutor CreateWithBuiltIns(
        StateSnapshot snapshot,
        AuthoritativeMemorySystem? memorySystem = null)
    {
      var dispatcher = new FunctionCallDispatcher();
      BuiltInContextFunctions.RegisterAll(dispatcher, snapshot, memorySystem);
      return new FunctionCallExecutor(dispatcher);
    }
  }
}
