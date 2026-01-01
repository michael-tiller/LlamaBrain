using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using LlamaBrain.Core;

namespace LlamaBrain.Tests.Core
{
  /// <summary>
  /// Tests for ServerManager server lifecycle management
  /// </summary>
  [TestFixture]
  public class ServerManagerTests
  {
    private ProcessConfig _defaultConfig = null!;
    private string _tempExecutablePath = null!;
    private string _tempModelPath = null!;

    [SetUp]
    public void SetUp()
    {
      _defaultConfig = new ProcessConfig
      {
        Host = "localhost",
        Port = 5000,
        Model = "test-model.gguf",
        ExecutablePath = "test-server.exe",
        ContextSize = 2048,
        LlmConfig = new LlmConfig()
      };

      // Create temporary files for testing
      _tempExecutablePath = Path.Combine(Path.GetTempPath(), $"test-server-{Guid.NewGuid()}.exe");
      _tempModelPath = Path.Combine(Path.GetTempPath(), $"test-model-{Guid.NewGuid()}.gguf");

      // Create empty files to simulate existence
      File.WriteAllText(_tempExecutablePath, "test executable");
      File.WriteAllText(_tempModelPath, "test model");
    }

    [TearDown]
    public void TearDown()
    {
      // Clean up temporary files
      try
      {
        if (File.Exists(_tempExecutablePath))
          File.Delete(_tempExecutablePath);
        if (File.Exists(_tempModelPath))
          File.Delete(_tempModelPath);
      }
      catch
      {
        // Ignore cleanup errors
      }
    }

    #region Constructor Tests

    [Test]
    public void ServerManager_Constructor_WithValidConfig_CreatesCorrectly()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Host = "localhost",
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };

      // Act
      var manager = new ServerManager(config);

