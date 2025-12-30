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
    /// The name of the trigger.
    /// </summary>
    string GetTriggerName();
    /// <summary>
    /// Called when the trigger is entered.
    /// </summary>
    void OnCollideEnter(NpcTriggerCollider other);
    /// <summary>
    /// Called when the trigger is exited.
    /// </summary>
    void OnCollideExit(NpcTriggerCollider other);
  }
}
