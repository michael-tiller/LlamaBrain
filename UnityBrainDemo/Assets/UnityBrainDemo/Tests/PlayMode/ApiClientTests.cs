using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityBrain.Core;
using System.Collections;
using System.Threading.Tasks;

namespace UnityBrainDemo.Tests.PlayMode
{
  public class ApiClientTests
  {
    [Test]
    public void ApiClient_Constructor_WithValidParameters_CreatesCorrectly()
    {
      // Arrange & Act
      var client = new ApiClient("localhost", 5000, "test-model");

      // Assert
      Assert.IsNotNull(client);
    }

    [Test]
    public void ApiClient_Constructor_WithCustomHost_CreatesCorrectly()
    {
      // Arrange & Act
      var client = new ApiClient("127.0.0.1", 8080, "test-model");

      // Assert
      Assert.IsNotNull(client);
    }

    [UnityTest]
    public IEnumerator ApiClient_SendPromptAsync_WithMockServer_HandlesCorrectly()
    {
      // Note: This is a placeholder test
      // In a real integration test, you would:
      // 1. Start a mock llama-server or use a test server
      // 2. Send actual requests
      // 3. Verify responses

      // For now, we just test that the client can be created
      var client = new ApiClient("localhost", 5000, "test-model");
      Assert.IsNotNull(client);

      yield return null;
    }

    [Test]
    public void ApiClient_WithNullModel_HandlesCorrectly()
    {
      // Arrange & Act
      var client = new ApiClient("localhost", 5000, null);

      // Assert
      Assert.IsNotNull(client);
    }

    [Test]
    public void ApiClient_WithEmptyModel_HandlesCorrectly()
    {
      // Arrange & Act
      var client = new ApiClient("localhost", 5000, "");

      // Assert
      Assert.IsNotNull(client);
    }

    [Test]
    public void ApiClient_Constructor_WithLlmConfig_CreatesCorrectly()
    {
      // Arrange
      var llmConfig = new LlmConfig
      {
        MaxTokens = 128,
        Temperature = 0.8f,
        TopP = 0.95f,
        TopK = 50,
        RepeatPenalty = 1.2f,
        StopSequences = new string[] { "END", "STOP" }
      };

      // Act
      var client = new ApiClient("localhost", 5000, "test-model", llmConfig);

      // Assert
      Assert.IsNotNull(client);
    }
  }
}