using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using LlamaBrain.Core.Audit;
using LlamaBrain.Persona;
using LlamaBrain.Utilities;

namespace LlamaBrain.Tests.Audit
{
  /// <summary>
  /// Tests for RollingFileAuditPersistence implementation.
  /// Verifies file rotation, retention policy, and JSON Lines format.
  /// </summary>
  [TestFixture]
  [Category("Audit")]
  public class RollingFileAuditPersistenceTests
  {
    private IFileSystem _fileSystem = null!;
    private IClock _clock = null!;
    private string _testLogDir = null!;
    private long _testTimeTicks;
    private Dictionary<string, string> _fileContents = null!;
    private Dictionary<string, long> _fileSizes = null!;

    [SetUp]
    public void SetUp()
    {
      _fileSystem = Substitute.For<IFileSystem>();
      _testTimeTicks = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc).Ticks;
      _clock = new FixedClock(_testTimeTicks);
      _testLogDir = @"C:\TestLogs";
      _fileContents = new Dictionary<string, string>();
      _fileSizes = new Dictionary<string, long>();

      // Default mock setup
      _fileSystem.CombinePath(Arg.Any<string>(), Arg.Any<string>())
        .Returns(callInfo => $"{callInfo.ArgAt<string>(0)}\\{callInfo.ArgAt<string>(1)}");
      _fileSystem.GetFileNameWithoutExtension(Arg.Any<string>())
        .Returns(callInfo => System.IO.Path.GetFileNameWithoutExtension(callInfo.ArgAt<string>(0)));
      _fileSystem.GetFiles(Arg.Any<string>(), Arg.Any<string>())
        .Returns(Array.Empty<string>());

      // Track file existence based on whether we've written to it
      _fileSystem.FileExists(Arg.Any<string>())
        .Returns(callInfo => _fileContents.ContainsKey(callInfo.ArgAt<string>(0)));

      // Return file info with tracked sizes
      _fileSystem.GetFileInfo(Arg.Any<string>())
        .Returns(callInfo =>
        {
          var path = callInfo.ArgAt<string>(0);
          var mockInfo = Substitute.For<IFileInfo>();
          mockInfo.Length.Returns(_fileSizes.TryGetValue(path, out var size) ? size : 0);
          return mockInfo;
        });

      // Track file contents for verification
      _fileSystem.When(x => x.WriteAllText(Arg.Any<string>(), Arg.Any<string>()))
        .Do(callInfo =>
        {
          var path = callInfo.ArgAt<string>(0);
          var content = callInfo.ArgAt<string>(1);
          _fileContents[path] = content;
          _fileSizes[path] = System.Text.Encoding.UTF8.GetByteCount(content);
        });

      _fileSystem.When(x => x.AppendAllText(Arg.Any<string>(), Arg.Any<string>()))
        .Do(callInfo =>
        {
          var path = callInfo.ArgAt<string>(0);
          var content = callInfo.ArgAt<string>(1);
          if (!_fileContents.ContainsKey(path))
            _fileContents[path] = "";
          _fileContents[path] += content;
          _fileSizes[path] = System.Text.Encoding.UTF8.GetByteCount(_fileContents[path]);
        });
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithValidArgs_SetsProperties()
    {
      // Act
      var persistence = new RollingFileAuditPersistence(_fileSystem, _clock, _testLogDir);

      // Assert
      Assert.That(persistence.LogDirectory, Is.EqualTo(_testLogDir));
      Assert.That(persistence.CurrentFilePath, Is.Null);
    }

    [Test]
    public void Constructor_WithNullFileSystem_ThrowsArgumentNull()
    {
      // Act & Assert
      Assert.Throws<ArgumentNullException>(() =>
        new RollingFileAuditPersistence(null!, _clock, _testLogDir));
    }

    [Test]
    public void Constructor_WithNullClock_ThrowsArgumentNull()
    {
      // Act & Assert
      Assert.Throws<ArgumentNullException>(() =>
        new RollingFileAuditPersistence(_fileSystem, null!, _testLogDir));
    }

