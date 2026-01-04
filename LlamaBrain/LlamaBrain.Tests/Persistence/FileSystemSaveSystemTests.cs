using System;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using LlamaBrain.Persona;
using LlamaBrain.Persistence;
using LlamaBrain.Utilities;
using Newtonsoft.Json;

namespace LlamaBrain.Tests.Persistence
{
  /// <summary>
  /// Tests for FileSystemSaveSystem.
  /// </summary>
  [TestFixture]
  public class FileSystemSaveSystemTests
  {
    private IFileSystem _fileSystem = null!;
    private IFileInfo _fileInfo = null!;
    private IClock _clock = null!;
    private string _testSaveDir = null!;

    [SetUp]
    public void SetUp()
    {
      _fileSystem = Substitute.For<IFileSystem>();
      _fileInfo = Substitute.For<IFileInfo>();
      _clock = new FixedClock(1000000000L);
      _testSaveDir = @"C:\TestSaves";

      // Default mock setup
      _fileSystem.CombinePath(Arg.Any<string>(), Arg.Any<string>())
        .Returns(callInfo => $"{callInfo.ArgAt<string>(0)}\\{callInfo.ArgAt<string>(1)}");
      _fileSystem.GetFileNameWithoutExtension(Arg.Any<string>())
        .Returns(callInfo => System.IO.Path.GetFileNameWithoutExtension(callInfo.ArgAt<string>(0)));
      _fileSystem.GetFileInfo(Arg.Any<string>()).Returns(_fileInfo);
      _fileInfo.Length.Returns(1000);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_CreatesDirectory()
    {
      // Act
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);

      // Assert
      _fileSystem.Received(1).CreateDirectory(_testSaveDir);
    }

    [Test]
    public void Constructor_WithNullDirectory_ThrowsArgumentNullException()
    {
      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => new FileSystemSaveSystem(null!, _fileSystem, _clock));
    }

