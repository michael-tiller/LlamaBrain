using System;
using Cysharp.Threading.Tasks;
using LlamaBrain.Runtime.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[RequireComponent(typeof(CanvasGroup))]
public class PausePanel : MonoBehaviour
{
  [SerializeField]
  private CanvasGroup canvasGroup;

  private bool isVisible;
  private bool isPaused;

  private void Reset()
  {
    canvasGroup = GetComponent<CanvasGroup>();
  }

  private void Start()
  {
    SetCanvasGroup(false);
  }

  public void ToggleCanvasGroup()
  {
    SetCanvasGroup(!isVisible);
  }

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

  public void Pause()
  {
    SetCanvasGroup(true);
    Time.timeScale = 0f;
    Cursor.lockState = CursorLockMode.None;
    isPaused = true;
  }

  public void Resume()
  {
    SetCanvasGroup(false);
    Time.timeScale = 1f;
    Cursor.lockState = CursorLockMode.Locked;
    isPaused = false;
  }

  public void ConfirmQuit()
  {
    RestartAsync().Forget();
  }

  private static async UniTaskVoid RestartAsync()
  {
    await SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    Time.timeScale = 1f;
    Cursor.lockState = CursorLockMode.None;
  }

  public void Save()
  {
    LlamaBrainSaveManager.Instance.SaveGame();
  }
}
