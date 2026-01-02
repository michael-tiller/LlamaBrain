using System;
using System.Collections.Generic;

namespace LlamaBrain.Core.FunctionCalling
{
  /// <summary>
  /// A function call request from the LLM.
  /// The LLM outputs these in structured JSON, and we dispatch them to registered handlers.
  /// </summary>
  [Serializable]
  public class FunctionCall
  {
    /// <summary>
    /// The name of the function to call (e.g., "get_memories", "get_constraints").
    /// </summary>
    public string FunctionName { get; set; } = "";

    /// <summary>
    /// Arguments for the function call as key-value pairs.
    /// </summary>
    public Dictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Optional call ID for tracking multiple function calls in a single response.
    /// </summary>
    public string? CallId { get; set; }

    /// <summary>
    /// Creates a new function call.
    /// </summary>
    /// <param name="functionName">The name of the function to call</param>
    /// <param name="arguments">Optional arguments for the function</param>
    /// <param name="callId">Optional call ID for tracking</param>
    /// <returns>A new FunctionCall instance</returns>
    public static FunctionCall Create(string functionName, Dictionary<string, object>? arguments = null, string? callId = null)
    {
      return new FunctionCall
      {
        FunctionName = functionName,
        Arguments = arguments ?? new Dictionary<string, object>(),
        CallId = callId
      };
    }

    /// <summary>
    /// Gets an argument value as a string.
    /// </summary>
    /// <param name="key">The argument key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>The argument value as a string</returns>
    public string GetArgumentString(string key, string defaultValue = "")
    {
      if (Arguments.TryGetValue(key, out var value))
      {
        return value?.ToString() ?? defaultValue;
      }
      return defaultValue;
    }

    /// <summary>
    /// Gets an argument value as an integer.
    /// </summary>
    /// <param name="key">The argument key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>The argument value as an integer</returns>
    public int GetArgumentInt(string key, int defaultValue = 0)
    {
      if (Arguments.TryGetValue(key, out var value))
      {
        if (value is int intValue)
          return intValue;
        if (int.TryParse(value?.ToString(), out var parsed))
          return parsed;
      }
      return defaultValue;
    }

    /// <summary>
    /// Gets an argument value as a boolean.
    /// </summary>
    /// <param name="key">The argument key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>The argument value as a boolean</returns>
    public bool GetArgumentBool(string key, bool defaultValue = false)
    {
      if (Arguments.TryGetValue(key, out var value))
      {
        if (value is bool boolValue)
          return boolValue;
        if (bool.TryParse(value?.ToString(), out var parsed))
          return parsed;
      }
      return defaultValue;
    }

    /// <summary>
    /// Gets an argument value as a double.
    /// </summary>
    /// <param name="key">The argument key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>The argument value as a double</returns>
    public double GetArgumentDouble(string key, double defaultValue = 0.0)
    {
      if (Arguments.TryGetValue(key, out var value))
      {
        if (value is double doubleValue)
          return doubleValue;
        if (value is float floatValue)
          return floatValue;
        if (double.TryParse(value?.ToString(), out var parsed))
          return parsed;
      }
      return defaultValue;
    }

    /// <summary>
    /// Returns a string representation of this function call.
    /// </summary>
    /// <returns>A string representation of the function call</returns>
    public override string ToString()
    {
      return $"FunctionCall[{FunctionName}]({Arguments.Count} args)";
    }
  }
}
