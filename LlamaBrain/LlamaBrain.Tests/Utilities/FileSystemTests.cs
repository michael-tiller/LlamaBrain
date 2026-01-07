using System;
using System.IO;
using NUnit.Framework;
using LlamaBrain.Utilities;

namespace LlamaBrain.Tests.Utilities
{
  /// <summary>
  /// Tests for FileSystem utility class.
  /// </summary>
  public class FileSystemTests
  {
    private FileSystem _fileSystem = null!;
    private string _testBaseDir = null!;
    private string _testFile = null!;

    [SetUp]
    public void SetUp()
    {
      _fileSystem = new FileSystem();
      _testBaseDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      _testFile = Path.Combine(_testBaseDir, "test.txt");
      
      // Ensure test directory exists
      Directory.CreateDirectory(_testBaseDir);
    }

    [TearDown]
    public void TearDown()
    {
      // Clean up test files and directories
      try
      {
        if (Directory.Exists(_testBaseDir))
        {
          Directory.Delete(_testBaseDir, true);
        }
      }
      catch
      {
        // Ignore cleanup errors
      }
    }

    #region CreateDirectory Tests

    [Test]
    public void CreateDirectory_NewDirectory_CreatesSuccessfully()
    {
      // Arrange
      var newDir = Path.Combine(_testBaseDir, "newdir");

      // Act
      _fileSystem.CreateDirectory(newDir);

      // Assert
      Assert.That(Directory.Exists(newDir), Is.True);
    }

    [Test]
    public void CreateDirectory_ExistingDirectory_DoesNotThrow()
    {
      // Arrange
      var existingDir = Path.Combine(_testBaseDir, "existing");
      Directory.CreateDirectory(existingDir);

      // Act & Assert
      Assert.DoesNotThrow(() => _fileSystem.CreateDirectory(existingDir));
      Assert.That(Directory.Exists(existingDir), Is.True);
    }

    [Test]
    public void CreateDirectory_NestedDirectory_CreatesAllLevels()
    {
      // Arrange
      var nestedDir = Path.Combine(_testBaseDir, "level1", "level2", "level3");

      // Act
      _fileSystem.CreateDirectory(nestedDir);

      // Assert
      Assert.That(Directory.Exists(nestedDir), Is.True);
    }

    #endregion

    #region FileExists Tests

