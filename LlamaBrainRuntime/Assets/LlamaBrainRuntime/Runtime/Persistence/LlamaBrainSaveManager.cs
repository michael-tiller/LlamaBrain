using System;
using System.Collections.Generic;
using System.Linq;
using LlamaBrain.Persistence;
using LlamaBrain.Runtime.Core;
using UnityEngine;

namespace LlamaBrain.Runtime.Persistence
{
  /// <summary>
  /// Central save/load manager for LlamaBrain state.
  /// Singleton that persists across scenes.
  /// Games trigger saves/loads through this component.
  /// </summary>
  public class LlamaBrainSaveManager : MonoBehaviour
  {
    private static LlamaBrainSaveManager _instance;

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static LlamaBrainSaveManager Instance => _instance;

    /// <summary>
    /// Returns true if this instance was detected as a duplicate and will be destroyed.
    /// Useful for testing singleton lifecycle.
    /// </summary>
    public bool IsDuplicateInstance { get; private set; }

    [Header("Configuration")]
    [SerializeField]
    [Tooltip("Automatically find and register all LlamaBrainAgent instances in the scene")]
    private bool autoFindAgents = true;

    [SerializeField]
    [Tooltip("Name of the default save slot for quick save/load")]
    private string defaultSlotName = "autosave";

    /// <summary>
    /// Event fired when a save operation completes.
    /// </summary>
    public event Action<SaveResult> OnSaveComplete;

    /// <summary>
    /// Event fired when a load operation completes.
    /// </summary>
    public event Action<bool> OnLoadComplete;

    private ISaveSystem _saveSystem;
    private readonly List<LlamaBrainAgent> _registeredAgents = new List<LlamaBrainAgent>();

    /// <summary>
    /// Gets the save system instance.
    /// </summary>
    public ISaveSystem SaveSystem => _saveSystem;

    /// <summary>
    /// Gets the list of registered agents.
    /// </summary>
    public IReadOnlyList<LlamaBrainAgent> RegisteredAgents => _registeredAgents;

    private void Awake()
    {
      // Singleton pattern with duplicate detection
      if (_instance != null && _instance != this)
      {
        IsDuplicateInstance = true;
        Debug.LogWarning("[LlamaBrain] Duplicate LlamaBrainSaveManager detected. Destroying duplicate.");
        Destroy(gameObject);
        return;
      }

      _instance = this;
      DontDestroyOnLoad(gameObject);

      // Default to SaveGameFree implementation
      _saveSystem = new SaveGameFreeSaveSystem();
    }

    private void OnDestroy()
    {
      // Clear singleton reference if this is the active instance
      if (_instance == this)
      {
        _instance = null;
      }
    }

    private void Start()
    {
      // Skip initialization if this is a duplicate being destroyed
      if (IsDuplicateInstance) return;

      if (autoFindAgents)
      {
        AutoRegisterAgents();
      }
    }

    /// <summary>
    /// Initializes the save manager with a custom save system.
    /// Call this before using save/load if you want to use a different implementation.
    /// </summary>
    /// <param name="saveSystem">The save system to use</param>
    public void Initialize(ISaveSystem saveSystem)
    {
      _saveSystem = saveSystem ?? throw new ArgumentNullException(nameof(saveSystem));
    }

    /// <summary>
    /// Registers an agent to be included in save/load operations.
    /// </summary>
    /// <param name="agent">The agent to register</param>
    public void RegisterAgent(LlamaBrainAgent agent)
    {
      if (agent == null) return;
      if (!_registeredAgents.Contains(agent))
      {
        _registeredAgents.Add(agent);
        Debug.Log($"[LlamaBrain] Registered agent: {agent.name}");
      }
    }

    /// <summary>
    /// Unregisters an agent from save/load operations.
    /// </summary>
    /// <param name="agent">The agent to unregister</param>
    public void UnregisterAgent(LlamaBrainAgent agent)
    {
      if (agent == null) return;
      _registeredAgents.Remove(agent);
    }

    /// <summary>
    /// Automatically finds and registers all LlamaBrainAgent instances in the scene.
    /// </summary>
    public void AutoRegisterAgents()
    {
      var agents = FindObjectsOfType<LlamaBrainAgent>();
      foreach (var agent in agents)
      {
        RegisterAgent(agent);
      }
      Debug.Log($"[LlamaBrain] Auto-registered {agents.Length} agents");
    }

