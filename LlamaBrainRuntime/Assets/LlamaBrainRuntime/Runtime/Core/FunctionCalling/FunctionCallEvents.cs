using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using LlamaBrain.Core.FunctionCalling;

namespace LlamaBrain.Runtime.Core.FunctionCalling
{
  /// <summary>
  /// Unity event for function call execution.
  /// Parameters: FunctionCall, FunctionCallResult
  /// </summary>
  [Serializable]
  public class FunctionCallEvent : UnityEvent<FunctionCall, FunctionCallResult> { }

  /// <summary>
  /// Unity event for function call results by function name.
  /// Parameters: functionName (string), FunctionCallResult
  /// </summary>
  [Serializable]
  public class FunctionCallResultEvent : UnityEvent<string, FunctionCallResult> { }

  /// <summary>
  /// Unity event for batch function call execution results.
  /// Parameters: Dictionary mapping function call IDs/names to their results
  /// </summary>
  [Serializable]
  public class FunctionCallResultsEvent : UnityEvent<Dictionary<string, FunctionCallResult>> { }
}
