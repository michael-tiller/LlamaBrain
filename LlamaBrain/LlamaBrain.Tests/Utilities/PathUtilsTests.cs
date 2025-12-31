using System;
using System.IO;
using NUnit.Framework;
using LlamaBrain.Utilities;

namespace LlamaBrain.Tests.Utilities
{
  /// <summary>
  /// Tests for PathUtils utility class.
  /// </summary>
  public class PathUtilsTests
  {
    private string _testBaseDir = null!;

    [SetUp]
    public void SetUp()
    {
      // Use temp directory as base for tests
      _testBaseDir = Path.GetTempPath();
    }

    #region CombinePath Tests

    [Test]
    public void CombinePath_RelativePath_ReturnsCombined()
    {
      // Arrange
      var relativePath = "subfolder";

      // Act
      var result = PathUtils.CombinePath(_testBaseDir, relativePath);

      // Assert
      Assert.That(result, Does.StartWith(_testBaseDir));
      Assert.That(result, Does.Contain("subfolder"));
    }

    [Test]
    public void CombinePath_RelativePathWithFile_ReturnsCombined()
    {
      // Arrange
      var relativePath = Path.Combine("subfolder", "file.txt");

      // Act
      var result = PathUtils.CombinePath(_testBaseDir, relativePath);

      // Assert
      Assert.That(result, Does.EndWith("file.txt"));
    }

