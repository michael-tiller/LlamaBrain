using UnityEngine;
using UnityEngine.Events;
using LlamaBrain.Runtime.RedRoom.AI;

namespace LlamaBrain.Runtime.RedRoom.UI
{
  [RequireComponent(typeof(Camera))]
  public class RedRoomPlayerRaycast : MonoBehaviour
  {
    private Camera _camera;

    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private float _raycastDistance = 100f;

    private GameObject _previousHitObject;

    public UnityEvent<RaycastHit> OnRaycastHit;
    public UnityEvent OnRaycastMiss;

    private void Awake()
    {
      _camera = GetComponent<Camera>();
    }

    private void Update()
    {
      if (_camera == null) return;

      Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
      if (Physics.Raycast(ray, out RaycastHit hit, _raycastDistance, _layerMask))
      {
        GameObject hitObject = hit.collider.gameObject;

        if (_previousHitObject != hitObject)
        {
          // Check if we hit an NPC (NpcFollowerExample is the main NPC component)
          bool isNpc = hitObject.GetComponentInParent<NpcFollowerExample>() != null;

          if (isNpc)
          {
            _previousHitObject = hitObject;
            OnRaycastHit?.Invoke(hit);
          }
          else if (_previousHitObject != null)
          {
            // Hit something but it's not an NPC - treat as miss
            _previousHitObject = null;
            OnRaycastMiss?.Invoke();
          }
        }
      }
      else if (_previousHitObject != null)
      {
        // Raycast missed
        _previousHitObject = null;
        OnRaycastMiss?.Invoke();
      }
    }
  }
}