    /// <summary>
    /// Saves all registered agents to the specified slot.
    /// </summary>
    /// <param name="slotName">The save slot name</param>
    /// <returns>Result of the save operation</returns>
    public SaveResult SaveToSlot(string slotName)
    {
      if (_saveSystem == null)
      {
        var result = SaveResult.Failed("Save system not initialized");
        OnSaveComplete?.Invoke(result);
        return result;
      }

      try
      {
        var saveData = SaveData.CreateNew();

        foreach (var agent in _registeredAgents.Where(a => a.IsInitialized))
        {
          var personaId = agent.RuntimeProfile?.PersonaId;
          if (string.IsNullOrEmpty(personaId)) continue;

          var agentData = agent.CreateSaveData();
          if (agentData != null)
          {
            saveData.PersonaMemories[personaId] = agentData.MemorySnapshot;
            saveData.ConversationHistories[personaId] = agentData.ConversationHistory;
          }
        }

        var result = _saveSystem.Save(slotName, saveData);
        OnSaveComplete?.Invoke(result);

        if (result.Success)
        {
          Debug.Log($"[LlamaBrain] Saved {saveData.PersonaMemories.Count} personas to slot '{slotName}'");
        }
        else
        {
          Debug.LogError($"[LlamaBrain] Save failed: {result.ErrorMessage}");
        }

        return result;
      }
      catch (Exception ex)
      {
        var result = SaveResult.Failed(ex.Message);
        OnSaveComplete?.Invoke(result);
        Debug.LogError($"[LlamaBrain] Save exception: {ex}");
        return result;
      }
    }

    /// <summary>
    /// Loads state from the specified slot into all registered agents.
    /// </summary>
    /// <param name="slotName">The save slot name</param>
    /// <returns>True if load was successful</returns>
    public bool LoadFromSlot(string slotName)
    {
      if (_saveSystem == null)
      {
        Debug.LogError("[LlamaBrain] Save system not initialized");
        OnLoadComplete?.Invoke(false);
        return false;
      }

      try
      {
        var saveData = _saveSystem.Load(slotName);
        if (saveData == null)
        {
          Debug.LogWarning($"[LlamaBrain] No save data found in slot '{slotName}'");
          OnLoadComplete?.Invoke(false);
          return false;
        }

        int loadedCount = 0;
        foreach (var agent in _registeredAgents.Where(a => a.IsInitialized))
        {
          var personaId = agent.RuntimeProfile?.PersonaId;
          if (string.IsNullOrEmpty(personaId)) continue;

          if (saveData.PersonaMemories.TryGetValue(personaId, out var memorySnapshot))
          {
            ConversationHistorySnapshot conversationSnapshot = null;
            saveData.ConversationHistories?.TryGetValue(personaId, out conversationSnapshot);

            var agentData = new AgentSaveData
            {
              PersonaId = personaId,
              MemorySnapshot = memorySnapshot,
              ConversationHistory = conversationSnapshot ?? new ConversationHistorySnapshot()
            };

            agent.RestoreFromSaveData(agentData);
            loadedCount++;
          }
        }

        Debug.Log($"[LlamaBrain] Loaded state for {loadedCount} agents from slot '{slotName}'");
        OnLoadComplete?.Invoke(true);
        return true;
      }
      catch (Exception ex)
      {
        Debug.LogError($"[LlamaBrain] Load exception: {ex}");
        OnLoadComplete?.Invoke(false);
        return false;
      }
    }

    /// <summary>
    /// Quick save to the default slot.
    /// </summary>
    public SaveResult QuickSave()
    {
      return SaveToSlot(defaultSlotName);
    }

    /// <summary>
    /// Quick load from the default slot.
    /// </summary>
    public bool QuickLoad()
    {
      return LoadFromSlot(defaultSlotName);
    }

    /// <summary>
    /// Checks if a save slot exists.
    /// </summary>
    public bool SlotExists(string slotName)
    {
      return _saveSystem?.SlotExists(slotName) ?? false;
    }

    /// <summary>
    /// Deletes a save slot.
    /// </summary>
    public bool DeleteSlot(string slotName)
    {
      return _saveSystem?.DeleteSlot(slotName) ?? false;
    }

    /// <summary>
    /// Gets a list of all available save slots.
    /// </summary>
    public IReadOnlyList<SaveSlotInfo> ListSlots()
    {
      return _saveSystem?.ListSlots() ?? new List<SaveSlotInfo>();
    }
  }

  /// <summary>
  /// Container for agent save data.
  /// </summary>
  public class AgentSaveData
  {
    public string PersonaId { get; set; }
    public PersonaMemorySnapshot MemorySnapshot { get; set; }
    public ConversationHistorySnapshot ConversationHistory { get; set; }
  }
}