    [Test]
    public void CombinePath_NullBaseDir_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => PathUtils.CombinePath(null!, "relative"));
    }

    [Test]
    public void CombinePath_EmptyBaseDir_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => PathUtils.CombinePath("", "relative"));
    }

    [Test]
    public void CombinePath_WhitespaceBaseDir_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => PathUtils.CombinePath("   ", "relative"));
    }

    [Test]
    public void CombinePath_NullRelativePath_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => PathUtils.CombinePath(_testBaseDir, null!));
    }

    [Test]
    public void CombinePath_EmptyRelativePath_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => PathUtils.CombinePath(_testBaseDir, ""));
    }

    #endregion

    #region ValidateAndSanitizePath Tests

    [Test]
    public void ValidateAndSanitizePath_ValidPath_ReturnsFullPath()
    {
      // Arrange
      var path = _testBaseDir;

      // Act
      var result = PathUtils.ValidateAndSanitizePath(path);

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(Path.IsPathRooted(result), Is.True);
    }

    [Test]
    public void ValidateAndSanitizePath_NullPath_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => PathUtils.ValidateAndSanitizePath(null!));
    }

    [Test]
    public void ValidateAndSanitizePath_EmptyPath_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => PathUtils.ValidateAndSanitizePath(""));
    }

    [Test]
    public void ValidateAndSanitizePath_PathTraversal_ThrowsArgumentException()
    {
      // Arrange
      var path = Path.Combine(_testBaseDir, "..", "escape");

      // Act & Assert
      Assert.Throws<ArgumentException>(() => PathUtils.ValidateAndSanitizePath(path));
    }

    #endregion

    #region ValidateAndSanitizeRelativePath Tests

    [Test]
    public void ValidateAndSanitizeRelativePath_ValidPath_ReturnsPath()
    {
      // Arrange
      var path = "subfolder/file.txt";

      // Act
      var result = PathUtils.ValidateAndSanitizeRelativePath(path);

      // Assert
      Assert.That(result, Is.EqualTo(path));
    }

    [Test]
    public void ValidateAndSanitizeRelativePath_NullPath_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => PathUtils.ValidateAndSanitizeRelativePath(null!));
    }

    [Test]
    public void ValidateAndSanitizeRelativePath_EmptyPath_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => PathUtils.ValidateAndSanitizeRelativePath(""));
    }

    [Test]
    public void ValidateAndSanitizeRelativePath_PathTraversal_ThrowsArgumentException()
    {
      // Arrange
      var path = "../escape/file.txt";

      // Act & Assert
      Assert.Throws<ArgumentException>(() => PathUtils.ValidateAndSanitizeRelativePath(path));
    }

    [Test]
    public void ValidateAndSanitizeRelativePath_TrimsWhitespace()
    {
      // Arrange
      var path = "  subfolder/file.txt  ";

      // Act
      var result = PathUtils.ValidateAndSanitizeRelativePath(path);

      // Assert
      Assert.That(result, Does.Not.StartWith(" "));
      Assert.That(result, Does.Not.EndWith(" "));
    }

    #endregion

    #region ContainsPathTraversal Tests

    [Test]
    public void ContainsPathTraversal_DotDotSlash_ReturnsTrue()
    {
      // Act
      var result = PathUtils.ContainsPathTraversal("../escape");

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public void ContainsPathTraversal_DotDotBackslash_ReturnsTrue()
    {
      // Act
      var result = PathUtils.ContainsPathTraversal("..\\escape");

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public void ContainsPathTraversal_DoubleDots_ReturnsTrue()
    {
      // Act
      var result = PathUtils.ContainsPathTraversal("folder..escape");

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public void ContainsPathTraversal_DoubleSlash_ReturnsTrue()
    {
      // Act
      var result = PathUtils.ContainsPathTraversal("folder//escape");

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public void ContainsPathTraversal_StartingSlash_ReturnsTrue()
    {
      // Act
      var result = PathUtils.ContainsPathTraversal("/absolute");

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public void ContainsPathTraversal_SafePath_ReturnsFalse()
    {
      // Act
      var result = PathUtils.ContainsPathTraversal("folder/subfolder/file.txt");

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void ContainsPathTraversal_NullPath_ReturnsFalse()
    {
      // Act
      var result = PathUtils.ContainsPathTraversal(null!);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void ContainsPathTraversal_EmptyPath_ReturnsFalse()
    {
      // Act
      var result = PathUtils.ContainsPathTraversal("");

      // Assert
      Assert.That(result, Is.False);
    }

    #endregion

    #region ContainsInvalidCharacters Tests

    [Test]
    public void ContainsInvalidCharacters_ValidPath_ReturnsFalse()
    {
      // Arrange
      var path = "folder/subfolder/file.txt";

      // Act
      var result = PathUtils.ContainsInvalidCharacters(path);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void ContainsInvalidCharacters_NullPath_ReturnsFalse()
    {
      // Act
      var result = PathUtils.ContainsInvalidCharacters(null!);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void ContainsInvalidCharacters_EmptyPath_ReturnsFalse()
    {
      // Act
      var result = PathUtils.ContainsInvalidCharacters("");

      // Assert
      Assert.That(result, Is.False);
    }

    #endregion

    #region IsPathWithinDirectory Tests

    [Test]
    public void IsPathWithinDirectory_PathInside_ReturnsTrue()
    {
      // Arrange
      var directory = _testBaseDir;
      var path = Path.Combine(_testBaseDir, "subfolder", "file.txt");

      // Act
      var result = PathUtils.IsPathWithinDirectory(path, directory);

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public void IsPathWithinDirectory_SamePath_ReturnsTrue()
    {
      // Arrange
      var directory = _testBaseDir;

      // Act
      var result = PathUtils.IsPathWithinDirectory(directory, directory);

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public void IsPathWithinDirectory_NullPath_ReturnsFalse()
    {
      // Act
      var result = PathUtils.IsPathWithinDirectory(null!, _testBaseDir);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void IsPathWithinDirectory_NullDirectory_ReturnsFalse()
    {
      // Arrange
      var path = Path.Combine(_testBaseDir, "file.txt");

      // Act
      var result = PathUtils.IsPathWithinDirectory(path, null!);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void IsPathWithinDirectory_EmptyPath_ReturnsFalse()
    {
      // Act
      var result = PathUtils.IsPathWithinDirectory("", _testBaseDir);

      // Assert
      Assert.That(result, Is.False);
    }

    #endregion

    #region GetDirectoryDepth Tests

    [Test]
    public void GetDirectoryDepth_SimplePath_ReturnsCorrectDepth()
    {
      // Arrange
      var path = Path.Combine(_testBaseDir, "level1", "level2", "file.txt");

      // Act
      var depth = PathUtils.GetDirectoryDepth(path);

      // Assert
      Assert.That(depth, Is.GreaterThan(0));
    }

    [Test]
    public void GetDirectoryDepth_NullPath_ReturnsZero()
    {
      // Act
      var depth = PathUtils.GetDirectoryDepth(null!);

      // Assert
      Assert.That(depth, Is.EqualTo(0));
    }

    [Test]
    public void GetDirectoryDepth_EmptyPath_ReturnsZero()
    {
      // Act
      var depth = PathUtils.GetDirectoryDepth("");

      // Assert
      Assert.That(depth, Is.EqualTo(0));
    }

    #endregion

    #region CreateSafeFilename Tests

    [Test]
    public void CreateSafeFilename_ValidFilename_ReturnsUnchanged()
    {
      // Arrange
      var filename = "valid_filename.txt";

      // Act
      var result = PathUtils.CreateSafeFilename(filename);

      // Assert
      Assert.That(result, Is.EqualTo(filename));
    }

    [Test]
    public void CreateSafeFilename_NullFilename_ReturnsUnnamed()
    {
      // Act
      var result = PathUtils.CreateSafeFilename(null!);

      // Assert
      Assert.That(result, Is.EqualTo("unnamed"));
    }

    [Test]
    public void CreateSafeFilename_EmptyFilename_ReturnsUnnamed()
    {
      // Act
      var result = PathUtils.CreateSafeFilename("");

      // Assert
      Assert.That(result, Is.EqualTo("unnamed"));
    }

    [Test]
    public void CreateSafeFilename_LeadingDots_RemovesThem()
    {
      // Arrange
      var filename = "...hidden.txt";

      // Act
      var result = PathUtils.CreateSafeFilename(filename);

      // Assert
      Assert.That(result, Does.Not.StartWith("."));
    }

    [Test]
    public void CreateSafeFilename_TrailingSpaces_RemovesThem()
    {
      // Arrange
      var filename = "file.txt   ";

      // Act
      var result = PathUtils.CreateSafeFilename(filename);

      // Assert
      Assert.That(result, Does.Not.EndWith(" "));
    }

    [Test]
    public void CreateSafeFilename_VeryLongFilename_TruncatesIt()
    {
      // Arrange
      var filename = new string('a', 300) + ".txt";

      // Act
      var result = PathUtils.CreateSafeFilename(filename);

      // Assert
      Assert.That(result.Length, Is.LessThanOrEqualTo(255));
    }

    [Test]
    public void CreateSafeFilename_OnlyInvalidChars_ReturnsUnnamed()
    {
      // Arrange
      var invalidChars = Path.GetInvalidFileNameChars();
      var filename = new string(invalidChars);

      // Act
      var result = PathUtils.CreateSafeFilename(filename);

      // Assert
      Assert.That(result, Is.EqualTo("unnamed"));
    }

    #endregion

    #region IsPathSafe Tests

    [Test]
    public void IsPathSafe_ValidPath_ReturnsTrue()
    {
      // Arrange
      var path = _testBaseDir;

      // Act
      var result = PathUtils.IsPathSafe(path);

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public void IsPathSafe_NullPath_ReturnsFalse()
    {
      // Act
      var result = PathUtils.IsPathSafe(null!);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void IsPathSafe_EmptyPath_ReturnsFalse()
    {
      // Act
      var result = PathUtils.IsPathSafe("");

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void IsPathSafe_PathTraversal_ReturnsFalse()
    {
      // Arrange
      var path = "../escape";

      // Act
      var result = PathUtils.IsPathSafe(path);

      // Assert
      Assert.That(result, Is.False);
    }

    #endregion

    #region GetRelativePath Tests

    [Test]
    public void GetRelativePath_ValidPaths_ReturnsRelative()
    {
      // Arrange
      var baseDir = _testBaseDir;
      var fullPath = Path.Combine(_testBaseDir, "subfolder", "file.txt");

      // Act
      var result = PathUtils.GetRelativePath(fullPath, baseDir);

      // Assert
      Assert.That(result, Does.Contain("subfolder"));
      Assert.That(result, Does.Contain("file.txt"));
      Assert.That(Path.IsPathRooted(result), Is.False);
    }

    [Test]
    public void GetRelativePath_NullFullPath_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => PathUtils.GetRelativePath(null!, _testBaseDir));
    }

    [Test]
    public void GetRelativePath_NullBaseDir_ThrowsArgumentException()
    {
      // Arrange
      var fullPath = Path.Combine(_testBaseDir, "file.txt");

      // Act & Assert
      Assert.Throws<ArgumentException>(() => PathUtils.GetRelativePath(fullPath, null!));
    }

    [Test]
    public void GetRelativePath_EmptyFullPath_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => PathUtils.GetRelativePath("", _testBaseDir));
    }

    [Test]
    public void GetRelativePath_EmptyBaseDir_ThrowsArgumentException()
    {
      // Arrange
      var fullPath = Path.Combine(_testBaseDir, "file.txt");

      // Act & Assert
      Assert.Throws<ArgumentException>(() => PathUtils.GetRelativePath(fullPath, ""));
    }

    #endregion
  }
}
