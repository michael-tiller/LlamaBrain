using System;
using LlamaBrain.Persistence;
using LlamaBrain.Runtime.Persistence;
using TMPro;
using UnityEngine;

namespace LlamaBrain.Runtime.RedRoom.UI
{
  /// <summary>
  /// UI element representing a single save slot in the load game menu scroll view.
  /// </summary>
  public class LoadGameScrollViewElement : MonoBehaviour
  {
  [SerializeField] private TextMeshProUGUI _slotNameText;
  [SerializeField] private TextMeshProUGUI _dateText;

  /// <summary>
  /// Gets the save slot information for this element.
  /// </summary>
  public SaveSlotInfo SlotInfo { get; private set; }
  
  private LoadGameMenu _loadGameMenu;

  private void OnEnable()
  {
    gameObject.transform.localScale = Vector3.one;
  }

  /// <summary>
  /// Initializes this element with save slot information.
  /// </summary>
  /// <param name="slot">The save slot information to display.</param>
  /// <param name="loadGameMenu">The parent load game menu that manages this element.</param>
  public void Initialize(SaveSlotInfo slot, LoadGameMenu loadGameMenu)
  {
    SlotInfo = slot;
    _loadGameMenu = loadGameMenu;
    _slotNameText.text = slot.SlotName;
    _dateText.text = new DateTime(slot.SavedAtUtcTicks).ToString("yyyy-MM-dd HH:mm:ss");
    _loadGameMenu.OnLoadSelected += OnLoadSelectedHandler;
  }

  private void OnDestroy()
  {
    _loadGameMenu.OnLoadSelected -= OnLoadSelectedHandler;
  }

  /// <summary>
  /// Called when this element is clicked. Selects this save slot.
  /// </summary>
  public void OnClick()
  {
    _loadGameMenu.Select(this);
  }
  /// <summary>
  /// Initiates the deletion process for this save slot.
  /// </summary>
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
}
