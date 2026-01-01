using UnityEngine;
using TMPro;
using LlamaBrain.Runtime.RedRoom.Interaction;
using LlamaBrain.Runtime.RedRoom.AI;

namespace LlamaBrain.Runtime.RedRoom.UI
{
  /// <summary>
  /// Manages the UI canvas for the Red Room demo, handling conversation display and raycast interactions.
  /// </summary>
  public class RedRoomCanvas : MonoBehaviour
  {
    [SerializeField] private GameObject _conversationPanel;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private GameObject _playerRaycastHitIndicator;

    private NpcFollowerExample currentNpc = null;

    /// <summary>
    /// Gets the singleton instance of the RedRoomCanvas.
    /// </summary>
    public static RedRoomCanvas Instance { get; private set; }

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(this.gameObject);
        return;
      }
      Instance = this;
    }

    private void Update()
    {
      // E key: Toggle conversation
      if (Input.GetKeyDown(KeyCode.E))
      {
        if (!_conversationPanel.activeSelf)
        {
          TryStartConversation();
        }
        else
        {
          EndConversation();
        }
      }

      // Escape: Close conversation
      if (Input.GetKeyDown(KeyCode.Escape) && _conversationPanel.activeSelf)
      {
        EndConversation();
      }

      // F2: Toggle Memory Mutation Overlay
      if (Input.GetKeyDown(KeyCode.F2))
      {
        ToggleMemoryOverlay();
      }
    }

    /// <summary>
    /// Toggles the Memory Mutation Overlay visibility.
    /// </summary>
    public void ToggleMemoryOverlay()
    {
      var overlay = MemoryMutationOverlay.Instance;
      if (overlay != null)
      {
        overlay.Toggle();
      }
      else
      {
        Debug.LogWarning("[RedRoomCanvas] MemoryMutationOverlay not found in scene.");
      }
    }

    private void TryStartConversation()
    {
      if (currentNpc == null) return;

      var trigger = currentNpc.CurrentDialogueTrigger;
      if (trigger == null) return;

      string text = trigger.ConversationText;
      if (!string.IsNullOrEmpty(text))
      {
        ShowConversation(text);
      }
      else
      {
        // Text not ready - trigger generation
        trigger.TriggerConversation(currentNpc);
      }
    }

    /// <summary>
    /// Called when the player raycast hits an NPC.
    /// </summary>
    /// <param name="hit">The raycast hit information</param>
    public void OnRaycastHit(RaycastHit hit)
    {
      var npc = hit.collider.GetComponentInParent<NpcFollowerExample>();

      if (npc == null)
      {
        ClearCurrentNpc();
        return;
      }

      currentNpc = npc;
      RefreshIndicator();
    }

    /// <summary>
    /// Called when the player raycast misses or no longer hits an NPC.
    /// </summary>
    public void OnRaycastMiss()
    {
      ClearCurrentNpc();
    }

    private void ClearCurrentNpc()
    {
      _playerRaycastHitIndicator.SetActive(false);
      currentNpc = null;
    }

    private void RefreshIndicator()
    {
      if (currentNpc == null)
      {
        _playerRaycastHitIndicator.SetActive(false);
        return;
      }

      var trigger = currentNpc.CurrentDialogueTrigger;
      bool hasDialogue = trigger != null && !string.IsNullOrEmpty(trigger.ConversationText);
      _playerRaycastHitIndicator.SetActive(hasDialogue);
    }

    private void ShowConversation(string message)
    {
      _playerRaycastHitIndicator.SetActive(false);
      _conversationPanel.SetActive(true);
      _text.text = message;
    }

    /// <summary>
    /// Ends the current conversation and hides the conversation panel.
    /// </summary>
    public void EndConversation()
    {
      _conversationPanel.SetActive(false);
      RefreshIndicator();
    }
  }
}