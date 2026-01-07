using UnityEngine;
using TMPro;
using LlamaBrain.Runtime.RedRoom.Interaction;
using LlamaBrain.Runtime.RedRoom.AI;
using UnityEngine.SceneManagement;

namespace LlamaBrain.Runtime.RedRoom.UI
{
  /// <summary>
  /// Manages the UI canvas for the Red Room demo, handling conversation display and raycast interactions.
  /// </summary>
  public class RedRoomCanvas : MonoBehaviour
  {
  /// <summary>
  /// Represents the current UI state of the Red Room canvas.
  /// </summary>
  public enum EState
  {
    /// <summary>
    /// No specific state (initial state).
    /// </summary>
    None = 0,
    /// <summary>
    /// Main menu is displayed.
    /// </summary>
    MainMenu = 1,
    /// <summary>
    /// Game is active and running.
    /// </summary>
    Game = 2,
    /// <summary>
    /// Load game menu is displayed.
    /// </summary>
    LoadGameMenu = 3
  }

    
    [SerializeField] private GameObject _conversationPanel;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private GameObject _playerRaycastHitIndicator;
    
    [SerializeField] private MainMenu mainMenuPanel;
    [SerializeField] private PausePanel pausePanel;
    [SerializeField] private LoadGameMenu loadGameMenu;

    private NpcFollowerExample currentNpc = null;
    
    /// <summary>
    /// Gets the current UI state of the canvas.
    /// </summary>
    public EState State { get; private set; } = EState.None;

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
      Time.timeScale = 0f;
      Cursor.lockState = CursorLockMode.None;
    }
    
    private void Start()
    {
      State = EState.None;
      ShowMainMenu();
      Time.timeScale = 0f;
      Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
      switch (State)
      {
        case EState.MainMenu:
        case EState.LoadGameMenu:
          UpdateMenus();
          break;
        case EState.Game:
          UpdateGame();
          break;
        default:
          break;
      }
    }

    private void UpdateMenus()
    {
      
      if (Cursor.lockState != CursorLockMode.None)
      {
        Cursor.lockState = CursorLockMode.None;
      }
      
    }
    
    private void UpdateGame()
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

      // F3: Toggle Validation Gate Overlay
      if (Input.GetKeyDown(KeyCode.F3))
      {
        ToggleValidationOverlay();
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

    /// <summary>
    /// Toggles the Validation Gate Overlay visibility.
    /// </summary>
    public void ToggleValidationOverlay()
    {
      var overlay = ValidationGateOverlay.Instance;
      if (overlay != null)
      {
        overlay.Toggle();
      }
      else
      {
        Debug.LogWarning("[RedRoomCanvas] ValidationGateOverlay not found in scene.");
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
        // Also speak the cached response if voice is enabled
        trigger.SpeakCachedResponse();
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
    
    /// <summary>
    /// Shows the main menu and sets the state to MainMenu.
    /// </summary>
    public void ShowMainMenu()
    {
      mainMenuPanel.gameObject.SetActive(true);
      loadGameMenu.gameObject.SetActive(false);
      State = EState.MainMenu;
    }
    
    /// <summary>
    /// Shows the load game menu and sets the state to LoadGameMenu.
    /// </summary>
    public void ShowLoadGameMenu()
    {
      mainMenuPanel.gameObject.SetActive(false);
      loadGameMenu.gameObject.SetActive(true);
      State = EState.LoadGameMenu;
    }

    /// <summary>
    /// Shows the game panels and sets the state to Game.
    /// </summary>
    public void ShowGamePanels()
    {
      loadGameMenu.gameObject.SetActive(false);
      mainMenuPanel.gameObject.SetActive(false);
      pausePanel.Resume();
      State = EState.Game;
    }

    /// <summary>
    /// Confirms quitting and loads the first scene.
    /// </summary>
    public void ConfirmQuit()
    {
      SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
  }
}