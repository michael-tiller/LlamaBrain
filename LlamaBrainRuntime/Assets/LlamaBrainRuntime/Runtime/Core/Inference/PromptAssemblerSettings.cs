using UnityEngine;
using LlamaBrain.Core.Inference;

namespace LlamaBrain.Runtime.Core.Inference
{
  /// <summary>
  /// Unity ScriptableObject for configuring the PromptAssembler.
  /// </summary>
  [CreateAssetMenu(fileName = "PromptAssemblerSettings", menuName = "LlamaBrain/Prompt Assembler Settings")]
  public class PromptAssemblerSettings : ScriptableObject
  {
    [Header("Token Limits")]
    [Tooltip("Maximum total tokens for the prompt (estimated).")]
    [Range(512, 8192)]
    [SerializeField] private int maxPromptTokens = 2048;

    [Tooltip("Reserve tokens for the response.")]
    [Range(64, 1024)]
    [SerializeField] private int reserveResponseTokens = 256;

    [Tooltip("Average characters per token for estimation (4 is typical for English).")]
    [Range(2f, 6f)]
    [SerializeField] private float charsPerToken = 4.0f;

    [Header("Working Memory Limits")]
    [Tooltip("Maximum number of dialogue exchanges to include.")]
    [Range(1, 20)]
    [SerializeField] private int maxDialogueExchanges = 5;

    [Tooltip("Maximum number of episodic memories to include.")]
    [Range(0, 20)]
    [SerializeField] private int maxEpisodicMemories = 5;

    [Tooltip("Maximum number of beliefs to include.")]
    [Range(0, 10)]
    [SerializeField] private int maxBeliefs = 3;

    [Tooltip("Maximum total characters for context (soft limit).")]
    [Range(500, 10000)]
    [SerializeField] private int maxContextCharacters = 2000;

    [Header("Content Inclusion")]
    [Tooltip("Always include all canonical facts (ignores character limit).")]
    [SerializeField] private bool alwaysIncludeCanonicalFacts = true;

    [Tooltip("Always include all world state (ignores character limit).")]
    [SerializeField] private bool alwaysIncludeWorldState = true;

    [Tooltip("Include constraint text in the prompt.")]
    [SerializeField] private bool includeConstraints = true;

    [Tooltip("Include retry feedback in the prompt.")]
    [SerializeField] private bool includeRetryFeedback = true;

    [Header("Format Strings")]
    [Tooltip("Format for system prompt. {0} = system prompt text.")]
    [SerializeField] private string systemPromptFormat = "System: {0}";

    [Tooltip("Header for context section.")]
    [SerializeField] private string contextHeader = "\n[Context]";

    [Tooltip("Header for conversation section.")]
    [SerializeField] private string conversationHeader = "\n[Conversation]";

    [Tooltip("Format for player input. {0} = player input text.")]
    [SerializeField] private string playerInputFormat = "\nPlayer: {0}";

    [Tooltip("Format for NPC response prompt. {0} = NPC name.")]
    [SerializeField] private string npcPromptFormat = "\n{0}:";

    [Tooltip("Default NPC name if not specified.")]
    [SerializeField] private string defaultNpcName = "NPC";

    /// <summary>
    /// Creates a PromptAssemblerConfig from these settings.
    /// </summary>
    /// <returns>A PromptAssemblerConfig instance with the values from these settings</returns>
    public PromptAssemblerConfig ToConfig()
    {
      return new PromptAssemblerConfig
      {
        MaxPromptTokens = maxPromptTokens,
        ReserveResponseTokens = reserveResponseTokens,
        CharsPerToken = charsPerToken,
        IncludeConstraints = includeConstraints,
        IncludeRetryFeedback = includeRetryFeedback,
        SystemPromptFormat = systemPromptFormat,
        ContextHeader = contextHeader,
        ConversationHeader = conversationHeader,
        PlayerInputFormat = playerInputFormat,
        NpcPromptFormat = npcPromptFormat,
        DefaultNpcName = defaultNpcName
      };
    }

    /// <summary>
    /// Creates a WorkingMemoryConfig from these settings.
    /// </summary>
    /// <returns>A WorkingMemoryConfig instance with the values from these settings</returns>
    public WorkingMemoryConfig ToWorkingMemoryConfig()
    {
      return new WorkingMemoryConfig
      {
        MaxDialogueExchanges = maxDialogueExchanges,
        MaxEpisodicMemories = maxEpisodicMemories,
        MaxBeliefs = maxBeliefs,
        MaxContextCharacters = maxContextCharacters,
        AlwaysIncludeCanonicalFacts = alwaysIncludeCanonicalFacts,
        AlwaysIncludeWorldState = alwaysIncludeWorldState
      };
    }

    /// <summary>
    /// Creates both configs together.
    /// </summary>
    /// <returns>A tuple containing both the PromptAssemblerConfig (assembler) and WorkingMemoryConfig (workingMemory).</returns>
    /// <remarks>This is a convenience method that calls both ToConfig() and ToWorkingMemoryConfig() and returns them as a tuple.</remarks>
    public (PromptAssemblerConfig assembler, WorkingMemoryConfig workingMemory) ToConfigs()
    {
      return (ToConfig(), ToWorkingMemoryConfig());
    }

    /// <summary>
    /// Gets the maximum characters for the prompt based on token settings.
    /// </summary>
    public int MaxPromptCharacters => (int)((maxPromptTokens - reserveResponseTokens) * charsPerToken);

    /// <summary>
    /// Estimates token count from character count.
    /// </summary>
    /// <param name="characters">The number of characters to estimate tokens for</param>
    /// <returns>The estimated number of tokens</returns>
    public int EstimateTokens(int characters)
    {
      return (int)System.Math.Ceiling(characters / charsPerToken);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Creates a default settings asset.
    /// </summary>
    [UnityEditor.MenuItem("Assets/Create/LlamaBrain/Default Prompt Assembler Settings")]
    public static void CreateDefaultAsset()
    {
      var asset = CreateInstance<PromptAssemblerSettings>();
      UnityEditor.AssetDatabase.CreateAsset(asset, "Assets/PromptAssemblerSettings.asset");
      UnityEditor.AssetDatabase.SaveAssets();
      UnityEditor.EditorUtility.FocusProjectWindow();
      UnityEditor.Selection.activeObject = asset;
    }
#endif
  }
}
