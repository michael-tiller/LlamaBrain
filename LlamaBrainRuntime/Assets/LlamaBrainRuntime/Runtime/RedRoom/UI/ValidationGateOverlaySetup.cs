using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LlamaBrain.Runtime.RedRoom.UI
{
  /// <summary>
  /// Helper component that instantiates the Validation Gate Overlay UI from a prefab at runtime.
  /// Attach this to the RedRoomCanvas or any persistent GameObject.
  /// </summary>
  public class ValidationGateOverlaySetup : MonoBehaviour
  {
    [Header("Auto-Setup Settings")]
    [SerializeField] private bool _autoSetup = true;
    [SerializeField] private Canvas _targetCanvas;

    [Header("Prefab Reference")]
    [SerializeField] private GameObject _overlayPrefab;

    private void Start()
    {
      if (_autoSetup && ValidationGateOverlay.Instance == null)
      {
        SetupOverlay();
      }
    }

    /// <summary>
    /// Instantiates the overlay UI from prefab and wires up references.
    /// </summary>
    [ContextMenu("Setup Overlay")]
    public void SetupOverlay()
    {
      if (_overlayPrefab == null)
      {
        Debug.LogError("[ValidationGateOverlaySetup] Overlay prefab is not assigned!");
        return;
      }

      // Instantiate prefab
      var overlayInstance = Instantiate(_overlayPrefab);
      overlayInstance.name = "ValidationGateOverlay";

      // Check if prefab has its own Canvas (for editor visibility)
      var prefabCanvas = overlayInstance.GetComponent<Canvas>();
      if (prefabCanvas == null)
      {
        prefabCanvas = overlayInstance.GetComponentInChildren<Canvas>();
      }

      // If prefab doesn't have a Canvas, parent it to the target Canvas
      if (prefabCanvas == null)
      {
        var canvas = _targetCanvas;
        if (canvas == null)
        {
          canvas = FindObjectOfType<Canvas>();
        }
        if (canvas == null)
        {
          Debug.LogError("[ValidationGateOverlaySetup] Prefab has no Canvas and no target Canvas found in scene!");
          Debug.LogError("[ValidationGateOverlaySetup] TO FIX: Add a Canvas component to the prefab root GameObject with Render Mode = Screen Space - Overlay");
          Destroy(overlayInstance);
          return;
        }
        overlayInstance.transform.SetParent(canvas.transform, false);
      }
      else
      {
        // Prefab has its own Canvas - ensure it's set up correctly
        if (prefabCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
          Debug.LogWarning($"[ValidationGateOverlaySetup] Prefab Canvas render mode is {prefabCanvas.renderMode}. For editor visibility, use Screen Space - Overlay.");
        }

        // Ensure GraphicRaycaster exists for interaction
        if (prefabCanvas.GetComponent<GraphicRaycaster>() == null)
        {
          prefabCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }
      }

      // Get the ValidationGateOverlay component
      var overlay = overlayInstance.GetComponent<ValidationGateOverlay>();
      if (overlay == null)
      {
        overlay = overlayInstance.GetComponentInChildren<ValidationGateOverlay>();
      }
      if (overlay == null)
      {
        Debug.LogError("[ValidationGateOverlaySetup] ValidationGateOverlay component not found in prefab!");
        Destroy(overlayInstance);
        return;
      }

      // Wire up references by finding components in the prefab hierarchy
      WireUpReferences(overlay, overlayInstance);

      // Start hidden
      var overlayPanel = FindGameObjectInChildren(overlayInstance, "OverlayPanel");
      if (overlayPanel != null)
      {
        overlayPanel.SetActive(false);
      }

      Debug.Log("[ValidationGateOverlaySetup] Overlay instantiated from prefab successfully. Press F3 to toggle.");
    }

    /// <summary>
    /// Wires up all references from the prefab hierarchy to the ValidationGateOverlay component.
    /// </summary>
    private void WireUpReferences(ValidationGateOverlay overlay, GameObject root)
    {
      int foundCount = 0;
      int missingCount = 0;

      // Find main panel
      var overlayPanel = FindGameObjectInChildren(root, "OverlayPanel");
      if (overlayPanel != null)
      {
        SetPrivateField(overlay, "_overlayPanel", overlayPanel);
        foundCount++;
      }
      else
      {
        Debug.LogWarning("[ValidationGateOverlaySetup] 'OverlayPanel' GameObject not found in prefab!");
        missingCount++;
      }

      // Find layout containers
      var headerContainer = FindComponentInChildren<RectTransform>(root, "HeaderContainer");
      if (headerContainer != null)
      {
        SetPrivateField(overlay, "_headerContainer", headerContainer);
      }

      var contentContainer = FindComponentInChildren<RectTransform>(root, "ContentContainer");
      if (contentContainer != null)
      {
        SetPrivateField(overlay, "_contentContainer", contentContainer);
      }

      var constraintsContainer = FindComponentInChildren<RectTransform>(root, "ConstraintsContainer");
      if (constraintsContainer != null)
      {
        SetPrivateField(overlay, "_constraintsContainer", constraintsContainer);
      }

      var validationContainer = FindComponentInChildren<RectTransform>(root, "ValidationContainer");
      if (validationContainer != null)
      {
        SetPrivateField(overlay, "_validationContainer", validationContainer);
      }

      // Find constraint section
      var constraintsScrollRect = FindComponentInChildren<ScrollRect>(root, "ConstraintsScrollView");
      if (constraintsScrollRect != null)
      {
        SetPrivateField(overlay, "_constraintsScrollRect", constraintsScrollRect);
      }

      var constraintsText = FindComponentInChildren<TextMeshProUGUI>(root, "ConstraintsText");
      if (constraintsText != null)
      {
        SetPrivateField(overlay, "_constraintsText", constraintsText);
      }

      // Find validation section
      var gateStatusText = FindComponentInChildren<TextMeshProUGUI>(root, "GateStatusText");
      if (gateStatusText != null)
      {
        SetPrivateField(overlay, "_gateStatusText", gateStatusText);
      }

      var failuresScrollRect = FindComponentInChildren<ScrollRect>(root, "FailuresScrollView");
      if (failuresScrollRect != null)
      {
        SetPrivateField(overlay, "_failuresScrollRect", failuresScrollRect);
      }

      var failuresText = FindComponentInChildren<TextMeshProUGUI>(root, "FailuresText");
      if (failuresText != null)
      {
        SetPrivateField(overlay, "_failuresText", failuresText);
      }

      // Find retry section
      var retryStatusText = FindComponentInChildren<TextMeshProUGUI>(root, "RetryStatusText");
      if (retryStatusText != null)
      {
        SetPrivateField(overlay, "_retryStatusText", retryStatusText);
      }

      var retryHistoryScrollRect = FindComponentInChildren<ScrollRect>(root, "RetryHistoryScrollView");
      if (retryHistoryScrollRect != null)
      {
        SetPrivateField(overlay, "_retryHistoryScrollRect", retryHistoryScrollRect);
      }

      var retryHistoryText = FindComponentInChildren<TextMeshProUGUI>(root, "RetryHistoryText");
      if (retryHistoryText != null)
      {
        SetPrivateField(overlay, "_retryHistoryText", retryHistoryText);
      }

      // Find stats
      var statsText = FindComponentInChildren<TextMeshProUGUI>(root, "StatsText");
      if (statsText != null)
      {
        SetPrivateField(overlay, "_statsText", statsText);
      }

      // Log summary
      Debug.Log($"[ValidationGateOverlaySetup] Wire-up complete. Found: {foundCount}, Missing: {missingCount}");

      // Warn about critical missing components
      if (overlayPanel == null)
      {
        Debug.LogError("[ValidationGateOverlaySetup] CRITICAL: OverlayPanel not found! The overlay will not be visible.");
      }
    }

    /// <summary>
    /// Finds a GameObject in the hierarchy by name (searches recursively).
    /// </summary>
    private GameObject FindGameObjectInChildren(GameObject root, string name)
    {
      if (root == null) return null;

      // Check root first
      if (root.name == name)
      {
        return root;
      }

      // Search children recursively
      foreach (Transform child in root.transform)
      {
        var found = FindGameObjectInChildren(child.gameObject, name);
        if (found != null) return found;
      }

      return null;
    }

    /// <summary>
    /// Finds a component of type T in the hierarchy by name (searches recursively).
    /// </summary>
    private T FindComponentInChildren<T>(GameObject root, string name) where T : Component
    {
      if (root == null) return null;

      // Check root first
      if (root.name == name)
      {
        var component = root.GetComponent<T>();
        if (component != null) return component;
      }

      // Search children recursively
      foreach (Transform child in root.transform)
      {
        var found = FindComponentInChildren<T>(child.gameObject, name);
        if (found != null) return found;
      }

      return null;
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
      var field = obj.GetType().GetField(fieldName,
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      if (field != null)
      {
        field.SetValue(obj, value);
      }
      else
      {
        Debug.LogWarning($"[ValidationGateOverlaySetup] Field '{fieldName}' not found.");
      }
    }
  }
}
