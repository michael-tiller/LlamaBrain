using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LlamaBrain.Core.FunctionCalling;

namespace LlamaBrain.Runtime.Core.FunctionCalling
{
  /// <summary>
  /// Component that holds NPC-specific function call configurations.
  /// Attach this to NPCs to give them custom function handlers.
  /// Similar to NpcExpectancyConfig pattern.
  /// </summary>
  public class NpcFunctionCallConfig : MonoBehaviour
  {
    [Header("NPC-Specific Function Configs")]
    [Tooltip("Function configurations that only apply to this NPC.")]
    [SerializeField] private List<FunctionCallConfigAsset> npcFunctionConfigs = new List<FunctionCallConfigAsset>();

    [Header("Settings")]
    [Tooltip("Override NPC ID used for function call context. If empty, uses GameObject name.")]
    [SerializeField] private string npcIdOverride;

    /// <summary>
    /// The NPC ID used for function call context.
    /// </summary>
    public string NpcId => string.IsNullOrEmpty(npcIdOverride) ? gameObject.name : npcIdOverride;

    /// <summary>
    /// The NPC-specific function configurations.
    /// </summary>
    public IReadOnlyList<FunctionCallConfigAsset> FunctionConfigs => npcFunctionConfigs;

    /// <summary>
    /// Registers NPC-specific functions with the FunctionCallController.
    /// Should be called during initialization (e.g., from LlamaBrainAgent.Initialize).
    /// </summary>
    /// <param name="controller">The FunctionCallController to register functions with</param>
    public void RegisterFunctionsWithController(FunctionCallController controller)
    {
      if (controller == null)
      {
        Debug.LogWarning($"[NpcFunctionCallConfig] Cannot register functions: FunctionCallController is null");
        return;
      }

      // Sort by priority (higher priority first)
      var sortedConfigs = npcFunctionConfigs
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
          controller.RegisterFunction(
            config.FunctionName,
            call => FunctionCallResult.FailureResult($"NPC-specific handler not implemented for {config.FunctionName}. Use RegisterFunction() to provide a handler."),
            config.Description,
            config.ParameterSchema
          );

          Debug.Log($"[NpcFunctionCallConfig] Registered NPC-specific function: {config.FunctionName} for {NpcId}");
        }
        catch (System.Exception ex)
        {
          Debug.LogError($"[NpcFunctionCallConfig] Failed to register function {config.FunctionName} for {NpcId}: {ex.Message}");
        }
      }
    }
  }
}
