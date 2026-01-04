using System.Collections.Generic;

namespace LlamaBrain.Persistence
{
  /// <summary>
  /// Engine-agnostic persistence interface for save/load operations.
  /// Implementations handle platform-specific storage details (e.g., Unity SaveGameFree, Unreal SaveGame).
  /// </summary>
  public interface ISaveSystem
  {
    /// <summary>
    /// Saves game state to the specified slot.
    /// </summary>
    /// <param name="slotName">The save slot name (e.g., "autosave", "slot1")</param>
    /// <param name="data">The save data to persist</param>
    /// <returns>A result indicating success or failure with details</returns>
    SaveResult Save(string slotName, SaveData data);

    /// <summary>
    /// Loads game state from the specified slot.
    /// </summary>
    /// <param name="slotName">The save slot name</param>
    /// <returns>The loaded save data, or null if slot doesn't exist</returns>
    SaveData? Load(string slotName);

    /// <summary>
    /// Checks if a save slot exists.
    /// </summary>
    /// <param name="slotName">The save slot name to check</param>
    /// <returns>True if the slot exists, false otherwise</returns>
    bool SlotExists(string slotName);

    /// <summary>
    /// Deletes a save slot.
    /// </summary>
    /// <param name="slotName">The save slot name to delete</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    bool DeleteSlot(string slotName);

    /// <summary>
    /// Lists all available save slots with metadata.
    /// </summary>
    /// <returns>A read-only list of save slot information</returns>
    IReadOnlyList<SaveSlotInfo> ListSlots();
  }
}
