using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using LlamaBrain.Runtime.Core;
using LlamaBrain.Runtime.Demo;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Linq;

namespace LlamaBrain.Runtime.Demo.UI
{
  /// <summary>
  /// UI component for managing and displaying memory and conversation history
  /// </summary>
  [RequireComponent(typeof(LlamaBrainAgent))]
  public class MemoryManagerUI : MonoBehaviour
  {
    [Header("References")]
    [SerializeField] private LlamaBrainAgent brainAgent;

    [Header("Memory UI")]
    [SerializeField] private TMP_Text memoryCountText;
    [SerializeField] private TMP_Text memoriesText;
    [SerializeField] private Button clearMemoriesButton;
    [SerializeField] private Button addMemoryButton;
    [SerializeField] private TMP_InputField memoryInputField;
    [SerializeField] private TMP_Dropdown memoryCategoryDropdown;
    [SerializeField] private Button filterMemoriesButton;

    [Header("Conversation History UI")]
    [SerializeField] private TMP_Text historyCountText;
    [SerializeField] private TMP_Text conversationHistoryText;
    [SerializeField] private Button clearHistoryButton;
    [SerializeField] private TMP_InputField historyCountInput;
    [SerializeField] private Button showRecentButton;

    [Header("Settings")]
    [SerializeField] private int defaultRecentHistoryCount = 5;
    [SerializeField] private bool autoUpdate = true;
    [SerializeField] private float updateInterval = 1f;

    private float lastUpdateTime;

    private void Reset()
    {
      brainAgent = GetComponent<LlamaBrainAgent>();
    }

    private void Start()
    {
      // Auto-find brain agent if not assigned
      if (brainAgent == null)
      {
        // First try to get it from the same GameObject
        brainAgent = GetComponent<LlamaBrainAgent>();
        if (brainAgent != null)
        {
          Debug.Log($"[MemoryManagerUI] Found LlamaBrainAgent on same GameObject: {brainAgent.name}");
        }

        // If not found on same GameObject, try to find an initialized one in the scene
        if (brainAgent == null || !brainAgent.IsInitialized)
        {
          var allAgents = FindObjectsByType<LlamaBrainAgent>(FindObjectsSortMode.None);
          Debug.Log($"[MemoryManagerUI] Found {allAgents.Length} LlamaBrainAgent(s) in scene during Start");

          foreach (var agent in allAgents)
          {
            Debug.Log($"[MemoryManagerUI] Agent: {agent.name}, IsInitialized: {agent.IsInitialized}");
            if (agent.IsInitialized)
            {
              brainAgent = agent;
              Debug.Log($"[MemoryManagerUI] Selected initialized agent during Start: {brainAgent.name}");
              break;
            }
          }

          // If no initialized agent found, just take the first one
          if (brainAgent == null && allAgents.Length > 0)
          {
            brainAgent = allAgents[0];
            Debug.Log($"[MemoryManagerUI] No initialized agent found during Start, selected: {brainAgent.name}");
          }
        }
      }

      if (brainAgent == null)
      {
        Debug.LogWarning("[MemoryManagerUI] No LlamaBrainAgent found in scene. UI will not function properly.");
      }

      // Setup button listeners
      if (clearMemoriesButton != null)
        clearMemoriesButton.onClick.AddListener(ClearMemories);

      if (addMemoryButton != null)
        addMemoryButton.onClick.AddListener(AddCustomMemory);

      if (filterMemoriesButton != null)
        filterMemoriesButton.onClick.AddListener(FilterMemoriesByCategory);

      if (clearHistoryButton != null)
        clearHistoryButton.onClick.AddListener(ClearConversationHistory);

      if (showRecentButton != null)
        showRecentButton.onClick.AddListener(ShowRecentHistory);

      // Initialize memory category dropdown
      InitializeMemoryCategoryDropdown();

      // Start async initialization check
      WaitForInitializationAsync().Forget();
    }

    private void Update()
    {
      if (autoUpdate && Time.time - lastUpdateTime > updateInterval)
      {
        if (brainAgent != null && brainAgent.IsInitialized)
        {
          UpdateUI();
          lastUpdateTime = Time.time;
        }
        else if (brainAgent == null)
        {
          // Try to find an initialized agent again if it's null
          var allAgents = FindObjectsByType<LlamaBrainAgent>(FindObjectsSortMode.None);
          foreach (var agent in allAgents)
          {
            if (agent.IsInitialized)
            {
              brainAgent = agent;
              Debug.Log($"[MemoryManagerUI] Found initialized agent during Update: {brainAgent.name}");
              UpdateUI();
              lastUpdateTime = Time.time;
              break;
            }
          }

          if (brainAgent == null && allAgents.Length > 0)
          {
            brainAgent = allAgents[0];
            Debug.Log($"[MemoryManagerUI] No initialized agent found during Update, selected: {brainAgent.name}");
          }
        }
        else if (brainAgent != null && !brainAgent.IsInitialized)
        {
          Debug.Log($"[MemoryManagerUI] Agent found but not initialized. Agent: {brainAgent.name}, Status: {brainAgent.ConnectionStatus}");
          brainAgent.DebugAgentState();
        }
      }
    }

