using UnityEngine;

namespace LlamaBrain.Runtime.RedRoom.Interaction
{
  /// <summary>
  /// Interface for trigger colliders that can interact with ITrigger objects.
  /// Provides access to essential properties and information needed by trigger objects.
  /// </summary>
  public interface ITriggerCollider
  {
    /// <summary>
    /// Gets the GameObject associated with this trigger collider.
    /// </summary>
    GameObject gameObject { get; }

    /// <summary>
    /// Gets the Transform component of this trigger collider.
    /// </summary>
    Transform transform { get; }
  }
}

