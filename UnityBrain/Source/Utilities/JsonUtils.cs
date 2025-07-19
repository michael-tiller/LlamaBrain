using Newtonsoft.Json;

/// <summary>
/// This namespace contains utility classes
/// </summary>
namespace UnityBrain.Utilities
{
  /// <summary>
  /// Utility class for JSON serialization and deserialization
  /// </summary>
  public static class JsonUtils
  {
    /// <summary>
    /// Serialize an object to JSON
    /// </summary>
    public static string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj);

    /// <summary>
    /// Deserialize a JSON string to an object
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize</typeparam>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>The deserialized object</returns>
    public static T? Deserialize<T>(string json) where T : class
    {
      var result = JsonConvert.DeserializeObject<T>(json);
      if (result == null)
      {
        Logger.Warn($"Failed to deserialize JSON to type {typeof(T).Name} - received null response");
      }
      return result;
    }
  }
}