    /// <summary>
    /// Waits for the agent to be fully initialized before updating the UI
    /// </summary>
    private async UniTaskVoid WaitForInitializationAsync()
    {
      Debug.Log("[MemoryManagerUI] Starting to wait for agent initialization...");

      // Wait for the agent to be found and initialized
      while (brainAgent == null || !brainAgent.IsInitialized)
      {
        // Always search for an initialized agent first
        var allAgents = FindObjectsByType<LlamaBrainAgent>(FindObjectsSortMode.None);
        Debug.Log($"[MemoryManagerUI] Found {allAgents.Length} LlamaBrainAgent(s) in scene");

        // Look for an initialized agent first
        LlamaBrainAgent initializedAgent = null;
        foreach (var agent in allAgents)
        {
          Debug.Log($"[MemoryManagerUI] Agent: {agent.name}, IsInitialized: {agent.IsInitialized}");
          if (agent.IsInitialized)
          {
            initializedAgent = agent;
            Debug.Log($"[MemoryManagerUI] Found initialized agent: {initializedAgent.name}");
            break;
          }
        }

        // If we found an initialized agent, use it
        if (initializedAgent != null)
        {
          brainAgent = initializedAgent;
          Debug.Log($"[MemoryManagerUI] Selected initialized agent: {brainAgent.name}");
          break;
        }

        // If no initialized agent found, wait and try again
        if (allAgents.Length > 0)
        {
          Debug.Log($"[MemoryManagerUI] No initialized agent found yet, waiting...");
        }
        else
        {
          Debug.Log($"[MemoryManagerUI] No agents found in scene, waiting...");
        }

        await UniTask.Yield();
      }

      Debug.Log($"[MemoryManagerUI] Agent {brainAgent.name} is now initialized! Updating UI...");

      // Refresh category dropdown with the agent's categories
      InitializeMemoryCategoryDropdown();

      UpdateUI();
    }

    /// <summary>
    /// Initialize the memory category dropdown with available categories
    /// </summary>
    private void InitializeMemoryCategoryDropdown()
    {
      if (memoryCategoryDropdown != null)
      {
        memoryCategoryDropdown.ClearOptions();

        if (brainAgent != null && brainAgent.MemoryCategoryManager != null)
        {
          // Use categories from the ScriptableObject
          var categories = brainAgent.MemoryCategoryManager.GetCategoryDisplayNames();
          memoryCategoryDropdown.AddOptions(categories);
        }
        else
        {
          // Fallback to hardcoded categories
          var categories = new List<string>
          {
            "All Categories",
            "PlayerInfo",
            "Preferences",
            "WorldInfo",
            "WorldEvents",
            "Relationships",
            "Quests",
            "Custom"
          };
          memoryCategoryDropdown.AddOptions(categories);
        }
      }
    }

    /// <summary>
    /// Update all UI elements with current memory and history data
    /// </summary>
    public void UpdateUI()
    {
      if (brainAgent == null)
      {
        Debug.LogWarning("[MemoryManagerUI] UpdateUI called but brainAgent is null");
        return;
      }

      Debug.Log($"[MemoryManagerUI] UpdateUI - Agent: {brainAgent.name}, IsInitialized: {brainAgent.IsInitialized}, ConnectionStatus: {brainAgent.ConnectionStatus}");

      if (!brainAgent.IsInitialized)
      {
        Debug.Log($"[MemoryManagerUI] Agent not initialized. ConnectionStatus: {brainAgent.ConnectionStatus}, SetupInstructions: {brainAgent.GetSetupInstructions()}");

        // Show initialization status
        if (memoryCountText != null)
          memoryCountText.text = brainAgent.ConnectionStatus;
        if (historyCountText != null)
          historyCountText.text = brainAgent.ConnectionStatus;
        if (memoriesText != null)
          memoriesText.text = brainAgent.GetSetupInstructions();
        if (conversationHistoryText != null)
          conversationHistoryText.text = brainAgent.GetSetupInstructions();

        // Disable buttons
        if (clearMemoriesButton != null)
          clearMemoriesButton.interactable = false;
        if (clearHistoryButton != null)
          clearHistoryButton.interactable = false;
        if (addMemoryButton != null)
          addMemoryButton.interactable = false;
        if (showRecentButton != null)
          showRecentButton.interactable = false;

        return;
      }

      UpdateMemoryUI();
      UpdateConversationHistoryUI();
    }

