using System;
using System.Collections.Generic;
using LlamaBrain.Persistence;
using LlamaBrain.Runtime.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoadGameMenu : MonoBehaviour
{
  
  [SerializeField]
  private Button _loadGameButton;
  
  [SerializeField]
  LoadGameScrollViewElement loadGameScrollViewElementPrototype;
  
  List<LoadGameScrollViewElement> _loadGameScrollViewElements = new List<LoadGameScrollViewElement>();
  
  LoadGameScrollViewElement selectedLoadGameScrollViewElement;
  public Action<LoadGameScrollViewElement> OnLoadSelected;

  IReadOnlyList<SaveSlotInfo> slots => LlamaBrainSaveManager.Instance?.ListSlots();
  
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

  public void Remove(string slotName)
  {
    foreach (LoadGameScrollViewElement v in _loadGameScrollViewElements)
    {
      if (v._slotInfo.SlotName == slotName)
      {
        if (LlamaBrainSaveManager.Instance.SlotExists(v._slotInfo.SlotName))
            LlamaBrainSaveManager.Instance.DeleteSlot(v._slotInfo.SlotName);
        
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

  public void Select(LoadGameScrollViewElement element)
  {

    selectedLoadGameScrollViewElement = element;
    OnLoadSelected?.Invoke(element);

    _loadGameButton.interactable = element != null;
    
  }

  public void LoadSelected()
  {
    LlamaBrainSaveManager.Instance.LoadGame(selectedLoadGameScrollViewElement._slotInfo.SlotName);
  }
  public void ShowConfirmDelete(LoadGameScrollViewElement elementToDelete)
  {
    deleteTarget = elementToDelete;
    deleteSlotText.text = elementToDelete._slotInfo.SlotName;
    confirmDeletePanel.SetActive(true);
    
  }

  public void ConfirmDelete()
  {
    Remove(deleteTarget._slotInfo.SlotName);
    confirmDeletePanel.SetActive(false);
  }

  public void CancelDelete()
  {
    confirmDeletePanel.SetActive(false);
  }
}
