using System.Collections.Generic;
using LlamaBrain.Persistence;
using LlamaBrain.Runtime.Persistence;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main menu UI for the Red Room demo. Provides options to continue a saved game or start a new game.
/// </summary>
public class MainMenu : MonoBehaviour
{
  IReadOnlyList<SaveSlotInfo> slots => LlamaBrainSaveManager.Instance?.ListSlots() ?? new List<SaveSlotInfo>();
  [SerializeField]
  private Button continueButton;

  private void OnEnable()
  {
    continueButton.interactable = slots.Count > 0;
  }

  /// <summary>
  /// Loads the most recent save game.
  /// </summary>
  public void Continue()
  {
    if (LlamaBrainSaveManager.Instance == null)
    {
      Debug.LogError("[MainMenu] LlamaBrainSaveManager not initialized");
      return;
    }

    LlamaBrainSaveManager.Instance.LoadMostRecentGame();
  }

  /// <summary>
  /// Starts a new game session.
  /// </summary>
  public void StartNewGame()
  {
    if (LlamaBrainSaveManager.Instance == null)
    {
      Debug.LogError("[MainMenu] LlamaBrainSaveManager not initialized");
      return;
    }

    LlamaBrainSaveManager.Instance.StartNewGame();
  }
}
