using NUnit.Framework;
using UnityEngine;
using UnityBrainDemo.Runtime.Core;
using UnityBrain.Core;
using System.IO;

namespace UnityBrainDemo.Tests.EditMode
{
  public class UnityBrainSettingsTests
  {
    private UnityBrainSettings settings;

    [SetUp]
    public void SetUp()
    {
      settings = ScriptableObject.CreateInstance<UnityBrainSettings>();
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
    public void UnityBrainSettings_DefaultValues_AreCorrect()
    {
      // Assert
      Assert.AreEqual(5000, settings.Port);
      Assert.AreEqual(2048, settings.ContextSize);
      Assert.IsNull(settings.ExecutablePath);
      Assert.IsNull(settings.ModelPath);
    }


  }
}