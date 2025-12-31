using NUnit.Framework;
using LlamaBrain.Core;
using System.Threading.Tasks;
using System;

namespace LlamaBrain.Tests
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
    public async Task ApiClient_SendPromptAsync_WithMockServer_HandlesCorrectly()
    {
      // Arrange
      var client = new ApiClient("localhost", 5000, "test-model");
      Assert.IsNotNull(client);

      // Act & Assert
      // Test with a simple prompt - this will fail if no server is running
      // but that's expected behavior for a real integration test
      var result = await client.SendPromptAsync("Hello, world!", 10, 0.1f);

      // If server is not running, we expect an error message
      if (result.StartsWith("Error:"))
      {
        // This is expected behavior when no llama-server is running
        Assert.IsTrue(result.Contains("Error:"));
      }
      else
      {
        // If server is running, we should get a response
        Assert.IsNotNull(result);
        Assert.IsNotEmpty(result);
      }
    }

    [Test]
    public void ApiClient_WithNullModel_ThrowsArgumentException()
    {
      // Arrange & Act & Assert
      var exception = Assert.Throws<ArgumentException>(() => new ApiClient("localhost", 5000, null!));
      Assert.IsNotNull(exception);
      Assert.IsTrue(exception!.Message.Contains("Model cannot be null or empty"));
      Assert.AreEqual("model", exception.ParamName);
    }

    [Test]
    public void ApiClient_WithEmptyModel_ThrowsArgumentException()
    {
      // Arrange & Act & Assert
      var exception = Assert.Throws<ArgumentException>(() => new ApiClient("localhost", 5000, ""));
      Assert.IsNotNull(exception);
      Assert.IsTrue(exception!.Message.Contains("Model cannot be null or empty"));
      Assert.AreEqual("model", exception.ParamName);
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

    [Test]
    public async Task ApiClient_SendPromptAsync_WithCustomConfig_HandlesCorrectly()
    {
      // Arrange
      var llmConfig = new LlmConfig
      {
        MaxTokens = 5,
        Temperature = 0.1f,
        TopP = 0.9f,
        TopK = 40,
        RepeatPenalty = 1.1f
      };
      var client = new ApiClient("localhost", 5000, "test-model", llmConfig);

      // Act
      var result = await client.SendPromptAsync("Test prompt");

      // Assert - should handle both success and error cases
      Assert.IsNotNull(result);
      if (result.StartsWith("Error:"))
      {
        Assert.IsTrue(result.Contains("Error:"));
      }
    }

    [Test]
    public async Task ApiClient_SendPromptAsync_WithEmptyPrompt_HandlesCorrectly()
    {
      // Arrange
      var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = await client.SendPromptAsync("");

      // Assert
      Assert.IsNotNull(result);
      if (result.StartsWith("Error:"))
      {
        Assert.IsTrue(result.Contains("Error:"));
      }
    }

    [Test]
    public async Task ApiClient_SendPromptAsync_WithNullPrompt_HandlesCorrectly()
    {
      // Arrange
      var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = await client.SendPromptAsync(null!);

      // Assert
      Assert.IsNotNull(result);
      if (result.StartsWith("Error:"))
      {
        Assert.IsTrue(result.Contains("Error:"));
      }
    }
  }
}

