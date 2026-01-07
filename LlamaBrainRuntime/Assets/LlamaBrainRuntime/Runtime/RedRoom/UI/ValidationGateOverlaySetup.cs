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
    /// If no prefab is assigned, auto-generates the UI.
    /// </summary>
    [ContextMenu("Setup Overlay")]
    public void SetupOverlay()
    {
      GameObject overlayInstance;
      
      if (_overlayPrefab == null)
      {
        Debug.LogWarning("[ValidationGateOverlaySetup] Overlay prefab is not assigned. Auto-generating UI...");
        overlayInstance = GenerateUI();
        if (overlayInstance == null)
        {
          Debug.LogError("[ValidationGateOverlaySetup] Failed to auto-generate UI!");
          return;
        }
      }
      else
      {
        overlayInstance = Instantiate(_overlayPrefab);
      }

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
          canvas = FindAnyObjectByType<Canvas>();
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

    /// <summary>
    /// Auto-generates the Validation Gate Overlay UI hierarchy programmatically.
    /// Creates a layout matching the prefab structure.
    /// </summary>
    private GameObject GenerateUI()
    {
      // Find or create canvas
      var canvas = _targetCanvas;
      if (canvas == null)
      {
        canvas = FindAnyObjectByType<Canvas>();
      }
      if (canvas == null)
      {
        // Create a new canvas if none exists
        var canvasObj = new GameObject("ValidationGateOverlayCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
      }

      // Create root overlay object
      var overlayRoot = new GameObject("ValidationGateOverlay");
      overlayRoot.transform.SetParent(canvas.transform, false);
      var overlay = overlayRoot.AddComponent<ValidationGateOverlay>();

      // Create main overlay panel (full screen, semi-transparent background)
      var overlayPanel = CreatePanel("OverlayPanel", overlayRoot.transform);
      var panelRect = overlayPanel.GetComponent<RectTransform>();
      panelRect.anchorMin = Vector2.zero;
      panelRect.anchorMax = Vector2.one;
      panelRect.sizeDelta = Vector2.zero;
      panelRect.anchoredPosition = Vector2.zero;

      var panelImage = overlayPanel.GetComponent<Image>();
      panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f); // Dark semi-transparent background

      // Create header container (top bar for stats)
      var headerContainer = CreateContainer("HeaderContainer", overlayPanel.transform);
      var headerRect = headerContainer.GetComponent<RectTransform>();
      headerRect.anchorMin = new Vector2(0, 1);
      headerRect.anchorMax = new Vector2(1, 1);
      headerRect.pivot = new Vector2(0.5f, 1);
      headerRect.sizeDelta = new Vector2(0, 40);
      headerRect.anchoredPosition = Vector2.zero;

      var headerLayout = headerContainer.AddComponent<HorizontalLayoutGroup>();
      headerLayout.padding = new RectOffset(10, 10, 5, 5);
      headerLayout.childAlignment = TextAnchor.MiddleLeft;
      headerLayout.childControlWidth = false;
      headerLayout.childControlHeight = true;
      headerLayout.childForceExpandWidth = false;
      headerLayout.childForceExpandHeight = true;

      // Create stats text in header
      var statsText = CreateText("StatsText", headerContainer.transform, "Agent: None");
      var statsRect = statsText.GetComponent<RectTransform>();
      statsRect.sizeDelta = new Vector2(600, 30);
      statsText.fontSize = 14;
      statsText.color = Color.white;

      // Create content container (main split layout)
      var contentContainer = CreateContainer("ContentContainer", overlayPanel.transform);
      var contentRect = contentContainer.GetComponent<RectTransform>();
      contentRect.anchorMin = new Vector2(0, 0);
      contentRect.anchorMax = new Vector2(1, 1);
      contentRect.sizeDelta = new Vector2(0, -40); // Leave room for header
      contentRect.anchoredPosition = new Vector2(0, 0);

      var contentLayout = contentContainer.AddComponent<HorizontalLayoutGroup>();
      contentLayout.padding = new RectOffset(10, 10, 5, 5);
      contentLayout.spacing = 10;
      contentLayout.childControlWidth = true;
      contentLayout.childControlHeight = true;
      contentLayout.childForceExpandWidth = true;
      contentLayout.childForceExpandHeight = true;

      // Create constraints container (left side - 50%)
      var constraintsContainer = CreateContainer("ConstraintsContainer", contentContainer.transform);
      var constraintsRect = constraintsContainer.GetComponent<RectTransform>();
      var constraintsLayout = constraintsContainer.AddComponent<VerticalLayoutGroup>();
      constraintsLayout.padding = new RectOffset(5, 5, 5, 5);
      constraintsLayout.spacing = 5;
      constraintsLayout.childControlWidth = true;
      constraintsLayout.childControlHeight = false;
      constraintsLayout.childForceExpandWidth = true;
      constraintsLayout.childForceExpandHeight = false;
      
      // Add layout element to control flex sizing (50%)
      var constraintsLayoutElement = constraintsContainer.AddComponent<LayoutElement>();
      constraintsLayoutElement.flexibleWidth = 1f;
      constraintsLayoutElement.preferredWidth = 0.5f;

      // Create constraints scroll view
      var constraintsScrollView = CreateScrollView("ConstraintsScrollView", constraintsContainer.transform);
      var constraintsScrollRect = constraintsScrollView.GetComponent<ScrollRect>();
      var constraintsScrollLayout = constraintsScrollView.gameObject.AddComponent<LayoutElement>();
      constraintsScrollLayout.flexibleHeight = 1f;
      var constraintsText = CreateText("ConstraintsText", constraintsScrollView.content, "=== ACTIVE CONSTRAINTS ===\nNo constraints.");
      constraintsText.fontSize = 12;
      constraintsText.color = Color.white;
      constraintsText.alignment = TextAlignmentOptions.TopLeft;
      var constraintsTextRect = constraintsText.GetComponent<RectTransform>();
      constraintsTextRect.anchorMin = new Vector2(0, 1);
      constraintsTextRect.anchorMax = new Vector2(1, 1);
      constraintsTextRect.pivot = new Vector2(0, 1);
      constraintsTextRect.sizeDelta = new Vector2(0, 100);

      // Create validation container (right side - 50%)
      var validationContainer = CreateContainer("ValidationContainer", contentContainer.transform);
      var validationRect = validationContainer.GetComponent<RectTransform>();
      var validationLayout = validationContainer.AddComponent<VerticalLayoutGroup>();
      validationLayout.padding = new RectOffset(5, 5, 5, 5);
      validationLayout.spacing = 5;
      validationLayout.childControlWidth = true;
      validationLayout.childControlHeight = false;
      validationLayout.childForceExpandWidth = true;
      validationLayout.childForceExpandHeight = false;
      
      // Add layout element to control flex sizing (50%)
      var validationLayoutElement = validationContainer.AddComponent<LayoutElement>();
      validationLayoutElement.flexibleWidth = 1f;
      validationLayoutElement.preferredWidth = 0.5f;

      // Create gate status text
      var gateStatusText = CreateText("GateStatusText", validationContainer.transform, "=== VALIDATION GATE ===\nNo validation results yet.");
      gateStatusText.fontSize = 12;
      gateStatusText.color = Color.white;
      gateStatusText.alignment = TextAlignmentOptions.TopLeft;
      var gateStatusRect = gateStatusText.GetComponent<RectTransform>();
      gateStatusRect.sizeDelta = new Vector2(0, 80);

      // Create failures scroll view
      var failuresScrollView = CreateScrollView("FailuresScrollView", validationContainer.transform);
      var failuresScrollRect = failuresScrollView.GetComponent<ScrollRect>();
      var failuresScrollLayout = failuresScrollView.gameObject.AddComponent<LayoutElement>();
      failuresScrollLayout.flexibleHeight = 1f;
      var failuresText = CreateText("FailuresText", failuresScrollView.content, "=== VALIDATION FAILURES ===\nNo failures.");
      failuresText.fontSize = 12;
      failuresText.color = Color.white;
      failuresText.alignment = TextAlignmentOptions.TopLeft;
      var failuresTextRect = failuresText.GetComponent<RectTransform>();
      failuresTextRect.anchorMin = new Vector2(0, 1);
      failuresTextRect.anchorMax = new Vector2(1, 1);
      failuresTextRect.pivot = new Vector2(0, 1);
      failuresTextRect.sizeDelta = new Vector2(0, 150);

      // Create retry status text
      var retryStatusText = CreateText("RetryStatusText", validationContainer.transform, "=== RETRY STATUS ===\nNo inference yet.");
      retryStatusText.fontSize = 12;
      retryStatusText.color = Color.white;
      retryStatusText.alignment = TextAlignmentOptions.TopLeft;
      var retryStatusRect = retryStatusText.GetComponent<RectTransform>();
      retryStatusRect.sizeDelta = new Vector2(0, 80);

      // Create retry history scroll view
      var retryHistoryScrollView = CreateScrollView("RetryHistoryScrollView", validationContainer.transform);
      var retryHistoryScrollRect = retryHistoryScrollView.GetComponent<ScrollRect>();
      var retryHistoryScrollLayout = retryHistoryScrollView.gameObject.AddComponent<LayoutElement>();
      retryHistoryScrollLayout.flexibleHeight = 1f;
      var retryHistoryText = CreateText("RetryHistoryText", retryHistoryScrollView.content, "=== RETRY HISTORY ===\nNo attempts recorded.");
      retryHistoryText.fontSize = 12;
      retryHistoryText.color = Color.white;
      retryHistoryText.alignment = TextAlignmentOptions.TopLeft;
      var retryHistoryTextRect = retryHistoryText.GetComponent<RectTransform>();
      retryHistoryTextRect.anchorMin = new Vector2(0, 1);
      retryHistoryTextRect.anchorMax = new Vector2(1, 1);
      retryHistoryTextRect.pivot = new Vector2(0, 1);
      retryHistoryTextRect.sizeDelta = new Vector2(0, 150);

      // Wire up all references
      WireUpReferences(overlay, overlayRoot);

      Debug.Log("[ValidationGateOverlaySetup] Auto-generated Validation Gate Overlay UI successfully.");
      return overlayRoot;
    }

    /// <summary>
    /// Creates a UI panel with Image component.
    /// </summary>
    private GameObject CreatePanel(string name, Transform parent)
    {
      var panel = new GameObject(name);
      panel.transform.SetParent(parent, false);
      var rect = panel.AddComponent<RectTransform>();
      var image = panel.AddComponent<Image>();
      image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
      return panel;
    }

    /// <summary>
    /// Creates a container with RectTransform (for layout groups).
    /// </summary>
    private GameObject CreateContainer(string name, Transform parent)
    {
      var container = new GameObject(name);
      container.transform.SetParent(parent, false);
      var rect = container.AddComponent<RectTransform>();
      rect.sizeDelta = Vector2.zero;
      return container;
    }

    /// <summary>
    /// Creates a TextMeshProUGUI text component.
    /// </summary>
    private TextMeshProUGUI CreateText(string name, Transform parent, string text)
    {
      var textObj = new GameObject(name);
      textObj.transform.SetParent(parent, false);
      var rect = textObj.AddComponent<RectTransform>();
      var textComp = textObj.AddComponent<TextMeshProUGUI>();
      textComp.text = text;
      textComp.fontSize = 14;
      textComp.color = Color.white;
      textComp.raycastTarget = false;
      return textComp;
    }

    /// <summary>
    /// Creates a ScrollRect with Viewport and Content.
    /// </summary>
    private ScrollRect CreateScrollView(string name, Transform parent)
    {
      var scrollView = new GameObject(name);
      scrollView.transform.SetParent(parent, false);
      var scrollViewRect = scrollView.AddComponent<RectTransform>();
      scrollViewRect.anchorMin = Vector2.zero;
      scrollViewRect.anchorMax = Vector2.one;
      scrollViewRect.sizeDelta = Vector2.zero;
      scrollViewRect.anchoredPosition = Vector2.zero;
      var scrollRect = scrollView.AddComponent<ScrollRect>();
      
      var viewport = new GameObject("Viewport");
      viewport.transform.SetParent(scrollView.transform, false);
      var viewportRect = viewport.AddComponent<RectTransform>();
      viewportRect.anchorMin = Vector2.zero;
      viewportRect.anchorMax = Vector2.one;
      viewportRect.sizeDelta = Vector2.zero;
      viewportRect.anchoredPosition = Vector2.zero;
      
      var viewportImage = viewport.AddComponent<Image>();
      viewportImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
      var mask = viewport.AddComponent<Mask>();
      mask.showMaskGraphic = false;

      var content = new GameObject("Content");
      content.transform.SetParent(viewport.transform, false);
      var contentRect = content.AddComponent<RectTransform>();
      contentRect.anchorMin = new Vector2(0, 1);
      contentRect.anchorMax = new Vector2(1, 1);
      contentRect.pivot = new Vector2(0, 1);
      contentRect.sizeDelta = new Vector2(0, 100);
      contentRect.anchoredPosition = Vector2.zero;

      var contentLayout = content.AddComponent<VerticalLayoutGroup>();
      contentLayout.childControlWidth = true;
      contentLayout.childControlHeight = false;
      contentLayout.childForceExpandWidth = true;
      contentLayout.childForceExpandHeight = false;

      var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
      contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

      scrollRect.content = contentRect;
      scrollRect.viewport = viewportRect;
      scrollRect.horizontal = false;
      scrollRect.vertical = true;
      scrollRect.movementType = ScrollRect.MovementType.Elastic;
      scrollRect.elasticity = 0.1f;
      scrollRect.inertia = true;
      scrollRect.decelerationRate = 0.135f;
      scrollRect.scrollSensitivity = 10f;

      return scrollRect;
    }
  }
}
