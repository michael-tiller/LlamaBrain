using Newtonsoft.Json;
using System;
using System.Text;

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
    /// Maximum JSON string length (1MB)
    /// </summary>
    private const int MaxJsonLength = 1024 * 1024;

    /// <summary>
    /// Maximum object depth for JSON deserialization
    /// </summary>
    private const int MaxDepth = 64;

    /// <summary>
    /// Maximum number of properties in a JSON object
    /// </summary>
    private const int MaxProperties = 1000;

    /// <summary>
    /// Serialize an object to JSON
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize</typeparam>
    /// <param name="obj">The object to serialize</param>
    /// <returns>The JSON string</returns>
    public static string Serialize<T>(T obj)
    {
      if (obj == null)
        throw new ArgumentNullException(nameof(obj));

      try
      {
        var json = JsonConvert.SerializeObject(obj);

        // Validate JSON size
        if (json.Length > MaxJsonLength)
        {
          throw new InvalidOperationException($"Serialized JSON too large: {json.Length} characters (max: {MaxJsonLength})");
        }

        return json;
      }
      catch (JsonException ex)
      {
        Logger.Error($"JSON serialization failed: {ex.Message}");
        throw;
      }
      catch (Exception ex)
      {
        Logger.Error($"Unexpected error during JSON serialization: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Serialize an object to JSON with formatting
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize</typeparam>
    /// <param name="obj">The object to serialize</param>
    /// <param name="formatting">The formatting to use</param>
    /// <returns>The JSON string</returns>
    public static string Serialize<T>(T obj, Formatting formatting)
    {
      if (obj == null)
        throw new ArgumentNullException(nameof(obj));

      try
      {
        var json = JsonConvert.SerializeObject(obj, formatting);

        // Validate JSON size
        if (json.Length > MaxJsonLength)
        {
          throw new InvalidOperationException($"Serialized JSON too large: {json.Length} characters (max: {MaxJsonLength})");
        }

        return json;
      }
      catch (JsonException ex)
      {
        Logger.Error($"JSON serialization failed: {ex.Message}");
        throw;
      }
      catch (Exception ex)
      {
        Logger.Error($"Unexpected error during JSON serialization: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Deserialize a JSON string to an object
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize</typeparam>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>The deserialized object</returns>
    public static T? Deserialize<T>(string json) where T : class
    {
      if (string.IsNullOrEmpty(json))
        throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

      // Validate JSON size
      if (json.Length > MaxJsonLength)
      {
        throw new ArgumentException($"JSON string too large: {json.Length} characters (max: {MaxJsonLength})");
      }

      try
      {
        // Create settings with safeguards
        var settings = new JsonSerializerSettings
        {
          MaxDepth = MaxDepth,
          TypeNameHandling = TypeNameHandling.None, // Disable type name handling for security
          ObjectCreationHandling = ObjectCreationHandling.Replace,
          MissingMemberHandling = MissingMemberHandling.Ignore,
          NullValueHandling = NullValueHandling.Ignore,
          DefaultValueHandling = DefaultValueHandling.Include,
          Error = (sender, args) =>
          {
            Logger.Warn($"JSON deserialization warning: {args.ErrorContext.Error.Message}");
            args.ErrorContext.Handled = true;
          }
        };

        var result = JsonConvert.DeserializeObject<T>(json, settings);
        if (result == null)
        {
          Logger.Warn($"Failed to deserialize JSON to type {typeof(T).Name} - received null response");
        }
        return result;
      }
      catch (JsonException ex)
      {
        Logger.Error($"JSON deserialization failed: {ex.Message}");
        throw;
      }
      catch (Exception ex)
      {
        Logger.Error($"Unexpected error during JSON deserialization: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Validate JSON string format
    /// </summary>
    /// <param name="json">The JSON string to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidJson(string json)
    {
      if (string.IsNullOrEmpty(json))
        return false;

      if (json.Length > MaxJsonLength)
        return false;

      try
      {
        // Try to parse the JSON
        var obj = JsonConvert.DeserializeObject(json);
        return obj != null;
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// Safely deserialize JSON with fallback
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize</typeparam>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="fallback">The fallback value if deserialization fails</param>
    /// <returns>The deserialized object or fallback</returns>
    public static T SafeDeserialize<T>(string json, T fallback) where T : class
    {
      try
      {
        var result = Deserialize<T>(json);
        return result ?? fallback;
      }
      catch (Exception ex)
      {
        Logger.Warn($"Safe JSON deserialization failed, using fallback: {ex.Message}");
        return fallback;
      }
    }

    /// <summary>
    /// Get JSON size in bytes
    /// </summary>
    /// <param name="json">The JSON string</param>
    /// <returns>Size in bytes</returns>
    public static int GetJsonSize(string json)
    {
      if (string.IsNullOrEmpty(json))
        return 0;

      return Encoding.UTF8.GetByteCount(json);
    }

    /// <summary>
    /// Truncate JSON if it's too large
    /// </summary>
    /// <param name="json">The JSON string</param>
    /// <param name="maxLength">Maximum length</param>
    /// <returns>Truncated JSON string</returns>
    public static string TruncateJson(string json, int maxLength = MaxJsonLength)
    {
      if (string.IsNullOrEmpty(json))
        return json;

      if (json.Length <= maxLength)
        return json;

      // Try to truncate at a reasonable point
      var truncated = json.Substring(0, maxLength);

      // Try to find a good truncation point (end of an object or array)
      var lastBrace = truncated.LastIndexOf('}');
      var lastBracket = truncated.LastIndexOf(']');
      var lastComma = truncated.LastIndexOf(',');

      var bestPoint = Math.Max(Math.Max(lastBrace, lastBracket), lastComma);

      if (bestPoint > maxLength * 0.8) // Only use if it's not too far back
      {
        truncated = truncated.Substring(0, bestPoint + 1);
      }

      return truncated + "... [truncated]";
    }
  }
}