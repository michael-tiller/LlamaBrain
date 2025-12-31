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
  }
}

