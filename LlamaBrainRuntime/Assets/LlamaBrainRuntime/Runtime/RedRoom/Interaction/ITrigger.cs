using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LlamaBrain.Runtime.RedRoom.Interaction
{
  /// <summary>
  /// Interface for triggers that can be used to interact with the player.
  /// </summary>
  public interface ITrigger
  {
    /// <summary>
    /// The ID of the trigger.
    /// </summary>
    System.Guid Id { get; }
    /// <summary>
    /// Gets the name of the trigger.
    /// </summary>
    /// <returns>The name of the trigger</returns>
    string GetTriggerName();
    /// <summary>
    /// Called when the trigger is entered.
    /// </summary>
    /// <param name="other">The trigger collider that entered</param>
    void OnCollideEnter(NpcTriggerCollider other);
    /// <summary>
    /// Called when the trigger is exited.
    /// </summary>
    /// <param name="other">The trigger collider that exited</param>
    void OnCollideExit(NpcTriggerCollider other);
  }
}