    [Test]
    public void Constructor_WithNullFileSystem_ThrowsArgumentNullException()
    {
      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => new FileSystemSaveSystem(_testSaveDir, null!, _clock));
    }

    #endregion

    #region Save Tests

    [Test]
    public void Save_WithValidData_WritesToTempThenMoves()
    {
      // Arrange
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);
      var data = new SaveData { SavedAtUtcTicks = 12345 };

      // Act
      var result = saveSystem.Save("slot1", data);

      // Assert
      Assert.That(result.Success, Is.True);
      _fileSystem.Received(1).WriteAllText(
        Arg.Is<string>(s => s.EndsWith(".tmp")),
        Arg.Any<string>());
      _fileSystem.Received(1).MoveFile(
        Arg.Is<string>(s => s.EndsWith(".tmp")),
        Arg.Is<string>(s => s.EndsWith(".llamasave")));
    }

    [Test]
    public void Save_WithExistingFile_DeletesOldBeforeMove()
    {
      // Arrange
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);
      _fileSystem.FileExists(Arg.Is<string>(s => s.EndsWith(".llamasave"))).Returns(true);
      var data = new SaveData { SavedAtUtcTicks = 12345 };

      // Act
      var result = saveSystem.Save("slot1", data);

      // Assert
      Assert.That(result.Success, Is.True);
      _fileSystem.Received(1).DeleteFile(Arg.Is<string>(s => s.EndsWith(".llamasave")));
    }

    [Test]
    public void Save_WithEmptySlotName_ReturnsFailed()
    {
      // Arrange
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);

      // Act
      var result = saveSystem.Save("", new SaveData());

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("null or empty"));
    }

    [Test]
    public void Save_WithNullData_ReturnsFailed()
    {
      // Arrange
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);

      // Act
      var result = saveSystem.Save("slot1", null!);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("null"));
    }

    [Test]
    public void Save_SanitizesSlotName()
    {
      // Arrange
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);
      var data = new SaveData { SavedAtUtcTicks = 12345 };

      // Act
      var result = saveSystem.Save("../../../etc/passwd", data);

      // Assert
      Assert.That(result.Success, Is.True);
      // Verify path separators were replaced (no / or \ in the filename part)
      // The sanitized name will be in the saves directory, not escaped
      _fileSystem.Received(1).MoveFile(
        Arg.Any<string>(),
        Arg.Is<string>(s => s.StartsWith(_testSaveDir) && !s.Contains("/")));
    }

    #endregion

    #region Load Tests

    [Test]
    public void Load_WithExistingSlot_ReturnsData()
    {
      // Arrange
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);
      var expectedData = new SaveData { SavedAtUtcTicks = 12345, Version = 1 };
      var json = JsonConvert.SerializeObject(expectedData);

      _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
      _fileSystem.ReadAllText(Arg.Any<string>()).Returns(json);

      // Act
      var result = saveSystem.Load("slot1");

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result!.SavedAtUtcTicks, Is.EqualTo(12345));
    }

    [Test]
    public void Load_WithNonExistentSlot_ReturnsNull()
    {
      // Arrange
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);
      _fileSystem.FileExists(Arg.Any<string>()).Returns(false);

      // Act
      var result = saveSystem.Load("nonexistent");

      // Assert
      Assert.That(result, Is.Null);
    }

    [Test]
    public void Load_WithEmptySlotName_ReturnsNull()
    {
      // Arrange
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);

      // Act
      var result = saveSystem.Load("");

      // Assert
      Assert.That(result, Is.Null);
    }

    [Test]
    public void Load_WithOversizedFile_ReturnsNull()
    {
      // Arrange
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);
      _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
      _fileInfo.Length.Returns(FileSystemSaveSystem.MaxFileSizeBytes + 1);

      // Act
      var result = saveSystem.Load("slot1");

      // Assert
      Assert.That(result, Is.Null);
    }

    #endregion

    #region SlotExists Tests

    [Test]
    public void SlotExists_WithExistingSlot_ReturnsTrue()
    {
      // Arrange
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);
      _fileSystem.FileExists(Arg.Any<string>()).Returns(true);

      // Act
      var result = saveSystem.SlotExists("slot1");

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public void SlotExists_WithNonExistentSlot_ReturnsFalse()
    {
      // Arrange
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);
      _fileSystem.FileExists(Arg.Any<string>()).Returns(false);

      // Act
      var result = saveSystem.SlotExists("nonexistent");

      // Assert
      Assert.That(result, Is.False);
    }

    #endregion

    #region DeleteSlot Tests

    [Test]
    public void DeleteSlot_WithExistingSlot_DeletesAndReturnsTrue()
    {
      // Arrange
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);
      _fileSystem.FileExists(Arg.Any<string>()).Returns(true);

      // Act
      var result = saveSystem.DeleteSlot("slot1");

      // Assert
      Assert.That(result, Is.True);
      _fileSystem.Received(1).DeleteFile(Arg.Any<string>());
    }

    [Test]
    public void DeleteSlot_WithNonExistentSlot_ReturnsFalse()
    {
      // Arrange
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);
      _fileSystem.FileExists(Arg.Any<string>()).Returns(false);

      // Act
      var result = saveSystem.DeleteSlot("nonexistent");

      // Assert
      Assert.That(result, Is.False);
      _fileSystem.DidNotReceive().DeleteFile(Arg.Any<string>());
    }

    #endregion

    #region ListSlots Tests

    [Test]
    public void ListSlots_WithNoSlots_ReturnsEmptyList()
    {
      // Arrange
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);
      _fileSystem.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(new string[0]);

      // Act
      var result = saveSystem.ListSlots();

      // Assert
      Assert.That(result, Is.Empty);
    }

    [Test]
    public void ListSlots_WithSlots_ReturnsSlotInfo()
    {
      // Arrange
      var saveSystem = new FileSystemSaveSystem(_testSaveDir, _fileSystem, _clock);
      var slot1Data = new SaveData { SavedAtUtcTicks = 2000, Version = 1 };
      var slot2Data = new SaveData { SavedAtUtcTicks = 1000, Version = 1 };

      _fileSystem.GetFiles(_testSaveDir, "*.llamasave").Returns(new[] { "slot1.llamasave", "slot2.llamasave" });
      _fileSystem.GetFileNameWithoutExtension("slot1.llamasave").Returns("slot1");
      _fileSystem.GetFileNameWithoutExtension("slot2.llamasave").Returns("slot2");
      _fileSystem.ReadAllText("slot1.llamasave").Returns(JsonConvert.SerializeObject(slot1Data));
      _fileSystem.ReadAllText("slot2.llamasave").Returns(JsonConvert.SerializeObject(slot2Data));

      // Act
      var result = saveSystem.ListSlots();

      // Assert
      Assert.That(result.Count, Is.EqualTo(2));
      // Should be ordered by save time descending
      Assert.That(result[0].SlotName, Is.EqualTo("slot1")); // newer
      Assert.That(result[1].SlotName, Is.EqualTo("slot2")); // older
    }

    #endregion
  }
}
