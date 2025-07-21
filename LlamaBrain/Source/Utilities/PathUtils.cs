using System;
using System.IO;
using System.Linq;

/// <summary>
/// This namespace contains utility classes
/// </summary>
namespace LlamaBrain.Utilities
{
  /// <summary>
  /// Utility class for path operations
  /// </summary>
  public static class PathUtils
  {
    /// <summary>
    /// Maximum path length
    /// </summary>
    private const int MaxPathLength = 260;

    /// <summary>
    /// Maximum filename length
    /// </summary>
    private const int MaxFilenameLength = 255;

    /// <summary>
    /// Maximum directory depth
    /// </summary>
    private const int MaxDirectoryDepth = 32;

    /// <summary>
    /// Combine a base directory with a relative or absolute path
    /// </summary>
    /// <param name="baseDir">The base directory</param>
    /// <param name="relativeOrAbsolute">The relative or absolute path</param>
    /// <returns>The combined path</returns>
    public static string CombinePath(string baseDir, string relativeOrAbsolute)
    {
      if (string.IsNullOrWhiteSpace(baseDir))
        throw new ArgumentException("Base directory cannot be null or empty", nameof(baseDir));

      if (string.IsNullOrWhiteSpace(relativeOrAbsolute))
        throw new ArgumentException("Relative or absolute path cannot be null or empty", nameof(relativeOrAbsolute));

      // Validate and sanitize base directory
      var safeBaseDir = ValidateAndSanitizePath(baseDir);

      // Check if the path is already absolute
      if (Path.IsPathRooted(relativeOrAbsolute))
      {
        // Validate the absolute path
        var safeAbsolutePath = ValidateAndSanitizePath(relativeOrAbsolute);

        // Check if the absolute path is within the base directory (security check)
        if (!IsPathWithinDirectory(safeAbsolutePath, safeBaseDir))
        {
          throw new ArgumentException("Absolute path is outside the base directory", nameof(relativeOrAbsolute));
        }

        return safeAbsolutePath;
      }

      // Validate and sanitize relative path
      var safeRelativePath = ValidateAndSanitizeRelativePath(relativeOrAbsolute);

      // Combine the paths
      var combinedPath = Path.Combine(safeBaseDir, safeRelativePath);

      // Final validation
      var finalPath = ValidateAndSanitizePath(combinedPath);

      // Check final path length
      if (finalPath.Length > MaxPathLength)
      {
        throw new ArgumentException($"Combined path too long: {finalPath.Length} characters (max: {MaxPathLength})");
      }

      return finalPath;
    }

    /// <summary>
    /// Validate and sanitize a path
    /// </summary>
    /// <param name="path">The path to validate</param>
    /// <returns>The sanitized path</returns>
    public static string ValidateAndSanitizePath(string path)
    {
      if (string.IsNullOrWhiteSpace(path))
        throw new ArgumentException("Path cannot be null or empty", nameof(path));

      // Check for path traversal attempts
      if (ContainsPathTraversal(path))
        throw new ArgumentException("Path contains traversal attempts", nameof(path));

      // Get the full path to resolve any relative paths
      var fullPath = Path.GetFullPath(path);

      // Check for invalid characters
      if (ContainsInvalidCharacters(fullPath))
        throw new ArgumentException("Path contains invalid characters", nameof(path));

      // Check path length
      if (fullPath.Length > MaxPathLength)
        throw new ArgumentException($"Path too long: {fullPath.Length} characters (max: {MaxPathLength})");

      // Check directory depth
      if (GetDirectoryDepth(fullPath) > MaxDirectoryDepth)
        throw new ArgumentException($"Path has too many directory levels: {GetDirectoryDepth(fullPath)} (max: {MaxDirectoryDepth})");

      return fullPath;
    }

    /// <summary>
    /// Validate and sanitize a relative path
    /// </summary>
    /// <param name="relativePath">The relative path to validate</param>
    /// <returns>The sanitized relative path</returns>
    public static string ValidateAndSanitizeRelativePath(string relativePath)
    {
      if (string.IsNullOrWhiteSpace(relativePath))
        throw new ArgumentException("Relative path cannot be null or empty", nameof(relativePath));

      // Check for path traversal attempts
      if (ContainsPathTraversal(relativePath))
        throw new ArgumentException("Relative path contains traversal attempts", nameof(relativePath));

      // Check for invalid characters
      if (ContainsInvalidCharacters(relativePath))
        throw new ArgumentException("Relative path contains invalid characters", nameof(relativePath));

      // Check filename length
      var filename = Path.GetFileName(relativePath);
      if (filename.Length > MaxFilenameLength)
        throw new ArgumentException($"Filename too long: {filename.Length} characters (max: {MaxFilenameLength})");

      return relativePath.Trim();
    }