    /// <summary>
    /// Update memory-related UI elements
    /// </summary>
    private void UpdateMemoryUI()
    {
      if (memoryCountText != null)
      {
        memoryCountText.text = $"{brainAgent.MemoryCount}";
      }

      if (memoriesText != null)
      {
        var memories = brainAgent.GetMemories();
        if (memories.Count > 0)
        {
          var memoryList = new List<string>();
          for (int i = 0; i < memories.Count; i++)
          {
            var memory = memories[i];
            // Format categorized memories nicely
            if (memory.StartsWith("[") && memory.Contains("]"))
            {
              var categoryEnd = memory.IndexOf("]");
              var category = memory.Substring(1, categoryEnd - 1);
              var content = memory.Substring(categoryEnd + 1).Trim();
              memoryList.Add($"{i + 1}. [{category}] {content}");
            }
            else
            {
              memoryList.Add($"{i + 1}. {memory}");
            }
          }
          memoriesText.text = string.Join("\n", memoryList);
        }
        else
        {
          memoriesText.text = "No long-term memories stored.\n\nMemory stores important facts like:\n• Player names and preferences\n• Important world events\n• Quest progress\n• Character relationships";
        }
      }

      if (clearMemoriesButton != null)
      {
        clearMemoriesButton.interactable = brainAgent.HasMemories;
      }
    }

    /// <summary>
    /// Update conversation history UI elements
    /// </summary>
    private void UpdateConversationHistoryUI()
    {
      if (historyCountText != null)
      {
        historyCountText.text = $"{brainAgent.ConversationHistoryCount}";
      }

      if (conversationHistoryText != null)
      {
        var history = brainAgent.GetConversationHistory();
        if (history.Count > 0)
        {
          var historyList = new List<string>();
          for (int i = 0; i < history.Count; i++)
          {
            historyList.Add($"{i + 1}. {history[i]}");
          }
          conversationHistoryText.text = string.Join("\n", historyList);
        }
        else
        {
          conversationHistoryText.text = "No recent conversation history.\n\nConversation history shows the current dialogue flow and is temporary.";
        }
      }

      if (clearHistoryButton != null)
      {
        clearHistoryButton.interactable = brainAgent.HasConversationHistory;
      }
    }

    /// <summary>
    /// Clear all memories for the brain agent
    /// </summary>
    public void ClearMemories()
    {
      if (brainAgent != null && brainAgent.IsInitialized)
      {
        brainAgent.ClearMemories();
        UpdateUI();
        Debug.Log("[MemoryManagerUI] Cleared all memories");
      }
      else if (brainAgent != null && !brainAgent.IsInitialized)
      {
        Debug.LogWarning("[MemoryManagerUI] Cannot clear memories: Brain agent is not initialized");
      }
    }

    /// <summary>
    /// Add a custom memory entry
    /// </summary>
    public void AddCustomMemory()
    {
      if (brainAgent != null && brainAgent.IsInitialized && memoryInputField != null && !string.IsNullOrEmpty(memoryInputField.text))
      {
        // Check if user specified a category (format: "Category: Content")
        var input = memoryInputField.text;
        if (input.Contains(":"))
        {
          var parts = input.Split(new[] { ':' }, 2);
          var category = parts[0].Trim();
          var content = parts[1].Trim();
          brainAgent.AddSpecificMemory(category, content);
        }
        else
        {
          brainAgent.AddMemory(input);
        }

        memoryInputField.text = "";
        UpdateUI();
        Debug.Log("[MemoryManagerUI] Added custom memory");
      }
      else if (brainAgent != null && !brainAgent.IsInitialized)
      {
        Debug.LogWarning("[MemoryManagerUI] Cannot add memory: Brain agent is not initialized");
      }
    }

