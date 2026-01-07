using System;
using System.Collections.Generic;
using LlamaBrain.Persistence;
using LlamaBrain.Runtime.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LlamaBrain.Runtime.RedRoom.UI
{
  /// <summary>
  /// UI menu for loading saved games. Displays a list of available save slots and allows selection and deletion.
  /// </summary>
  public class LoadGameMenu : MonoBehaviour
  {
  
  [SerializeField]
  private Button _loadGameButton;
  
  [SerializeField]
  LoadGameScrollViewElement loadGameScrollViewElementPrototype;

  List<LoadGameScrollViewElement> _loadGameScrollViewElements = new List<LoadGameScrollViewElement>();
  
  LoadGameScrollViewElement selectedLoadGameScrollViewElement;
  
  /// <summary>
  /// Event fired when a save slot is selected.
  /// </summary>
  public Action<LoadGameScrollViewElement> OnLoadSelected;

  IReadOnlyList<SaveSlotInfo> slots => LlamaBrainSaveManager.Instance?.ListSlots() ?? Array.Empty<SaveSlotInfo>();
  
  [SerializeField]
  TextMeshProUGUI emptySlotText;
  
  [SerializeField]
  GameObject confirmDeletePanel;

  private LoadGameScrollViewElement deleteTarget = null;
  [SerializeField]
  private TextMeshProUGUI deleteSlotText;
  
  private void Awake()
  {
    loadGameScrollViewElementPrototype.gameObject.SetActive(false);
  }

  private void OnEnable()
  {
    selectedLoadGameScrollViewElement = null;
    _loadGameButton.interactable = false;
    Refresh();
    TryDisplayEmptySlotText();
  }

  private void TryDisplayEmptySlotText()
  {
    
    if (slots == null || slots.Count == 0)
      emptySlotText.gameObject.SetActive(true);
  }

  /// <summary>
  /// Removes a save slot from the list and deletes it from the save system.
  /// </summary>
  /// <param name="slotName">The name of the slot to remove.</param>
  public void Remove(string slotName)
  {
    foreach (LoadGameScrollViewElement v in _loadGameScrollViewElements)
    {
      if (v.SlotInfo.SlotName == slotName)
      {
        if (LlamaBrainSaveManager.Instance.SlotExists(v.SlotInfo.SlotName))
            LlamaBrainSaveManager.Instance.DeleteSlot(v.SlotInfo.SlotName);
        
        _loadGameScrollViewElements.Remove(v);
        Destroy(v.gameObject);
        return;
      }
    }
    TryDisplayEmptySlotText();
  }

  private void Refresh()
  {
    Clear();
    foreach (var slot in slots)
    {
      var element = Instantiate(loadGameScrollViewElementPrototype, loadGameScrollViewElementPrototype.transform.parent);
      element.Initialize(slot, this);
      element.gameObject.SetActive(true);
      AddUnique(element);
      
    }
    TryDisplayEmptySlotText();
  }

  private void Clear()
  {
    foreach (var loadGameScrollViewElement in _loadGameScrollViewElements)
      Destroy(loadGameScrollViewElement.gameObject);
    _loadGameScrollViewElements.Clear();
  }

  private void AddUnique(LoadGameScrollViewElement element)
  {
    if (_loadGameScrollViewElements.Contains(element))
      return;
    _loadGameScrollViewElements.Add(element);
  }

  /// <summary>
  /// Selects a save slot element and enables the load button.
  /// </summary>
  /// <param name="element">The save slot element to select.</param>
  public void Select(LoadGameScrollViewElement element)
  {

    selectedLoadGameScrollViewElement = element;
    OnLoadSelected?.Invoke(element);

    _loadGameButton.interactable = element != null;
    
  }

  /// <summary>
  /// Loads the currently selected save slot.
  /// </summary>
  public void LoadSelected()
  {
    if (selectedLoadGameScrollViewElement == null)
    {
      Debug.LogWarning("[LoadGameMenu] No save slot selected");
      return;
    }

    LlamaBrainSaveManager.Instance.LoadGame(selectedLoadGameScrollViewElement.SlotInfo.SlotName);
  }
  /// <summary>
  /// Shows the confirmation dialog for deleting a save slot.
  /// </summary>
  /// <param name="elementToDelete">The save slot element to delete.</param>
  public void ShowConfirmDelete(LoadGameScrollViewElement elementToDelete)
  {
    deleteTarget = elementToDelete;
    deleteSlotText.text = elementToDelete.SlotInfo.SlotName;
    confirmDeletePanel.SetActive(true);
    
  }

  /// <summary>
  /// Confirms and executes the deletion of the selected save slot.
  /// </summary>
  public void ConfirmDelete()
  {
    Remove(deleteTarget.SlotInfo.SlotName);
    confirmDeletePanel.SetActive(false);
  }

  /// <summary>
  /// Cancels the delete operation and hides the confirmation dialog.
  /// </summary>
  public void CancelDelete()
  {
    confirmDeletePanel.SetActive(false);
  }
  }
}
