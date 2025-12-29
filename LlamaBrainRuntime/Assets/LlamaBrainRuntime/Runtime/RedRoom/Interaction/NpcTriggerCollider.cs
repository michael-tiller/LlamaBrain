using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace LlamaBrain.Runtime.RedRoom.Interaction
{
  /// <summary>
  /// Component that detects when objects with a specific tag enter or exit a trigger or collision zone.
  /// Maintains a list of currently tracked objects and invokes UnityEvents when objects enter, exit, or when the trigger becomes empty.
  /// Supports both trigger colliders and regular collision detection.
  /// </summary>
  [RequireComponent(typeof(Collider))]
  [RequireComponent(typeof(Rigidbody))]
  public class NpcTriggerCollider : MonoBehaviour, ITriggerCollider
  {
    /// <summary>
    /// The collider component to use for the trigger.
    /// </summary>
    [SerializeField]
    new Collider collider;

    /// <summary>
    /// The list of objects that are currently in the trigger.
    /// </summary>
    List<ITrigger> triggerObjects = new List<ITrigger>();


    public List<ITrigger> TriggerObjects => triggerObjects;

    /// <summary>
    /// Event invoked when an object with the trigger tag enters the collider.
    /// The event passes the current list of all tracked trigger objects.
    /// </summary>
    public UnityEvent<List<ITrigger>> onCollisionEnterTrigger;
    /// <summary>
    /// Event invoked when an object with the trigger tag exits the collider.
    /// The event passes the current list of all tracked trigger objects (after the exiting object is removed).
    /// </summary>
    public UnityEvent<List<ITrigger>> onCollisionExitTrigger;
    /// <summary>
    /// Event invoked when the last object with the trigger tag exits the collider, leaving it empty.
    /// The event passes an empty list of trigger objects.
    /// </summary>
    public UnityEvent<List<ITrigger>> onCollisionEmptyTrigger;

    /// <summary>
    /// Unity editor callback that initializes default values when the component is first added or reset.
    /// Automatically finds the Collider component and sets up the Rigidbody.
    /// </summary>
    void Reset()
    {
      collider = GetComponent<Collider>();
      SetupRigidbody();
    }

    /// <summary>
    /// Initializes the component by ensuring the collider is assigned and setting up the Rigidbody.
    /// Called when the GameObject is first created or becomes active.
    /// </summary>
    void Awake()
    {
      if (collider == null)
      {
        collider = GetComponent<Collider>();
      }
      SetupRigidbody();
    }

    /// <summary>
    /// Configures the Rigidbody component to be kinematic and disable gravity.
    /// This ensures the trigger collider doesn't respond to physics forces while still detecting collisions.
    /// </summary>
    void SetupRigidbody()
    {
      Rigidbody rb = GetComponent<Rigidbody>();
      if (rb != null)
      {
        rb.isKinematic = true;
        rb.useGravity = false;
      }
    }

    /// <summary>
    /// Unity callback invoked when another collider enters this trigger collider.
    /// Only processes objects with the configured trigger tag.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    void OnTriggerEnter(Collider other)
    {
      Debug.LogFormat("{0}: {1} - {2}", nameof(NpcTriggerCollider), nameof(OnTriggerEnter), other.gameObject.name);
      if (other.TryGetComponent<ITrigger>(out var trigger))
      {
        OnEnterTrigger(trigger);
      }
    }

    /// <summary>
    /// Unity callback invoked when another collider exits this trigger collider.
    /// Only processes objects with the configured trigger tag.
    /// </summary>
    /// <param name="other">The collider that exited the trigger.</param>
    void OnTriggerExit(Collider other)
    {
      Debug.LogFormat("{0}: {1} - {2}", nameof(NpcTriggerCollider), nameof(OnTriggerExit), other.gameObject.name);
      if (other.TryGetComponent<ITrigger>(out var trigger))
      {
        OnExitTrigger(trigger);
      }
    }

    /// <summary>
    /// Unity callback invoked when this collider collides with a non-trigger collider.
    /// Only processes objects with the configured trigger tag.
    /// </summary>
    /// <param name="collision">The collision data containing information about the collision.</param>
    void OnCollisionEnter(Collision collision)
    {
      Debug.LogFormat("{0}: {1} - {2}", nameof(NpcTriggerCollider), nameof(OnCollisionEnter), collision.gameObject.name);
      if (collision.gameObject.TryGetComponent<ITrigger>(out var trigger))
      {
        OnEnterTrigger(trigger);
      }
    }

    /// <summary>
    /// Unity callback invoked when this collider stops colliding with a non-trigger collider.
    /// Only processes objects with the configured trigger tag.
    /// </summary>
    /// <param name="collision">The collision data containing information about the collision.</param>
    void OnCollisionExit(Collision collision)
    {
      Debug.LogFormat("{0}: {1} - {2}", nameof(NpcTriggerCollider), nameof(OnCollisionExit), collision.gameObject.name);
      if (collision.gameObject.TryGetComponent<ITrigger>(out var trigger))
      {
        OnExitTrigger(trigger);
      }
    }

    /// <summary>
    /// Attempts to add a GameObject to the tracked trigger objects list if it matches the trigger tag and isn't already in the list.
    /// </summary>
    /// <param name="other">The GameObject to add to the tracked list.</param>
    /// <returns>True if the object was added, false if it was already in the list or doesn't match the trigger tag.</returns>
    private bool TryAddTriggerObjectUnique(ITrigger other)
    {
      if (!triggerObjects.Contains(other))
      {
        triggerObjects.Add(other);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Attempts to remove a GameObject from the tracked trigger objects list.
    /// </summary>
    /// <param name="other">The GameObject to remove from the tracked list.</param>
    /// <returns>True if the object was removed, false if it wasn't in the list.</returns>
    private bool TryRemoveTriggerObject(ITrigger other)
    {
      if (triggerObjects.Contains(other))
      {
        triggerObjects.Remove(other);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Called when an object enters the trigger zone. Adds the object to the tracked list and invokes the enter event.
    /// Can be overridden in derived classes to customize behavior.
    /// </summary>
    /// <param name="other">The GameObject that entered the trigger.</param>
    protected virtual void OnEnterTrigger(ITrigger other)
    {
      Debug.LogFormat("{0}: {1} - {2}", nameof(NpcTriggerCollider), nameof(OnEnterTrigger), other.GetTriggerName());
      if (TryAddTriggerObjectUnique(other))
      {
        onCollisionEnterTrigger?.Invoke(triggerObjects);
        other.OnCollideEnter(this);
      }
    }

    /// <summary>
    /// Called when an object exits the trigger zone. Removes the object from the tracked list and invokes the exit event.
    /// Also invokes the empty event if this was the last object in the trigger.
    /// Can be overridden in derived classes to customize behavior.
    /// </summary>
    /// <param name="other">The GameObject that exited the trigger.</param>
    protected virtual void OnExitTrigger(ITrigger other)
    {
      Debug.LogFormat("{0}: {1} - {2}", nameof(NpcTriggerCollider), nameof(OnExitTrigger), other.GetTriggerName());
      if (TryRemoveTriggerObject(other))
      {
        onCollisionExitTrigger?.Invoke(triggerObjects);

        other.OnCollideExit(this);

        if (triggerObjects.Count == 0)
        {
          onCollisionEmptyTrigger?.Invoke(triggerObjects);
        }
      }
    }
  }
}
