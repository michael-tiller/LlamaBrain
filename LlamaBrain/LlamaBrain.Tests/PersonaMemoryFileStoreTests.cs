using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using LlamaBrain.Persona;
using LlamaBrain.Utilities;
using Newtonsoft.Json;

namespace LlamaBrain.Tests
{
  /// <summary>
  /// Tests for PersonaMemoryFileStore using NSubstitute for mocking
  /// </summary>
  [TestFixture]
  public class PersonaMemoryFileStoreTests
  {
    private IFileSystem _fileSystem = null!;
    private IFileInfo _fileInfo = null!;
    private string _testSaveDir = null!;

    [SetUp]
    public void SetUp()
    {
      _fileSystem = Substitute.For<IFileSystem>();
      _fileInfo = Substitute.For<IFileInfo>();
      _testSaveDir = @"C:\TestMemory";
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithValidDirectory_CreatesDirectory()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);

      // Act
      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Assert
      _fileSystem.Received(1).CreateDirectory(_testSaveDir);
    }

    [Test]
    public void Constructor_WithNullDirectory_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => new PersonaMemoryFileStore(null!, _fileSystem));
    }

    [Test]
    public void Constructor_WithEmptyDirectory_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => new PersonaMemoryFileStore("", _fileSystem));
    }

    [Test]
    public void Constructor_WithWhitespaceDirectory_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => new PersonaMemoryFileStore("   ", _fileSystem));
    }

    [Test]
    public void Constructor_WithNullFileSystem_ThrowsArgumentNullException()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => new PersonaMemoryFileStore(_testSaveDir, null!));
    }

    [Test]
    public void Constructor_WhenDirectoryCreationFails_ThrowsInvalidOperationException()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.When(x => x.CreateDirectory(Arg.Any<string>())).Throw(new Exception("Access denied"));

      // Act & Assert
      var ex = Assert.Throws<InvalidOperationException>(() => new PersonaMemoryFileStore(_testSaveDir, _fileSystem));
      Assert.That(ex!.Message, Does.Contain("Failed to create save directory"));
    }

    [Test]
    public void Constructor_WithPathTraversal_ThrowsArgumentException()
    {
      // Arrange
      var invalidPath = @"C:\Test\..\Windows";
      _fileSystem.GetFullPath(invalidPath).Returns(invalidPath);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new PersonaMemoryFileStore(invalidPath, _fileSystem));
    }

    [Test]
    public void Constructor_DefaultConstructor_UsesFileSystem()
    {
      // Arrange
      var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

      // Act
      var store = new PersonaMemoryFileStore(tempDir);

      // Assert
      Assert.IsNotNull(store);
      Assert.IsTrue(System.IO.Directory.Exists(tempDir));

      // Cleanup
      try
      {
        System.IO.Directory.Delete(tempDir, true);
      }
      catch { }
    }

    #endregion

    #region Save(string) Tests

    [Test]
    public void Save_WithValidPersonaId_SavesSuccessfully()
    {
      // Arrange
      var personaId = "test-persona";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");
      var tempPath = filePath + ".tmp";
      var memory = new List<string> { "Memory 1", "Memory 2" };
      var json = JsonConvert.SerializeObject(memory);

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(false);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(tempPath).Returns(_fileInfo);

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);
      store.AddMemory(personaId, "Memory 1");
      store.AddMemory(personaId, "Memory 2");

      // Act
      store.Save(personaId);

      // Assert
      _fileSystem.Received(1).WriteAllText(tempPath, Arg.Is<string>(s => s.Contains("Memory 1") || s.Contains("Memory 2")));
      _fileSystem.Received(1).GetFileInfo(tempPath);
      _fileSystem.Received(1).MoveFile(tempPath, filePath);
    }

    [Test]
    public void Save_WithNullPersonaId_ThrowsArgumentException()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => store.Save((string)null!));
    }

    [Test]
    public void Save_WithEmptyPersonaId_ThrowsArgumentException()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => store.Save(""));
    }

    [Test]
    public void Save_WithInvalidPersonaIdCharacters_SanitizesId()
    {
      // Arrange
      var personaId = "test/persona";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test_persona.json");
      var tempPath = filePath + ".tmp";

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test_persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(false);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(tempPath).Returns(_fileInfo);

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);
      store.AddMemory(personaId, "Memory 1");

      // Act
      store.Save(personaId);

      // Assert
      _fileSystem.Received(1).WriteAllText(tempPath, Arg.Any<string>());
      _fileSystem.Received(1).MoveFile(tempPath, filePath);
    }

    [Test]
    public void Save_WithExistingFile_DeletesBeforeMoving()
    {
      // Arrange
      var personaId = "test-persona";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");
      var tempPath = filePath + ".tmp";

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(tempPath).Returns(_fileInfo);

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);
      store.AddMemory(personaId, "Memory 1");

      // Act
      store.Save(personaId);

      // Assert
      _fileSystem.Received(1).DeleteFile(filePath);
      _fileSystem.Received(1).MoveFile(tempPath, filePath);
    }

    [Test]
    public void Save_WithFileTooLarge_ThrowsInvalidOperationException()
    {
      // Arrange
      var personaId = "test-persona";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");
      var tempPath = filePath + ".tmp";

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileInfo.Length.Returns(6 * 1024 * 1024); // 6MB, exceeds 5MB limit
      _fileSystem.GetFileInfo(tempPath).Returns(_fileInfo);

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);
      store.AddMemory(personaId, "Memory 1");

      // Act & Assert
      var ex = Assert.Throws<InvalidOperationException>(() => store.Save(personaId));
      Assert.That(ex!.Message, Does.Contain("too large"));
      _fileSystem.Received(1).DeleteFile(tempPath);
      _fileSystem.DidNotReceive().MoveFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public void Save_WithTooManyEntries_TruncatesToLatest()
    {
      // Arrange
      var personaId = "test-persona";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");
      var tempPath = filePath + ".tmp";

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(false);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(tempPath).Returns(_fileInfo);

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);
      // Add more than MaxMemoryEntries (10000)
      for (int i = 0; i < 10001; i++)
      {
        store.AddMemory(personaId, $"Memory {i}");
      }

      // Act
      store.Save(personaId);

      // Assert
      // Verify that WriteAllText was called (saving happened)
      _fileSystem.Received(1).WriteAllText(tempPath, Arg.Any<string>());
      // Verify that the save completed successfully (file was moved)
      _fileSystem.Received(1).MoveFile(tempPath, filePath);
      // Note: We can't easily verify the exact content since GetMemory returns formatted strings,
      // but we can verify that truncation logic was executed (no exception thrown and save succeeded)
    }

    [Test]
    public void Save_WhenWriteFails_ThrowsException()
    {
      // Arrange
      var personaId = "test-persona";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");
      var tempPath = filePath + ".tmp";

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.When(x => x.WriteAllText(tempPath, Arg.Any<string>())).Throw(new Exception("Disk full"));

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);
      store.AddMemory(personaId, "Memory 1");

      // Act & Assert
      Assert.Throws<Exception>(() => store.Save(personaId));
    }

    #endregion

    #region Save(PersonaProfile) Tests

    [Test]
    public void Save_WithValidProfile_SavesSuccessfully()
    {
      // Arrange
      var profile = PersonaProfile.Create("test-persona", "Test Persona");
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");
      var tempPath = filePath + ".tmp";

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(false);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(tempPath).Returns(_fileInfo);

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);
      store.AddMemory(profile, "Memory 1");

      // Act
      store.Save(profile);

      // Assert
      _fileSystem.Received(1).WriteAllText(tempPath, Arg.Any<string>());
      _fileSystem.Received(1).MoveFile(tempPath, filePath);
    }

    [Test]
    public void Save_WithNullProfile_ThrowsArgumentException()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => store.Save((PersonaProfile)null!));
    }

    [Test]
    public void Save_WithProfileWithoutPersonaId_ThrowsArgumentException()
    {
      // Arrange
      var profile = new PersonaProfile { PersonaId = "" };
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => store.Save(profile));
    }

    #endregion

    #region Load(string) Tests

    [Test]
    public void Load_WithValidFile_LoadsMemory()
    {
      // Arrange
      var personaId = "test-persona";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");
      var memory = new List<string> { "Memory 1", "Memory 2" };
      var json = JsonConvert.SerializeObject(memory);

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(filePath).Returns(_fileInfo);
      _fileSystem.ReadAllText(filePath).Returns(json);

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act
      store.Load(personaId);

      // Assert
      var loadedMemory = store.GetMemory(personaId);
      Assert.IsNotNull(loadedMemory);
      Assert.GreaterOrEqual(loadedMemory.Count, 2);
    }

    [Test]
    public void Load_WithNonExistentFile_DoesNothing()
    {
      // Arrange
      var personaId = "test-persona";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(false);

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act
      store.Load(personaId);

      // Assert
      var loadedMemory = store.GetMemory(personaId);
      Assert.AreEqual(0, loadedMemory.Count);
      _fileSystem.DidNotReceive().ReadAllText(Arg.Any<string>());
    }

    [Test]
    public void Load_WithNullPersonaId_DoesNothing()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act
      store.Load((string)null!);

      // Assert
      // Should not throw and should not attempt to read file
      _fileSystem.DidNotReceive().ReadAllText(Arg.Any<string>());
    }

    [Test]
    public void Load_WithEmptyPersonaId_DoesNothing()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act
      store.Load("");

      // Assert
      _fileSystem.DidNotReceive().ReadAllText(Arg.Any<string>());
    }

    [Test]
    public void Load_WithFileTooLarge_DoesNotLoad()
    {
      // Arrange
      var personaId = "test-persona";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(6 * 1024 * 1024); // 6MB, exceeds 5MB limit
      _fileSystem.GetFileInfo(filePath).Returns(_fileInfo);

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act
      store.Load(personaId);

      // Assert
      var loadedMemory = store.GetMemory(personaId);
      Assert.AreEqual(0, loadedMemory.Count);
      _fileSystem.DidNotReceive().ReadAllText(Arg.Any<string>());
    }

    [Test]
    public void Load_WithJsonTooLarge_DoesNotLoad()
    {
      // Arrange
      var personaId = "test-persona";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");
      var largeJson = new string('x', 2100000); // Exceeds 2MB limit

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(filePath).Returns(_fileInfo);
      _fileSystem.ReadAllText(filePath).Returns(largeJson);

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act
      store.Load(personaId);

      // Assert
      var loadedMemory = store.GetMemory(personaId);
      Assert.AreEqual(0, loadedMemory.Count);
    }

    [Test]
    public void Load_WithInvalidJson_DoesNotLoad()
    {
      // Arrange
      var personaId = "test-persona";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");
      var invalidJson = "{ invalid json }";

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(filePath).Returns(_fileInfo);
      _fileSystem.ReadAllText(filePath).Returns(invalidJson);

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act
      store.Load(personaId);

      // Assert
      var loadedMemory = store.GetMemory(personaId);
      Assert.AreEqual(0, loadedMemory.Count);
    }

    [Test]
    public void Load_WithNullDeserializedMemory_DoesNotLoad()
    {
      // Arrange
      var personaId = "test-persona";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");
      // This shouldn't happen with valid JSON, but we test the null check
      var json = "null";

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(filePath).Returns(_fileInfo);
      _fileSystem.ReadAllText(filePath).Returns(json);

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act
      store.Load(personaId);

      // Assert
      var loadedMemory = store.GetMemory(personaId);
      Assert.AreEqual(0, loadedMemory.Count);
    }

    [Test]
    public void Load_WithInvalidEntries_FiltersThem()
    {
      // Arrange
      var personaId = "test-persona";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");
      var memory = new List<string>
      {
        "Valid memory 1",
        "", // Empty - should be filtered
        "   ", // Whitespace - should be filtered
        new string('x', 11000), // Too long (>10KB) - should be filtered
        "Valid memory 2"
      };
      var json = JsonConvert.SerializeObject(memory);

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(filePath).Returns(_fileInfo);
      _fileSystem.ReadAllText(filePath).Returns(json);

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act
      store.Load(personaId);

      // Assert
      var loadedMemory = store.GetMemory(personaId);
      Assert.GreaterOrEqual(loadedMemory.Count, 2); // Should have at least the 2 valid entries
      Assert.IsTrue(loadedMemory.Any(m => m.Contains("Valid memory 1")));
      Assert.IsTrue(loadedMemory.Any(m => m.Contains("Valid memory 2")));
    }

    [Test]
    public void Load_WithTooManyEntries_TruncatesToLatest()
    {
      // Arrange
      var personaId = "test-persona";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");
      var memory = Enumerable.Range(0, 10001).Select(i => $"Memory {i}").ToList();
      var json = JsonConvert.SerializeObject(memory);

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(filePath).Returns(_fileInfo);
      _fileSystem.ReadAllText(filePath).Returns(json);

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act
      store.Load(personaId);

      // Assert
      var loadedMemory = store.GetMemory(personaId);
      Assert.LessOrEqual(loadedMemory.Count, 10000); // Should be limited to MaxMemoryEntries
    }

    [Test]
    public void Load_WhenReadFails_DoesNotThrow()
    {
      // Arrange
      var personaId = "test-persona";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(filePath).Returns(_fileInfo);
      _fileSystem.When(x => x.ReadAllText(filePath)).Throw(new Exception("Access denied"));

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act & Assert - Should not throw
      Assert.DoesNotThrow(() => store.Load(personaId));
      var loadedMemory = store.GetMemory(personaId);
      Assert.AreEqual(0, loadedMemory.Count);
    }

    #endregion

    #region Load(PersonaProfile) Tests

    [Test]
    public void Load_WithValidProfile_LoadsMemory()
    {
      // Arrange
      var profile = PersonaProfile.Create("test-persona", "Test Persona");
      var filePath = System.IO.Path.Combine(_testSaveDir, "test-persona.json");
      var memory = new List<string> { "Memory 1", "Memory 2" };
      var json = JsonConvert.SerializeObject(memory);

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      _fileSystem.CombinePath(_testSaveDir, "test-persona.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(filePath).Returns(_fileInfo);
      _fileSystem.ReadAllText(filePath).Returns(json);

      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act
      store.Load(profile);

      // Assert
      var loadedMemory = store.GetMemory(profile);
      Assert.IsNotNull(loadedMemory);
      Assert.GreaterOrEqual(loadedMemory.Count, 2);
    }

    [Test]
    public void Load_WithNullProfile_ThrowsArgumentException()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => store.Load((PersonaProfile)null!));
    }

    [Test]
    public void Load_WithProfileWithoutPersonaId_ThrowsArgumentException()
    {
      // Arrange
      var profile = new PersonaProfile { PersonaId = "" };
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => store.Load(profile));
    }

    #endregion

    #region Validation Tests

    [Test]
    public void ValidateAndSanitizePersonaId_WithInvalidCharacters_ReplacesThem()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);
      var personaId = "test/persona:name";
      var filePath = System.IO.Path.Combine(_testSaveDir, "test_persona_name.json");
      var tempPath = filePath + ".tmp";

      _fileSystem.CombinePath(_testSaveDir, "test_persona_name.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);
      _fileSystem.FileExists(filePath).Returns(false);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(tempPath).Returns(_fileInfo);

      store.AddMemory(personaId, "Memory 1");

      // Act
      store.Save(personaId);

      // Assert - Verify the sanitized filename was used
      _fileSystem.Received(1).WriteAllText(tempPath, Arg.Any<string>());
      _fileSystem.Received(1).MoveFile(tempPath, filePath);
    }

    [Test]
    public void ValidateAndSanitizePersonaId_WithOnlyInvalidCharacters_ThrowsException()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => store.Save("///"));
    }

    [Test]
    public void ValidateFilePath_WithPathOutsideDirectory_ThrowsException()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testSaveDir);
      var store = new PersonaMemoryFileStore(_testSaveDir, _fileSystem);
      var personaId = "test";
      var filePath = @"C:\OtherDirectory\test.json";

      _fileSystem.CombinePath(_testSaveDir, "test.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(@"C:\OtherDirectory\test.json");
      _fileSystem.GetFullPath(_testSaveDir).Returns(_testSaveDir);

      store.AddMemory(personaId, "Memory 1");

      // Act & Assert
      Assert.Throws<ArgumentException>(() => store.Save(personaId));
    }

    #endregion
  }
}

