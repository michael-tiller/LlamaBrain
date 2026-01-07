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
    private string _activeSlotName;

    /// <summary>
    /// Gets the save system instance.
    /// </summary>
    public ISaveSystem SaveSystem => _saveSystem;

    /// <summary>
    /// Gets the currently active save slot name.
    /// Returns null if no game is active.
    /// </summary>
    public string ActiveSlotName => _activeSlotName;

    /// <summary>
    /// Returns true if there is an active save slot (game in progress).
    /// </summary>
    public bool HasActiveGame => !string.IsNullOrEmpty(_activeSlotName);

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
      var saveGameFreeSystem = new SaveGameFreeSaveSystem();
      _saveSystem = saveGameFreeSystem;

      // Load persisted active slot
      _activeSlotName = saveGameFreeSystem.GetActiveSlot();
      if (!string.IsNullOrEmpty(_activeSlotName))
      {
        Debug.Log($"[LlamaBrain] Restored active slot: {_activeSlotName}");
      }
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

    #region Active Slot Game Management

    /// <summary>
    /// Starts a new game by creating a new save slot and setting it as active.
    /// Clears agent state for a fresh start.
    /// </summary>
    /// <returns>The name of the newly created slot</returns>
    public string StartNewGame()
    {
      // Generate a new sequential slot name
      var newSlotName = GenerateNextSlotName();

      // Clear all registered agents' state
      foreach (var agent in _registeredAgents.Where(a => a.IsInitialized))
      {
        agent.ClearDialogueHistory();
      }

      // Set as active and persist
      SetActiveSlotInternal(newSlotName);

      Debug.Log($"[LlamaBrain] Started new game with slot: {newSlotName}");
      return newSlotName;
    }

    /// <summary>
    /// Loads the most recently saved game and sets it as active.
    /// </summary>
    /// <returns>True if a save was found and loaded successfully</returns>
    public bool LoadMostRecentGame()
    {
      var slots = ListSlots();
      if (slots.Count == 0)
      {
        Debug.LogWarning("[LlamaBrain] No saved games found");
        return false;
      }

      // ListSlots returns sorted by SavedAtUtcTicks descending (newest first)
      var mostRecent = slots[0];
      return LoadGame(mostRecent.SlotName);
    }

    /// <summary>
    /// Loads a game from the specified slot and sets it as the active slot.
    /// </summary>
    /// <param name="slotName">The slot to load</param>
    /// <returns>True if load was successful</returns>
    public bool LoadGame(string slotName)
    {
      if (string.IsNullOrEmpty(slotName))
      {
        Debug.LogError("[LlamaBrain] Cannot load game: slot name is null or empty");
        return false;
      }

      if (!LoadFromSlot(slotName))
      {
        return false;
      }

      // Set as active and persist
      SetActiveSlotInternal(slotName);

      Debug.Log($"[LlamaBrain] Loaded game and set active slot: {slotName}");
      return true;
    }

    /// <summary>
    /// Saves the current game to the active slot.
    /// If no active slot exists, creates a new game first.
    /// </summary>
    /// <returns>Result of the save operation</returns>
    public SaveResult SaveGame()
    {
      // Auto-create new game if no active slot
      if (!HasActiveGame)
      {
        StartNewGame();
      }

      return SaveToSlot(_activeSlotName);
    }

    /// <summary>
    /// Generates the next sequential slot name (save_001, save_002, etc.)
    /// </summary>
    private string GenerateNextSlotName()
    {
      var existingSlots = ListSlots();
      int maxNumber = 0;

      foreach (var slot in existingSlots)
      {
        if (slot.SlotName.StartsWith("save_") && slot.SlotName.Length == 8)
        {
          if (int.TryParse(slot.SlotName.Substring(5), out var number))
          {
            if (number > maxNumber)
              maxNumber = number;
          }
        }
      }

      return $"save_{(maxNumber + 1):D3}";
    }

    /// <summary>
    /// Sets the active slot and persists it to disk.
    /// </summary>
    private void SetActiveSlotInternal(string slotName)
    {
      _activeSlotName = slotName;

      // Persist to disk if using SaveGameFree system
      if (_saveSystem is SaveGameFreeSaveSystem saveGameFreeSystem)
      {
        saveGameFreeSystem.SetActiveSlot(slotName);
      }
    }

    /// <summary>
    /// Clears the active slot (e.g., when returning to main menu).
    /// Does not delete the save data.
    /// </summary>
    public void ClearActiveSlot()
    {
      _activeSlotName = null;

      if (_saveSystem is SaveGameFreeSaveSystem saveGameFreeSystem)
      {
        saveGameFreeSystem.SetActiveSlot(null);
      }

      Debug.Log("[LlamaBrain] Cleared active slot");
    }

    #endregion

    /// <summary>
    /// Quick save to the active slot.
    /// If no active slot exists, creates a new game first.
    /// </summary>
    public SaveResult QuickSave()
    {
      return SaveGame();
    }

    /// <summary>
    /// Quick load from the active slot.
    /// Returns false if no active slot exists.
    /// </summary>
    public bool QuickLoad()
    {
      if (!HasActiveGame)
      {
        Debug.LogWarning("[LlamaBrain] No active slot to load from");
        return false;
      }
      return LoadFromSlot(_activeSlotName);
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
    /// If the deleted slot is the active slot, clears the active slot.
    /// </summary>
    public bool DeleteSlot(string slotName)
    {
      var result = _saveSystem?.DeleteSlot(slotName) ?? false;

      // Clear active slot if we just deleted it
      if (result && _activeSlotName == slotName)
      {
        ClearActiveSlot();
      }

      return result;
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
