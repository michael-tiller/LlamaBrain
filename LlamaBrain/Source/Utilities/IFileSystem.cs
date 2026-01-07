using System.IO;

namespace LlamaBrain.Utilities
{
  /// <summary>
  /// Interface for file system operations to enable testing
  /// </summary>
  public interface IFileSystem
  {
    /// <summary>
    /// Creates a directory
    /// </summary>
    /// <param name="path">The path of the directory to create</param>
    void CreateDirectory(string path);

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    /// <param name="path">The path of the file to check</param>
    /// <returns>True if the file exists, false otherwise</returns>
    bool FileExists(string path);

    /// <summary>
    /// Reads all text from a file
    /// </summary>
    /// <param name="path">The path of the file to read</param>
    /// <returns>The contents of the file as a string</returns>
    string ReadAllText(string path);

    /// <summary>
    /// Writes all text to a file
    /// </summary>
    /// <param name="path">The path of the file to write</param>
    /// <param name="contents">The text content to write to the file</param>
    void WriteAllText(string path, string contents);

    /// <summary>
    /// Deletes a file
    /// </summary>
    /// <param name="path">The path of the file to delete</param>
    void DeleteFile(string path);

    /// <summary>
    /// Moves a file from source to destination
    /// </summary>
    /// <param name="sourceFileName">The path of the source file</param>
    /// <param name="destFileName">The path of the destination file</param>
    void MoveFile(string sourceFileName, string destFileName);

    /// <summary>
    /// Atomically replaces a destination file with a source file.
    /// If destinationFileName doesn't exist, this behaves like MoveFile.
    /// Uses atomic replacement when possible to prevent data loss on crash.
    /// </summary>
    /// <param name="sourceFileName">The path of the source file (will be deleted after replacement)</param>
    /// <param name="destinationFileName">The path of the destination file to replace</param>
    void ReplaceFile(string sourceFileName, string destinationFileName);

    /// <summary>
    /// Gets file information
    /// </summary>
    /// <param name="fileName">The path of the file</param>
    /// <returns>An IFileInfo instance containing file information</returns>
    IFileInfo GetFileInfo(string fileName);

    /// <summary>
    /// Gets all files matching a pattern in a directory
    /// </summary>
    /// <param name="path">The directory path to search</param>
    /// <param name="searchPattern">The search pattern to match files (e.g., "*.txt")</param>
    /// <returns>An array of file paths matching the pattern</returns>
    string[] GetFiles(string path, string searchPattern);

    /// <summary>
    /// Combines path strings
    /// </summary>
    /// <param name="path1">The first path component</param>
    /// <param name="path2">The second path component</param>
    /// <returns>A combined path string</returns>
    string CombinePath(string path1, string path2);

    /// <summary>
    /// Gets the full path
    /// </summary>
    /// <param name="path">The path to convert to a full path</param>
    /// <returns>The full absolute path</returns>
    string GetFullPath(string path);

    /// <summary>
    /// Gets the filename without extension
    /// </summary>
    /// <param name="path">The file path</param>
    /// <returns>The filename without its extension</returns>
    string GetFileNameWithoutExtension(string path);
  }

  /// <summary>
  /// Interface for file information
  /// </summary>
  public interface IFileInfo
  {
    /// <summary>
    /// Gets the file length in bytes
    /// </summary>
    long Length { get; }
  }
}

