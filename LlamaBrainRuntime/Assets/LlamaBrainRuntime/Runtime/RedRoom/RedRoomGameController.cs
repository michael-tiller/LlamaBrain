using UnityEngine;

/// <summary>
/// Singleton game controller for the Red Room demo scene.
/// </summary>
public class RedRoomGameController : MonoBehaviour
{
  /// <summary>
  /// Gets the singleton instance of the RedRoomGameController.
  /// </summary>
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
