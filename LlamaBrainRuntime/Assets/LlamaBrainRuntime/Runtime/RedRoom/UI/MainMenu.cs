using System.Collections.Generic;
using LlamaBrain.Persistence;
using LlamaBrain.Runtime.Persistence;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
  IReadOnlyList<SaveSlotInfo> slots => LlamaBrainSaveManager.Instance?.ListSlots();
  [SerializeField]
  private Button continueButton;

  private void OnEnable()
  {
    continueButton.interactable = slots.Count > 0;
  }

  public void Continue()
  {
    LlamaBrainSaveManager.Instance.LoadMostRecentGame();
  }
  public void StartNewGame()
  {
    LlamaBrainSaveManager.Instance.StartNewGame();
  }
}
