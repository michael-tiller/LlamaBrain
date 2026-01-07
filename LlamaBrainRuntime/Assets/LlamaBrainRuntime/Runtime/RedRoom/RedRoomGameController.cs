using UnityEngine;

public class RedRoomGameController : MonoBehaviour
{
  public static RedRoomGameController Instance { get; private set; }
  private void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(this.gameObject);
      return;
    }
    Instance = this;
  }
}