    /// <summary>
    /// Check if a path contains traversal attempts
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>True if path contains traversal attempts</returns>
    public static bool ContainsPathTraversal(string path)
    {
      if (string.IsNullOrEmpty(path))
        return false;

      // Check for common path traversal patterns
      var normalizedPath = path.Replace('\\', '/').ToLowerInvariant();

      return normalizedPath.Contains("../") ||
             normalizedPath.Contains("..\\") ||
             normalizedPath.Contains("..") ||
             normalizedPath.Contains("//") ||
             normalizedPath.Contains("\\\\") ||
             normalizedPath.StartsWith("/") ||
             normalizedPath.StartsWith("\\");
    }

    /// <summary>
    /// Check if a path contains invalid characters
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>True if path contains invalid characters</returns>
    public static bool ContainsInvalidCharacters(string path)
    {
      if (string.IsNullOrEmpty(path))
        return false;

      var invalidChars = Path.GetInvalidPathChars();
      return path.Any(c => invalidChars.Contains(c));
    }

    /// <summary>
    /// Check if a path is within a directory
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <param name="directory">The directory to check against</param>
    /// <returns>True if the path is within the directory</returns>
    public static bool IsPathWithinDirectory(string path, string directory)
    {
      if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(directory))
        return false;

      try
      {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = Path.GetFullPath(directory);

        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// Get the directory depth of a path
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>The number of directory levels</returns>
    public static int GetDirectoryDepth(string path)
    {
      if (string.IsNullOrEmpty(path))
        return 0;

      try
      {
        var fullPath = Path.GetFullPath(path);
        var parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Length - 1; // Subtract 1 for the root
      }
      catch
      {
        return 0;
      }
    }

    /// <summary>
    /// Create a safe filename from a string
    /// </summary>
    /// <param name="filename">The filename to sanitize</param>
    /// <returns>The safe filename</returns>
    public static string CreateSafeFilename(string filename)
    {
      if (string.IsNullOrEmpty(filename))
        return "unnamed";

      // Remove invalid characters
      var invalidChars = Path.GetInvalidFileNameChars();
      var safeFilename = new string(filename.Where(c => !invalidChars.Contains(c)).ToArray());

      // Remove leading/trailing dots and spaces
      safeFilename = safeFilename.Trim('.', ' ');

      // Limit length
      if (safeFilename.Length > MaxFilenameLength)
        safeFilename = safeFilename.Substring(0, MaxFilenameLength);

      // Ensure it's not empty
      if (string.IsNullOrWhiteSpace(safeFilename))
        safeFilename = "unnamed";

      return safeFilename;
    }

    /// <summary>
    /// Check if a path is safe to use
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>True if the path is safe</returns>
    public static bool IsPathSafe(string path)
    {
      if (string.IsNullOrEmpty(path))
        return false;

      try
      {
        ValidateAndSanitizePath(path);
        return true;
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// Get the relative path from a base directory
    /// </summary>
    /// <param name="fullPath">The full path</param>
    /// <param name="baseDirectory">The base directory</param>
    /// <returns>The relative path</returns>
    public static string GetRelativePath(string fullPath, string baseDirectory)
    {
      if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(baseDirectory))
        throw new ArgumentException("Full path and base directory cannot be null or empty");

      var safeFullPath = ValidateAndSanitizePath(fullPath);
      var safeBaseDirectory = ValidateAndSanitizePath(baseDirectory);

      if (!IsPathWithinDirectory(safeFullPath, safeBaseDirectory))
        throw new ArgumentException("Full path is not within the base directory");

      try
      {
        var relativePath = Path.GetRelativePath(safeBaseDirectory, safeFullPath);
        return ValidateAndSanitizeRelativePath(relativePath);
      }
      catch (Exception ex)
      {
        throw new ArgumentException($"Failed to get relative path: {ex.Message}", ex);
      }
    }
  }
}