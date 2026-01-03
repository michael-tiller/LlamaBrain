using Newtonsoft.Json;
using System;
using System.Text;
using System.Collections.Generic;

namespace LlamaBrain.Utilities
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

    /// <summary>
    /// Validate JSON schema against a template
    /// </summary>
    /// <param name="json">The JSON string to validate</param>
    /// <param name="requiredProperties">Required property names</param>
    /// <returns>Validation result</returns>
    public static JsonValidationResult ValidateJsonSchema(string json, string[] requiredProperties)
    {
      var result = new JsonValidationResult
      {
        IsValid = true,
        Errors = new List<string>()
      };

      if (string.IsNullOrEmpty(json))
      {
        result.IsValid = false;
        result.Errors.Add("JSON string is null or empty");
        return result;
      }

      try
      {
        var obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        if (obj == null)
        {
          result.IsValid = false;
          result.Errors.Add("JSON could not be parsed as an object");
          return result;
        }

        // Check required properties
        foreach (var property in requiredProperties)
        {
          if (!obj.ContainsKey(property))
          {
            result.IsValid = false;
            result.Errors.Add($"Missing required property: {property}");
          }
        }

        return result;
      }
      catch (Exception ex)
      {
        result.IsValid = false;
        result.Errors.Add($"JSON validation error: {ex.Message}");
        return result;
      }
    }

    /// <summary>
    /// Sanitize JSON string for safe storage
    /// </summary>
    /// <param name="json">The JSON string to sanitize</param>
    /// <returns>Sanitized JSON string</returns>
    public static string SanitizeJson(string json)
    {
      if (string.IsNullOrEmpty(json))
        return json;

      try
      {
        // Parse and re-serialize to remove any potential issues
        var obj = JsonConvert.DeserializeObject(json);
        return JsonConvert.SerializeObject(obj);
      }
      catch (Exception ex)
      {
        Logger.Error($"Error sanitizing JSON: {ex.Message}");
        return json;
      }
    }

    /// <summary>
    /// Compress JSON by removing unnecessary whitespace
    /// </summary>
    /// <param name="json">The JSON string to compress</param>
    /// <returns>Compressed JSON string</returns>
    public static string CompressJson(string json)
    {
      if (string.IsNullOrEmpty(json))
        return json;

      try
      {
        var obj = JsonConvert.DeserializeObject(json);
        return JsonConvert.SerializeObject(obj, Formatting.None);
      }
      catch (Exception ex)
      {
        Logger.Error($"Error compressing JSON: {ex.Message}");
        return json;
      }
    }

    /// <summary>
    /// Pretty print JSON with proper formatting
    /// </summary>
    /// <param name="json">The JSON string to format</param>
    /// <returns>Formatted JSON string</returns>
    public static string PrettyPrintJson(string json)
    {
      if (string.IsNullOrEmpty(json))
        return json;

      try
      {
        var obj = JsonConvert.DeserializeObject(json);
        return JsonConvert.SerializeObject(obj, Formatting.Indented);
      }
      catch (Exception ex)
      {
        Logger.Error($"Error pretty printing JSON: {ex.Message}");
        return json;
      }
    }

    /// <summary>
    /// Get JSON statistics
    /// </summary>
    /// <param name="json">The JSON string</param>
    /// <returns>JSON statistics</returns>
    public static JsonStatistics GetJsonStatistics(string json)
    {
      var stats = new JsonStatistics
      {
        CharacterCount = 0,
        ByteCount = 0,
        PropertyCount = 0,
        ArrayCount = 0,
        NestingDepth = 0
      };

      if (string.IsNullOrEmpty(json))
        return stats;

      try
      {
        stats.CharacterCount = json.Length;
        stats.ByteCount = Encoding.UTF8.GetByteCount(json);

        var obj = JsonConvert.DeserializeObject(json);
        if (obj != null)
        {
          AnalyzeJsonStructure(obj, stats, 0);
        }

        return stats;
      }
      catch (Exception ex)
      {
        Logger.Error($"Error getting JSON statistics: {ex.Message}");
        return stats;
      }
    }

    /// <summary>
    /// Analyze JSON structure recursively
    /// </summary>
    /// <param name="obj">The object to analyze</param>
    /// <param name="stats">Statistics to update</param>
    /// <param name="depth">Current nesting depth</param>
    private static void AnalyzeJsonStructure(object obj, JsonStatistics stats, int depth)
    {
      stats.NestingDepth = Math.Max(stats.NestingDepth, depth);

      if (obj is Newtonsoft.Json.Linq.JObject jObject)
      {
        stats.PropertyCount += jObject.Count;
        foreach (var property in jObject.Properties())
        {
          AnalyzeJsonStructure(property.Value, stats, depth + 1);
        }
      }
      else if (obj is Newtonsoft.Json.Linq.JArray jArray)
      {
        stats.ArrayCount++;
        foreach (var item in jArray)
        {
          AnalyzeJsonStructure(item, stats, depth + 1);
        }
      }
    }
  }

  /// <summary>
  /// JSON validation result
  /// </summary>
  public class JsonValidationResult
  {
    /// <summary>
    /// Whether the JSON is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();
  }

  /// <summary>
  /// JSON statistics
  /// </summary>
  public class JsonStatistics
  {
    /// <summary>
    /// Number of characters
    /// </summary>
    public int CharacterCount { get; set; }

    /// <summary>
    /// Number of bytes
    /// </summary>
    public int ByteCount { get; set; }

    /// <summary>
    /// Number of properties
    /// </summary>
    public int PropertyCount { get; set; }

    /// <summary>
    /// Number of arrays
    /// </summary>
    public int ArrayCount { get; set; }

    /// <summary>
    /// Maximum nesting depth
    /// </summary>
    public int NestingDepth { get; set; }
  }
}