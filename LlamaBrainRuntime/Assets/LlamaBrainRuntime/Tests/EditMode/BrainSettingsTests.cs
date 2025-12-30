using NUnit.Framework;
using UnityEngine;
using LlamaBrain.Runtime.Core;
using LlamaBrain.Core;
using System.IO;

namespace LlamaBrain.Tests.EditMode
{
  public class BrainSettingsTests
  {
    private BrainSettings settings;

    [SetUp]
    public void SetUp()
    {
      settings = ScriptableObject.CreateInstance<BrainSettings>();
    }

    [TearDown]
    public void TearDown()
    {
      if (settings != null)
      {
        Object.DestroyImmediate(settings);
      }
    }

    [Test]
    public void ToProcessConfig_WithValidSettings_ReturnsCorrectConfig()
    {
      // Arrange
      settings.ExecutablePath = "test/path/llama-server.exe";
      settings.ModelPath = "test/path/model.gguf";
      settings.Port = 5001;
      settings.ContextSize = 4096;

      // Act
      var config = settings.ToProcessConfig();

      // Assert
      Assert.AreEqual("localhost", config.Host);
      Assert.AreEqual(5001, config.Port);
      Assert.AreEqual("test/path/model.gguf", config.Model);
      Assert.AreEqual("test/path/llama-server.exe", config.ExecutablePath);
      Assert.AreEqual(4096, config.ContextSize);
    }

    [Test]
    public void ToProcessConfig_WithDefaultSettings_ReturnsDefaultConfig()
    {
      // Act
      var config = settings.ToProcessConfig();

      // Assert
      Assert.AreEqual("localhost", config.Host);
      Assert.AreEqual(5000, config.Port);
      Assert.AreEqual(string.Empty, config.Model);
      Assert.AreEqual(string.Empty, config.ExecutablePath);
      Assert.AreEqual(2048, config.ContextSize);
    }

    [Test]
    public void ToProcessConfig_WithLlmSettings_ReturnsCorrectLlmConfig()
    {
      // Arrange
      settings.MaxTokens = 128;
      settings.Temperature = 0.8f;
      settings.TopP = 0.95f;
      settings.TopK = 50;
      settings.RepeatPenalty = 1.2f;
      settings.StopSequences = new string[] { "END", "STOP" };

      // Act
      var config = settings.ToProcessConfig();

      // Assert
      Assert.IsNotNull(config.LlmConfig);
      Assert.AreEqual(128, config.LlmConfig.MaxTokens);
      Assert.AreEqual(0.8f, config.LlmConfig.Temperature);
      Assert.AreEqual(0.95f, config.LlmConfig.TopP);
      Assert.AreEqual(50, config.LlmConfig.TopK);
      Assert.AreEqual(1.2f, config.LlmConfig.RepeatPenalty);
      Assert.AreEqual(new string[] { "END", "STOP" }, config.LlmConfig.StopSequences);
    }

    [Test]
    public void ToLlmConfig_WithCustomSettings_ReturnsCorrectConfig()
    {
      // Arrange
      settings.MaxTokens = 256;
      settings.Temperature = 0.5f;
      settings.TopP = 0.8f;
      settings.TopK = 30;
      settings.RepeatPenalty = 1.5f;
      settings.StopSequences = new string[] { "DONE", "FINISH" };

      // Act
      var llmConfig = settings.ToLlmConfig();

      // Assert
      Assert.AreEqual(256, llmConfig.MaxTokens);
      Assert.AreEqual(0.5f, llmConfig.Temperature);
      Assert.AreEqual(0.8f, llmConfig.TopP);
      Assert.AreEqual(30, llmConfig.TopK);
      Assert.AreEqual(1.5f, llmConfig.RepeatPenalty);
      Assert.AreEqual(new string[] { "DONE", "FINISH" }, llmConfig.StopSequences);
    }

  }
}