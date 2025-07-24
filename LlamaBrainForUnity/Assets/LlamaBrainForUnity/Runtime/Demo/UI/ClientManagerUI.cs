using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using LlamaBrain.Unity.Runtime.Core;
using UnityEngine.SceneManagement;

namespace LlamaBrain.Unity.Runtime.Demo.UI
{
  /// <summary>
  /// UI component for managing and monitoring BrainServer client connections
  /// </summary>
  public class ClientManagerUI : MonoBehaviour
  {
    [Header("References")]
    [SerializeField] private BrainServer brainServer;

    [Header("Status UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text connectionStatusText;
    [SerializeField] private TMP_Text serverStatusText;
    [SerializeField] private Image statusIndicator;

    [Header("Control UI")]
    [SerializeField] private Button checkHealthButton;
    [SerializeField] private Button restartServerButton;
    [SerializeField] private Button waitForServerButton;
    [SerializeField] private TMP_InputField timeoutInput;

    [Header("Information UI")]
    [SerializeField] private TMP_Text startupTimeText;
    [SerializeField] private TMP_Text connectionAttemptsText;
    [SerializeField] private TMP_Text errorMessageText;
    [SerializeField] private TMP_Text detailedStatusText;

    [Header("Settings")]
    [SerializeField] private bool autoUpdate = true;
    [SerializeField] private float updateInterval = 2f;
    [SerializeField] private Color connectedColor = Color.green;
    [SerializeField] private Color disconnectedColor = Color.red;
    [SerializeField] private Color checkingColor = Color.yellow;

    private float lastUpdateTime;
    private bool isWaitingForServer = false;

    private void Start()
    {
      StartAsync().Forget();
    }

    private async UniTask StartAsync()
    {
      await UniTask.Delay(1000);

      // Auto-find brain server if not assigned
      if (brainServer == null)
      {
        brainServer = FindObjectOfType<BrainServer>();
      }

      // Setup button listeners
      if (checkHealthButton != null)
        checkHealthButton.onClick.AddListener(CheckServerHealth);

      if (restartServerButton != null)
        restartServerButton.onClick.AddListener(RestartServer);

      if (waitForServerButton != null)
        waitForServerButton.onClick.AddListener(WaitForServer);

      // Set default timeout
      if (timeoutInput != null)
        timeoutInput.text = "30";

      // Initial update
      UpdateUI();

      CheckServerHealth();
    }

    private void Update()
    {
      if (autoUpdate && Time.time - lastUpdateTime > updateInterval)
      {
        UpdateUI();
        lastUpdateTime = Time.time;
      }
    }

    /// <summary>
    /// Update all UI elements with current server status
    /// </summary>
    public void UpdateUI()
    {
      if (brainServer == null)
      {
        SetStatus("No BrainServer Found", disconnectedColor);
        return;
      }

      UpdateStatusUI();
      UpdateControlUI();
      UpdateInformationUI();
    }

    /// <summary>
    /// Update status-related UI elements
    /// </summary>
    private void UpdateStatusUI()
    {
      if (statusText != null)
      {
        statusText.text = brainServer.IsInitialized ? "Initialized" : "Not Initialized";
      }

      if (connectionStatusText != null)
      {
        connectionStatusText.text = brainServer.ConnectionStatus;
      }

      if (serverStatusText != null)
      {
        serverStatusText.text = brainServer.IsServerRunning ? "Running" : "Not Running";
      }

      // Update status indicator color
      if (statusIndicator != null)
      {
        if (brainServer.IsServerRunning)
        {
          SetStatus("Connected", connectedColor);
        }
        else if (brainServer.ConnectionStatus.Contains("Checking") || brainServer.ConnectionStatus.Contains("Waiting"))
        {
          SetStatus("Checking", checkingColor);
        }
        else
        {
          SetStatus("Disconnected", disconnectedColor);
        }
      }
    }

    /// <summary>
    /// Update control-related UI elements
    /// </summary>
    private void UpdateControlUI()
    {
      bool canInteract = brainServer.IsInitialized && !isWaitingForServer;

      if (checkHealthButton != null)
        checkHealthButton.interactable = canInteract;

      if (restartServerButton != null)
        restartServerButton.interactable = canInteract;

      if (waitForServerButton != null)
      {
        waitForServerButton.interactable = canInteract;
        waitForServerButton.GetComponentInChildren<TMP_Text>().text =
            isWaitingForServer ? "Waiting..." : "Wait for Server";
      }
    }

    /// <summary>
    /// Update information-related UI elements
    /// </summary>
    private void UpdateInformationUI()
    {
      if (startupTimeText != null)
      {
        startupTimeText.text = $"Startup Time: {brainServer.ServerStartupTime:F1}s";
      }

      if (connectionAttemptsText != null)
      {
        connectionAttemptsText.text = $"Connection Attempts: {brainServer.ConnectionAttempts}";
      }

      if (errorMessageText != null)
      {
        if (!string.IsNullOrEmpty(brainServer.LastErrorMessage))
        {
          errorMessageText.text = $"Last Error: {brainServer.LastErrorMessage}";
          errorMessageText.color = Color.red;
        }
        else
        {
          errorMessageText.text = "No errors";
          errorMessageText.color = Color.green;
        }
      }

      if (detailedStatusText != null)
      {
        detailedStatusText.text = brainServer.GetServerStatus();
      }
    }

    /// <summary>
    /// Set the status indicator color and text
    /// </summary>
    private void SetStatus(string status, Color color)
    {
      if (statusIndicator != null)
      {
        statusIndicator.color = color;
      }
    }

    /// <summary>
    /// Check server health manually
    /// </summary>
    public async void CheckServerHealth()
    {
      if (brainServer == null) return;

      Debug.Log("[ClientManagerUI] Checking server health...");
      await brainServer.IsServerRunningAsync();
      UpdateUI();
    }

    /// <summary>
    /// Restart the server
    /// </summary>
    public async void RestartServer()
    {
      if (brainServer == null) return;

      Debug.Log("[ClientManagerUI] Restarting server...");
      isWaitingForServer = true;
      UpdateUI();

      brainServer.RestartServer();

      // Wait a bit for the restart to complete
      await UniTask.Delay(5000);
      isWaitingForServer = false;
      UpdateUI();
    }

    /// <summary>
    /// Wait for server to be ready
    /// </summary>
    public async void WaitForServer()
    {
      if (brainServer == null) return;

      Debug.Log("[ClientManagerUI] Waiting for server...");
      isWaitingForServer = true;
      UpdateUI();

      int timeout = 30;
      if (timeoutInput != null && int.TryParse(timeoutInput.text, out int inputTimeout))
      {
        timeout = Mathf.Clamp(inputTimeout, 5, 300);
      }

      var result = await brainServer.WaitForServerAsync(timeout);
      Debug.Log($"[ClientManagerUI] Wait result: {result}");

      isWaitingForServer = false;
      UpdateUI();
    }

    /// <summary>
    /// Manually refresh the UI
    /// </summary>
    [ContextMenu("Refresh UI")]
    public void RefreshUI()
    {
      Debug.Log("[ClientManagerUI] Manual refresh requested");
      UpdateUI();
    }

    /// <summary>
    /// Force find and connect to a BrainServer
    /// </summary>
    [ContextMenu("Force Find Server")]
    public void ForceFindServer()
    {
      brainServer = FindObjectOfType<BrainServer>();
      if (brainServer != null)
      {
        Debug.Log($"[ClientManagerUI] Force found BrainServer: {brainServer.name}");
        UpdateUI();
      }
      else
      {
        Debug.LogWarning("[ClientManagerUI] No BrainServer found in scene");
      }
    }

    /// <summary>
    /// Debug the current state of the UI and server connection
    /// </summary>
    [ContextMenu("Debug UI State")]
    public void DebugUIState()
    {
      Debug.Log($"[ClientManagerUI] Debug UI State:");
      Debug.Log($"  - brainServer: {(brainServer != null ? brainServer.name : "null")}");
      Debug.Log($"  - autoUpdate: {autoUpdate}");
      Debug.Log($"  - updateInterval: {updateInterval}");
      Debug.Log($"  - lastUpdateTime: {lastUpdateTime}");
      Debug.Log($"  - isWaitingForServer: {isWaitingForServer}");

      if (brainServer != null)
      {
        Debug.Log($"  - Server IsInitialized: {brainServer.IsInitialized}");
        Debug.Log($"  - Server IsServerRunning: {brainServer.IsServerRunning}");
        Debug.Log($"  - Server ConnectionStatus: {brainServer.ConnectionStatus}");
        Debug.Log($"  - Server StartupTime: {brainServer.ServerStartupTime}");
        Debug.Log($"  - Server ConnectionAttempts: {brainServer.ConnectionAttempts}");
        Debug.Log($"  - Server LastErrorMessage: {brainServer.LastErrorMessage}");
      }
    }

    /// <summary>
    /// Toggle auto-update mode
    /// </summary>
    [ContextMenu("Toggle Auto Update")]
    public void ToggleAutoUpdate()
    {
      autoUpdate = !autoUpdate;
      Debug.Log($"[ClientManagerUI] Auto-update {(autoUpdate ? "enabled" : "disabled")}");
    }
    public void BackToMainMenu()
    {
      SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
  }
}