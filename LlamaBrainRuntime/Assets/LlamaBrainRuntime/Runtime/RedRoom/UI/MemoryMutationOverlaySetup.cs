using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LlamaBrain.Runtime.RedRoom.UI
{
  /// <summary>
  /// Helper component that instantiates the Memory Mutation Overlay UI from a prefab at runtime.
  /// Attach this to the RedRoomCanvas or any persistent GameObject.
  /// </summary>
  public class MemoryMutationOverlaySetup : MonoBehaviour
  {
    [Header("Auto-Setup Settings")]
    [SerializeField] private bool _autoSetup = true;
    [SerializeField] private Canvas _targetCanvas;

    [Header("Prefab Reference")]
    [SerializeField] private GameObject _overlayPrefab;

    private void Start()
    {
      if (_autoSetup && MemoryMutationOverlay.Instance == null)
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
        Debug.LogError("[MemoryMutationOverlaySetup] Overlay prefab is not assigned!");
        return;
      }

      // Instantiate prefab
      var overlayInstance = Instantiate(_overlayPrefab);
      overlayInstance.name = "MemoryMutationOverlay";

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
          canvas = FindAnyObjectByType<Canvas>();
        }
        if (canvas == null)
        {
          Debug.LogError("[MemoryMutationOverlaySetup] Prefab has no Canvas and no target Canvas found in scene!");
          Debug.LogError("[MemoryMutationOverlaySetup] TO FIX: Add a Canvas component to the prefab root GameObject with Render Mode = Screen Space - Overlay");
          Destroy(overlayInstance);
          return;
        }
        overlayInstance.transform.SetParent(canvas.transform, false);
      }
      else
      {
        // Prefab has its own Canvas - ensure it's set up correctly for editor visibility
        if (prefabCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
          Debug.LogWarning($"[MemoryMutationOverlaySetup] Prefab Canvas render mode is {prefabCanvas.renderMode}. For editor visibility, use Screen Space - Overlay.");
        }
        
        // Ensure GraphicRaycaster exists for interaction
        if (prefabCanvas.GetComponent<GraphicRaycaster>() == null)
        {
          prefabCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }
      }

      // Get the MemoryMutationOverlay component (should be on root or find it)
      var overlay = overlayInstance.GetComponent<MemoryMutationOverlay>();
      if (overlay == null)
      {
        overlay = overlayInstance.GetComponentInChildren<MemoryMutationOverlay>();
      }
      if (overlay == null)
      {
        Debug.LogError("[MemoryMutationOverlaySetup] MemoryMutationOverlay component not found in prefab!");
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

      Debug.Log("[MemoryMutationOverlaySetup] Overlay instantiated from prefab successfully. Press F2 to toggle.");
    }

    /// <summary>
    /// Wires up all references from the prefab hierarchy to the MemoryMutationOverlay component.
    /// </summary>
    private void WireUpReferences(MemoryMutationOverlay overlay, GameObject root)
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
        Debug.LogWarning("[MemoryMutationOverlaySetup] 'OverlayPanel' GameObject not found in prefab!");
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

      var memoryContainer = FindComponentInChildren<RectTransform>(root, "MemoryContainer");
      if (memoryContainer != null)
      {
        SetPrivateField(overlay, "_memoryContainer", memoryContainer);
      }

      var mutationLogContainer = FindComponentInChildren<RectTransform>(root, "MutationLogContainer");
      if (mutationLogContainer != null)
      {
        SetPrivateField(overlay, "_mutationLogContainer", mutationLogContainer);
      }

      // Find text components
      var memoryText = FindComponentInChildren<TextMeshProUGUI>(root, "MemoryText");
      if (memoryText != null)
      {
        SetPrivateField(overlay, "_memoryText", memoryText);
      }

      var mutationLogText = FindComponentInChildren<TextMeshProUGUI>(root, "MutationLogText");
      if (mutationLogText != null)
      {
        SetPrivateField(overlay, "_mutationLogText", mutationLogText);
      }

      var statsText = FindComponentInChildren<TextMeshProUGUI>(root, "StatsText");
      if (statsText != null)
      {
        SetPrivateField(overlay, "_statsText", statsText);
      }

      // Find toggles
      var canonicalToggle = FindComponentInChildren<Toggle>(root, "ShowCanonical");
      if (canonicalToggle != null)
      {
        SetPrivateField(overlay, "_showCanonicalFacts", canonicalToggle);
      }

      var worldStateToggle = FindComponentInChildren<Toggle>(root, "ShowWorldState");
      if (worldStateToggle != null)
      {
        SetPrivateField(overlay, "_showWorldState", worldStateToggle);
      }

      var episodicToggle = FindComponentInChildren<Toggle>(root, "ShowEpisodic");
      if (episodicToggle != null)
      {
        SetPrivateField(overlay, "_showEpisodicMemories", episodicToggle);
      }

      var beliefToggle = FindComponentInChildren<Toggle>(root, "ShowBeliefs");
      if (beliefToggle != null)
      {
        SetPrivateField(overlay, "_showBeliefs", beliefToggle);
      }

      var mutationToggle = FindComponentInChildren<Toggle>(root, "ShowMutations");
      if (mutationToggle != null)
      {
        SetPrivateField(overlay, "_showMutationLog", mutationToggle);
      }

      // Optional: Find individual scroll areas if they exist in the prefab
      var canonicalFactsScrollRect = FindComponentInChildren<ScrollRect>(root, "CanonicalFactsScrollView");
      if (canonicalFactsScrollRect != null)
      {
        SetPrivateField(overlay, "_canonicalFactsScrollRect", canonicalFactsScrollRect);
      }

      var canonicalFactsText = FindComponentInChildren<TextMeshProUGUI>(root, "CanonicalFactsText");
      if (canonicalFactsText != null)
      {
        SetPrivateField(overlay, "_canonicalFactsText", canonicalFactsText);
      }

      var worldStateScrollRect = FindComponentInChildren<ScrollRect>(root, "WorldStateScrollView");
      if (worldStateScrollRect != null)
      {
        SetPrivateField(overlay, "_worldStateScrollRect", worldStateScrollRect);
      }

      var worldStateText = FindComponentInChildren<TextMeshProUGUI>(root, "WorldStateText");
      if (worldStateText != null)
      {
        SetPrivateField(overlay, "_worldStateText", worldStateText);
      }

      var episodicMemoriesScrollRect = FindComponentInChildren<ScrollRect>(root, "EpisodicMemoriesScrollView");
      if (episodicMemoriesScrollRect != null)
      {
        SetPrivateField(overlay, "_episodicMemoriesScrollRect", episodicMemoriesScrollRect);
      }

      var episodicMemoriesText = FindComponentInChildren<TextMeshProUGUI>(root, "EpisodicMemoriesText");
      if (episodicMemoriesText != null)
      {
        SetPrivateField(overlay, "_episodicMemoriesText", episodicMemoriesText);
      }

      var beliefsScrollRect = FindComponentInChildren<ScrollRect>(root, "BeliefsScrollView");
      if (beliefsScrollRect != null)
      {
        SetPrivateField(overlay, "_beliefsScrollRect", beliefsScrollRect);
      }

      var beliefsText = FindComponentInChildren<TextMeshProUGUI>(root, "BeliefsText");
      if (beliefsText != null)
      {
        SetPrivateField(overlay, "_beliefsText", beliefsText);
      }

      // Log summary
      Debug.Log($"[MemoryMutationOverlaySetup] Wire-up complete. Found: {foundCount}, Missing: {missingCount}");
      
      // Warn about critical missing components
      if (overlayPanel == null)
      {
        Debug.LogError("[MemoryMutationOverlaySetup] CRITICAL: OverlayPanel not found! The overlay will not be visible.");
      }
      if (FindComponentInChildren<TextMeshProUGUI>(root, "MemoryText") == null && 
          FindComponentInChildren<TextMeshProUGUI>(root, "StatsText") == null)
      {
        Debug.LogWarning("[MemoryMutationOverlaySetup] No text components found. The overlay may not display content.");
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
        Debug.LogWarning($"[MemoryMutationOverlaySetup] Field '{fieldName}' not found.");
      }
    }
  }
}
