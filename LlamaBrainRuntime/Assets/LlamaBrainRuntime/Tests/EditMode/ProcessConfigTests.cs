using NUnit.Framework;
using LlamaBrain.Core;

namespace LlamaBrain.Tests.EditMode
{
  public class ProcessConfigTests
  {
    [Test]
    public void ProcessConfig_DefaultValues_AreCorrect()
    {
      // Arrange & Act
      var config = new ProcessConfig();

      // Assert
      Assert.AreEqual("localhost", config.Host);
      Assert.AreEqual(5000, config.Port);
      Assert.AreEqual("", config.Model);
      Assert.AreEqual("", config.ExecutablePath);
      Assert.AreEqual(2048, config.ContextSize);
    }

    [Test]
    public void ProcessConfig_WithCustomValues_StoresCorrectly()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Host = "127.0.0.1",
        Port = 8080,
        Model = "test-model.gguf",
        ExecutablePath = "test-server.exe",
        ContextSize = 4096
      };

      // Assert
      Assert.AreEqual("127.0.0.1", config.Host);
      Assert.AreEqual(8080, config.Port);
      Assert.AreEqual("test-model.gguf", config.Model);
      Assert.AreEqual("test-server.exe", config.ExecutablePath);
      Assert.AreEqual(4096, config.ContextSize);
    }

    [Test]
    public void ProcessConfig_WithNullValues_HandlesCorrectly()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Host = null,
        Model = null,
        ExecutablePath = null
      };

      // Assert
      Assert.IsNull(config.Host);
      Assert.IsNull(config.Model);
      Assert.IsNull(config.ExecutablePath);
      Assert.AreEqual(5000, config.Port);
      Assert.AreEqual(2048, config.ContextSize);
    }

    [Test]
    public void ProcessConfig_WithLlmConfig_StoresCorrectly()
    {
      // Arrange
      var llmConfig = new LlmConfig
      {
        MaxTokens = 256,
        Temperature = 0.8f,
        TopP = 0.95f,
        TopK = 50,
        RepeatPenalty = 1.2f,
        StopSequences = new string[] { "END", "STOP" }
      };

      var config = new ProcessConfig
      {
        Host = "localhost",
        Port = 5000,
        Model = "test-model.gguf",
        ExecutablePath = "test-server.exe",
        ContextSize = 2048,
        LlmConfig = llmConfig
      };

      // Assert
      Assert.AreEqual("localhost", config.Host);
      Assert.AreEqual(5000, config.Port);
      Assert.AreEqual("test-model.gguf", config.Model);
      Assert.AreEqual("test-server.exe", config.ExecutablePath);
      Assert.AreEqual(2048, config.ContextSize);
      Assert.IsNotNull(config.LlmConfig);
      Assert.AreEqual(256, config.LlmConfig.MaxTokens);
      Assert.AreEqual(0.8f, config.LlmConfig.Temperature);
      Assert.AreEqual(0.95f, config.LlmConfig.TopP);
      Assert.AreEqual(50, config.LlmConfig.TopK);
      Assert.AreEqual(1.2f, config.LlmConfig.RepeatPenalty);
      Assert.IsNotNull(config.LlmConfig.StopSequences);
      Assert.AreEqual(2, config.LlmConfig.StopSequences.Length);
      Assert.Contains("END", config.LlmConfig.StopSequences);
      Assert.Contains("STOP", config.LlmConfig.StopSequences);
    }

    [Test]
    public void ProcessConfig_WithEmptyLlmConfig_InitializesCorrectly()
    {
      // Arrange
      var config = new ProcessConfig
      {
        LlmConfig = null
      };

      // Act
      config.LlmConfig = new LlmConfig();

      // Assert
      Assert.IsNotNull(config.LlmConfig);
      Assert.AreEqual(64, config.LlmConfig.MaxTokens);
      Assert.AreEqual(0.7f, config.LlmConfig.Temperature);
      Assert.AreEqual(0.9f, config.LlmConfig.TopP);
      Assert.AreEqual(40, config.LlmConfig.TopK);
      Assert.AreEqual(1.1f, config.LlmConfig.RepeatPenalty);
    }

    [Test]
    public void ProcessConfig_WithEdgeCaseValues_HandlesCorrectly()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Host = "",
        Port = 0,
        Model = "",
        ExecutablePath = "",
        ContextSize = 0
      };

      // Assert
      Assert.AreEqual("", config.Host);
      Assert.AreEqual(0, config.Port);
      Assert.AreEqual("", config.Model);
      Assert.AreEqual("", config.ExecutablePath);
      Assert.AreEqual(0, config.ContextSize);
    }

    [Test]
    public void ProcessConfig_WithLargeValues_HandlesCorrectly()
    {
      // Arrange
      var config = new ProcessConfig
      {
        Host = "very-long-host-name-that-exceeds-normal-length-limits.example.com",
        Port = 65535,
        Model = "very-long-model-path/with/many/subdirectories/and/a/very/long/filename.gguf",
        ExecutablePath = "very-long-executable-path/with/many/subdirectories/and/a/very/long/filename.exe",
        ContextSize = int.MaxValue
      };

      // Assert
      Assert.AreEqual("very-long-host-name-that-exceeds-normal-length-limits.example.com", config.Host);
      Assert.AreEqual(65535, config.Port);
      Assert.AreEqual("very-long-model-path/with/many/subdirectories/and/a/very/long/filename.gguf", config.Model);
      Assert.AreEqual("very-long-executable-path/with/many/subdirectories/and/a/very/long/filename.exe", config.ExecutablePath);
      Assert.AreEqual(int.MaxValue, config.ContextSize);
    }

    [Test]
    public void ProcessConfig_PropertyModification_WorksCorrectly()
    {
      // Arrange
      var config = new ProcessConfig();

      // Act - Modify properties after creation
      config.Host = "modified-host";
      config.Port = 9999;
      config.Model = "modified-model.gguf";
      config.ExecutablePath = "modified-server.exe";
      config.ContextSize = 8192;

      // Assert
      Assert.AreEqual("modified-host", config.Host);
      Assert.AreEqual(9999, config.Port);
      Assert.AreEqual("modified-model.gguf", config.Model);
      Assert.AreEqual("modified-server.exe", config.ExecutablePath);
      Assert.AreEqual(8192, config.ContextSize);
    }
  }
}