    /// <summary>
    /// Filter memories by category
    /// </summary>
    public void FilterMemoriesByCategory()
    {
      if (brainAgent != null && brainAgent.IsInitialized && memoryCategoryDropdown != null)
      {
        var selectedDisplayName = memoryCategoryDropdown.options[memoryCategoryDropdown.value].text;
        if (selectedDisplayName == "All Categories")
        {
          UpdateMemoryUI(); // Show all memories
        }
        else
        {
          // Find the category by display name
          string categoryName = selectedDisplayName;
          if (brainAgent.MemoryCategoryManager != null)
          {
            var category = brainAgent.MemoryCategoryManager.MemoryCategories
                .FirstOrDefault(cat => cat.DisplayName == selectedDisplayName);
            if (category != null)
            {
              categoryName = category.CategoryName;
            }
          }

          var filteredMemories = brainAgent.GetMemoriesByCategory(categoryName);
          if (memoriesText != null)
          {
            if (filteredMemories.Count > 0)
            {
              var memoryList = new List<string>();
              for (int i = 0; i < filteredMemories.Count; i++)
              {
                var memory = filteredMemories[i];
                if (memory.StartsWith($"[{categoryName}]"))
                {
                  var content = memory.Substring(categoryName.Length + 3).Trim(); // Remove "[Category] "
                  memoryList.Add($"{i + 1}. {content}");
                }
              }
              memoriesText.text = $"Memories in {selectedDisplayName}:\n" + string.Join("\n", memoryList);
            }
            else
            {
              memoriesText.text = $"No memories found in category: {selectedDisplayName}";
            }
          }
        }
      }
    }

    /// <summary>
    /// Clear conversation history
    /// </summary>
    public void ClearConversationHistory()
    {
      if (brainAgent != null && brainAgent.IsInitialized)
      {
        brainAgent.ClearDialogueHistory();
        UpdateUI();
        Debug.Log("[MemoryManagerUI] Cleared conversation history");
      }
      else if (brainAgent != null && !brainAgent.IsInitialized)
      {
        Debug.LogWarning("[MemoryManagerUI] Cannot clear conversation history: Brain agent is not initialized");
      }
    }

    /// <summary>
    /// Show recent conversation history
    /// </summary>
    public void ShowRecentHistory()
    {
      if (brainAgent != null && conversationHistoryText != null)
      {
        int count = defaultRecentHistoryCount;
        if (historyCountInput != null && int.TryParse(historyCountInput.text, out int inputCount))
        {
          count = Mathf.Clamp(inputCount, 1, 50);
        }

        var recentHistory = brainAgent.GetRecentHistory(count);
        if (recentHistory.Count > 0)
        {
          var historyList = new List<string>();
          for (int i = 0; i < recentHistory.Count; i++)
          {
            var entry = recentHistory[i];
            historyList.Add($"{i + 1}. {entry.Speaker}: {entry.Text}");
          }
          conversationHistoryText.text = $"Recent History (Last {count}):\n" + string.Join("\n", historyList);
        }
        else
        {
          conversationHistoryText.text = "No recent conversation history.";
        }
      }
    }

    /// <summary>
    /// Manually refresh the UI
    /// </summary>
    [ContextMenu("Refresh UI")]
    public void RefreshUI()
    {
      Debug.Log("[MemoryManagerUI] Manual refresh requested");
      UpdateUI();
    }

    /// <summary>
    /// Force find and connect to a LlamaBrainAgent
    /// </summary>
    [ContextMenu("Force Find Agent")]
    public void ForceFindAgent()
    {
      brainAgent = FindAnyObjectByType<LlamaBrainAgent>();
      if (brainAgent != null)
      {
        Debug.Log($"[MemoryManagerUI] Force found LlamaBrainAgent: {brainAgent.name}. IsInitialized: {brainAgent.IsInitialized}");
        brainAgent.DebugAgentState();
        UpdateUI();
      }
      else
      {
        Debug.LogWarning("[MemoryManagerUI] No LlamaBrainAgent found in scene");
      }
    }

    /// <summary>
    /// Debug the current state of the UI and agent connection
    /// </summary>
    [ContextMenu("Debug UI State")]
    public void DebugUIState()
    {
      Debug.Log($"[MemoryManagerUI] Debug UI State:");
      Debug.Log($"  - brainAgent: {(brainAgent != null ? brainAgent.name : "null")}");
      Debug.Log($"  - autoUpdate: {autoUpdate}");
      Debug.Log($"  - updateInterval: {updateInterval}");
      Debug.Log($"  - lastUpdateTime: {lastUpdateTime}");

      if (brainAgent != null)
      {
        Debug.Log($"  - Agent IsInitialized: {brainAgent.IsInitialized}");
        Debug.Log($"  - Agent ConnectionStatus: {brainAgent.ConnectionStatus}");
        brainAgent.DebugAgentState();
      }
    }

    /// <summary>
    /// Toggle auto-update mode
    /// </summary>
    [ContextMenu("Toggle Auto Update")]
    public void ToggleAutoUpdate()
    {
      autoUpdate = !autoUpdate;
      Debug.Log($"[MemoryManagerUI] Auto-update {(autoUpdate ? "enabled" : "disabled")}");
    }
  }
}