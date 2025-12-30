using UnityEngine;
using TMPro;
using LlamaBrain.Runtime.RedRoom.Interaction;
using LlamaBrain.Runtime.RedRoom.AI;

namespace LlamaBrain.Runtime.RedRoom.UI
{
  public class RedRoomCanvas : MonoBehaviour
  {
    [SerializeField] private GameObject _conversationPanel;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private GameObject _playerRaycastHitIndicator;

    private NpcFollowerExample currentNpc = null;

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

      if (Input.GetKeyDown(KeyCode.Escape) && _conversationPanel.activeSelf)
      {
        EndConversation();
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

    public void EndConversation()
    {
      _conversationPanel.SetActive(false);
      RefreshIndicator();
    }
  }
}