    [Test]
    public void Constructor_WithEmptyDirectory_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() =>
        new RollingFileAuditPersistence(_fileSystem, _clock, ""));
      Assert.Throws<ArgumentException>(() =>
        new RollingFileAuditPersistence(_fileSystem, _clock, "   "));
    }

    [Test]
    public void Constructor_WithInvalidOptions_ThrowsArgumentOutOfRange()
    {
      // Act & Assert
      var invalidOptions = new RollingFileOptions { MaxFileSizeBytes = 100 }; // Too small
      Assert.Throws<ArgumentOutOfRangeException>(() =>
        new RollingFileAuditPersistence(_fileSystem, _clock, _testLogDir, invalidOptions));
    }

    #endregion

    #region Persist Tests

    [Test]
    public void Persist_FirstRecord_CreatesNewFile()
    {
      // Arrange
      var persistence = new RollingFileAuditPersistence(_fileSystem, _clock, _testLogDir);
      var record = CreateRecord("npc-1");

      // Act
      persistence.Persist(record);

      // Assert
      _fileSystem.Received(1).CreateDirectory(_testLogDir);
      _fileSystem.Received().WriteAllText(
        Arg.Is<string>(s => s.Contains("audit-") && s.EndsWith(".jsonl")),
        Arg.Is<string>(s => s == ""));
      Assert.That(persistence.CurrentFilePath, Is.Not.Null);
      Assert.That(persistence.CurrentFilePath, Does.Contain("audit-20240115-103000-001.jsonl"));
    }

    [Test]
    public void Persist_Record_WritesJsonLine()
    {
      // Arrange
      var persistence = new RollingFileAuditPersistence(_fileSystem, _clock, _testLogDir);
      var record = CreateRecord("npc-1", "hello");

      // Act
      persistence.Persist(record);

      // Assert
      _fileSystem.Received().AppendAllText(
        Arg.Any<string>(),
        Arg.Is<string>(s => s.Contains("\"NpcId\":\"npc-1\"") && s.Contains("\"PlayerInput\":\"hello\"") && s.EndsWith(Environment.NewLine)));
    }

    [Test]
    public void Persist_MultipleRecords_AppendsToSameFile()
    {
      // Arrange
      var persistence = new RollingFileAuditPersistence(_fileSystem, _clock, _testLogDir);

      // Act
      persistence.Persist(CreateRecord("npc-1", "first"));
      persistence.Persist(CreateRecord("npc-1", "second"));
      persistence.Persist(CreateRecord("npc-1", "third"));

      // Assert - Should only create one file
      _fileSystem.Received(1).WriteAllText(Arg.Any<string>(), Arg.Any<string>());
      _fileSystem.Received(3).AppendAllText(Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public void Persist_NullRecord_ThrowsArgumentNull()
    {
      // Arrange
      var persistence = new RollingFileAuditPersistence(_fileSystem, _clock, _testLogDir);

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => persistence.Persist(null!));
    }

    #endregion

    #region File Rotation Tests

    [Test]
    public void Persist_ExceedsMaxSize_RotatesToNewFile()
    {
      // Arrange
      var options = new RollingFileOptions { MaxFileSizeBytes = 1024 };
      var persistence = new RollingFileAuditPersistence(_fileSystem, _clock, _testLogDir, options);

      // Act - Write enough records to exceed 1KB
      for (int i = 0; i < 20; i++)
      {
        persistence.Persist(CreateRecord("npc-1", $"long-input-text-{i}-padding-to-make-it-bigger"));
      }

      // Assert - Should have rotated to new file at some point (more than 1 file created)
      Assert.That(_fileContents.Count, Is.GreaterThan(1), "Expected file rotation to create multiple files");
    }

    [Test]
    public void Persist_AfterRotation_IncrementSequence()
    {
      // Arrange
      var options = new RollingFileOptions { MaxFileSizeBytes = 1024 };
      var persistence = new RollingFileAuditPersistence(_fileSystem, _clock, _testLogDir, options);

      // Act
      for (int i = 0; i < 20; i++)
      {
        persistence.Persist(CreateRecord("npc-1", $"long-input-text-{i}-padding"));
      }

      // Assert - Files should have incrementing sequence numbers
      var files = _fileContents.Keys.OrderBy(f => f).ToList();
      Assert.That(files.Count, Is.GreaterThan(1));
      Assert.That(files[0], Does.Contain("-001.jsonl"));
      Assert.That(files[1], Does.Contain("-002.jsonl"));
    }

    #endregion

    #region Retention Policy Tests

    [Test]
    public void Persist_ExceedsMaxFileCount_DeletesOldestFiles()
    {
      // Arrange
      var options = new RollingFileOptions
      {
        MaxFileSizeBytes = 1024,
        MaxFileCount = 3
      };

      // Pre-populate existing files
      var existingFiles = new[]
      {
        @"C:\TestLogs\audit-20240115-100000-001.jsonl",
        @"C:\TestLogs\audit-20240115-101000-002.jsonl",
        @"C:\TestLogs\audit-20240115-102000-003.jsonl"
      };
      foreach (var file in existingFiles)
      {
        _fileContents[file] = new string('x', 2000); // Make them "full"
        _fileSizes[file] = 2000;
      }

      // Dynamic GetFiles that returns all files in _fileContents matching the pattern
      _fileSystem.GetFiles(_testLogDir, Arg.Any<string>())
        .Returns(callInfo =>
        {
          var pattern = callInfo.ArgAt<string>(1);
          var prefix = pattern.Replace("-*", "-").Replace("*.jsonl", ".jsonl");
          return _fileContents.Keys
            .Where(f => f.StartsWith(_testLogDir) && f.EndsWith(".jsonl"))
            .OrderBy(f => f)
            .ToArray();
        });

      // Track deletions
      var deletedFiles = new List<string>();
      _fileSystem.When(x => x.DeleteFile(Arg.Any<string>()))
        .Do(callInfo =>
        {
          var path = callInfo.ArgAt<string>(0);
          deletedFiles.Add(path);
          _fileContents.Remove(path);
          _fileSizes.Remove(path);
        });

      var persistence = new RollingFileAuditPersistence(_fileSystem, _clock, _testLogDir, options);

      // Act - Persist triggers rotation because existing files are full
      persistence.Persist(CreateRecord("npc-1", "new-record"));

      // Assert - Should delete oldest file
      Assert.That(deletedFiles, Does.Contain(@"C:\TestLogs\audit-20240115-100000-001.jsonl"));
    }

    #endregion

    #region Recovery Tests

    [Test]
    public void Persist_WithExistingFiles_ContinuesLatestFile()
    {
      // Arrange
      var existingFiles = new[]
      {
        @"C:\TestLogs\audit-20240115-100000-001.jsonl",
        @"C:\TestLogs\audit-20240115-101000-002.jsonl"
      };
      // Pre-populate with small content (under limit)
      foreach (var file in existingFiles)
      {
        _fileContents[file] = "existing content";
        _fileSizes[file] = 500;
      }
      _fileSystem.GetFiles(_testLogDir, "audit-*.jsonl").Returns(existingFiles);

      var persistence = new RollingFileAuditPersistence(_fileSystem, _clock, _testLogDir);

      // Act
      persistence.Persist(CreateRecord("npc-1"));

      // Assert - Should NOT create a new file, should append to existing
      _fileSystem.DidNotReceive().WriteAllText(Arg.Any<string>(), Arg.Any<string>());
      _fileSystem.Received(1).AppendAllText(
        Arg.Is<string>(s => s.Contains("-002.jsonl")),
        Arg.Any<string>());
    }

    [Test]
    public void Persist_WithExistingFullFile_CreatesNewFile()
    {
      // Arrange
      var options = new RollingFileOptions { MaxFileSizeBytes = 1024 };
      var existingFiles = new[]
      {
        @"C:\TestLogs\audit-20240115-100000-005.jsonl"
      };
      // Pre-populate with content over the limit
      foreach (var file in existingFiles)
      {
        _fileContents[file] = new string('x', 2000);
        _fileSizes[file] = 2000;
      }
      _fileSystem.GetFiles(_testLogDir, "audit-*.jsonl").Returns(existingFiles);

      var persistence = new RollingFileAuditPersistence(_fileSystem, _clock, _testLogDir, options);

      // Act
      persistence.Persist(CreateRecord("npc-1"));

      // Assert - Should create new file with sequence 006
      _fileSystem.Received(1).WriteAllText(
        Arg.Is<string>(s => s.Contains("-006.jsonl")),
        Arg.Is<string>(s => s == ""));
    }

    #endregion

    #region GetLogFiles Tests

    [Test]
    public void GetLogFiles_ReturnsFilesInOrder()
    {
      // Arrange
      var existingFiles = new[]
      {
        @"C:\TestLogs\audit-20240115-100000-001.jsonl",
        @"C:\TestLogs\audit-20240115-101000-002.jsonl",
        @"C:\TestLogs\audit-20240115-102000-003.jsonl"
      };
      _fileSystem.GetFiles(_testLogDir, "audit-*.jsonl").Returns(existingFiles);
      var persistence = new RollingFileAuditPersistence(_fileSystem, _clock, _testLogDir);

      // Act
      var files = persistence.GetLogFiles();

      // Assert
      Assert.That(files.Count, Is.EqualTo(3));
      Assert.That(files[0], Does.Contain("-001.jsonl"));
      Assert.That(files[1], Does.Contain("-002.jsonl"));
      Assert.That(files[2], Does.Contain("-003.jsonl"));
    }

    [Test]
    public void GetLogFiles_EmptyDirectory_ReturnsEmptyList()
    {
      // Arrange
      _fileSystem.GetFiles(_testLogDir, "audit-*.jsonl").Returns(Array.Empty<string>());
      var persistence = new RollingFileAuditPersistence(_fileSystem, _clock, _testLogDir);

      // Act
      var files = persistence.GetLogFiles();

      // Assert
      Assert.That(files, Is.Empty);
    }

    #endregion

    #region GetTotalSizeBytes Tests

    [Test]
    public void GetTotalSizeBytes_SumsAllFileSizes()
    {
      // Arrange
      var existingFiles = new[]
      {
        @"C:\TestLogs\audit-001.jsonl",
        @"C:\TestLogs\audit-002.jsonl",
        @"C:\TestLogs\audit-003.jsonl"
      };
      // Pre-populate with different sizes
      _fileContents[@"C:\TestLogs\audit-001.jsonl"] = new string('a', 1000);
      _fileSizes[@"C:\TestLogs\audit-001.jsonl"] = 1000;
      _fileContents[@"C:\TestLogs\audit-002.jsonl"] = new string('b', 2000);
      _fileSizes[@"C:\TestLogs\audit-002.jsonl"] = 2000;
      _fileContents[@"C:\TestLogs\audit-003.jsonl"] = new string('c', 3000);
      _fileSizes[@"C:\TestLogs\audit-003.jsonl"] = 3000;

      _fileSystem.GetFiles(_testLogDir, "audit-*.jsonl").Returns(existingFiles);

      var persistence = new RollingFileAuditPersistence(_fileSystem, _clock, _testLogDir);

      // Act
      var totalSize = persistence.GetTotalSizeBytes();

      // Assert
      Assert.That(totalSize, Is.EqualTo(6000));
    }

    #endregion

    #region Options Tests

    [Test]
    public void CustomFilePrefix_UsesCustomPrefix()
    {
      // Arrange
      var options = new RollingFileOptions { FilePrefix = "npc-audit" };
      _fileSystem.GetFiles(_testLogDir, "npc-audit-*.jsonl").Returns(Array.Empty<string>());
      var persistence = new RollingFileAuditPersistence(_fileSystem, _clock, _testLogDir, options);

      // Act
      persistence.Persist(CreateRecord("npc-1"));

      // Assert
      _fileSystem.Received().WriteAllText(
        Arg.Is<string>(s => s.Contains("npc-audit-") && s.EndsWith(".jsonl")),
        Arg.Any<string>());
    }

    #endregion

    #region Helper Methods

    private static AuditRecord CreateRecord(string npcId, string playerInput = "test")
    {
      return new AuditRecord
      {
        RecordId = Guid.NewGuid().ToString("N").Substring(0, 16),
        NpcId = npcId,
        PlayerInput = playerInput,
        CapturedAtUtcTicks = DateTimeOffset.UtcNow.UtcTicks
      };
    }

    #endregion
  }
}
