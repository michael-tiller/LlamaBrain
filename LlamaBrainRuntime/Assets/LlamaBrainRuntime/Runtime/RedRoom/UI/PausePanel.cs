using System;
using Cysharp.Threading.Tasks;
using LlamaBrain.Runtime.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// Pause menu panel that can be toggled during gameplay. Provides options to save, resume, or quit.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PausePanel : MonoBehaviour
{
  [SerializeField]
  private CanvasGroup canvasGroup;

  private bool isVisible;
#pragma warning disable CS0414 // isPaused tracks pause state for external queries
  private bool isPaused;
#pragma warning restore CS0414

  private void Reset()
  {
    canvasGroup = GetComponent<CanvasGroup>();
  }

  private void Start()
  {
    SetCanvasGroup(false);
  }

  /// <summary>
  /// Toggles the visibility of the pause panel.
  /// </summary>
  public void ToggleCanvasGroup()
  {
    SetCanvasGroup(!isVisible);
  }

  /// <summary>
  /// Sets the visibility and interactability of the pause panel.
  /// </summary>
  /// <param name="value">True to show and enable the panel, false to hide and disable it.</param>
  public void SetCanvasGroup(bool value)
  {
    canvasGroup.alpha = value ? 1 : 0;
    canvasGroup.interactable = value;
    canvasGroup.blocksRaycasts = value;
    isVisible = value;
  }

  private void Update()
  {
    if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
  }

  private void TogglePause()
  {
    if (Mathf.Approximately(Time.timeScale, 1f)) Pause();
    else Resume();
  }

  /// <summary>
  /// Pauses the game and shows the pause panel.
  /// </summary>
  public void Pause()
  {
    SetCanvasGroup(true);
    Time.timeScale = 0f;
    Cursor.lockState = CursorLockMode.None;
    isPaused = true;
  }

  /// <summary>
  /// Resumes the game and hides the pause panel.
  /// </summary>
  public void Resume()
  {
    SetCanvasGroup(false);
    Time.timeScale = 1f;
    Cursor.lockState = CursorLockMode.Locked;
    isPaused = false;
  }

  /// <summary>
  /// Confirms restarting and restarts the scene.
  /// </summary>
  public void ConfirmRestart()
  {
    RestartAsync().Forget();
  }

  private static async UniTaskVoid RestartAsync()
  {
    await SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    Time.timeScale = 1f;
    Cursor.lockState = CursorLockMode.None;
  }

  /// <summary>
  /// Saves the current game state.
  /// </summary>
  public void Save()
  {
    if (LlamaBrainSaveManager.Instance == null)
    {
      Debug.LogWarning("Cannot save game: LlamaBrainSaveManager.Instance is null.");
      return;
    }
    LlamaBrainSaveManager.Instance.SaveGame();
  }
}
