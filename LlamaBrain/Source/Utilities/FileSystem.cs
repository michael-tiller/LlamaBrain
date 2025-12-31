using System.IO;

namespace LlamaBrain.Utilities
{
  /// <summary>
  /// Default implementation of IFileSystem using System.IO
  /// </summary>
  public sealed class FileSystem : IFileSystem
  {
    /// <summary>
    /// Creates a directory
    /// </summary>
    /// <param name="path">The path of the directory to create</param>
    public void CreateDirectory(string path)
    {
      Directory.CreateDirectory(path);
    }

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    /// <param name="path">The path of the file to check</param>
    /// <returns>True if the file exists, false otherwise</returns>
    public bool FileExists(string path)
    {
      return File.Exists(path);
    }

    /// <summary>
    /// Reads all text from a file
    /// </summary>
    /// <param name="path">The path of the file to read</param>
    /// <returns>The contents of the file as a string</returns>
    public string ReadAllText(string path)
    {
      return File.ReadAllText(path);
    }

    /// <summary>
    /// Writes all text to a file
    /// </summary>
    /// <param name="path">The path of the file to write</param>
    /// <param name="contents">The text content to write to the file</param>
    public void WriteAllText(string path, string contents)
    {
      File.WriteAllText(path, contents);
    }

    /// <summary>
    /// Deletes a file
    /// </summary>
    /// <param name="path">The path of the file to delete</param>
    public void DeleteFile(string path)
    {
      File.Delete(path);
    }

    /// <summary>
    /// Moves a file from source to destination
    /// </summary>
    /// <param name="sourceFileName">The path of the source file</param>
    /// <param name="destFileName">The path of the destination file</param>
    public void MoveFile(string sourceFileName, string destFileName)
    {
      File.Move(sourceFileName, destFileName);
    }

    /// <summary>
    /// Gets file information
    /// </summary>
    /// <param name="fileName">The path of the file</param>
    /// <returns>An IFileInfo instance containing file information</returns>
    public IFileInfo GetFileInfo(string fileName)
    {
      var fileInfo = new FileInfo(fileName);
      return new FileInfoWrapper(fileInfo);
    }

    /// <summary>
    /// Gets all files matching a pattern in a directory
    /// </summary>
    /// <param name="path">The directory path to search</param>
    /// <param name="searchPattern">The search pattern to match files (e.g., "*.txt")</param>
    /// <returns>An array of file paths matching the pattern</returns>
    public string[] GetFiles(string path, string searchPattern)
    {
      return Directory.GetFiles(path, searchPattern);
    }

    /// <summary>
    /// Combines path strings
    /// </summary>
    /// <param name="path1">The first path component</param>
    /// <param name="path2">The second path component</param>
    /// <returns>A combined path string</returns>
    public string CombinePath(string path1, string path2)
    {
      return Path.Combine(path1, path2);
    }

    /// <summary>
    /// Gets the full path
    /// </summary>
    /// <param name="path">The path to convert to a full path</param>
    /// <returns>The full absolute path</returns>
    public string GetFullPath(string path)
    {
      return Path.GetFullPath(path);
    }

    /// <summary>
    /// Gets the filename without extension
    /// </summary>
    /// <param name="path">The file path</param>
    /// <returns>The filename without its extension</returns>
    public string GetFileNameWithoutExtension(string path)
    {
      return Path.GetFileNameWithoutExtension(path);
    }

    /// <summary>
    /// Wrapper for FileInfo to implement IFileInfo
    /// </summary>
    private sealed class FileInfoWrapper : IFileInfo
    {
      private readonly FileInfo _fileInfo;

      public FileInfoWrapper(FileInfo fileInfo)
      {
        _fileInfo = fileInfo;
      }

      public long Length => _fileInfo.Length;
    }
  }
}

