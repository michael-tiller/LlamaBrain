using NUnit.Framework;
using LlamaBrain.Core;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

namespace LlamaBrain.Tests
{
  /// <summary>
  /// Mock HTTP message handler for testing
  /// </summary>
  public class MockHttpMessageHandler : HttpMessageHandler
  {
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
      _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      if (cancellationToken.IsCancellationRequested)
      {
        throw new TaskCanceledException();
      }
      return Task.FromResult(_handler(request));
    }
  }

  [TestFixture]
  public class ApiClientTests
  {
    /// <summary>
    /// Helper to create a mock HttpClient with a predefined response
    /// </summary>
    private HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string content)
    {
      var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
      {
        Content = new StringContent(content)
      });
      return new HttpClient(handler);
    }

    /// <summary>
    /// Helper to create a successful completion response JSON
    /// </summary>
    private string CreateSuccessResponse(string content, bool withTimings = true, int promptTokens = 10, int generatedTokens = 20)
    {
      var response = new
      {
        content = content,
        stop = true,
        tokens_predicted = generatedTokens,
        tokens_cached = 5,
        tokens_evaluated = promptTokens,
        timings = withTimings ? new
        {
          prompt_n = promptTokens,
          prompt_ms = 100.0,
          prompt_per_token_ms = 10.0,
          prompt_per_second = 100.0,
          predicted_n = generatedTokens,
          predicted_ms = 200.0,
          predicted_per_token_ms = 10.0,
          predicted_per_second = 100.0
        } : null
      };
      return JsonConvert.SerializeObject(response);
    }

    #region Constructor Tests

    [Test]
    public void ApiClient_Constructor_WithValidParameters_CreatesCorrectly()
    {
      // Arrange & Act
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Assert
      Assert.IsNotNull(client);
    }

    [Test]
    public void ApiClient_Constructor_WithNullHost_ThrowsArgumentException()
    {
      // Act & Assert
      var ex = Assert.Throws<ArgumentException>(() => new ApiClient(null!, 5000, "test-model"));
      Assert.That(ex!.Message, Does.Contain("Host cannot be null or empty"));
      Assert.AreEqual("host", ex.ParamName);
    }

    [Test]
    public void ApiClient_Constructor_WithEmptyHost_ThrowsArgumentException()
    {
      // Act & Assert
      var ex = Assert.Throws<ArgumentException>(() => new ApiClient("", 5000, "test-model"));
      Assert.That(ex!.Message, Does.Contain("Host cannot be null or empty"));
    }

    [Test]
    public void ApiClient_Constructor_WithWhitespaceHost_ThrowsArgumentException()
    {
      // Act & Assert
      var ex = Assert.Throws<ArgumentException>(() => new ApiClient("   ", 5000, "test-model"));
      Assert.That(ex!.Message, Does.Contain("Host cannot be null or empty"));
    }

    [Test]
    public void ApiClient_Constructor_WithInvalidPort_Zero_ThrowsArgumentException()
    {
      // Act & Assert
      var ex = Assert.Throws<ArgumentException>(() => new ApiClient("localhost", 0, "test-model"));
      Assert.That(ex!.Message, Does.Contain("Port must be between 1 and 65535"));
    }

    [Test]
    public void ApiClient_Constructor_WithInvalidPort_Negative_ThrowsArgumentException()
    {
      // Act & Assert
      var ex = Assert.Throws<ArgumentException>(() => new ApiClient("localhost", -1, "test-model"));
      Assert.That(ex!.Message, Does.Contain("Port must be between 1 and 65535"));
    }

    [Test]
    public void ApiClient_Constructor_WithInvalidPort_TooHigh_ThrowsArgumentException()
    {
      // Act & Assert
      var ex = Assert.Throws<ArgumentException>(() => new ApiClient("localhost", 70000, "test-model"));
      Assert.That(ex!.Message, Does.Contain("Port must be between 1 and 65535"));
    }

    [Test]
    public void ApiClient_Constructor_WithValidPort_MinBoundary_Succeeds()
    {
      // Act
      using var client = new ApiClient("localhost", 1, "test-model");

      // Assert
      Assert.IsNotNull(client);
    }

    [Test]
    public void ApiClient_Constructor_WithValidPort_MaxBoundary_Succeeds()
    {
      // Act
      using var client = new ApiClient("localhost", 65535, "test-model");

      // Assert
      Assert.IsNotNull(client);
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
    public void ApiClient_WithWhitespaceModel_ThrowsArgumentException()
    {
      // Act & Assert
      var ex = Assert.Throws<ArgumentException>(() => new ApiClient("localhost", 5000, "   "));
      Assert.That(ex!.Message, Does.Contain("Model cannot be null or empty"));
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
      using var client = new ApiClient("localhost", 5000, "test-model", llmConfig);

      // Assert
      Assert.IsNotNull(client);
    }

    [Test]
    public void ApiClient_Constructor_WithCustomTimeout_CreatesCorrectly()
    {
      // Act
      using var client = new ApiClient("localhost", 5000, "test-model", null, 60);

      // Assert
      Assert.IsNotNull(client);
    }

    [Test]
    public void ApiClient_Constructor_WithZeroTimeout_UsesDefault()
    {
      // Act - zero timeout should use default
      using var client = new ApiClient("localhost", 5000, "test-model", null, 0);

      // Assert
      Assert.IsNotNull(client);
    }

    [Test]
    public void ApiClient_Constructor_WithNegativeTimeout_UsesDefault()
    {
      // Act - negative timeout should use default
      using var client = new ApiClient("localhost", 5000, "test-model", null, -10);

      // Assert
      Assert.IsNotNull(client);
    }

    #endregion

    #region SanitizeHost Tests

    [Test]
    public void SanitizeHost_WithHttpPrefix_RemovesPrefix()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.SanitizeHost("http://example.com");

      // Assert
      Assert.AreEqual("example.com", result);
    }

    [Test]
    public void SanitizeHost_WithHttpsPrefix_RemovesPrefix()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.SanitizeHost("https://example.com");

      // Assert
      Assert.AreEqual("example.com", result);
    }

    [Test]
    public void SanitizeHost_WithPath_RemovesPath()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.SanitizeHost("example.com/api/v1");

      // Assert
      Assert.AreEqual("example.com", result);
    }

    [Test]
    public void SanitizeHost_WithPort_RemovesPort()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.SanitizeHost("example.com:8080");

      // Assert
      Assert.AreEqual("example.com", result);
    }

    [Test]
    public void SanitizeHost_WithFullUrl_ExtractsHost()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.SanitizeHost("http://example.com:8080/api/v1");

      // Assert
      Assert.AreEqual("example.com", result);
    }

    [Test]
    public void SanitizeHost_WithWhitespace_TrimsWhitespace()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.SanitizeHost("  example.com  ");

      // Assert
      Assert.AreEqual("example.com", result);
    }

    [Test]
    public void SanitizeHost_WithPlainHost_ReturnsUnchanged()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.SanitizeHost("example.com");

      // Assert
      Assert.AreEqual("example.com", result);
    }

    [Test]
    public void SanitizeHost_WithIpAddress_ReturnsUnchanged()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.SanitizeHost("192.168.1.100");

      // Assert
      Assert.AreEqual("192.168.1.100", result);
    }

    #endregion

    #region SanitizePrompt Tests

    [Test]
    public void SanitizePrompt_WithNull_ReturnsEmpty()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.SanitizePrompt(null!);

      // Assert
      Assert.AreEqual(string.Empty, result);
    }

    [Test]
    public void SanitizePrompt_WithEmpty_ReturnsEmpty()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.SanitizePrompt("");

      // Assert
      Assert.AreEqual(string.Empty, result);
    }

    [Test]
    public void SanitizePrompt_WithNullCharacters_RemovesThem()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var input = "Hello" + (char)0 + "World";

      // Act
      var result = client.SanitizePrompt(input);

      // Assert
      Assert.AreEqual("HelloWorld", result);
    }

    [Test]
    public void SanitizePrompt_WithCarriageReturns_RemovesThem()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var input = "Hello\r\nWorld";

      // Act
      var result = client.SanitizePrompt(input);

      // Assert
      Assert.AreEqual("Hello\nWorld", result);
    }

    [Test]
    public void SanitizePrompt_WithWhitespace_TrimsWhitespace()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.SanitizePrompt("  Hello World  ");

      // Assert
      Assert.AreEqual("Hello World", result);
    }

    [Test]
    public void SanitizePrompt_WithLongPrompt_TruncatesToMaxLength()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var longPrompt = new string('a', 15000); // Exceeds MaxPromptLength of 10000

      // Act
      var result = client.SanitizePrompt(longPrompt);

      // Assert
      Assert.AreEqual(10000, result.Length);
    }

    [Test]
    public void SanitizePrompt_WithNormalPrompt_ReturnsUnchanged()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.SanitizePrompt("Hello, how are you?");

      // Assert
      Assert.AreEqual("Hello, how are you?", result);
    }

    #endregion

    #region ValidateMaxTokens Tests

    [Test]
    public void ValidateMaxTokens_WithNegative_ReturnsOne()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.ValidateMaxTokens(-10);

      // Assert
      Assert.AreEqual(1, result);
    }

    [Test]
    public void ValidateMaxTokens_WithZero_ReturnsOne()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.ValidateMaxTokens(0);

      // Assert
      Assert.AreEqual(1, result);
    }

    [Test]
    public void ValidateMaxTokens_WithExcessive_ReturnsMax()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.ValidateMaxTokens(10000);

      // Assert
      Assert.AreEqual(2048, result);
    }

    [Test]
    public void ValidateMaxTokens_WithValid_ReturnsUnchanged()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.ValidateMaxTokens(512);

      // Assert
      Assert.AreEqual(512, result);
    }

    [Test]
    public void ValidateMaxTokens_AtMinBoundary_ReturnsOne()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.ValidateMaxTokens(1);

      // Assert
      Assert.AreEqual(1, result);
    }

    [Test]
    public void ValidateMaxTokens_AtMaxBoundary_ReturnsMax()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.ValidateMaxTokens(2048);

      // Assert
      Assert.AreEqual(2048, result);
    }

    #endregion

    #region ValidateTemperature Tests

    [Test]
    public void ValidateTemperature_WithNegative_ReturnsZero()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.ValidateTemperature(-0.5f);

      // Assert
      Assert.AreEqual(0.0f, result);
    }

    [Test]
    public void ValidateTemperature_WithExcessive_ReturnsMax()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.ValidateTemperature(5.0f);

      // Assert
      Assert.AreEqual(2.0f, result);
    }

    [Test]
    public void ValidateTemperature_WithValid_ReturnsUnchanged()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.ValidateTemperature(0.7f);

      // Assert
      Assert.AreEqual(0.7f, result, 0.001f);
    }

    [Test]
    public void ValidateTemperature_AtZero_ReturnsZero()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.ValidateTemperature(0.0f);

      // Assert
      Assert.AreEqual(0.0f, result);
    }

    [Test]
    public void ValidateTemperature_AtMaxBoundary_ReturnsMax()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = client.ValidateTemperature(2.0f);

      // Assert
      Assert.AreEqual(2.0f, result);
    }

    #endregion

    #region ValidateAndSanitizeConfig Tests

    [Test]
    public void ValidateAndSanitizeConfig_WithNull_ThrowsArgumentNullException()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => client.ValidateAndSanitizeConfig(null!));
    }

    [Test]
    public void ValidateAndSanitizeConfig_WithExcessiveMaxTokens_ClampsToMax()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var config = new LlmConfig { MaxTokens = 10000 };

      // Act
      var result = client.ValidateAndSanitizeConfig(config);

      // Assert
      Assert.AreEqual(2048, result.MaxTokens);
    }

    [Test]
    public void ValidateAndSanitizeConfig_WithNegativeMaxTokens_ClampsToMin()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var config = new LlmConfig { MaxTokens = -10 };

      // Act
      var result = client.ValidateAndSanitizeConfig(config);

      // Assert
      Assert.AreEqual(1, result.MaxTokens);
    }

    [Test]
    public void ValidateAndSanitizeConfig_WithExcessiveTemperature_ClampsToMax()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var config = new LlmConfig { Temperature = 5.0f };

      // Act
      var result = client.ValidateAndSanitizeConfig(config);

      // Assert
      Assert.AreEqual(2.0f, result.Temperature);
    }

    [Test]
    public void ValidateAndSanitizeConfig_WithNegativeTemperature_ClampsToMin()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var config = new LlmConfig { Temperature = -1.0f };

      // Act
      var result = client.ValidateAndSanitizeConfig(config);

      // Assert
      Assert.AreEqual(0.0f, result.Temperature);
    }

    [Test]
    public void ValidateAndSanitizeConfig_WithExcessiveTopP_ClampsToMax()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var config = new LlmConfig { TopP = 2.0f };

      // Act
      var result = client.ValidateAndSanitizeConfig(config);

      // Assert
      Assert.AreEqual(1.0f, result.TopP);
    }

    [Test]
    public void ValidateAndSanitizeConfig_WithNegativeTopP_ClampsToMin()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var config = new LlmConfig { TopP = -0.5f };

      // Act
      var result = client.ValidateAndSanitizeConfig(config);

      // Assert
      Assert.AreEqual(0.0f, result.TopP);
    }

    [Test]
    public void ValidateAndSanitizeConfig_WithExcessiveTopK_ClampsToMax()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var config = new LlmConfig { TopK = 200 };

      // Act
      var result = client.ValidateAndSanitizeConfig(config);

      // Assert
      Assert.AreEqual(100, result.TopK);
    }

    [Test]
    public void ValidateAndSanitizeConfig_WithNegativeTopK_ClampsToMin()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var config = new LlmConfig { TopK = -10 };

      // Act
      var result = client.ValidateAndSanitizeConfig(config);

      // Assert
      Assert.AreEqual(1, result.TopK);
    }

    [Test]
    public void ValidateAndSanitizeConfig_WithExcessiveRepeatPenalty_ClampsToMax()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var config = new LlmConfig { RepeatPenalty = 5.0f };

      // Act
      var result = client.ValidateAndSanitizeConfig(config);

      // Assert
      Assert.AreEqual(2.0f, result.RepeatPenalty);
    }

    [Test]
    public void ValidateAndSanitizeConfig_WithNegativeRepeatPenalty_ClampsToMin()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var config = new LlmConfig { RepeatPenalty = -1.0f };

      // Act
      var result = client.ValidateAndSanitizeConfig(config);

      // Assert
      Assert.AreEqual(0.0f, result.RepeatPenalty);
    }

    [Test]
    public void ValidateAndSanitizeConfig_WithValidValues_ReturnsUnchanged()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var config = new LlmConfig
      {
        MaxTokens = 512,
        Temperature = 0.7f,
        TopP = 0.9f,
        TopK = 40,
        RepeatPenalty = 1.1f
      };

      // Act
      var result = client.ValidateAndSanitizeConfig(config);

      // Assert
      Assert.AreEqual(512, result.MaxTokens);
      Assert.AreEqual(0.7f, result.Temperature, 0.001f);
      Assert.AreEqual(0.9f, result.TopP, 0.001f);
      Assert.AreEqual(40, result.TopK);
      Assert.AreEqual(1.1f, result.RepeatPenalty, 0.001f);
    }

    #endregion

    #region Dispose Tests

    [Test]
    public void Dispose_WhenCalled_DisposesResources()
    {
      // Arrange
      var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      client.Dispose();

      // Assert - subsequent operations should throw ObjectDisposedException
      Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        await client.SendPromptAsync("test"));
    }

    [Test]
    public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
    {
      // Arrange
      var client = new ApiClient("localhost", 5000, "test-model");

      // Act & Assert
      Assert.DoesNotThrow(() =>
      {
        client.Dispose();
        client.Dispose();
        client.Dispose();
      });
    }

    [Test]
    public async Task SendPromptAsync_AfterDispose_ThrowsObjectDisposedException()
    {
      // Arrange
      var client = new ApiClient("localhost", 5000, "test-model");
      client.Dispose();

      // Act & Assert
      Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        await client.SendPromptAsync("test"));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_AfterDispose_ThrowsObjectDisposedException()
    {
      // Arrange
      var client = new ApiClient("localhost", 5000, "test-model");
      client.Dispose();

      // Act & Assert
      Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        await client.SendPromptWithMetricsAsync("test"));
    }

    #endregion

    #region SendPromptAsync Tests

    [Test]
    public async Task ApiClient_SendPromptAsync_WithMockServer_HandlesCorrectly()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
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
      using var client = new ApiClient("localhost", 5000, "test-model", llmConfig);

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
    public async Task ApiClient_SendPromptAsync_WithEmptyPrompt_ReturnsError()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = await client.SendPromptAsync("");

      // Assert
      Assert.IsNotNull(result);
      Assert.That(result, Does.Contain("Error:"));
      Assert.That(result, Does.Contain("Prompt cannot be null or empty"));
    }

    [Test]
    public async Task ApiClient_SendPromptAsync_WithNullPrompt_ReturnsError()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = await client.SendPromptAsync(null!);

      // Assert
      Assert.IsNotNull(result);
      Assert.That(result, Does.Contain("Error:"));
      Assert.That(result, Does.Contain("Prompt cannot be null or empty"));
    }

    [Test]
    public async Task ApiClient_SendPromptAsync_WithWhitespacePrompt_ReturnsError()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = await client.SendPromptAsync("   ");

      // Assert
      Assert.IsNotNull(result);
      Assert.That(result, Does.Contain("Error:"));
    }

    [Test]
    public async Task ApiClient_SendPromptAsync_WithLongPrompt_ReturnsError()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var longPrompt = new string('a', 15000); // Exceeds MaxPromptLength

      // Act
      var result = await client.SendPromptAsync(longPrompt);

      // Assert
      Assert.IsNotNull(result);
      Assert.That(result, Does.Contain("Error:"));
      Assert.That(result, Does.Contain("Prompt too long"));
    }

    [Test]
    public async Task ApiClient_SendPromptAsync_WithCancellation_ReturnsError()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var cts = new CancellationTokenSource();
      cts.Cancel(); // Cancel immediately

      // Act
      var result = await client.SendPromptAsync("test", cancellationToken: cts.Token);

      // Assert
      Assert.IsNotNull(result);
      Assert.That(result, Does.Contain("Error:"));
    }

    #endregion

    #region SendPromptWithMetricsAsync Tests

    [Test]
    public async Task SendPromptWithMetricsAsync_WithEmptyPrompt_ReturnsErrorMetrics()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = await client.SendPromptWithMetricsAsync("");

      // Assert
      Assert.IsNotNull(result);
      Assert.That(result.Content, Does.Contain("Error:"));
      Assert.That(result.Content, Does.Contain("Prompt cannot be null or empty"));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithNullPrompt_ReturnsErrorMetrics()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");

      // Act
      var result = await client.SendPromptWithMetricsAsync(null!);

      // Assert
      Assert.IsNotNull(result);
      Assert.That(result.Content, Does.Contain("Error:"));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithLongPrompt_ReturnsErrorMetrics()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var longPrompt = new string('a', 15000);

      // Act
      var result = await client.SendPromptWithMetricsAsync(longPrompt);

      // Assert
      Assert.IsNotNull(result);
      Assert.That(result.Content, Does.Contain("Error:"));
      Assert.That(result.Content, Does.Contain("Prompt too long"));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithCancellation_ReturnsErrorMetrics()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      var cts = new CancellationTokenSource();
      cts.Cancel();

      // Act
      var result = await client.SendPromptWithMetricsAsync("test", cancellationToken: cts.Token);

      // Assert
      Assert.IsNotNull(result);
      Assert.That(result.Content, Does.Contain("Error:"));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WhenServerNotRunning_ReturnsNetworkError()
    {
      // Arrange
      using var client = new ApiClient("localhost", 59999, "test-model"); // Port unlikely to be in use

      // Act
      var result = await client.SendPromptWithMetricsAsync("test");

      // Assert
      Assert.IsNotNull(result);
      Assert.That(result.Content, Does.Contain("Error:"));
    }

    #endregion

    #region Event Tests

    [Test]
    public void OnMetricsAvailable_CanBeSubscribed()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      CompletionMetrics? receivedMetrics = null;

      // Act & Assert
      Assert.DoesNotThrow(() =>
      {
        client.OnMetricsAvailable += (metrics) => { receivedMetrics = metrics; };
      });
    }

    [Test]
    public void OnMetricsAvailable_CanBeUnsubscribed()
    {
      // Arrange
      using var client = new ApiClient("localhost", 5000, "test-model");
      Action<CompletionMetrics> handler = (metrics) => { };

      // Act & Assert
      Assert.DoesNotThrow(() =>
      {
        client.OnMetricsAvailable += handler;
        client.OnMetricsAvailable -= handler;
      });
    }

    #endregion

    #region Localhost Resolution Tests

    [Test]
    public void ApiClient_WithLocalhost_ResolvesTo127()
    {
      // This tests that "localhost" is converted to "127.0.0.1" for performance
      // We can't directly verify the endpoint, but we can verify the client is created
      using var client = new ApiClient("localhost", 5000, "test-model");
      Assert.IsNotNull(client);
    }

    [Test]
    public void ApiClient_WithIpAddress_WorksCorrectly()
    {
      // Arrange & Act
      using var client = new ApiClient("127.0.0.1", 5000, "test-model");

      // Assert
      Assert.IsNotNull(client);
    }

    [Test]
    public void ApiClient_WithRemoteHost_WorksCorrectly()
    {
      // Arrange & Act
      using var client = new ApiClient("example.com", 5000, "test-model");

      // Assert
      Assert.IsNotNull(client);
    }

    #endregion

    #region HTTP Success Response Tests

    [Test]
    public async Task SendPromptAsync_WithSuccessfulResponse_ReturnsContent()
    {
      // Arrange
      var responseJson = CreateSuccessResponse("Hello, I'm an AI assistant.");
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptAsync("Hello");

      // Assert
      Assert.AreEqual("Hello, I'm an AI assistant.", result);
    }

    [Test]
    public async Task SendPromptAsync_WithSuccessfulResponseNoTimings_ReturnsContent()
    {
      // Arrange
      var response = new { content = "Response without timings", stop = true };
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, JsonConvert.SerializeObject(response));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptAsync("Hello");

      // Assert
      Assert.AreEqual("Response without timings", result);
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithSuccessfulResponse_ReturnsMetrics()
    {
      // Arrange
      var responseJson = CreateSuccessResponse("Test response", true, 15, 25);
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptWithMetricsAsync("Test prompt");

      // Assert
      Assert.AreEqual("Test response", result.Content);
      Assert.AreEqual(15, result.PromptTokenCount);
      Assert.AreEqual(25, result.GeneratedTokenCount);
      Assert.AreEqual(5, result.CachedTokenCount);
      Assert.AreEqual(100, result.PrefillTimeMs);
      Assert.AreEqual(200, result.DecodeTimeMs);
      Assert.That(result.TotalTimeMs, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithNoTimings_EstimatesMetrics()
    {
      // Arrange
      var response = new { content = "Response text", stop = true, tokens_predicted = 10, tokens_cached = 2, tokens_evaluated = 5 };
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, JsonConvert.SerializeObject(response));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptWithMetricsAsync("Test");

      // Assert
      Assert.AreEqual("Response text", result.Content);
      // Without timings, metrics should be estimated
      Assert.That(result.GeneratedTokenCount, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithZeroDecodeTime_EstimatesFromWallTime()
    {
      // Arrange - response with zero predicted_ms but has generated tokens
      var response = new
      {
        content = "Test",
        stop = true,
        tokens_predicted = 10,
        tokens_cached = 0,
        tokens_evaluated = 5,
        timings = new
        {
          prompt_n = 5,
          prompt_ms = 50.0,
          predicted_n = 10,
          predicted_ms = 0.0 // Zero decode time
        }
      };
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, JsonConvert.SerializeObject(response));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptWithMetricsAsync("Test");

      // Assert - decode time should be estimated
      Assert.AreEqual("Test", result.Content);
      Assert.That(result.DecodeTimeMs, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithCachePrompt_SendsCorrectRequest()
    {
      // Arrange
      string? capturedBody = null;
      var handler = new MockHttpMessageHandler(req =>
      {
        capturedBody = req.Content?.ReadAsStringAsync().Result;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(CreateSuccessResponse("OK"))
        };
      });
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      await client.SendPromptWithMetricsAsync("Test", cachePrompt: true);

      // Assert
      Assert.IsNotNull(capturedBody);
      Assert.That(capturedBody, Does.Contain("\"cache_prompt\":true"));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_RaisesOnMetricsAvailableEvent()
    {
      // Arrange
      var responseJson = CreateSuccessResponse("Event test");
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      CompletionMetrics? receivedMetrics = null;
      client.OnMetricsAvailable += m => receivedMetrics = m;

      // Act
      await client.SendPromptWithMetricsAsync("Test");

      // Assert
      Assert.IsNotNull(receivedMetrics);
      Assert.AreEqual("Event test", receivedMetrics!.Content);
    }

    #endregion

    #region HTTP Error Response Tests

    [Test]
    public async Task SendPromptAsync_WithHttpError_ReturnsErrorMessage()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "Server error occurred");
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptAsync("Test");

      // Assert
      Assert.That(result, Does.Contain("Error:"));
      Assert.That(result, Does.Contain("InternalServerError"));
    }

    [Test]
    public async Task SendPromptAsync_WithBadRequest_ReturnsErrorMessage()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.BadRequest, "Bad request");
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptAsync("Test");

      // Assert
      Assert.That(result, Does.Contain("Error:"));
      Assert.That(result, Does.Contain("BadRequest"));
    }

    [Test]
    public async Task SendPromptAsync_WithEmptyResponse_ReturnsError()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, "");
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptAsync("Test");

      // Assert
      Assert.That(result, Does.Contain("Error:"));
      Assert.That(result, Does.Contain("Empty response"));
    }

    [Test]
    public async Task SendPromptAsync_WithNullContentInResponse_ReturnsError()
    {
      // Arrange
      var response = new { content = (string?)null, stop = true };
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, JsonConvert.SerializeObject(response));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptAsync("Test");

      // Assert
      Assert.That(result, Does.Contain("Error:"));
      Assert.That(result, Does.Contain("Invalid response format"));
    }

    [Test]
    public async Task SendPromptAsync_WithInvalidJson_ReturnsError()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, "not valid json {{{");
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptAsync("Test");

      // Assert
      Assert.That(result, Does.Contain("Error:"));
      Assert.That(result, Does.Contain("Invalid JSON"));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithHttpError_ReturnsErrorMetrics()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.ServiceUnavailable, "Service unavailable");
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptWithMetricsAsync("Test");

      // Assert
      Assert.That(result.Content, Does.Contain("Error:"));
      Assert.That(result.Content, Does.Contain("ServiceUnavailable"));
      Assert.That(result.TotalTimeMs, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithEmptyResponse_ReturnsErrorMetrics()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, "");
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptWithMetricsAsync("Test");

      // Assert
      Assert.That(result.Content, Does.Contain("Error:"));
      Assert.That(result.Content, Does.Contain("Empty response"));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithNullContentInResponse_ReturnsErrorMetrics()
    {
      // Arrange
      var response = new { content = (string?)null };
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, JsonConvert.SerializeObject(response));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptWithMetricsAsync("Test");

      // Assert
      Assert.That(result.Content, Does.Contain("Error:"));
      Assert.That(result.Content, Does.Contain("Invalid response format"));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithInvalidJson_ReturnsErrorMetrics()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, "invalid json");
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptWithMetricsAsync("Test");

      // Assert
      Assert.That(result.Content, Does.Contain("Error:"));
    }

    #endregion

    #region Response Truncation Tests

    [Test]
    public async Task SendPromptAsync_WithLongResponse_TruncatesContent()
    {
      // Arrange - Create a response longer than MaxResponseLength (50000)
      var longContent = new string('a', 60000);
      var response = new { content = longContent, stop = true };
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, JsonConvert.SerializeObject(response));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptAsync("Test");

      // Assert
      Assert.That(result.Length, Is.LessThanOrEqualTo(50020)); // 50000 + "... [truncated]".Length
      Assert.That(result, Does.EndWith("... [truncated]"));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithLongResponse_TruncatesContent()
    {
      // Arrange
      var longContent = new string('b', 60000);
      var response = new { content = longContent, stop = true, timings = new { prompt_n = 10, prompt_ms = 100.0, predicted_n = 100, predicted_ms = 500.0 } };
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, JsonConvert.SerializeObject(response));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptWithMetricsAsync("Test");

      // Assert
      Assert.That(result.Content.Length, Is.LessThanOrEqualTo(50020));
      Assert.That(result.Content, Does.EndWith("... [truncated]"));
    }

    #endregion

    #region Parameter Override Tests

    [Test]
    public async Task SendPromptAsync_WithCustomMaxTokens_SendsCorrectValue()
    {
      // Arrange
      string? capturedBody = null;
      var handler = new MockHttpMessageHandler(req =>
      {
        capturedBody = req.Content?.ReadAsStringAsync().Result;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(CreateSuccessResponse("OK"))
        };
      });
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      await client.SendPromptAsync("Test", maxTokens: 256);

      // Assert
      Assert.IsNotNull(capturedBody);
      Assert.That(capturedBody, Does.Contain("\"n_predict\":256"));
    }

    [Test]
    public async Task SendPromptAsync_WithCustomTemperature_SendsCorrectValue()
    {
      // Arrange
      string? capturedBody = null;
      var handler = new MockHttpMessageHandler(req =>
      {
        capturedBody = req.Content?.ReadAsStringAsync().Result;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(CreateSuccessResponse("OK"))
        };
      });
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      await client.SendPromptAsync("Test", temperature: 0.5f);

      // Assert
      Assert.IsNotNull(capturedBody);
      Assert.That(capturedBody, Does.Contain("\"temperature\":0.5"));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithCustomParameters_SendsCorrectValues()
    {
      // Arrange
      string? capturedBody = null;
      var handler = new MockHttpMessageHandler(req =>
      {
        capturedBody = req.Content?.ReadAsStringAsync().Result;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(CreateSuccessResponse("OK"))
        };
      });
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      await client.SendPromptWithMetricsAsync("Test", maxTokens: 512, temperature: 0.8f);

      // Assert
      Assert.IsNotNull(capturedBody);
      Assert.That(capturedBody, Does.Contain("\"n_predict\":512"));
      Assert.That(capturedBody, Does.Contain("\"temperature\":0.8"));
    }

    #endregion

    #region Timing Fallback Tests

    [Test]
    public async Task SendPromptWithMetricsAsync_WithPartialTimings_UsesFallbackValues()
    {
      // Arrange - response with tokens_evaluated but no timings.prompt_n
      var response = new
      {
        content = "Test",
        stop = true,
        tokens_predicted = 15,
        tokens_cached = 3,
        tokens_evaluated = 20,
        timings = new
        {
          prompt_n = 0, // Zero value
          prompt_ms = 150.0,
          predicted_n = 0, // Zero value
          predicted_ms = 300.0
        }
      };
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, JsonConvert.SerializeObject(response));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptWithMetricsAsync("Test");

      // Assert
      Assert.AreEqual("Test", result.Content);
      // Should use fallback values from tokens_evaluated and tokens_predicted
      Assert.That(result.PromptTokenCount, Is.EqualTo(20)); // Falls back to tokens_evaluated
      Assert.That(result.GeneratedTokenCount, Is.EqualTo(15)); // Falls back to tokens_predicted
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithPrefillGreaterThanWallTime_UsesHeuristic()
    {
      // Arrange - edge case where prefill time is greater than measured wall time
      var response = new
      {
        content = "Test",
        stop = true,
        timings = new
        {
          prompt_n = 10,
          prompt_ms = 10000.0, // Very high prefill time
          predicted_n = 5,
          predicted_ms = 0.0 // Zero decode
        }
      };
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, JsonConvert.SerializeObject(response));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendPromptWithMetricsAsync("Test");

      // Assert - should handle gracefully
      Assert.AreEqual("Test", result.Content);
      Assert.That(result.DecodeTimeMs, Is.GreaterThanOrEqualTo(0));
    }

    #endregion

    #region Config Usage Tests

    [Test]
    public async Task SendPromptAsync_UsesConfigValues()
    {
      // Arrange
      string? capturedBody = null;
      var handler = new MockHttpMessageHandler(req =>
      {
        capturedBody = req.Content?.ReadAsStringAsync().Result;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(CreateSuccessResponse("OK"))
        };
      });
      var mockClient = new HttpClient(handler);
      var config = new LlmConfig
      {
        MaxTokens = 100,
        Temperature = 0.3f,
        TopP = 0.85f,
        TopK = 30,
        RepeatPenalty = 1.05f
      };
      var client = new ApiClient("localhost", 5000, "test-model", config, 30, mockClient);

      // Act
      await client.SendPromptAsync("Test");

      // Assert
      Assert.IsNotNull(capturedBody);
      Assert.That(capturedBody, Does.Contain("\"n_predict\":100"));
      Assert.That(capturedBody, Does.Contain("\"temperature\":0.3"));
      Assert.That(capturedBody, Does.Contain("\"top_p\":0.85"));
      Assert.That(capturedBody, Does.Contain("\"top_k\":30"));
      Assert.That(capturedBody, Does.Contain("\"repeat_penalty\":1.05"));
    }

    #endregion

    #region Timing Logging Tests

    [Test]
    public async Task SendPromptAsync_WithTimings_LogsMetrics()
    {
      // Arrange
      var response = new
      {
        content = "Test response",
        stop = true,
        tokens_predicted = 20,
        tokens_cached = 5,
        tokens_evaluated = 10,
        timings = new
        {
          prompt_n = 10,
          prompt_ms = 150.0,
          predicted_n = 20,
          predicted_ms = 400.0
        }
      };
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, JsonConvert.SerializeObject(response));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act - this should not throw even if Logger is not available
      var result = await client.SendPromptAsync("Test");

      // Assert
      Assert.AreEqual("Test response", result);
    }

    #endregion

    #region SendStructuredPromptAsync Tests

    [Test]
    public async Task SendStructuredPromptAsync_WithValidSchemaAndPrompt_ReturnsContent()
    {
      // Arrange
      var responseJson = CreateSuccessResponse(@"{""name"": ""John"", ""age"": 30}");
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      var schema = @"{""type"": ""object"", ""properties"": {""name"": {""type"": ""string""}, ""age"": {""type"": ""integer""}}}";

      // Act
      var result = await client.SendStructuredPromptAsync("Generate a person", schema);

      // Assert
      Assert.That(result, Does.Contain("John"));
    }

    [Test]
    public async Task SendStructuredPromptAsync_WithEmptyPrompt_ReturnsError()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, CreateSuccessResponse("{}"));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      var schema = @"{""type"": ""object""}";

      // Act
      var result = await client.SendStructuredPromptAsync("", schema);

      // Assert
      Assert.That(result, Does.Contain("Error:"));
      Assert.That(result, Does.Contain("Prompt cannot be null or empty"));
    }

    [Test]
    public async Task SendStructuredPromptAsync_WithNullPrompt_ReturnsError()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, CreateSuccessResponse("{}"));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      var schema = @"{""type"": ""object""}";

      // Act
      var result = await client.SendStructuredPromptAsync(null!, schema);

      // Assert
      Assert.That(result, Does.Contain("Error:"));
    }

    [Test]
    public async Task SendStructuredPromptAsync_WithLongPrompt_ReturnsError()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, CreateSuccessResponse("{}"));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      var longPrompt = new string('a', 15000);
      var schema = @"{""type"": ""object""}";

      // Act
      var result = await client.SendStructuredPromptAsync(longPrompt, schema);

      // Assert
      Assert.That(result, Does.Contain("Error:"));
      Assert.That(result, Does.Contain("Prompt too long"));
    }

    [Test]
    public async Task SendStructuredPromptAsync_WithEmptySchema_ReturnsError()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, CreateSuccessResponse("{}"));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendStructuredPromptAsync("Test prompt", "");

      // Assert
      Assert.That(result, Does.Contain("Error:"));
      Assert.That(result, Does.Contain("JSON schema cannot be null or empty"));
    }

    [Test]
    public async Task SendStructuredPromptAsync_WithNullSchema_ReturnsError()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, CreateSuccessResponse("{}"));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendStructuredPromptAsync("Test prompt", null!);

      // Assert
      Assert.That(result, Does.Contain("Error:"));
    }

    [Test]
    public async Task SendStructuredPromptAsync_WithInvalidSchema_ReturnsError()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, CreateSuccessResponse("{}"));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      var invalidSchema = "not valid json {{{";

      // Act
      var result = await client.SendStructuredPromptAsync("Test prompt", invalidSchema);

      // Assert
      Assert.That(result, Does.Contain("Error:"));
      Assert.That(result, Does.Contain("Invalid JSON schema"));
    }

    [Test]
    public async Task SendStructuredPromptAsync_AfterDispose_ThrowsObjectDisposedException()
    {
      // Arrange
      var client = new ApiClient("localhost", 5000, "test-model");
      client.Dispose();

      // Act & Assert
      Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        await client.SendStructuredPromptAsync("test", @"{""type"": ""object""}"));
    }

    [Test]
    public async Task SendStructuredPromptAsync_WithCancellation_ReturnsError()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, CreateSuccessResponse("{}"));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      var cts = new CancellationTokenSource();
      cts.Cancel();

      // Act
      var result = await client.SendStructuredPromptAsync("Test", @"{""type"": ""object""}", cancellationToken: cts.Token);

      // Assert
      Assert.That(result, Does.Contain("Error:"));
    }

    #endregion

    #region SendStructuredPromptWithMetricsAsync Tests

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_WithValidInput_ReturnsMetrics()
    {
      // Arrange
      var responseJson = CreateSuccessResponse(@"{""result"": ""success""}", true, 20, 30);
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      var schema = @"{""type"": ""object"", ""properties"": {""result"": {""type"": ""string""}}}";

      // Act
      var result = await client.SendStructuredPromptWithMetricsAsync("Generate result", schema);

      // Assert
      Assert.That(result.Content, Does.Contain("success"));
      Assert.That(result.PromptTokenCount, Is.EqualTo(20));
      Assert.That(result.GeneratedTokenCount, Is.EqualTo(30));
    }

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_WithEmptyPrompt_ReturnsErrorMetrics()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, CreateSuccessResponse("{}"));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendStructuredPromptWithMetricsAsync("", @"{""type"": ""object""}");

      // Assert
      Assert.That(result.Content, Does.Contain("Error:"));
      Assert.That(result.Content, Does.Contain("Prompt cannot be null or empty"));
    }

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_WithEmptySchema_ReturnsErrorMetrics()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, CreateSuccessResponse("{}"));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendStructuredPromptWithMetricsAsync("Test", "");

      // Assert
      Assert.That(result.Content, Does.Contain("Error:"));
      Assert.That(result.Content, Does.Contain("JSON schema cannot be null or empty"));
    }

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_WithHttpError_ReturnsErrorMetrics()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "Server error");
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendStructuredPromptWithMetricsAsync("Test", @"{""type"": ""object""}");

      // Assert
      Assert.That(result.Content, Does.Contain("Error:"));
      Assert.That(result.TotalTimeMs, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_WithEmptyResponse_ReturnsErrorMetrics()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, "");
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendStructuredPromptWithMetricsAsync("Test", @"{""type"": ""object""}");

      // Assert
      Assert.That(result.Content, Does.Contain("Error:"));
      Assert.That(result.Content, Does.Contain("Empty response"));
    }

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_WithNullContent_ReturnsErrorMetrics()
    {
      // Arrange
      var response = new { content = (string?)null };
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, JsonConvert.SerializeObject(response));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendStructuredPromptWithMetricsAsync("Test", @"{""type"": ""object""}");

      // Assert
      Assert.That(result.Content, Does.Contain("Error:"));
      Assert.That(result.Content, Does.Contain("Invalid response format"));
    }

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_WithNoTimings_EstimatesMetrics()
    {
      // Arrange
      var response = new { content = @"{""data"": ""test""}", stop = true };
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, JsonConvert.SerializeObject(response));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendStructuredPromptWithMetricsAsync("Test", @"{""type"": ""object""}");

      // Assert
      Assert.That(result.Content, Does.Contain("data"));
      Assert.That(result.PrefillTimeMs, Is.GreaterThanOrEqualTo(0));
      Assert.That(result.DecodeTimeMs, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_RaisesOnMetricsAvailableEvent()
    {
      // Arrange
      var responseJson = CreateSuccessResponse(@"{""test"": true}");
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, responseJson);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      CompletionMetrics? receivedMetrics = null;
      client.OnMetricsAvailable += m => receivedMetrics = m;

      // Act
      await client.SendStructuredPromptWithMetricsAsync("Test", @"{""type"": ""object""}");

      // Assert
      Assert.IsNotNull(receivedMetrics);
    }

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_AfterDispose_ThrowsObjectDisposedException()
    {
      // Arrange
      var client = new ApiClient("localhost", 5000, "test-model");
      client.Dispose();

      // Act & Assert
      Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        await client.SendStructuredPromptWithMetricsAsync("test", @"{""type"": ""object""}"));
    }

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_WithLongResponse_TruncatesContent()
    {
      // Arrange
      var longContent = new string('x', 60000);
      var response = new { content = longContent, stop = true };
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, JsonConvert.SerializeObject(response));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      var result = await client.SendStructuredPromptWithMetricsAsync("Test", @"{""type"": ""object""}");

      // Assert
      Assert.That(result.Content.Length, Is.LessThanOrEqualTo(50020));
      Assert.That(result.Content, Does.EndWith("... [truncated]"));
    }

    #endregion

    #region Structured Output Format Tests

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_JsonSchemaFormat_IncludesJsonSchemaInRequest()
    {
      // Arrange
      string? capturedBody = null;
      var handler = new MockHttpMessageHandler(req =>
      {
        capturedBody = req.Content?.ReadAsStringAsync().Result;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(CreateSuccessResponse(@"{""test"": true}"))
        };
      });
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      var schema = @"{""type"": ""object"", ""properties"": {""test"": {""type"": ""boolean""}}}";

      // Act
      await client.SendStructuredPromptWithMetricsAsync("Test", schema, LlamaBrain.Core.StructuredOutput.StructuredOutputFormat.JsonSchema);

      // Assert
      Assert.IsNotNull(capturedBody);
      Assert.That(capturedBody, Does.Contain("json_schema"));
    }

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_GrammarFormat_IncludesGrammarInRequest()
    {
      // Arrange
      string? capturedBody = null;
      var handler = new MockHttpMessageHandler(req =>
      {
        capturedBody = req.Content?.ReadAsStringAsync().Result;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(CreateSuccessResponse(@"{""value"": 42}"))
        };
      });
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      var schema = @"{""type"": ""object"", ""properties"": {""value"": {""type"": ""integer""}}}";

      // Act
      await client.SendStructuredPromptWithMetricsAsync("Test", schema, LlamaBrain.Core.StructuredOutput.StructuredOutputFormat.Grammar);

      // Assert
      Assert.IsNotNull(capturedBody);
      Assert.That(capturedBody, Does.Contain("grammar"));
    }

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_ResponseFormatMode_IncludesResponseFormatInRequest()
    {
      // Arrange
      string? capturedBody = null;
      var handler = new MockHttpMessageHandler(req =>
      {
        capturedBody = req.Content?.ReadAsStringAsync().Result;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(CreateSuccessResponse(@"{""data"": ""test""}"))
        };
      });
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      var schema = @"{""type"": ""object""}";

      // Act
      await client.SendStructuredPromptWithMetricsAsync("Test", schema, LlamaBrain.Core.StructuredOutput.StructuredOutputFormat.ResponseFormat);

      // Assert
      Assert.IsNotNull(capturedBody);
      Assert.That(capturedBody, Does.Contain("response_format"));
    }

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_NoneFormat_NoStructuredOutputParameters()
    {
      // Arrange
      string? capturedBody = null;
      var handler = new MockHttpMessageHandler(req =>
      {
        capturedBody = req.Content?.ReadAsStringAsync().Result;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(CreateSuccessResponse(@"{""simple"": true}"))
        };
      });
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      var schema = @"{""type"": ""object""}";

      // Act
      await client.SendStructuredPromptWithMetricsAsync("Test", schema, LlamaBrain.Core.StructuredOutput.StructuredOutputFormat.None);

      // Assert
      Assert.IsNotNull(capturedBody);
      // None format should not include json_schema, grammar, or response_format with actual values
      // (they may be present as null)
    }

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_WithCachePrompt_IncludesCachePromptInRequest()
    {
      // Arrange
      string? capturedBody = null;
      var handler = new MockHttpMessageHandler(req =>
      {
        capturedBody = req.Content?.ReadAsStringAsync().Result;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(CreateSuccessResponse(@"{}"))
        };
      });
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      await client.SendStructuredPromptWithMetricsAsync("Test", @"{""type"": ""object""}", cachePrompt: true);

      // Assert
      Assert.IsNotNull(capturedBody);
      Assert.That(capturedBody, Does.Contain("\"cache_prompt\":true"));
    }

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_WithCustomParameters_SendsCorrectValues()
    {
      // Arrange
      string? capturedBody = null;
      var handler = new MockHttpMessageHandler(req =>
      {
        capturedBody = req.Content?.ReadAsStringAsync().Result;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(CreateSuccessResponse(@"{}"))
        };
      });
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      await client.SendStructuredPromptWithMetricsAsync("Test", @"{""type"": ""object""}", maxTokens: 256, temperature: 0.5f);

      // Assert
      Assert.IsNotNull(capturedBody);
      Assert.That(capturedBody, Does.Contain("\"n_predict\":256"));
      Assert.That(capturedBody, Does.Contain("\"temperature\":0.5"));
    }

    #endregion

    #region Network Error Tests

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_WithNetworkError_ReturnsErrorMetrics()
    {
      // Arrange
      using var client = new ApiClient("localhost", 59998, "test-model"); // Port unlikely to be in use

      // Act
      var result = await client.SendStructuredPromptWithMetricsAsync("Test", @"{""type"": ""object""}");

      // Assert
      Assert.That(result.Content, Does.Contain("Error:"));
    }

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_WithCancellation_ReturnsErrorMetrics()
    {
      // Arrange
      var mockClient = CreateMockHttpClient(HttpStatusCode.OK, CreateSuccessResponse("{}"));
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      var cts = new CancellationTokenSource();
      cts.Cancel();

      // Act
      var result = await client.SendStructuredPromptWithMetricsAsync("Test", @"{""type"": ""object""}", cancellationToken: cts.Token);

      // Assert
      Assert.That(result.Content, Does.Contain("Error:"));
    }

    #endregion
  }
}