    [Test]
    public void FileExists_ExistingFile_ReturnsTrue()
    {
      // Arrange
      File.WriteAllText(_testFile, "test content");

      // Act
      var result = _fileSystem.FileExists(_testFile);

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public void FileExists_NonExistentFile_ReturnsFalse()
    {
      // Arrange
      var nonExistentFile = Path.Combine(_testBaseDir, "nonexistent.txt");

      // Act
      var result = _fileSystem.FileExists(nonExistentFile);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void FileExists_DirectoryPath_ReturnsFalse()
    {
      // Act
      var result = _fileSystem.FileExists(_testBaseDir);

      // Assert
      Assert.That(result, Is.False);
    }

    #endregion

    #region ReadAllText Tests

    [Test]
    public void ReadAllText_ExistingFile_ReturnsContent()
    {
      // Arrange
      var content = "Hello, World!";
      File.WriteAllText(_testFile, content);

      // Act
      var result = _fileSystem.ReadAllText(_testFile);

      // Assert
      Assert.That(result, Is.EqualTo(content));
    }

    [Test]
    public void ReadAllText_EmptyFile_ReturnsEmptyString()
    {
      // Arrange
      File.WriteAllText(_testFile, string.Empty);

      // Act
      var result = _fileSystem.ReadAllText(_testFile);

      // Assert
      Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ReadAllText_MultilineContent_ReturnsAllLines()
    {
      // Arrange
      var content = "Line 1\nLine 2\nLine 3";
      File.WriteAllText(_testFile, content);

      // Act
      var result = _fileSystem.ReadAllText(_testFile);

      // Assert
      Assert.That(result, Is.EqualTo(content));
    }

    [Test]
    public void ReadAllText_NonExistentFile_ThrowsFileNotFoundException()
    {
      // Arrange
      var nonExistentFile = Path.Combine(_testBaseDir, "nonexistent.txt");

      // Act & Assert
      Assert.Throws<FileNotFoundException>(() => _fileSystem.ReadAllText(nonExistentFile));
    }

    #endregion

    #region WriteAllText Tests

    [Test]
    public void WriteAllText_NewFile_CreatesAndWritesContent()
    {
      // Arrange
      var content = "Test content";

      // Act
      _fileSystem.WriteAllText(_testFile, content);

      // Assert
      Assert.That(File.Exists(_testFile), Is.True);
      Assert.That(File.ReadAllText(_testFile), Is.EqualTo(content));
    }

    [Test]
    public void WriteAllText_ExistingFile_OverwritesContent()
    {
      // Arrange
      File.WriteAllText(_testFile, "Old content");
      var newContent = "New content";

      // Act
      _fileSystem.WriteAllText(_testFile, newContent);

      // Assert
      Assert.That(File.ReadAllText(_testFile), Is.EqualTo(newContent));
    }

    [Test]
    public void WriteAllText_EmptyContent_CreatesEmptyFile()
    {
      // Act
      _fileSystem.WriteAllText(_testFile, string.Empty);

      // Assert
      Assert.That(File.Exists(_testFile), Is.True);
      Assert.That(File.ReadAllText(_testFile), Is.EqualTo(string.Empty));
    }

    #endregion

    #region DeleteFile Tests

    [Test]
    public void DeleteFile_ExistingFile_DeletesSuccessfully()
    {
      // Arrange
      File.WriteAllText(_testFile, "content");

      // Act
      _fileSystem.DeleteFile(_testFile);

      // Assert
      Assert.That(File.Exists(_testFile), Is.False);
    }

    [Test]
    public void DeleteFile_NonExistentFile_DoesNotThrow()
    {
      // Arrange
      var nonExistentFile = Path.Combine(_testBaseDir, "nonexistent.txt");

      // Act & Assert - File.Delete doesn't throw for non-existent files
      Assert.DoesNotThrow(() => _fileSystem.DeleteFile(nonExistentFile));
    }

    #endregion

    #region MoveFile Tests

    [Test]
    public void MoveFile_ExistingFile_MovesSuccessfully()
    {
      // Arrange
      var content = "Test content";
      File.WriteAllText(_testFile, content);
      var destFile = Path.Combine(_testBaseDir, "moved.txt");

      // Act
      _fileSystem.MoveFile(_testFile, destFile);

      // Assert
      Assert.That(File.Exists(_testFile), Is.False);
      Assert.That(File.Exists(destFile), Is.True);
      Assert.That(File.ReadAllText(destFile), Is.EqualTo(content));
    }

    [Test]
    public void MoveFile_NonExistentSource_ThrowsFileNotFoundException()
    {
      // Arrange
      var nonExistentFile = Path.Combine(_testBaseDir, "nonexistent.txt");
      var destFile = Path.Combine(_testBaseDir, "dest.txt");

      // Act & Assert
      Assert.Throws<FileNotFoundException>(() => _fileSystem.MoveFile(nonExistentFile, destFile));
    }

    [Test]
    public void MoveFile_ExistingDestination_ThrowsIOException()
    {
      // Arrange
      File.WriteAllText(_testFile, "source content");
      var destFile = Path.Combine(_testBaseDir, "dest.txt");
      File.WriteAllText(destFile, "dest content");

      // Act & Assert
      Assert.Throws<IOException>(() => _fileSystem.MoveFile(_testFile, destFile));
    }

    [Test]
    public void MoveFile_ToDifferentDirectory_MovesSuccessfully()
    {
      // Arrange
      var content = "Test content";
      File.WriteAllText(_testFile, content);
      var subDir = Path.Combine(_testBaseDir, "subdir");
      Directory.CreateDirectory(subDir);
      var destFile = Path.Combine(subDir, "moved.txt");

      // Act
      _fileSystem.MoveFile(_testFile, destFile);

      // Assert
      Assert.That(File.Exists(_testFile), Is.False);
      Assert.That(File.Exists(destFile), Is.True);
      Assert.That(File.ReadAllText(destFile), Is.EqualTo(content));
    }

    #endregion

    #region GetFileInfo Tests

    [Test]
    public void GetFileInfo_ExistingFile_ReturnsFileInfo()
    {
      // Arrange
      var content = "Test content";
      File.WriteAllText(_testFile, content);

      // Act
      var fileInfo = _fileSystem.GetFileInfo(_testFile);

      // Assert
      Assert.That(fileInfo, Is.Not.Null);
      Assert.That(fileInfo.Length, Is.EqualTo(content.Length));
    }

    [Test]
    public void GetFileInfo_EmptyFile_ReturnsZeroLength()
    {
      // Arrange
      File.WriteAllText(_testFile, string.Empty);

      // Act
      var fileInfo = _fileSystem.GetFileInfo(_testFile);

      // Assert
      Assert.That(fileInfo, Is.Not.Null);
      Assert.That(fileInfo.Length, Is.EqualTo(0));
    }

    [Test]
    public void GetFileInfo_LargeFile_ReturnsCorrectLength()
    {
      // Arrange
      var largeContent = new string('A', 10000);
      File.WriteAllText(_testFile, largeContent);

      // Act
      var fileInfo = _fileSystem.GetFileInfo(_testFile);

      // Assert
      Assert.That(fileInfo, Is.Not.Null);
      Assert.That(fileInfo.Length, Is.EqualTo(largeContent.Length));
    }

    [Test]
    public void GetFileInfo_NonExistentFile_ThrowsOnPropertyAccess()
    {
      // Arrange
      var nonExistentFile = Path.Combine(_testBaseDir, "nonexistent.txt");

      // Act
      var fileInfo = _fileSystem.GetFileInfo(nonExistentFile);

      // Assert - FileInfo constructor doesn't throw, but accessing Length does
      Assert.That(fileInfo, Is.Not.Null);
      Assert.Throws<FileNotFoundException>(() => { var length = fileInfo.Length; });
    }

    #endregion

    #region GetFiles Tests

    [Test]
    public void GetFiles_MatchingPattern_ReturnsMatchingFiles()
    {
      // Arrange
      File.WriteAllText(Path.Combine(_testBaseDir, "file1.txt"), "content");
      File.WriteAllText(Path.Combine(_testBaseDir, "file2.txt"), "content");
      File.WriteAllText(Path.Combine(_testBaseDir, "file3.dat"), "content");

      // Act
      var files = _fileSystem.GetFiles(_testBaseDir, "*.txt");

      // Assert
      Assert.That(files, Is.Not.Null);
      Assert.That(files.Length, Is.EqualTo(2));
      Assert.That(Array.Exists(files, f => f.EndsWith("file1.txt")), Is.True);
      Assert.That(Array.Exists(files, f => f.EndsWith("file2.txt")), Is.True);
    }

    [Test]
    public void GetFiles_NoMatches_ReturnsEmptyArray()
    {
      // Arrange
      File.WriteAllText(Path.Combine(_testBaseDir, "file1.txt"), "content");

      // Act
      var files = _fileSystem.GetFiles(_testBaseDir, "*.dat");

      // Assert
      Assert.That(files, Is.Not.Null);
      Assert.That(files.Length, Is.EqualTo(0));
    }

    [Test]
    public void GetFiles_AllFiles_ReturnsAllFiles()
    {
      // Arrange
      File.WriteAllText(Path.Combine(_testBaseDir, "file1.txt"), "content");
      File.WriteAllText(Path.Combine(_testBaseDir, "file2.dat"), "content");

      // Act
      var files = _fileSystem.GetFiles(_testBaseDir, "*.*");

      // Assert
      Assert.That(files, Is.Not.Null);
      Assert.That(files.Length, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void GetFiles_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
      // Arrange
      var nonExistentDir = Path.Combine(_testBaseDir, "nonexistent");

      // Act & Assert
      Assert.Throws<DirectoryNotFoundException>(() => _fileSystem.GetFiles(nonExistentDir, "*.*"));
    }

    [Test]
    public void GetFiles_EmptyDirectory_ReturnsEmptyArray()
    {
      // Act
      var files = _fileSystem.GetFiles(_testBaseDir, "*.*");

      // Assert
      Assert.That(files, Is.Not.Null);
      Assert.That(files.Length, Is.EqualTo(0));
    }

    #endregion

    #region CombinePath Tests

    [Test]
    public void CombinePath_TwoPaths_ReturnsCombinedPath()
    {
      // Arrange
      var path1 = "C:\\temp";
      var path2 = "file.txt";

      // Act
      var result = _fileSystem.CombinePath(path1, path2);

      // Assert
      Assert.That(result, Is.EqualTo(Path.Combine(path1, path2)));
    }

    [Test]
    public void CombinePath_MultipleLevels_ReturnsCombinedPath()
    {
      // Arrange
      var path1 = "C:\\temp";
      var path2 = "subdir\\file.txt";

      // Act
      var result = _fileSystem.CombinePath(path1, path2);

      // Assert
      Assert.That(result, Is.EqualTo(Path.Combine(path1, path2)));
    }

    [Test]
    public void CombinePath_RelativePaths_ReturnsCombinedPath()
    {
      // Arrange
      var path1 = "folder1";
      var path2 = "folder2\\file.txt";

      // Act
      var result = _fileSystem.CombinePath(path1, path2);

      // Assert
      Assert.That(result, Is.EqualTo(Path.Combine(path1, path2)));
    }

    [Test]
    public void CombinePath_EmptySecondPath_ReturnsFirstPath()
    {
      // Arrange
      var path1 = "C:\\temp";
      var path2 = string.Empty;

      // Act
      var result = _fileSystem.CombinePath(path1, path2);

      // Assert
      Assert.That(result, Is.EqualTo(Path.Combine(path1, path2)));
    }

    #endregion

    #region GetFullPath Tests

    [Test]
    public void GetFullPath_RelativePath_ReturnsFullPath()
    {
      // Arrange
      var relativePath = "test.txt";

      // Act
      var result = _fileSystem.GetFullPath(relativePath);

      // Assert
      Assert.That(Path.IsPathRooted(result), Is.True);
      Assert.That(result, Does.EndWith("test.txt"));
    }

    [Test]
    public void GetFullPath_AbsolutePath_ReturnsSamePath()
    {
      // Arrange
      var absolutePath = Path.GetFullPath(_testBaseDir);

      // Act
      var result = _fileSystem.GetFullPath(absolutePath);

      // Assert
      Assert.That(result, Is.EqualTo(absolutePath));
    }

    [Test]
    public void GetFullPath_CurrentDirectory_ReturnsFullPath()
    {
      // Arrange
      var currentDir = ".";

      // Act
      var result = _fileSystem.GetFullPath(currentDir);

      // Assert
      Assert.That(Path.IsPathRooted(result), Is.True);
    }

    [Test]
    public void GetFullPath_ParentDirectory_ReturnsFullPath()
    {
      // Arrange
      var parentDir = "..";

      // Act
      var result = _fileSystem.GetFullPath(parentDir);

      // Assert
      Assert.That(Path.IsPathRooted(result), Is.True);
    }

    #endregion

    #region GetFileNameWithoutExtension Tests

    [Test]
    public void GetFileNameWithoutExtension_FileWithExtension_ReturnsNameWithoutExtension()
    {
      // Arrange
      var path = "C:\\temp\\file.txt";

      // Act
      var result = _fileSystem.GetFileNameWithoutExtension(path);

      // Assert
      Assert.That(result, Is.EqualTo("file"));
    }

    [Test]
    public void GetFileNameWithoutExtension_FileWithoutExtension_ReturnsFileName()
    {
      // Arrange
      var path = "C:\\temp\\file";

      // Act
      var result = _fileSystem.GetFileNameWithoutExtension(path);

      // Assert
      Assert.That(result, Is.EqualTo("file"));
    }

    [Test]
    public void GetFileNameWithoutExtension_MultipleExtensions_ReturnsNameWithoutLastExtension()
    {
      // Arrange
      var path = "C:\\temp\\file.tar.gz";

      // Act
      var result = _fileSystem.GetFileNameWithoutExtension(path);

      // Assert
      Assert.That(result, Is.EqualTo("file.tar"));
    }

    [Test]
    public void GetFileNameWithoutExtension_RelativePath_ReturnsNameWithoutExtension()
    {
      // Arrange
      var path = "folder\\file.txt";

      // Act
      var result = _fileSystem.GetFileNameWithoutExtension(path);

      // Assert
      Assert.That(result, Is.EqualTo("file"));
    }

    [Test]
    public void GetFileNameWithoutExtension_JustFileName_ReturnsNameWithoutExtension()
    {
      // Arrange
      var path = "file.txt";

      // Act
      var result = _fileSystem.GetFileNameWithoutExtension(path);

      // Assert
      Assert.That(result, Is.EqualTo("file"));
    }

    #endregion

    #region ReplaceFile Tests

    [Test]
    public void ReplaceFile_DestinationDoesNotExist_MovesSourceToDestination()
    {
      // Arrange
      var sourceContent = "Source content";
      var sourceFile = Path.Combine(_testBaseDir, "source.txt");
      var destFile = Path.Combine(_testBaseDir, "dest.txt");
      File.WriteAllText(sourceFile, sourceContent);

      // Act
      _fileSystem.ReplaceFile(sourceFile, destFile);

      // Assert
      Assert.That(File.Exists(sourceFile), Is.False);
      Assert.That(File.Exists(destFile), Is.True);
      Assert.That(File.ReadAllText(destFile), Is.EqualTo(sourceContent));
    }

    [Test]
    public void ReplaceFile_DestinationExists_ReplacesAtomically()
    {
      // Arrange
      var sourceContent = "New content";
      var destContent = "Old content";
      var sourceFile = Path.Combine(_testBaseDir, "source.txt");
      var destFile = Path.Combine(_testBaseDir, "dest.txt");
      File.WriteAllText(sourceFile, sourceContent);
      File.WriteAllText(destFile, destContent);

      // Act
      _fileSystem.ReplaceFile(sourceFile, destFile);

      // Assert
      Assert.That(File.Exists(sourceFile), Is.False);
      Assert.That(File.Exists(destFile), Is.True);
      Assert.That(File.ReadAllText(destFile), Is.EqualTo(sourceContent));
    }

    [Test]
    public void ReplaceFile_DestinationExists_CleansUpBackupFile()
    {
      // Arrange
      var sourceFile = Path.Combine(_testBaseDir, "source.txt");
      var destFile = Path.Combine(_testBaseDir, "dest.txt");
      var backupFile = destFile + ".bak";
      File.WriteAllText(sourceFile, "source");
      File.WriteAllText(destFile, "dest");

      // Act
      _fileSystem.ReplaceFile(sourceFile, destFile);

      // Assert
      Assert.That(File.Exists(backupFile), Is.False);
    }

    [Test]
    public void ReplaceFile_StaleBackupExists_RemovesStaleBackup()
    {
      // Arrange
      var sourceFile = Path.Combine(_testBaseDir, "source.txt");
      var destFile = Path.Combine(_testBaseDir, "dest.txt");
      var backupFile = destFile + ".bak";
      File.WriteAllText(sourceFile, "source");
      File.WriteAllText(destFile, "dest");
      File.WriteAllText(backupFile, "stale backup");

      // Act
      _fileSystem.ReplaceFile(sourceFile, destFile);

      // Assert
      Assert.That(File.Exists(backupFile), Is.False);
      Assert.That(File.ReadAllText(destFile), Is.EqualTo("source"));
    }

    [Test]
    public void ReplaceFile_NonExistentSource_ThrowsFileNotFoundException()
    {
      // Arrange
      var nonExistentSource = Path.Combine(_testBaseDir, "nonexistent.txt");
      var destFile = Path.Combine(_testBaseDir, "dest.txt");

      // Act & Assert
      Assert.Throws<FileNotFoundException>(() => _fileSystem.ReplaceFile(nonExistentSource, destFile));
    }

    [Test]
    public void ReplaceFile_PreservesContentIntegrity()
    {
      // Arrange - test with larger content to ensure atomic replacement
      var sourceContent = new string('X', 10000);
      var destContent = new string('Y', 5000);
      var sourceFile = Path.Combine(_testBaseDir, "source.txt");
      var destFile = Path.Combine(_testBaseDir, "dest.txt");
      File.WriteAllText(sourceFile, sourceContent);
      File.WriteAllText(destFile, destContent);

      // Act
      _fileSystem.ReplaceFile(sourceFile, destFile);

      // Assert
      Assert.That(File.ReadAllText(destFile), Is.EqualTo(sourceContent));
      Assert.That(File.ReadAllText(destFile).Length, Is.EqualTo(10000));
    }

    [Test]
    public void ReplaceFile_SpecialCharactersInContent_PreservesContent()
    {
      // Arrange
      var sourceContent = "Special chars: <>&'\"\nNewline\tTab\r\nCRLF";
      var sourceFile = Path.Combine(_testBaseDir, "source.txt");
      var destFile = Path.Combine(_testBaseDir, "dest.txt");
      File.WriteAllText(sourceFile, sourceContent);
      File.WriteAllText(destFile, "old content");

      // Act
      _fileSystem.ReplaceFile(sourceFile, destFile);

      // Assert
      Assert.That(File.ReadAllText(destFile), Is.EqualTo(sourceContent));
    }

    #endregion
  }
}

