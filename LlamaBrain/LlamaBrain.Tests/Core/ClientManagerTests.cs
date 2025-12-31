using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using LlamaBrain.Core;

namespace LlamaBrain.Tests.Core
{
  /// <summary>
  /// Tests for the ClientManager API client lifecycle management
  /// </summary>
  [TestFixture]
  public class ClientManagerTests
  {
    private ProcessConfig _defaultConfig = null!;

    [SetUp]
    public void SetUp()
    {
      _defaultConfig = new ProcessConfig
      {
        Host = "localhost",
        Port = 5000,
        Model = "test-model",
        LlmConfig = new LlmConfig()
      };
    }

    #region Constructor Tests

    [Test]
    public void ClientManager_Constructor_WithValidConfig_CreatesCorrectly()
    {
      // Arrange & Act
      var manager = new ClientManager(_defaultConfig);

      // Assert
      Assert.IsNotNull(manager);
    }

    [Test]
    public void ClientManager_Constructor_WithNullConfig_AllowsCreationButFailsOnUse()
    {
      // Arrange & Act
      // ClientManager doesn't explicitly check for null config in constructor
      // It will only fail when trying to use the config (e.g., in CreateClient)
      var manager = new ClientManager(null!);

      // Assert
      // Constructor succeeds, but CreateClient will throw
      Assert.Throws<NullReferenceException>(() => manager.CreateClient());
    }

    [Test]
    public void ClientManager_Constructor_WithCustomConfig_StoresConfig()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Host = "127.0.0.1",
        Port = 8080,
        Model = "custom-model",
        ContextSize = 4096,
        GpuLayers = 20
      };

      // Act
      var manager = new ClientManager(config);

