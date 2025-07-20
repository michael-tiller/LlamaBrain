using NUnit.Framework;
using UnityBrain.Core;

namespace UnityBrainDemo.Tests.EditMode
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
  }
}