using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LlamaBrain.Runtime.RedRoom.UI
{

  /// <summary>
  /// Example Billboard UI for Red Room.
  /// This script is used to make a UI element face the camera.
  /// </summary>
  public sealed class ExampleBillboardUI : MonoBehaviour
  {
    /// <summary>
    /// The head transform to face the camera towards.
    /// </summary>
    [SerializeField] private Transform head;
    /// <summary>
    /// The offset from the head transform.
    /// </summary>
    [SerializeField] private Vector3 offset = new(0f, 0.2f, 0f);
    /// <summary>
    /// The camera to face the UI towards.
    /// </summary>
    [SerializeField] private Camera cam;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    private void Awake()
    {
      if (!cam) cam = Camera.main;

      var rt = (RectTransform)transform;
      rt.localPosition = Vector3.zero;
      rt.localRotation = Quaternion.identity;
      rt.localScale = Vector3.one * 0.01f;
    }

    /// <summary>
    /// LateUpdate is called after all Update functions have been called.
    /// </summary>
    private void LateUpdate()
    {
      if (!head || !cam) return;

      transform.position = head.position + offset;

      // Face camera
      var fwd = transform.position - cam.transform.position;
      fwd.y = 0f;
      if (fwd.sqrMagnitude > 0.0001f)
        transform.rotation = Quaternion.LookRotation(fwd);
    }
  }
}