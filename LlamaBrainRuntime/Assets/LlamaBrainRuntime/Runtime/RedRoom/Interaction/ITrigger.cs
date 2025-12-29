using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LlamaBrain.Runtime.RedRoom.Interaction
{
  public interface ITrigger
  {
    System.Guid Id { get; }
    string GetTriggerName();
    void OnCollideEnter(NpcTriggerCollider other);
    void OnCollideExit(NpcTriggerCollider other);
  }
}
