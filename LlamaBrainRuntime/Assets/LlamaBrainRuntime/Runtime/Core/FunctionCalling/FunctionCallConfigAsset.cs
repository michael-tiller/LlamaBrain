using System;
using UnityEngine;

namespace LlamaBrain.Runtime.Core.FunctionCalling
{
  /// <summary>
  /// ScriptableObject-based function call configuration for declarative function definition in the Unity Editor.
  /// Create these in your project to define functions without writing code.
  /// Similar to ExpectancyRuleAsset but for function calls.
  /// </summary>
  [CreateAssetMenu(fileName = "New Function Call Config", menuName = "LlamaBrain/Function Call Config")]
  public class FunctionCallConfigAsset : ScriptableObject
  {
    [Header("Function Identity")]
    [Tooltip("The name of the function (e.g., 'get_memories', 'get_constraints').")]
    [SerializeField] private string functionName = "";

    [TextArea(2, 4)]
    [Tooltip("Description of what this function does.")]
    [SerializeField] private string description = "";

    [Header("Function Settings")]
    [Tooltip("Whether this function is currently enabled.")]
    [SerializeField] private bool isEnabled = true;

    [Tooltip("Priority/ordering for function registration (higher priority = registered first).")]
    [SerializeField] private int priority = 0;

    [Header("Parameter Schema")]
    [TextArea(3, 8)]
    [Tooltip("JSON schema for function parameters (optional). Used for LLM function calling.")]
    [SerializeField] private string parameterSchema = "";

    [Header("Handler Configuration")]
    [Tooltip("Method name to call (optional - can use UnityEvent instead).")]
    [SerializeField] private string handlerMethod = "";

    /// <summary>
    /// Gets the function name.
    /// </summary>
    public string FunctionName => string.IsNullOrEmpty(functionName) ? name : functionName;

    /// <summary>
    /// Gets the description of the function.
    /// </summary>
    public string Description => description;

    /// <summary>
    /// Gets whether this function is enabled.
    /// </summary>
    public bool IsEnabled => isEnabled;

    /// <summary>
    /// Gets the priority of this function.
    /// </summary>
    public int Priority => priority;

    /// <summary>
    /// Gets the parameter schema JSON.
    /// </summary>
    public string ParameterSchema => parameterSchema;

    /// <summary>
    /// Gets the handler method name.
    /// </summary>
    public string HandlerMethod => handlerMethod;

    private void Reset()
    {
      functionName = "new_function";
      description = "A new function call handler";
      isEnabled = true;
      priority = 0;
    }
  }
}