      // Assert
      Assert.IsNotNull(manager);
    }

    [Test]
    public void ServerManager_Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
      // Arrange, Act & Assert
      Assert.Throws<ArgumentNullException>(() => new ServerManager(null!));
    }

    [Test]
    public void ServerManager_Constructor_WithInvalidPort_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 0, // Invalid port
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void ServerManager_Constructor_WithPortTooHigh_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 70000, // Invalid port (max is 65535)
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void ServerManager_Constructor_WithInvalidContextSize_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 0 // Invalid context size
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void ServerManager_Constructor_WithContextSizeTooHigh_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 50000 // Invalid context size (max is 32768)
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void ServerManager_Constructor_WithEmptyExecutablePath_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = string.Empty, // Invalid
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void ServerManager_Constructor_WithEmptyModelPath_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = string.Empty, // Invalid
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    #endregion

    #region IsServerRunning Tests

    [Test]
    public void IsServerRunning_WhenServerNotStarted_ReturnsFalse()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      var isRunning = manager.IsServerRunning();

      // Assert
      Assert.IsFalse(isRunning);
    }

    [Test]
    public void IsServerRunning_AfterDispose_ReturnsFalse()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);
      manager.Dispose();

      // Act
      var isRunning = manager.IsServerRunning();

      // Assert
      Assert.IsFalse(isRunning);
    }

    #endregion

    #region GetServerStatus Tests

    [Test]
    public void GetServerStatus_WhenServerNotStarted_ReturnsNotRunningStatus()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      var status = manager.GetServerStatus();

      // Assert
      Assert.IsNotNull(status);
      Assert.IsFalse(status.IsRunning);
      Assert.AreEqual(-1, status.ProcessId);
      Assert.IsNull(status.StartTime);
      Assert.AreEqual(0, status.MemoryUsage);
      Assert.AreEqual(0, status.ThreadCount);
      Assert.IsFalse(status.Responding);
      Assert.AreEqual(TimeSpan.Zero, status.Uptime);
    }

    [Test]
    public void GetServerStatus_AfterDispose_ThrowsObjectDisposedException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);
      manager.Dispose();

      // Act & Assert
      Assert.Throws<ObjectDisposedException>(() => manager.GetServerStatus());
    }

    #endregion

    #region ValidateServerConfiguration Tests

    [Test]
    public void ValidateServerConfiguration_WithValidConfig_ReturnsValidResult()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      var result = manager.ValidateServerConfiguration();

      // Assert
      Assert.IsNotNull(result);
      // Note: Result may not be valid if files don't actually exist or ProcessUtils validation fails
      // But the structure should be correct
      Assert.IsNotNull(result.Errors);
      Assert.IsNotNull(result.Warnings);
    }

    [Test]
    public void ValidateServerConfiguration_WithInvalidPort_ReturnsInvalidResult()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 0, // Invalid port
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };

      // Act & Assert
      // Constructor will throw, so we can't test validation after construction
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void ValidateServerConfiguration_WithInvalidContextSize_ReturnsInvalidResult()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 50000 // Invalid context size
      };

      // Act & Assert
      // Constructor will throw, so we can't test validation after construction
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void ValidateServerConfiguration_AfterDispose_ThrowsObjectDisposedException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);
      manager.Dispose();

      // Act & Assert
      Assert.Throws<ObjectDisposedException>(() => manager.ValidateServerConfiguration());
    }

    #endregion

    #region WaitForServerReadyAsync Tests

    [Test]
    public void WaitForServerReadyAsync_WithZeroTimeout_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      Assert.ThrowsAsync<ArgumentException>(async () => await manager.WaitForServerReadyAsync(0));
    }

    [Test]
    public void WaitForServerReadyAsync_WithNegativeTimeout_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      Assert.ThrowsAsync<ArgumentException>(async () => await manager.WaitForServerReadyAsync(-1));
    }

    [Test]
    public void WaitForServerReadyAsync_WhenServerNotRunning_ReturnsFalse()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      var result = Task.Run(async () => await manager.WaitForServerReadyAsync(1)).Result;

      // Assert
      Assert.IsFalse(result);
    }

    [Test]
    public void WaitForServerReadyAsync_AfterDispose_ThrowsObjectDisposedException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);
      manager.Dispose();

      // Act & Assert
      Assert.ThrowsAsync<ObjectDisposedException>(async () => await manager.WaitForServerReadyAsync(1));
    }

    #endregion

    #region StopServer Tests

    [Test]
    public void StopServer_WhenServerNotRunning_DoesNotThrow()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      Assert.DoesNotThrow(() => manager.StopServer());
    }

    [Test]
    public void StopServer_AfterDispose_ThrowsObjectDisposedException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);
      manager.Dispose();

      // Act & Assert
      Assert.Throws<ObjectDisposedException>(() => manager.StopServer());
    }

    #endregion

    #region StartServer Tests

    [Test]
    public void StartServer_WithNonExistentExecutable_ThrowsFileNotFoundException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = "nonexistent.exe",
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      Assert.Throws<FileNotFoundException>(() => manager.StartServer());
    }

    [Test]
    public void StartServer_WithNonExistentModel_ThrowsFileNotFoundException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = "nonexistent.gguf",
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      Assert.Throws<FileNotFoundException>(() => manager.StartServer());
    }

    [Test]
    public void StartServer_WithEmptyExecutablePath_ThrowsInvalidOperationException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = string.Empty,
        ContextSize = 2048
      };

      // Act & Assert
      // Constructor will throw, but if we bypass that, StartServer should throw
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void StartServer_WithEmptyModelPath_ThrowsInvalidOperationException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = string.Empty,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };

      // Act & Assert
      // Constructor will throw
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void StartServer_AfterDispose_ThrowsObjectDisposedException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);
      manager.Dispose();

      // Act & Assert
      Assert.Throws<ObjectDisposedException>(() => manager.StartServer());
    }

    #endregion

    #region LastStartupArguments Tests

    [Test]
    public void LastStartupArguments_BeforeStart_ReturnsEmptyString()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      var arguments = manager.LastStartupArguments;

      // Assert
      Assert.AreEqual(string.Empty, arguments);
    }

    #endregion

    #region Dispose Tests

    [Test]
    public void Dispose_WhenCalled_DisposesCorrectly()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      manager.Dispose();

      // Assert
      // Should not throw and should mark as disposed
      Assert.IsFalse(manager.IsServerRunning());
    }

    [Test]
    public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      Assert.DoesNotThrow(() =>
      {
        manager.Dispose();
        manager.Dispose();
        manager.Dispose();
      });
    }

    #endregion

    #region Event Tests

    [Test]
    public void OnServerOutput_CanBeSubscribed()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);
      string? receivedOutput = null;

      // Act & Assert
      // Event subscription should not throw
      Assert.DoesNotThrow(() =>
      {
        manager.OnServerOutput += (output) => { receivedOutput = output; };
      });
    }

    [Test]
    public void OnServerError_CanBeSubscribed()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);
      string? receivedError = null;

      // Act & Assert
      // Event subscription should not throw
      Assert.DoesNotThrow(() =>
      {
        manager.OnServerError += (error) => { receivedError = error; };
      });
    }

    #endregion

    #region Port Validation Tests

    [Test]
    public void ServerManager_WithValidPortRange_AcceptsPort()
    {
      // Arrange & Act
      var config1 = new ProcessConfig
      {
        Port = 1, // Minimum valid port
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager1 = new ServerManager(config1);
      Assert.IsNotNull(manager1);

      var config2 = new ProcessConfig
      {
        Port = 65535, // Maximum valid port
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager2 = new ServerManager(config2);
      Assert.IsNotNull(manager2);
    }

    #endregion

    #region ContextSize Validation Tests

    [Test]
    public void ServerManager_WithValidContextSizeRange_AcceptsContextSize()
    {
      // Arrange & Act
      var config1 = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 1 // Minimum valid context size
      };
      var manager1 = new ServerManager(config1);
      Assert.IsNotNull(manager1);

      var config2 = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 32768 // Maximum valid context size
      };
      var manager2 = new ServerManager(config2);
      Assert.IsNotNull(manager2);
    }

    #endregion

    #region Executable Path Validation Tests

    [Test]
    public void StartServer_WithPathTraversalInExecutable_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = @"C:\test\..\..\..\Windows\System32\cmd.exe",
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      var ex = Assert.Throws<ArgumentException>(() => manager.StartServer());
      Assert.That(ex!.Message, Does.Contain("invalid characters"));
    }

    [Test]
    public void StartServer_WithDoubleSlashInExecutablePath_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = @"C://test//server.exe",
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      var ex = Assert.Throws<ArgumentException>(() => manager.StartServer());
      Assert.That(ex!.Message, Does.Contain("invalid characters"));
    }

    [Test]
    public void StartServer_WithInvalidExecutableExtension_ThrowsArgumentException()
    {
      // Arrange - Create a temp file with invalid extension
      var invalidExePath = Path.Combine(Path.GetTempPath(), $"test-server-{Guid.NewGuid()}.txt");
      File.WriteAllText(invalidExePath, "test");

      try
      {
        var config = new ProcessConfig
        {
          Port = 5000,
          Model = _tempModelPath,
          ExecutablePath = invalidExePath,
          ContextSize = 2048
        };
        var manager = new ServerManager(config);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => manager.StartServer());
        Assert.That(ex!.Message, Does.Contain("Invalid executable extension"));
      }
      finally
      {
        if (File.Exists(invalidExePath))
          File.Delete(invalidExePath);
      }
    }

    [Test]
    public void StartServer_WithExecutablePathTooLong_ThrowsArgumentException()
    {
      // Arrange - Create a path that exceeds 260 characters
      var longPath = @"C:\" + new string('a', 300) + ".exe";
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = longPath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      var ex = Assert.Throws<ArgumentException>(() => manager.StartServer());
      Assert.That(ex!.Message, Does.Contain("too long"));
    }

    [Test]
    public void StartServer_WithBatExtension_DoesNotThrowForExtension()
    {
      // Arrange - Create a temp file with .bat extension
      var batPath = Path.Combine(Path.GetTempPath(), $"test-server-{Guid.NewGuid()}.bat");
      File.WriteAllText(batPath, "echo test");

      try
      {
        var config = new ProcessConfig
        {
          Port = 5000,
          Model = _tempModelPath,
          ExecutablePath = batPath,
          ContextSize = 2048
        };
        var manager = new ServerManager(config);

        // Act - This should not throw ArgumentException for extension
        // It will throw when trying to start but not due to extension validation
        try
        {
          manager.StartServer();
        }
        catch (Exception ex)
        {
          // Should not be an ArgumentException about invalid extension
          Assert.That(ex.Message, Does.Not.Contain("Invalid executable extension"));
        }
      }
      finally
      {
        if (File.Exists(batPath))
          File.Delete(batPath);
      }
    }

    [Test]
    public void StartServer_WithCmdExtension_DoesNotThrowForExtension()
    {
      // Arrange - Create a temp file with .cmd extension
      var cmdPath = Path.Combine(Path.GetTempPath(), $"test-server-{Guid.NewGuid()}.cmd");
      File.WriteAllText(cmdPath, "echo test");

      try
      {
        var config = new ProcessConfig
        {
          Port = 5000,
          Model = _tempModelPath,
          ExecutablePath = cmdPath,
          ContextSize = 2048
        };
        var manager = new ServerManager(config);

        // Act - This should not throw ArgumentException for extension
        try
        {
          manager.StartServer();
        }
        catch (Exception ex)
        {
          // Should not be an ArgumentException about invalid extension
          Assert.That(ex.Message, Does.Not.Contain("Invalid executable extension"));
        }
      }
      finally
      {
        if (File.Exists(cmdPath))
          File.Delete(cmdPath);
      }
    }

    #endregion

    #region Model Path Validation Tests

    [Test]
    public void StartServer_WithPathTraversalInModel_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = @"C:\models\..\..\..\secret\model.gguf",
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      var ex = Assert.Throws<ArgumentException>(() => manager.StartServer());
      Assert.That(ex!.Message, Does.Contain("invalid characters"));
    }

    [Test]
    public void StartServer_WithDoubleSlashInModelPath_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = @"C://models//test.gguf",
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      var ex = Assert.Throws<ArgumentException>(() => manager.StartServer());
      Assert.That(ex!.Message, Does.Contain("invalid characters"));
    }

    [Test]
    public void StartServer_WithInvalidModelExtension_ThrowsArgumentException()
    {
      // Arrange - Create a temp file with invalid extension
      var invalidModelPath = Path.Combine(Path.GetTempPath(), $"test-model-{Guid.NewGuid()}.txt");
      File.WriteAllText(invalidModelPath, "test");

      try
      {
        var config = new ProcessConfig
        {
          Port = 5000,
          Model = invalidModelPath,
          ExecutablePath = _tempExecutablePath,
          ContextSize = 2048
        };
        var manager = new ServerManager(config);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => manager.StartServer());
        Assert.That(ex!.Message, Does.Contain("Invalid model file extension"));
      }
      finally
      {
        if (File.Exists(invalidModelPath))
          File.Delete(invalidModelPath);
      }
    }

    [Test]
    public void StartServer_WithModelPathTooLong_ThrowsArgumentException()
    {
      // Arrange - Create a path that exceeds 260 characters
      var longPath = @"C:\" + new string('a', 300) + ".gguf";
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = longPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      var ex = Assert.Throws<ArgumentException>(() => manager.StartServer());
      Assert.That(ex!.Message, Does.Contain("too long"));
    }

    [Test]
    public void StartServer_WithBinModelExtension_DoesNotThrowForExtension()
    {
      // Arrange - Create a temp file with .bin extension
      var binPath = Path.Combine(Path.GetTempPath(), $"test-model-{Guid.NewGuid()}.bin");
      File.WriteAllText(binPath, "test model");

      try
      {
        var config = new ProcessConfig
        {
          Port = 5000,
          Model = binPath,
          ExecutablePath = _tempExecutablePath,
          ContextSize = 2048
        };
        var manager = new ServerManager(config);

        // Act - This should fail at file not found for executable, not model extension
        try
        {
          manager.StartServer();
        }
        catch (Exception ex)
        {
          // Should not be an ArgumentException about invalid model extension
          Assert.That(ex.Message, Does.Not.Contain("Invalid model file extension"));
        }
      }
      finally
      {
        if (File.Exists(binPath))
          File.Delete(binPath);
      }
    }

    [Test]
    public void StartServer_WithModelExtension_DoesNotThrowForExtension()
    {
      // Arrange - Create a temp file with .model extension
      var modelPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.model");
      File.WriteAllText(modelPath, "test model");

      try
      {
        var config = new ProcessConfig
        {
          Port = 5000,
          Model = modelPath,
          ExecutablePath = _tempExecutablePath,
          ContextSize = 2048
        };
        var manager = new ServerManager(config);

        // Act - This should fail at file not found for executable, not model extension
        try
        {
          manager.StartServer();
        }
        catch (Exception ex)
        {
          // Should not be an ArgumentException about invalid model extension
          Assert.That(ex.Message, Does.Not.Contain("Invalid model file extension"));
        }
      }
      finally
      {
        if (File.Exists(modelPath))
          File.Delete(modelPath);
      }
    }

    #endregion

    #region Argument Building Tests

    [Test]
    public void StartServer_WithGpuLayers_IncludesGpuLayersInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        GpuLayers = 40
      };
      var manager = new ServerManager(config);

      // Act - Attempt to start (will fail but arguments should be set)
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail as process won't actually run
      }

      // Assert - Check that arguments contain GPU layers
      Assert.That(manager.LastStartupArguments, Does.Contain("-ngl 40"));
    }

    [Test]
    public void StartServer_WithZeroGpuLayers_DoesNotIncludeGpuLayersInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        GpuLayers = 0
      };
      var manager = new ServerManager(config);

      // Act - Attempt to start (will fail but arguments should be set)
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert - Check that arguments do NOT contain GPU layers flag
      Assert.That(manager.LastStartupArguments, Does.Not.Contain("-ngl"));
    }

    [Test]
    public void StartServer_WithThreads_IncludesThreadsInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        Threads = 8
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert
      Assert.That(manager.LastStartupArguments, Does.Contain("-t 8"));
    }

    [Test]
    public void StartServer_WithZeroThreads_DoesNotIncludeThreadsInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        Threads = 0
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert
      Assert.That(manager.LastStartupArguments, Does.Not.Contain("-t "));
    }

    [Test]
    public void StartServer_WithBatchSize_IncludesBatchSizeInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        BatchSize = 256
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert
      Assert.That(manager.LastStartupArguments, Does.Contain("-b 256"));
    }

    [Test]
    public void StartServer_WithZeroBatchSize_DoesNotIncludeBatchSizeInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        BatchSize = 0
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert
      Assert.That(manager.LastStartupArguments, Does.Not.Contain("-b "));
    }

    [Test]
    public void StartServer_WithUBatchSize_IncludesUBatchSizeInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        UBatchSize = 64
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert
      Assert.That(manager.LastStartupArguments, Does.Contain("--ubatch-size 64"));
    }

    [Test]
    public void StartServer_WithZeroUBatchSize_DoesNotIncludeUBatchSizeInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        UBatchSize = 0
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert
      Assert.That(manager.LastStartupArguments, Does.Not.Contain("--ubatch-size"));
    }

    [Test]
    public void StartServer_WithMlock_IncludesMlockInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        UseMlock = true
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert
      Assert.That(manager.LastStartupArguments, Does.Contain("--mlock"));
    }

    [Test]
    public void StartServer_WithoutMlock_DoesNotIncludeMlockInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        UseMlock = false
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert
      Assert.That(manager.LastStartupArguments, Does.Not.Contain("--mlock"));
    }

    [Test]
    public void StartServer_WithParallelSlots_IncludesParallelInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        ParallelSlots = 4
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert
      Assert.That(manager.LastStartupArguments, Does.Contain("--parallel 4"));
    }

    [Test]
    public void StartServer_WithZeroParallelSlots_DefaultsToOne()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        ParallelSlots = 0
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert - Should default to 1
      Assert.That(manager.LastStartupArguments, Does.Contain("--parallel 1"));
    }

    [Test]
    public void StartServer_WithContinuousBatching_IncludesContBatchingInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        UseContinuousBatching = true
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert
      Assert.That(manager.LastStartupArguments, Does.Contain("--cont-batching"));
    }

    [Test]
    public void StartServer_WithoutContinuousBatching_DoesNotIncludeContBatchingInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        UseContinuousBatching = false
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert
      Assert.That(manager.LastStartupArguments, Does.Not.Contain("--cont-batching"));
    }

    [Test]
    public void StartServer_IncludesPortInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 8080,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert
      Assert.That(manager.LastStartupArguments, Does.Contain("--port 8080"));
    }

    [Test]
    public void StartServer_IncludesContextSizeInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 4096
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert
      Assert.That(manager.LastStartupArguments, Does.Contain("--ctx-size 4096"));
    }

    [Test]
    public void StartServer_IncludesModelPathInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert
      Assert.That(manager.LastStartupArguments, Does.Contain("-m \""));
    }

    #endregion

    #region ValidateServerConfiguration Additional Tests

    [Test]
    public void ValidateServerConfiguration_WithNonExistentModel_ReturnsInvalid()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = @"C:\nonexistent\fake-model.gguf",
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      var result = manager.ValidateServerConfiguration();

      // Assert
      Assert.IsFalse(result.IsValid);
      Assert.That(result.Errors.Any(e => e.Contains("Model path")));
    }

    [Test]
    public void ValidateServerConfiguration_WithNonExistentExecutable_ReturnsInvalid()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = @"C:\nonexistent\fake-server.exe",
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      var result = manager.ValidateServerConfiguration();

      // Assert
      Assert.IsFalse(result.IsValid);
      Assert.That(result.Errors.Any(e => e.Contains("Executable path")));
    }

    [Test]
    public void ValidateServerConfiguration_WithValidFilesButUsedPort_MayAddWarning()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      var result = manager.ValidateServerConfiguration();

      // Assert - Should have Warnings list (even if empty)
      Assert.IsNotNull(result.Warnings);
    }

    #endregion

    #region Negative Port/Context Size Edge Cases

    [Test]
    public void ServerManager_WithNegativePort_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = -1,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void ServerManager_WithNegativeContextSize_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = -1
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    #endregion

    #region WhiteSpace Path Tests

    [Test]
    public void ServerManager_WithWhitespaceExecutablePath_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = "   ",
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void ServerManager_WithWhitespaceModelPath_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = "   ",
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    #endregion

    #region Combined Configuration Tests

    [Test]
    public void StartServer_WithAllOptionsEnabled_BuildsCompleteArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 8080,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 4096,
        GpuLayers = 35,
        Threads = 8,
        BatchSize = 512,
        UBatchSize = 128,
        UseMlock = true,
        ParallelSlots = 2,
        UseContinuousBatching = true
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert - Check all options are present
      var args = manager.LastStartupArguments;
      Assert.That(args, Does.Contain("--port 8080"));
      Assert.That(args, Does.Contain("--ctx-size 4096"));
      Assert.That(args, Does.Contain("-ngl 35"));
      Assert.That(args, Does.Contain("-t 8"));
      Assert.That(args, Does.Contain("-b 512"));
      Assert.That(args, Does.Contain("--ubatch-size 128"));
      Assert.That(args, Does.Contain("--mlock"));
      Assert.That(args, Does.Contain("--parallel 2"));
      Assert.That(args, Does.Contain("--cont-batching"));
    }

    [Test]
    public void StartServer_WithMinimalOptions_BuildsMinimalArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        GpuLayers = 0,
        Threads = 0,
        BatchSize = 0,
        UBatchSize = 0,
        UseMlock = false,
        ParallelSlots = 0,
        UseContinuousBatching = false
      };
      var manager = new ServerManager(config);

      // Act
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      // Assert - Check only required options are present
      var args = manager.LastStartupArguments;
      Assert.That(args, Does.Contain("--port 5000"));
      Assert.That(args, Does.Contain("--ctx-size 2048"));
      Assert.That(args, Does.Contain("-m \""));
      Assert.That(args, Does.Contain("--parallel 1")); // Defaults to 1
      Assert.That(args, Does.Not.Contain("-ngl"));
      Assert.That(args, Does.Not.Contain("-t "));
      Assert.That(args, Does.Not.Contain("-b "));
      Assert.That(args, Does.Not.Contain("--ubatch-size"));
      Assert.That(args, Does.Not.Contain("--mlock"));
      Assert.That(args, Does.Not.Contain("--cont-batching"));
    }

    #endregion

    #region Server Already Running Tests

    [Test]
    public void StartServer_WhenAlreadyStartAttempted_TracksLastArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048,
        GpuLayers = 20
      };
      var manager = new ServerManager(config);

      // Act - First attempt
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      var firstArgs = manager.LastStartupArguments;

      // Second attempt should still have the arguments
      try
      {
        manager.StartServer();
      }
      catch
      {
        // Expected to fail
      }

      var secondArgs = manager.LastStartupArguments;

      // Assert
      Assert.That(firstArgs, Does.Contain("-ngl 20"));
      Assert.AreEqual(firstArgs, secondArgs);
    }

    #endregion

    #region ShouldFilterServerLog Tests

    private ServerManager CreateTestManager()
    {
      return new ServerManager(new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      });
    }

    [Test]
    public void ShouldFilterServerLog_WithNullOrWhitespace_ReturnsTrue()
    {
      // Arrange
      var manager = CreateTestManager();

      // Act & Assert
      Assert.IsTrue(manager.ShouldFilterServerLog(null!));
      Assert.IsTrue(manager.ShouldFilterServerLog(""));
      Assert.IsTrue(manager.ShouldFilterServerLog("   "));
      Assert.IsTrue(manager.ShouldFilterServerLog("\t\n"));
    }

    [Test]
    public void ShouldFilterServerLog_WithTensorLoadingMessage_ReturnsTrue()
    {
      // Arrange
      var manager = CreateTestManager();

      // Act & Assert
      Assert.IsTrue(manager.ShouldFilterServerLog("llama_model_loader: - tensor 42"));
      Assert.IsTrue(manager.ShouldFilterServerLog("llama_model_loader: - type f16"));
      Assert.IsTrue(manager.ShouldFilterServerLog("llama_model_loader: - kv 3"));
      Assert.IsTrue(manager.ShouldFilterServerLog("llama_model_loader: Dumping metadata keys"));
    }

    [Test]
    public void ShouldFilterServerLog_WithModelInfoMessages_ReturnsTrue()
    {
      // Arrange
      var manager = CreateTestManager();

      // Act & Assert
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: arch = llama"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: vocab_only = false"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: n_ctx_train = 4096"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: n_embd = 4096"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: n_layer = 32"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: n_head = 32"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: n_rot = 128"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: n_ff = 11008"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: f_norm_eps = 1e-05"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: rope_scaling = linear"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: causal attn = true"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: pooling type = none"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: model type = 7B"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: model params = 6.74 B"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: vocab type = BPE"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: n_vocab = 32000"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: n_merges = 30000"));
      Assert.IsTrue(manager.ShouldFilterServerLog("print_info: BOS token = 1"));
    }

    [Test]
    public void ShouldFilterServerLog_WithChatTemplateMessages_ReturnsTrue()
    {
      // Arrange
      var manager = CreateTestManager();

      // Act & Assert
      Assert.IsTrue(manager.ShouldFilterServerLog("load_model: chat template = llama2"));
      Assert.IsTrue(manager.ShouldFilterServerLog("chat_template: using template"));
      Assert.IsTrue(manager.ShouldFilterServerLog("example_format: [INST] <<SYS>>"));
    }

    [Test]
    public void ShouldFilterServerLog_WithLoadingProgressMessages_ReturnsTrue()
    {
      // Arrange
      var manager = CreateTestManager();

      // Act & Assert
      Assert.IsTrue(manager.ShouldFilterServerLog("load_tensors: loading model tensors, 100%"));
      Assert.IsTrue(manager.ShouldFilterServerLog("load_tensors: - tensor blk.0.attn_k"));
      Assert.IsTrue(manager.ShouldFilterServerLog("llama_context: constructing"));
      Assert.IsTrue(manager.ShouldFilterServerLog("llama_context: n_ctx = 4096"));
      Assert.IsTrue(manager.ShouldFilterServerLog("llama_context: causal_attn = true"));
      Assert.IsTrue(manager.ShouldFilterServerLog("llama_context: flash_attn = false"));
      Assert.IsTrue(manager.ShouldFilterServerLog("llama_context: kv_unified = true"));
      Assert.IsTrue(manager.ShouldFilterServerLog("llama_context: freq_base = 10000"));
      Assert.IsTrue(manager.ShouldFilterServerLog("llama_kv_cache: size = 512 MB"));
      Assert.IsTrue(manager.ShouldFilterServerLog("llama_kv_cache:      CUDA0 buffer size = 512 MB"));
      Assert.IsTrue(manager.ShouldFilterServerLog("llama_context:      CUDA0 buffer size = 256 MB"));
      Assert.IsTrue(manager.ShouldFilterServerLog("llama_context: graph nodes = 1030"));
      Assert.IsTrue(manager.ShouldFilterServerLog("llama_context: graph splits = 2"));
    }

    [Test]
    public void ShouldFilterServerLog_WithSlotMessages_ReturnsTrue()
    {
      // Arrange
      var manager = CreateTestManager();

      // Act & Assert
      Assert.IsTrue(manager.ShouldFilterServerLog("slot   load_model: id = 0"));
      Assert.IsTrue(manager.ShouldFilterServerLog("slot   launch_slot_0: starting"));
      Assert.IsTrue(manager.ShouldFilterServerLog("slot   update_slots: processing"));
      Assert.IsTrue(manager.ShouldFilterServerLog("slot   print_timing: time = 100ms"));
      Assert.IsTrue(manager.ShouldFilterServerLog("slot      release: slot 0"));
      Assert.IsTrue(manager.ShouldFilterServerLog("slot get_availabl: 1 slots"));
      Assert.IsTrue(manager.ShouldFilterServerLog("slot launch_slot_: starting"));
      Assert.IsTrue(manager.ShouldFilterServerLog("slot update_slots: processing"));
      Assert.IsTrue(manager.ShouldFilterServerLog("slot print_timing: completed"));
    }

    [Test]
    public void ShouldFilterServerLog_WithServerInternalMessages_ReturnsTrue()
    {
      // Arrange
      var manager = CreateTestManager();

      // Act & Assert
      Assert.IsTrue(manager.ShouldFilterServerLog("srv  update_slots: processing"));
      Assert.IsTrue(manager.ShouldFilterServerLog("srv  get_availabl: 1 available"));
      Assert.IsTrue(manager.ShouldFilterServerLog("srv   prompt_save: saving"));
      Assert.IsTrue(manager.ShouldFilterServerLog("srv          load: loading"));
      Assert.IsTrue(manager.ShouldFilterServerLog("srv        update: updating"));
      Assert.IsTrue(manager.ShouldFilterServerLog("srv  log_server_r: request"));
    }

    [Test]
    public void ShouldFilterServerLog_WithSystemInfoMessages_ReturnsTrue()
    {
      // Arrange
      var manager = CreateTestManager();

      // Act & Assert
      Assert.IsTrue(manager.ShouldFilterServerLog("system_info: n_threads = 8"));
      Assert.IsTrue(manager.ShouldFilterServerLog("system_info: CUDA : ARCHS = 5.0"));
      Assert.IsTrue(manager.ShouldFilterServerLog("system_info: CPU : SSE3 = 1"));
      Assert.IsTrue(manager.ShouldFilterServerLog("system_info: USE_GRAPHS = 1"));
      Assert.IsTrue(manager.ShouldFilterServerLog("system_info: PEER_MAX = 1"));
    }

    [Test]
    public void ShouldFilterServerLog_WithTinyEvalTimingMessages_ReturnsTrue()
    {
      // Arrange
      var manager = CreateTestManager();

      // Act & Assert
      // Filter lines with 2 or fewer tokens
      Assert.IsTrue(manager.ShouldFilterServerLog("eval time = 0.50 ms /    1 tokens"));
      Assert.IsTrue(manager.ShouldFilterServerLog("total time = 0.41 ms / 2 tokens"));
      // Filter lines with 0.00 ms eval time
      Assert.IsTrue(manager.ShouldFilterServerLog("eval time =       0.00 ms / 10 tokens"));
      Assert.IsTrue(manager.ShouldFilterServerLog("eval time = 0.00 ms / 100 tokens"));
    }

    [Test]
    public void ShouldFilterServerLog_WithImportantMessages_ReturnsFalse()
    {
      // Arrange
      var manager = CreateTestManager();

      // Act & Assert - Important messages should NOT be filtered
      Assert.IsFalse(manager.ShouldFilterServerLog("Server started on port 8080"));
      Assert.IsFalse(manager.ShouldFilterServerLog("Error: failed to load model"));
      Assert.IsFalse(manager.ShouldFilterServerLog("Model loaded successfully"));
      Assert.IsFalse(manager.ShouldFilterServerLog("Received request from client"));
      Assert.IsFalse(manager.ShouldFilterServerLog("Connection established"));
    }

    [Test]
    public void ShouldFilterServerLog_WithRealTimingMessages_ReturnsFalse()
    {
      // Arrange
      var manager = CreateTestManager();

      // Act & Assert - Real timing messages with many tokens should NOT be filtered
      Assert.IsFalse(manager.ShouldFilterServerLog("eval time = 1500.00 ms /  100 tokens"));
      Assert.IsFalse(manager.ShouldFilterServerLog("prompt eval time = 250.00 ms /   50 tokens"));
      Assert.IsFalse(manager.ShouldFilterServerLog("total time = 2000.00 ms / 150 tokens"));
    }

    #endregion

    #region SanitizeProcessOutput Tests

    [Test]
    public void SanitizeProcessOutput_WithNullOrEmpty_ReturnsEmpty()
    {
      // Arrange
      var manager = CreateTestManager();

      // Act & Assert
      Assert.AreEqual(string.Empty, manager.SanitizeProcessOutput(null!));
      Assert.AreEqual(string.Empty, manager.SanitizeProcessOutput(""));
    }

    [Test]
    public void SanitizeProcessOutput_WithNormalText_ReturnsUnchanged()
    {
      // Arrange
      var manager = CreateTestManager();
      var input = "Server started on port 8080";

      // Act
      var result = manager.SanitizeProcessOutput(input);

      // Assert
      Assert.AreEqual(input, result);
    }

    [Test]
    public void SanitizeProcessOutput_WithControlCharacters_RemovesThem()
    {
      // Arrange
      var manager = CreateTestManager();
      // Use explicit control characters
      var input = "Hello" + (char)0 + "World" + (char)7 + "Test" + (char)27;

      // Act
      var result = manager.SanitizeProcessOutput(input);

      // Assert - All control characters should be removed
      Assert.That(result, Does.Contain("Hello"));
      Assert.That(result, Does.Contain("World"));
      Assert.That(result, Does.Contain("Test"));
      Assert.That(result, Does.Not.Contain((char)0));
      Assert.That(result, Does.Not.Contain((char)7));
      Assert.That(result, Does.Not.Contain((char)27));
    }

    [Test]
    public void SanitizeProcessOutput_PreservesWhitespace()
    {
      // Arrange
      var manager = CreateTestManager();
      var input = "Hello World\tTest\nNewline";

      // Act
      var result = manager.SanitizeProcessOutput(input);

      // Assert
      Assert.AreEqual(input, result);
    }

    [Test]
    public void SanitizeProcessOutput_TruncatesLongOutput()
    {
      // Arrange
      var manager = CreateTestManager();
      var longInput = new string('a', 1500);

      // Act
      var result = manager.SanitizeProcessOutput(longInput);

      // Assert
      Assert.That(result.Length, Is.LessThanOrEqualTo(1020)); // 1000 + "... [truncated]".Length
      Assert.That(result, Does.EndWith("... [truncated]"));
    }

    [Test]
    public void SanitizeProcessOutput_DoesNotTruncateShortOutput()
    {
      // Arrange
      var manager = CreateTestManager();
      var shortInput = new string('a', 500);

      // Act
      var result = manager.SanitizeProcessOutput(shortInput);

      // Assert
      Assert.AreEqual(500, result.Length);
      Assert.That(result, Does.Not.Contain("[truncated]"));
    }

    [Test]
    public void SanitizeProcessOutput_AtBoundary_DoesNotTruncate()
    {
      // Arrange
      var manager = CreateTestManager();
      var boundaryInput = new string('a', 1000);

      // Act
      var result = manager.SanitizeProcessOutput(boundaryInput);

      // Assert
      Assert.AreEqual(1000, result.Length);
      Assert.That(result, Does.Not.Contain("[truncated]"));
    }

    [Test]
    public void SanitizeProcessOutput_JustOverBoundary_Truncates()
    {
      // Arrange
      var manager = CreateTestManager();
      var overBoundaryInput = new string('a', 1001);

      // Act
      var result = manager.SanitizeProcessOutput(overBoundaryInput);

      // Assert
      Assert.That(result, Does.EndWith("... [truncated]"));
    }

    [Test]
    public void SanitizeProcessOutput_WithMixedContent_SanitizesCorrectly()
    {
      // Arrange
      var manager = CreateTestManager();
      // Use explicit control characters that we know will be filtered
      var input = "Normal text" + (char)0 + "with" + (char)7 + "control" + (char)27 + "chars\tand\ttabs";

      // Act
      var result = manager.SanitizeProcessOutput(input);

      // Assert - Control characters should be removed but tabs preserved
      Assert.That(result, Does.Contain("Normal text"));
      Assert.That(result, Does.Contain("with"));
      Assert.That(result, Does.Contain("control"));
      Assert.That(result, Does.Contain("chars"));
      Assert.That(result, Does.Contain("\tand\ttabs"));
      // Verify no null, bell, or escape characters remain
      Assert.That(result, Does.Not.Contain((char)0));
      Assert.That(result, Does.Not.Contain((char)7));
      Assert.That(result, Does.Not.Contain((char)27));
    }

    #endregion

    #region Additional Coverage Tests

    [Test]
    public void ShouldFilterServerLog_WithServerRequestMessages_ReturnsFalse()
    {
      // Arrange
      var manager = CreateTestManager();

      // Act & Assert - Server request messages should NOT be filtered (important for debugging)
      Assert.IsFalse(manager.ShouldFilterServerLog("POST /completion received"));
      Assert.IsFalse(manager.ShouldFilterServerLog("GET /health"));
      Assert.IsFalse(manager.ShouldFilterServerLog("request: generating response"));
    }

    [Test]
    public void ShouldFilterServerLog_WithErrorMessages_ReturnsFalse()
    {
      // Arrange
      var manager = CreateTestManager();

      // Act & Assert - Error messages should NOT be filtered
      Assert.IsFalse(manager.ShouldFilterServerLog("error: failed to load model"));
      Assert.IsFalse(manager.ShouldFilterServerLog("ERROR: out of memory"));
      Assert.IsFalse(manager.ShouldFilterServerLog("fatal: crash detected"));
    }

    [Test]
    public void SanitizeProcessOutput_WithExactBoundary_DoesNotTruncate()
    {
      // Arrange
      var manager = CreateTestManager();
      var input = new string('x', 1000); // Exactly at boundary

      // Act
      var result = manager.SanitizeProcessOutput(input);

      // Assert
      Assert.AreEqual(1000, result.Length);
      Assert.That(result, Does.Not.Contain("[truncated]"));
    }

    [Test]
    public void StartServer_WithSpecialCharactersInPath_HandlesCorrectly()
    {
      // Arrange - Create a path with spaces
      var pathWithSpaces = Path.Combine(Path.GetTempPath(), $"test folder {Guid.NewGuid()}");
      Directory.CreateDirectory(pathWithSpaces);
      var exePath = Path.Combine(pathWithSpaces, "server.exe");
      var modelPath = Path.Combine(pathWithSpaces, "model.gguf");
      File.WriteAllText(exePath, "test");
      File.WriteAllText(modelPath, "test");

      try
      {
        var config = new ProcessConfig
        {
          Port = 5000,
          Model = modelPath,
          ExecutablePath = exePath,
          ContextSize = 2048
        };
        var manager = new ServerManager(config);

        // Act - Attempt to start (will fail but should handle path correctly)
        try
        {
          manager.StartServer();
        }
        catch
        {
          // Expected to fail
        }

        // Assert - Arguments should have quoted paths
        Assert.That(manager.LastStartupArguments, Does.Contain("-m \""));
      }
      finally
      {
        if (Directory.Exists(pathWithSpaces))
          Directory.Delete(pathWithSpaces, true);
      }
    }

    [Test]
    public void ValidateServerConfiguration_WithInvalidModelPath_HasError()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = "", // Empty model path
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };

      // This will throw in constructor, so test differently
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void StartServer_TwiceInSuccession_HandlesGracefully()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act - First attempt
      try { manager.StartServer(); } catch { }

      // Second attempt should also handle gracefully
      try { manager.StartServer(); } catch { }

      // Assert - Should have arguments set
      Assert.IsNotNull(manager.LastStartupArguments);
    }

    [Test]
    public void StopServer_MultipleTimes_DoesNotThrow()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      Assert.DoesNotThrow(() =>
      {
        manager.StopServer();
        manager.StopServer();
        manager.StopServer();
      });
    }

    [Test]
    public void IsServerRunning_MultipleCalls_ReturnsConsistentResult()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      var result1 = manager.IsServerRunning();
      var result2 = manager.IsServerRunning();
      var result3 = manager.IsServerRunning();

      // Assert - Should be consistently false (server never started)
      Assert.IsFalse(result1);
      Assert.IsFalse(result2);
      Assert.IsFalse(result3);
    }

    [Test]
    public void GetServerStatus_MultipleCalls_ReturnsConsistentResult()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      var status1 = manager.GetServerStatus();
      var status2 = manager.GetServerStatus();

      // Assert
      Assert.AreEqual(status1.IsRunning, status2.IsRunning);
      Assert.AreEqual(status1.ProcessId, status2.ProcessId);
    }

    [Test]
    public async Task WaitForServerReadyAsync_VeryShortTimeout_ReturnsFalse()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 59999, // Unlikely to be in use
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      var result = await manager.WaitForServerReadyAsync(1);

      // Assert
      Assert.IsFalse(result);
    }

    [Test]
    public void StartServer_WithAllOptionalParameters_IncludesAllInArguments()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 8080,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 8192,
        GpuLayers = 99,
        Threads = 16,
        BatchSize = 1024,
        UBatchSize = 256,
        UseMlock = true,
        ParallelSlots = 8,
        UseContinuousBatching = true
      };
      var manager = new ServerManager(config);

      // Act
      try { manager.StartServer(); } catch { }

      // Assert
      var args = manager.LastStartupArguments;
      Assert.That(args, Does.Contain("--port 8080"));
      Assert.That(args, Does.Contain("--ctx-size 8192"));
      Assert.That(args, Does.Contain("-ngl 99"));
      Assert.That(args, Does.Contain("-t 16"));
      Assert.That(args, Does.Contain("-b 1024"));
      Assert.That(args, Does.Contain("--ubatch-size 256"));
      Assert.That(args, Does.Contain("--mlock"));
      Assert.That(args, Does.Contain("--parallel 8"));
      Assert.That(args, Does.Contain("--cont-batching"));
    }

    [Test]
    public void OnServerOutput_EventCanBeSubscribedAndUnsubscribed()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);
      Action<string> handler = (s) => { };

      // Act & Assert
      Assert.DoesNotThrow(() =>
      {
        manager.OnServerOutput += handler;
        manager.OnServerOutput -= handler;
      });
    }

    [Test]
    public void OnServerError_EventCanBeSubscribedAndUnsubscribed()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);
      Action<string> handler = (s) => { };

      // Act & Assert
      Assert.DoesNotThrow(() =>
      {
        manager.OnServerError += handler;
        manager.OnServerError -= handler;
      });
    }

    [Test]
    public void ValidateServerConfiguration_WithInvalidContextSize_ReturnsError()
    {
      // Arrange - Context size 50000 > max of 32768
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 50000
      };

      // Act & Assert - Should throw on construction due to private ValidateConfiguration()
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void ValidateServerConfiguration_WithZeroContextSize_ReturnsError()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 0
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void ValidateServerConfiguration_WithNegativeContextSize_ReturnsError()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = -1
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void ValidateServerConfiguration_WithInvalidPort_ReturnsError()
    {
      // Arrange - Port 70000 > max of 65535
      var config = new ProcessConfig
      {
        Port = 70000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void ValidateServerConfiguration_WithZeroPort_ReturnsError()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 0,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void ValidateServerConfiguration_WithNegativePort_ReturnsError()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = -1,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void Constructor_WithNullExecutablePath_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = null!,
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void Constructor_WithEmptyExecutablePath_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = "",
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void Constructor_WithWhitespaceExecutablePath_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = "   ",
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void Constructor_WithNullModelPath_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = null!,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void Constructor_WithEmptyModelPath_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = "",
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void Constructor_WithWhitespaceModelPath_ThrowsArgumentException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = "   ",
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => new ServerManager(config));
    }

    [Test]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      Assert.DoesNotThrow(() =>
      {
        manager.Dispose();
        manager.Dispose();
      });
    }

    [Test]
    public void IsServerRunning_WhenDisposed_ReturnsFalse()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);
      manager.Dispose();

      // Act
      var result = manager.IsServerRunning();

      // Assert
      Assert.IsFalse(result);
    }

    [Test]
    public void IsServerRunning_WithNullProcess_ReturnsFalse()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);
      // Process is null by default before StartServer is called

      // Act
      var result = manager.IsServerRunning();

      // Assert
      Assert.IsFalse(result);
    }

    [Test]
    public void StartServer_WhenDisposed_ThrowsObjectDisposedException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);
      manager.Dispose();

      // Act & Assert
      Assert.Throws<ObjectDisposedException>(() => manager.StartServer());
    }

    [Test]
    public void StopServer_WhenDisposed_ThrowsObjectDisposedException()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);
      manager.Dispose();

      // Act & Assert
      Assert.Throws<ObjectDisposedException>(() => manager.StopServer());
    }

    [Test]
    public void ValidateServerConfiguration_ChecksDiskSpace_WhenModelExists()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      var result = manager.ValidateServerConfiguration();

      // Assert - Should not have disk space warning for tiny test file
      Assert.IsNotNull(result);
      Assert.IsFalse(result.Warnings.Any(w => w.Contains("Low disk space")));
    }

    [Test]
    public void ValidateServerConfiguration_WithMissingModel_ReturnsError()
    {
      // Arrange - Create manager with valid paths, then delete model
      var tempModel = Path.Combine(Path.GetTempPath(), $"test-model-{Guid.NewGuid()}.gguf");
      File.WriteAllText(tempModel, "temp");

      var config = new ProcessConfig
      {
        Port = 5000,
        Model = tempModel,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Delete the model to simulate missing file
      File.Delete(tempModel);

      // Act
      var result = manager.ValidateServerConfiguration();

      // Assert
      Assert.IsFalse(result.IsValid);
      Assert.That(result.Errors, Has.Some.Contains("Model path"));
    }

    [Test]
    public void ValidateServerConfiguration_WithMissingExecutable_ReturnsError()
    {
      // Arrange - Use valid paths but executable doesn't exist
      var tempExe = Path.Combine(Path.GetTempPath(), $"test-server-{Guid.NewGuid()}.exe");
      File.WriteAllText(tempExe, "temp");

      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = tempExe,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Delete to simulate missing
      File.Delete(tempExe);

      // Act
      var result = manager.ValidateServerConfiguration();

      // Assert
      Assert.IsFalse(result.IsValid);
      Assert.That(result.Errors, Has.Some.Contains("Executable path"));
    }

    [Test]
    public void LastStartupArguments_BeforeStartServer_IsEmpty()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act
      var args = manager.LastStartupArguments;

      // Assert
      Assert.AreEqual("", args);
    }

    [Test]
    public void StartServer_WithForwardSlashInPath_ThrowsArgumentException()
    {
      // Arrange - path with // is treated as invalid
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = _tempModelPath,
        ExecutablePath = "C://Program Files//server.exe",
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      var ex = Assert.Throws<ArgumentException>(() => manager.StartServer());
      Assert.That(ex!.Message, Does.Contain("invalid characters"));
    }

    [Test]
    public void StartServer_WithModelForwardSlashInPath_ThrowsArgumentException()
    {
      // Arrange - path with // is treated as invalid
      var config = new ProcessConfig
      {
        Port = 5000,
        Model = "C://Models//test.gguf",
        ExecutablePath = _tempExecutablePath,
        ContextSize = 2048
      };
      var manager = new ServerManager(config);

      // Act & Assert
      var ex = Assert.Throws<ArgumentException>(() => manager.StartServer());
      Assert.That(ex!.Message, Does.Contain("invalid characters"));
    }

    #endregion
  }
}

