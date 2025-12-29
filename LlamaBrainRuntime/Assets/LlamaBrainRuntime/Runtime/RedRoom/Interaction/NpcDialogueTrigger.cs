using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LlamaBrain.Runtime.RedRoom.Interaction
{
  public class NpcDialogueTrigger : MonoBehaviour, ITrigger
  {
    [SerializeField]
    private string _id;

    public System.Guid Id => System.Guid.Parse(_id);


    [SerializeField]
    [TextArea(3, 10)]
    private string promptText = "";
    [SerializeField]
    private List<string> fallbackText = new List<string>();

    private string currentFallbackText = "";

    private const string DEFAULT_PROMPT_TEXT = "MISSING TEXT";

    private string GetRandomFallbackText()
    {
      if (fallbackText.Count == 0) return DEFAULT_PROMPT_TEXT;
      return fallbackText[Random.Range(0, fallbackText.Count)];
    }


    private void Reset()
    {
      if (string.IsNullOrEmpty(_id)) _id = System.Guid.NewGuid().ToString();
    }

    public void Awake()
    {
      if (string.IsNullOrEmpty(_id)) _id = System.Guid.NewGuid().ToString();
    }
    private void Start()
    {
      currentFallbackText = GetRandomFallbackText();
    }

    public void OnCollideEnter(NpcTriggerCollider other)
    {
      Debug.LogFormat("{0}: {1} - {2}: {3}", nameof(NpcDialogueTrigger), nameof(OnCollideEnter), other.gameObject.name, _id, promptText);
    }

    public void OnCollideExit(NpcTriggerCollider other)
    {
      Debug.LogFormat("{0}: {1} - {2}", nameof(NpcDialogueTrigger), nameof(OnCollideExit), other.gameObject.name);
    }

    public string GetTriggerName()
    {
      return gameObject.name;
    }
  }
}
