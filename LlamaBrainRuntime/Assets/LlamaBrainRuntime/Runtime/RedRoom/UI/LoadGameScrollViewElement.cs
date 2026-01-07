using System;
using LlamaBrain.Persistence;
using LlamaBrain.Runtime.Persistence;
using TMPro;
using UnityEngine;

public class LoadGameScrollViewElement : MonoBehaviour
{
  [SerializeField] private TextMeshProUGUI _slotNameText;
  [SerializeField] private TextMeshProUGUI _dateText;

  public SaveSlotInfo _slotInfo { get; private set;}
  
  private LoadGameMenu _loadGameMenu;

  private void OnEnable()
  {
    gameObject.transform.localScale = Vector3.one;
  }

  public void Initialize(SaveSlotInfo slot, LoadGameMenu loadGameMenu)
  {
    _slotInfo = slot;
    _loadGameMenu = loadGameMenu;
    _slotNameText.text = slot.SlotName;
    _dateText.text = new DateTime(slot.SavedAtUtcTicks).ToString("yyyy-MM-dd HH:mm:ss");
    _loadGameMenu.OnLoadSelected += OnLoadSelectedHandler;
  }

  private void OnDestroy()
  {
    _loadGameMenu.OnLoadSelected -= OnLoadSelectedHandler;
  }

  public void OnClick()
  {
    _loadGameMenu.Select(this);
  }
  public void Delete()
  {
    _loadGameMenu.ShowConfirmDelete(this);
  }
  
  private void OnLoadSelectedHandler(LoadGameScrollViewElement element)
  {
    if (element == this)
    {
      // grow
      gameObject.transform.localScale = Vector3.one * 1.05f;
    }
    else
    {
      gameObject.transform.localScale = Vector3.one;
    }
  }
}
