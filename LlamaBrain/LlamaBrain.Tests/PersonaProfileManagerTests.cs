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
  /// Tests for PersonaProfileManager using NSubstitute for mocking
  /// </summary>
  [TestFixture]
  public class PersonaProfileManagerTests
  {
    private IFileSystem _fileSystem = null!;
    private IFileInfo _fileInfo = null!;
    private string _testProfilesDir = null!;

    [SetUp]
    public void SetUp()
    {
      _fileSystem = Substitute.For<IFileSystem>();
      _fileInfo = Substitute.For<IFileInfo>();
      _testProfilesDir = @"C:\TestProfiles";
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithValidDirectory_CreatesDirectory()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);

      // Act
      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Assert
      _fileSystem.Received(1).CreateDirectory(_testProfilesDir);
    }

    [Test]
    public void Constructor_WithNullDirectory_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => new PersonaProfileManager(null!, _fileSystem));
    }

    [Test]
    public void Constructor_WithEmptyDirectory_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => new PersonaProfileManager("", _fileSystem));
    }

    [Test]
    public void Constructor_WithWhitespaceDirectory_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => new PersonaProfileManager("   ", _fileSystem));
    }

    [Test]
    public void Constructor_WithNullFileSystem_ThrowsArgumentNullException()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => new PersonaProfileManager(_testProfilesDir, null!));
    }

    [Test]
    public void Constructor_WhenDirectoryCreationFails_ThrowsInvalidOperationException()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.When(x => x.CreateDirectory(Arg.Any<string>())).Throw(new Exception("Access denied"));

      // Act & Assert
      var ex = Assert.Throws<InvalidOperationException>(() => new PersonaProfileManager(_testProfilesDir, _fileSystem));
      Assert.That(ex!.Message, Does.Contain("Failed to create profiles directory"));
    }

    [Test]
    public void Constructor_WithPathTraversal_ThrowsArgumentException()
    {
      // Arrange
      var invalidPath = @"C:\Test\..\Windows";
      _fileSystem.GetFullPath(invalidPath).Returns(invalidPath);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new PersonaProfileManager(invalidPath, _fileSystem));
    }

    [Test]
    public void Constructor_DefaultConstructor_UsesFileSystem()
    {
      // Arrange
      var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

      // Act
      var manager = new PersonaProfileManager(tempDir);

      // Assert
      Assert.IsNotNull(manager);
      Assert.IsTrue(System.IO.Directory.Exists(tempDir));

      // Cleanup
      try
      {
        System.IO.Directory.Delete(tempDir, true);
      }
      catch { }
    }

    #endregion

    #region SaveProfile Tests

    [Test]
    public void SaveProfile_WithValidProfile_SavesSuccessfully()
    {
      // Arrange
      var profile = PersonaProfile.Create("test-persona", "Test Persona");
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test-persona.profile.json");
      var tempPath = filePath + ".tmp";
      var json = JsonConvert.SerializeObject(profile, Formatting.Indented);

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test-persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(false);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(tempPath).Returns(_fileInfo);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      manager.SaveProfile(profile);

      // Assert
      _fileSystem.Received(1).WriteAllText(tempPath, Arg.Is<string>(s => s.Contains("test-persona")));
      _fileSystem.Received(1).GetFileInfo(tempPath);
      _fileSystem.Received(1).MoveFile(tempPath, filePath);
    }

    [Test]
    public void SaveProfile_WithNullProfile_ThrowsArgumentException()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => manager.SaveProfile(null!));
    }

    [Test]
    public void SaveProfile_WithEmptyPersonaId_ThrowsArgumentException()
    {
      // Arrange
      var profile = new PersonaProfile { PersonaId = "" };
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => manager.SaveProfile(profile));
    }

    [Test]
    public void SaveProfile_WithInvalidPersonaIdCharacters_SanitizesId()
    {
      // Arrange
      var profile = PersonaProfile.Create("test/persona", "Test");
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test_persona.profile.json");
      var tempPath = filePath + ".tmp";
      var json = JsonConvert.SerializeObject(profile, Formatting.Indented);

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test_persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(false);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(tempPath).Returns(_fileInfo);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      manager.SaveProfile(profile);

      // Assert
      _fileSystem.Received(1).WriteAllText(tempPath, Arg.Any<string>());
      _fileSystem.Received(1).MoveFile(tempPath, filePath);
    }

    [Test]
    public void SaveProfile_WithExistingFile_DeletesBeforeMoving()
    {
      // Arrange
      var profile = PersonaProfile.Create("test-persona", "Test Persona");
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test-persona.profile.json");
      var tempPath = filePath + ".tmp";

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test-persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(tempPath).Returns(_fileInfo);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      manager.SaveProfile(profile);

      // Assert
      _fileSystem.Received(1).DeleteFile(filePath);
      _fileSystem.Received(1).MoveFile(tempPath, filePath);
    }

    [Test]
    public void SaveProfile_WithFileTooLarge_ThrowsInvalidOperationException()
    {
      // Arrange
      var profile = PersonaProfile.Create("test-persona", "Test Persona");
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test-persona.profile.json");
      var tempPath = filePath + ".tmp";

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test-persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileInfo.Length.Returns(2 * 1024 * 1024); // 2MB, exceeds 1MB limit
      _fileSystem.GetFileInfo(tempPath).Returns(_fileInfo);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act & Assert
      var ex = Assert.Throws<InvalidOperationException>(() => manager.SaveProfile(profile));
      Assert.That(ex!.Message, Does.Contain("too large"));
      _fileSystem.Received(1).DeleteFile(tempPath);
      _fileSystem.DidNotReceive().MoveFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public void SaveProfile_WithJsonTooLarge_ThrowsInvalidOperationException()
    {
      // Arrange
      var profile = PersonaProfile.Create("test-persona", "Test Persona");
      profile.Description = new string('x', 600000); // Exceeds 500KB limit
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test-persona.profile.json");
      var tempPath = filePath + ".tmp";

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test-persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act & Assert
      var ex = Assert.Throws<InvalidOperationException>(() => manager.SaveProfile(profile));
      Assert.That(ex!.Message, Does.Contain("JSON too large"));
    }

    [Test]
    public void SaveProfile_WhenWriteFails_ThrowsException()
    {
      // Arrange
      var profile = PersonaProfile.Create("test-persona", "Test Persona");
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test-persona.profile.json");
      var tempPath = filePath + ".tmp";

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test-persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.When(x => x.WriteAllText(tempPath, Arg.Any<string>())).Throw(new Exception("Disk full"));

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act & Assert
      Assert.Throws<Exception>(() => manager.SaveProfile(profile));
    }

    #endregion

    #region LoadProfile Tests

    [Test]
    public void LoadProfile_WithValidProfile_ReturnsProfile()
    {
      // Arrange
      var profile = PersonaProfile.Create("test-persona", "Test Persona");
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test-persona.profile.json");
      var json = JsonConvert.SerializeObject(profile, Formatting.Indented);

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test-persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(filePath).Returns(_fileInfo);
      _fileSystem.ReadAllText(filePath).Returns(json);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.LoadProfile("test-persona");

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual("test-persona", result!.PersonaId);
      Assert.AreEqual("Test Persona", result.Name);
    }

    [Test]
    public void LoadProfile_WithNonExistentProfile_ReturnsNull()
    {
      // Arrange
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test-persona.profile.json");

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test-persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(false);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.LoadProfile("test-persona");

      // Assert
      Assert.IsNull(result);
    }

    [Test]
    public void LoadProfile_WithNullPersonaId_ReturnsNull()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.LoadProfile(null!);

      // Assert
      Assert.IsNull(result);
    }

    [Test]
    public void LoadProfile_WithEmptyPersonaId_ReturnsNull()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.LoadProfile("");

      // Assert
      Assert.IsNull(result);
    }

    [Test]
    public void LoadProfile_WithFileTooLarge_ReturnsNull()
    {
      // Arrange
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test-persona.profile.json");

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test-persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(2 * 1024 * 1024); // 2MB, exceeds 1MB limit
      _fileSystem.GetFileInfo(filePath).Returns(_fileInfo);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.LoadProfile("test-persona");

      // Assert
      Assert.IsNull(result);
    }

    [Test]
    public void LoadProfile_WithJsonTooLarge_ReturnsNull()
    {
      // Arrange
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test-persona.profile.json");
      var largeJson = new string('x', 600000); // Exceeds 500KB limit

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test-persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(filePath).Returns(_fileInfo);
      _fileSystem.ReadAllText(filePath).Returns(largeJson);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.LoadProfile("test-persona");

      // Assert
      Assert.IsNull(result);
    }

    [Test]
    public void LoadProfile_WithInvalidJson_ReturnsNull()
    {
      // Arrange
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test-persona.profile.json");
      var invalidJson = "{ invalid json }";

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test-persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(filePath).Returns(_fileInfo);
      _fileSystem.ReadAllText(filePath).Returns(invalidJson);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.LoadProfile("test-persona");

      // Assert
      Assert.IsNull(result);
    }

    [Test]
    public void LoadProfile_WithEmptyPersonaIdInFile_FixesPersonaId()
    {
      // Arrange
      var profile = new PersonaProfile { PersonaId = "", Name = "Test" };
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test-persona.profile.json");
      var json = JsonConvert.SerializeObject(profile, Formatting.Indented);

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test-persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(filePath).Returns(_fileInfo);
      _fileSystem.ReadAllText(filePath).Returns(json);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.LoadProfile("test-persona");

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual("test-persona", result!.PersonaId);
    }

    [Test]
    public void LoadProfile_WhenReadFails_ReturnsNull()
    {
      // Arrange
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test-persona.profile.json");

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test-persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(filePath).Returns(_fileInfo);
      _fileSystem.When(x => x.ReadAllText(filePath)).Throw(new Exception("Access denied"));

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.LoadProfile("test-persona");

      // Assert
      Assert.IsNull(result);
    }

    #endregion

    #region GetAvailableProfileIds Tests

    [Test]
    public void GetAvailableProfileIds_WithNoFiles_ReturnsEmptyList()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.GetFiles(_testProfilesDir, "*.profile.json").Returns(Array.Empty<string>());

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.GetAvailableProfileIds();

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(0, result.Count);
    }

    [Test]
    public void GetAvailableProfileIds_WithValidFiles_ReturnsIds()
    {
      // Arrange
      var files = new[]
      {
        System.IO.Path.Combine(_testProfilesDir, "persona1.profile.json"),
        System.IO.Path.Combine(_testProfilesDir, "persona2.profile.json")
      };

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.GetFiles(_testProfilesDir, "*.profile.json").Returns(files);
      _fileSystem.GetFullPath(files[0]).Returns(files[0]);
      _fileSystem.GetFullPath(files[1]).Returns(files[1]);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.GetFileNameWithoutExtension(files[0]).Returns("persona1.profile");
      _fileSystem.GetFileNameWithoutExtension(files[1]).Returns("persona2.profile");

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.GetAvailableProfileIds();

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(2, result.Count);
      Assert.Contains("persona1", result);
      Assert.Contains("persona2", result);
    }

    [Test]
    public void GetAvailableProfileIds_WithTooManyFiles_LimitsResults()
    {
      // Arrange
      var files = Enumerable.Range(0, 1500)
        .Select(i => System.IO.Path.Combine(_testProfilesDir, $"persona{i}.profile.json"))
        .ToArray();

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.GetFiles(_testProfilesDir, "*.profile.json").Returns(files);

      // Mock file validation for first 1000 files
      foreach (var file in files.Take(1000))
      {
        _fileSystem.GetFullPath(file).Returns(file);
        _fileSystem.GetFileNameWithoutExtension(file).Returns(System.IO.Path.GetFileNameWithoutExtension(file));
      }
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.GetAvailableProfileIds();

      // Assert
      Assert.IsNotNull(result);
      Assert.LessOrEqual(result.Count, 1000);
    }

    [Test]
    public void GetAvailableProfileIds_WithInvalidFilenames_SkipsInvalid()
    {
      // Arrange
      var files = new[]
      {
        System.IO.Path.Combine(_testProfilesDir, "valid.profile.json"),
        System.IO.Path.Combine(_testProfilesDir, "invalid.json") // Missing .profile
      };

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.GetFiles(_testProfilesDir, "*.profile.json").Returns(files);
      _fileSystem.GetFullPath(files[0]).Returns(files[0]);
      _fileSystem.GetFullPath(files[1]).Returns(files[1]);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.GetFileNameWithoutExtension(files[0]).Returns("valid.profile");
      _fileSystem.GetFileNameWithoutExtension(files[1]).Returns("invalid");

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.GetAvailableProfileIds();

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(1, result.Count);
      Assert.Contains("valid", result);
    }

    [Test]
    public void GetAvailableProfileIds_WhenGetFilesFails_ReturnsEmptyList()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.When(x => x.GetFiles(_testProfilesDir, "*.profile.json")).Throw(new Exception("Access denied"));

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.GetAvailableProfileIds();

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(0, result.Count);
    }

    [Test]
    public void GetAvailableProfileIds_WithInvalidPersonaIdInFilename_SkipsFile()
    {
      // Arrange
      var files = new[]
      {
        System.IO.Path.Combine(_testProfilesDir, "valid.profile.json"),
        System.IO.Path.Combine(_testProfilesDir, "invalid/persona.profile.json") // Contains invalid char
      };

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.GetFiles(_testProfilesDir, "*.profile.json").Returns(files);
      _fileSystem.GetFullPath(files[0]).Returns(files[0]);
      _fileSystem.GetFullPath(files[1]).Returns(files[1]);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.GetFileNameWithoutExtension(files[0]).Returns("valid.profile");
      _fileSystem.GetFileNameWithoutExtension(files[1]).Returns("invalid/persona.profile");

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.GetAvailableProfileIds();

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(1, result.Count);
      Assert.Contains("valid", result);
    }

    #endregion

    #region LoadAllProfiles Tests

    [Test]
    public void LoadAllProfiles_WithNoProfiles_ReturnsEmptyDictionary()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.GetFiles(_testProfilesDir, "*.profile.json").Returns(Array.Empty<string>());

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.LoadAllProfiles();

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(0, result.Count);
    }

    [Test]
    public void LoadAllProfiles_WithValidProfiles_ReturnsAllProfiles()
    {
      // Arrange
      var profile1 = PersonaProfile.Create("persona1", "Persona 1");
      var profile2 = PersonaProfile.Create("persona2", "Persona 2");
      var files = new[]
      {
        System.IO.Path.Combine(_testProfilesDir, "persona1.profile.json"),
        System.IO.Path.Combine(_testProfilesDir, "persona2.profile.json")
      };
      var json1 = JsonConvert.SerializeObject(profile1, Formatting.Indented);
      var json2 = JsonConvert.SerializeObject(profile2, Formatting.Indented);

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.GetFiles(_testProfilesDir, "*.profile.json").Returns(files);
      _fileSystem.GetFullPath(files[0]).Returns(files[0]);
      _fileSystem.GetFullPath(files[1]).Returns(files[1]);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.GetFileNameWithoutExtension(files[0]).Returns("persona1.profile");
      _fileSystem.GetFileNameWithoutExtension(files[1]).Returns("persona2.profile");
      _fileSystem.FileExists(files[0]).Returns(true);
      _fileSystem.FileExists(files[1]).Returns(true);
      _fileSystem.CombinePath(_testProfilesDir, "persona1.profile.json").Returns(files[0]);
      _fileSystem.CombinePath(_testProfilesDir, "persona2.profile.json").Returns(files[1]);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(files[0]).Returns(_fileInfo);
      _fileSystem.GetFileInfo(files[1]).Returns(_fileInfo);
      _fileSystem.ReadAllText(files[0]).Returns(json1);
      _fileSystem.ReadAllText(files[1]).Returns(json2);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.LoadAllProfiles();

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(2, result.Count);
      Assert.IsTrue(result.ContainsKey("persona1"));
      Assert.IsTrue(result.ContainsKey("persona2"));
      Assert.AreEqual("Persona 1", result["persona1"].Name);
      Assert.AreEqual("Persona 2", result["persona2"].Name);
    }

    [Test]
    public void LoadAllProfiles_WithSomeInvalidProfiles_LoadsValidOnes()
    {
      // Arrange
      var profile1 = PersonaProfile.Create("persona1", "Persona 1");
      var files = new[]
      {
        System.IO.Path.Combine(_testProfilesDir, "persona1.profile.json"),
        System.IO.Path.Combine(_testProfilesDir, "invalid.profile.json")
      };
      var json1 = JsonConvert.SerializeObject(profile1, Formatting.Indented);

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.GetFiles(_testProfilesDir, "*.profile.json").Returns(files);
      _fileSystem.GetFullPath(files[0]).Returns(files[0]);
      _fileSystem.GetFullPath(files[1]).Returns(files[1]);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.GetFileNameWithoutExtension(files[0]).Returns("persona1.profile");
      _fileSystem.GetFileNameWithoutExtension(files[1]).Returns("invalid.profile");
      _fileSystem.FileExists(files[0]).Returns(true);
      _fileSystem.FileExists(files[1]).Returns(false);
      _fileSystem.CombinePath(_testProfilesDir, "persona1.profile.json").Returns(files[0]);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(files[0]).Returns(_fileInfo);
      _fileSystem.ReadAllText(files[0]).Returns(json1);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.LoadAllProfiles();

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(1, result.Count);
      Assert.IsTrue(result.ContainsKey("persona1"));
    }

    [Test]
    public void LoadAllProfiles_WhenLoadFails_ContinuesWithOtherProfiles()
    {
      // Arrange
      var profile1 = PersonaProfile.Create("persona1", "Persona 1");
      var files = new[]
      {
        System.IO.Path.Combine(_testProfilesDir, "persona1.profile.json"),
        System.IO.Path.Combine(_testProfilesDir, "persona2.profile.json")
      };
      var json1 = JsonConvert.SerializeObject(profile1, Formatting.Indented);

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.GetFiles(_testProfilesDir, "*.profile.json").Returns(files);
      _fileSystem.GetFullPath(files[0]).Returns(files[0]);
      _fileSystem.GetFullPath(files[1]).Returns(files[1]);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.GetFileNameWithoutExtension(files[0]).Returns("persona1.profile");
      _fileSystem.GetFileNameWithoutExtension(files[1]).Returns("persona2.profile");
      _fileSystem.FileExists(files[0]).Returns(true);
      _fileSystem.FileExists(files[1]).Returns(true);
      _fileSystem.CombinePath(_testProfilesDir, "persona1.profile.json").Returns(files[0]);
      _fileSystem.CombinePath(_testProfilesDir, "persona2.profile.json").Returns(files[1]);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(files[0]).Returns(_fileInfo);
      _fileSystem.GetFileInfo(files[1]).Returns(_fileInfo);
      _fileSystem.ReadAllText(files[0]).Returns(json1);
      _fileSystem.When(x => x.ReadAllText(files[1])).Throw(new Exception("Read error"));

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.LoadAllProfiles();

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(1, result.Count);
      Assert.IsTrue(result.ContainsKey("persona1"));
    }

    #endregion

    #region DeleteProfile Tests

    [Test]
    public void DeleteProfile_WithExistingProfile_ReturnsTrue()
    {
      // Arrange
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test-persona.profile.json");

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test-persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(true);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.DeleteProfile("test-persona");

      // Assert
      Assert.IsTrue(result);
      _fileSystem.Received(1).DeleteFile(filePath);
    }

    [Test]
    public void DeleteProfile_WithNonExistentProfile_ReturnsFalse()
    {
      // Arrange
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test-persona.profile.json");

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test-persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(false);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.DeleteProfile("test-persona");

      // Assert
      Assert.IsFalse(result);
      _fileSystem.DidNotReceive().DeleteFile(Arg.Any<string>());
    }

    [Test]
    public void DeleteProfile_WithNullPersonaId_ReturnsFalse()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.DeleteProfile(null!);

      // Assert
      Assert.IsFalse(result);
    }

    [Test]
    public void DeleteProfile_WithEmptyPersonaId_ReturnsFalse()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.DeleteProfile("");

      // Assert
      Assert.IsFalse(result);
    }

    [Test]
    public void DeleteProfile_WhenDeleteFails_ReturnsFalse()
    {
      // Arrange
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test-persona.profile.json");

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test-persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(true);
      _fileSystem.When(x => x.DeleteFile(filePath)).Throw(new Exception("Access denied"));

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.DeleteProfile("test-persona");

      // Assert
      Assert.IsFalse(result);
    }

    [Test]
    public void DeleteProfile_WithInvalidPersonaIdCharacters_SanitizesId()
    {
      // Arrange
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test_persona.profile.json");

      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      _fileSystem.CombinePath(_testProfilesDir, "test_persona.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(true);

      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);

      // Act
      var result = manager.DeleteProfile("test/persona");

      // Assert
      Assert.IsTrue(result);
      _fileSystem.Received(1).DeleteFile(filePath);
    }

    #endregion

    #region Validation Tests

    [Test]
    public void ValidateAndSanitizePersonaId_WithInvalidCharacters_ReplacesThem()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);
      var profile = PersonaProfile.Create("test/persona:name", "Test");
      var filePath = System.IO.Path.Combine(_testProfilesDir, "test_persona_name.profile.json");
      var tempPath = filePath + ".tmp";

      _fileSystem.CombinePath(_testProfilesDir, "test_persona_name.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(false);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(tempPath).Returns(_fileInfo);

      // Act
      manager.SaveProfile(profile);

      // Assert - Verify the sanitized filename was used
      _fileSystem.Received(1).WriteAllText(tempPath, Arg.Any<string>());
      _fileSystem.Received(1).MoveFile(tempPath, filePath);
    }

    [Test]
    public void ValidateAndSanitizePersonaId_WithOnlyInvalidCharacters_ThrowsException()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);
      var profile = new PersonaProfile { PersonaId = "///" };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => manager.SaveProfile(profile));
    }

    [Test]
    public void ValidateAndSanitizePersonaId_WithLongId_Truncates()
    {
      // Arrange
      var longId = new string('a', 150);
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);
      var profile = PersonaProfile.Create(longId, "Test");
      var truncatedId = longId.Substring(0, 100);
      var filePath = System.IO.Path.Combine(_testProfilesDir, $"{truncatedId}.profile.json");
      var tempPath = filePath + ".tmp";

      _fileSystem.CombinePath(_testProfilesDir, $"{truncatedId}.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(filePath);
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);
      _fileSystem.FileExists(filePath).Returns(false);
      _fileInfo.Length.Returns(100);
      _fileSystem.GetFileInfo(tempPath).Returns(_fileInfo);

      // Act
      manager.SaveProfile(profile);

      // Assert
      _fileSystem.Received(1).WriteAllText(tempPath, Arg.Any<string>());
    }

    [Test]
    public void ValidateFilePath_WithPathOutsideDirectory_ThrowsException()
    {
      // Arrange
      _fileSystem.GetFullPath(Arg.Any<string>()).Returns(_testProfilesDir);
      var manager = new PersonaProfileManager(_testProfilesDir, _fileSystem);
      var profile = PersonaProfile.Create("test", "Test");
      var filePath = @"C:\OtherDirectory\test.profile.json";

      _fileSystem.CombinePath(_testProfilesDir, "test.profile.json").Returns(filePath);
      _fileSystem.GetFullPath(filePath).Returns(@"C:\OtherDirectory\test.profile.json");
      _fileSystem.GetFullPath(_testProfilesDir).Returns(_testProfilesDir);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => manager.SaveProfile(profile));
    }

    #endregion
  }
}

