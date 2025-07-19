using System.IO;

/// <summary>
/// This namespace contains utility classes for path operations
/// </summary>
namespace UnityBrain.Utilities
{

  /// <summary>
  /// Utility class for path operations
  /// </summary>
  public static class PathUtils
  {
    /// <summary>
    /// Resolve a path relative to a base directory
    /// </summary>
    public static string ResolvePath(string baseDir, string relativeOrAbsolute)
    {
      if (Path.IsPathRooted(relativeOrAbsolute)) return relativeOrAbsolute;
      return Path.Combine(baseDir, relativeOrAbsolute);
    }
  }
}