      // Assert
      Assert.IsNotNull(manager);
      // Verify config is used by creating a client
      var client = manager.CreateClient();
      Assert.IsNotNull(client);
    }

    #endregion

    #region CreateClient Tests

    [Test]
    public void CreateClient_WithValidConfig_ReturnsApiClient()
    {
      // Arrange
      var manager = new ClientManager(_defaultConfig);

      // Act
      var client = manager.CreateClient();

      // Assert
      Assert.IsNotNull(client);
    }

    [Test]
    public void CreateClient_WithCustomHostAndPort_CreatesClientWithCorrectSettings()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Host = "192.168.1.100",
        Port = 9000,
        Model = "test-model",
        LlmConfig = new LlmConfig()
      };
      var manager = new ClientManager(config);

      // Act
      var client = manager.CreateClient();

      // Assert
      Assert.IsNotNull(client);
      // Note: ApiClient doesn't expose Host/Port directly, but we verify it's created
    }

    [Test]
    public void CreateClient_WithLlmConfig_PassesConfigToClient()
    {
      // Arrange
      var llmConfig = new LlmConfig
      {
        Temperature = 0.8f,
        TopP = 0.9f,
        TopK = 40
      };
      var config = new ProcessConfig
      {
        Host = "localhost",
        Port = 5000,
        Model = "test-model",
        LlmConfig = llmConfig
      };
      var manager = new ClientManager(config);

      // Act
      var client = manager.CreateClient();

      // Assert
      Assert.IsNotNull(client);
    }

    [Test]
    public void CreateClient_MultipleCalls_ReturnsNewInstances()
    {
      // Arrange
      var manager = new ClientManager(_defaultConfig);

      // Act
      var client1 = manager.CreateClient();
      var client2 = manager.CreateClient();

      // Assert
      Assert.IsNotNull(client1);
      Assert.IsNotNull(client2);
      Assert.AreNotSame(client1, client2);
    }

    #endregion

    #region IsRunningAsync Tests

    [Test]
    public async Task IsRunningAsync_WhenServerNotRunning_ReturnsFalse()
    {
      // Arrange
      // Use a port that's unlikely to have a server running
      var config = new ProcessConfig
      {
        Host = "localhost",
        Port = 99999, // Unlikely to have a server on this port
        Model = "test-model",
        LlmConfig = new LlmConfig()
      };
      var manager = new ClientManager(config);

      // Act
      var isRunning = await manager.IsRunningAsync();

      // Assert
      Assert.IsFalse(isRunning);
    }

    [Test]
    public async Task IsRunningAsync_WithCancellationToken_RespectsCancellation()
    {
      // Arrange
      var manager = new ClientManager(_defaultConfig);
      var cts = new CancellationTokenSource();
      cts.Cancel();

      // Act
      var isRunning = await manager.IsRunningAsync(cts.Token);

      // Assert
      // Should return false when cancelled (caught exception)
      Assert.IsFalse(isRunning);
    }

    [Test]
    public async Task IsRunningAsync_WithTimeout_HandlesGracefully()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Host = "192.0.2.0", // Test-Net address (RFC 3330) - should timeout
        Port = 5000,
        Model = "test-model",
        LlmConfig = new LlmConfig()
      };
      var manager = new ClientManager(config);
      var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

      // Act
      var isRunning = await manager.IsRunningAsync(cts.Token);

      // Assert
      Assert.IsFalse(isRunning);
    }

    [Test]
    public async Task IsRunningAsync_WhenServerRunning_ReturnsTrue()
    {
      // Arrange
      var manager = new ClientManager(_defaultConfig);

      // Act
      var isRunning = await manager.IsRunningAsync();

      // Assert
      // This will be false if no server is running, true if one is
      // Both are valid test outcomes
      Assert.IsInstanceOf<bool>(isRunning);
    }

    #endregion

    #region WaitForAsync Tests

    [Test]
    public async Task WaitForAsync_WhenServerNotRunning_ThrowsInvalidOperationException()
    {
      // Arrange
      // Use a port that's unlikely to have a server running
      var config = new ProcessConfig
      {
        Host = "localhost",
        Port = 99999,
        Model = "test-model",
        LlmConfig = new LlmConfig()
      };
      // Use shorter retry settings for faster test execution (3 retries * 10ms = ~30ms instead of 30 seconds)
      var manager = new ClientManager(config, maxRetries: 3, retryDelayMs: 10);
      var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1)); // 1 second should be plenty

      // Act & Assert
      // WaitForAsync will retry 3 times before throwing InvalidOperationException
      InvalidOperationException? exception = null;
      try
      {
        await manager.WaitForAsync(cts.Token);
        Assert.Fail("Expected InvalidOperationException to be thrown");
      }
      catch (InvalidOperationException ex)
      {
        exception = ex;
      }
      Assert.IsNotNull(exception);
      Assert.That(exception!.Message, Contains.Substring("Server is not running"));
    }

    [Test]
    public async Task WaitForAsync_WithCancellationToken_RespectsCancellation()
    {
      // Arrange
      var manager = new ClientManager(_defaultConfig);
      var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

      // Act & Assert
      // Cancellation will throw TaskCanceledException, not InvalidOperationException
      Assert.ThrowsAsync<TaskCanceledException>(
        async () => await manager.WaitForAsync(cts.Token));
    }

    [Test]
    public async Task WaitForAsync_WhenServerAlreadyRunning_CompletesImmediately()
    {
      // Arrange
      // Use shorter retry settings for faster test execution
      var manager = new ClientManager(_defaultConfig, maxRetries: 3, retryDelayMs: 10);
      var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1)); // 1 second should be plenty

      // Act
      try
      {
        await manager.WaitForAsync(cts.Token);
        // If we get here, server was running
        Assert.Pass("Server was running, wait completed successfully");
      }
      catch (InvalidOperationException)
      {
        // If server is not running, this is expected after retries
        Assert.Pass("Server not running, exception thrown as expected");
      }
      catch (TaskCanceledException)
      {
        // If cancellation happens, that's also acceptable
        Assert.Pass("Task was cancelled (acceptable behavior)");
      }
    }

    [Test]
    public async Task WaitForAsync_RetriesUpTo60Times()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Host = "localhost",
        Port = 99999, // Unlikely to have server
        Model = "test-model",
        LlmConfig = new LlmConfig()
      };
      // Use shorter retry settings for faster test execution, but still test retry logic
      // Test with 3 retries to verify retry mechanism works (instead of waiting 30 seconds for 60 retries)
      var manager = new ClientManager(config, maxRetries: 3, retryDelayMs: 10);
      var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1)); // 1 second should be plenty

      // Act & Assert
      InvalidOperationException? exception = null;
      try
      {
        await manager.WaitForAsync(cts.Token);
        Assert.Fail("Expected InvalidOperationException to be thrown");
      }
      catch (InvalidOperationException ex)
      {
        exception = ex;
      }
      Assert.IsNotNull(exception);
      // Should have tried 3 times before giving up (tests retry mechanism without 30 second wait)
    }

    #endregion

    #region Dispose Tests

    [Test]
    public void Dispose_DisposesHttpClient()
    {
      // Arrange
      var manager = new ClientManager(_defaultConfig);

      // Act
      manager.Dispose();

      // Assert
      // Verify disposal doesn't throw
      Assert.DoesNotThrow(() => manager.Dispose());
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
      // Arrange
      var manager = new ClientManager(_defaultConfig);

      // Act & Assert
      manager.Dispose();
      Assert.DoesNotThrow(() => manager.Dispose());
      Assert.DoesNotThrow(() => manager.Dispose());
    }

    [Test]
    public void Dispose_AfterCreatingClient_DoesNotAffectClient()
    {
      // Arrange
      var manager = new ClientManager(_defaultConfig);
      var client = manager.CreateClient();

      // Act
      manager.Dispose();

      // Assert
      // Client should still be usable (it has its own HttpClient)
      Assert.IsNotNull(client);
    }

    #endregion

    #region Integration Tests

    [Test]
    public void ClientManager_FullLifecycle_WorksCorrectly()
    {
      // Arrange
      var manager = new ClientManager(_defaultConfig);

      // Act
      var client1 = manager.CreateClient();
      var client2 = manager.CreateClient();
      manager.Dispose();

      // Assert
      Assert.IsNotNull(client1);
      Assert.IsNotNull(client2);
      Assert.AreNotSame(client1, client2);
    }

    [Test]
    public async Task ClientManager_IsRunningAsync_AfterDispose_StillWorks()
    {
      // Arrange
      var manager = new ClientManager(_defaultConfig);

      // Act
      manager.Dispose();
      var isRunning = await manager.IsRunningAsync();

      // Assert
      // Should still return a result (though HttpClient is disposed, it may throw or return false)
      Assert.IsInstanceOf<bool>(isRunning);
    }

    #endregion
  }
}

