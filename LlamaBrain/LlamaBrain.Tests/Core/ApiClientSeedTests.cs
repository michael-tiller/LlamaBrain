using NUnit.Framework;
using LlamaBrain.Core;
using LlamaBrain.Core.StructuredOutput;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

namespace LlamaBrain.Tests.Core
{
  /// <summary>
  /// Tests for seed parameter support in ApiClient.
  /// Feature 14.1: API layer seed parameter support.
  /// </summary>
  [TestFixture]
  public class ApiClientSeedTests
  {
    /// <summary>
    /// Mock HTTP message handler that captures request body
    /// </summary>
    private class CaptureHttpMessageHandler : HttpMessageHandler
    {
      public string? CapturedBody { get; private set; }
      private readonly string _responseContent;

      public CaptureHttpMessageHandler(string responseContent)
      {
        _responseContent = responseContent;
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
      {
        CapturedBody = request.Content?.ReadAsStringAsync().Result;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(_responseContent)
        });
      }
    }

    /// <summary>
    /// Helper to create a successful completion response JSON
    /// </summary>
    private static string CreateSuccessResponse(string content)
    {
      var response = new
      {
        content = content,
        stop = true,
        tokens_predicted = 20,
        tokens_cached = 5,
        tokens_evaluated = 10,
        timings = new
        {
          prompt_n = 10,
          prompt_ms = 100.0,
          predicted_n = 20,
          predicted_ms = 200.0
        }
      };
      return JsonConvert.SerializeObject(response);
    }

    #region SendPromptWithMetricsAsync Seed Tests

    [Test]
    public async Task SendPromptWithMetricsAsync_WithSeed_IncludesSeedInRequest()
    {
      // Arrange
      var handler = new CaptureHttpMessageHandler(CreateSuccessResponse("OK"));
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      await client.SendPromptWithMetricsAsync("Test prompt", seed: 42);

      // Assert
      Assert.IsNotNull(handler.CapturedBody);
      Assert.That(handler.CapturedBody, Does.Contain("\"seed\":42"));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithNullSeed_OmitsSeedFromRequest()
    {
      // Arrange
      var handler = new CaptureHttpMessageHandler(CreateSuccessResponse("OK"));
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      await client.SendPromptWithMetricsAsync("Test prompt", seed: null);

      // Assert
      Assert.IsNotNull(handler.CapturedBody);
      // null values are omitted from JSON serialization by default in Newtonsoft.Json
      Assert.That(handler.CapturedBody, Does.Not.Contain("\"seed\":"));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithNegativeSeed_PassesThroughToRequest()
    {
      // Arrange
      var handler = new CaptureHttpMessageHandler(CreateSuccessResponse("OK"));
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act - seed = -1 means random in llama.cpp
      await client.SendPromptWithMetricsAsync("Test prompt", seed: -1);

      // Assert
      Assert.IsNotNull(handler.CapturedBody);
      Assert.That(handler.CapturedBody, Does.Contain("\"seed\":-1"));
    }

    [Test]
    public async Task SendPromptWithMetricsAsync_WithZeroSeed_IncludesSeedInRequest()
    {
      // Arrange
      var handler = new CaptureHttpMessageHandler(CreateSuccessResponse("OK"));
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act - seed = 0 is a valid deterministic seed
      await client.SendPromptWithMetricsAsync("Test prompt", seed: 0);

      // Assert
      Assert.IsNotNull(handler.CapturedBody);
      Assert.That(handler.CapturedBody, Does.Contain("\"seed\":0"));
    }

    #endregion

    #region SendPromptAsync Seed Tests

    [Test]
    public async Task SendPromptAsync_WithSeed_IncludesSeedInRequest()
    {
      // Arrange
      var handler = new CaptureHttpMessageHandler(CreateSuccessResponse("OK"));
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      await client.SendPromptAsync("Test prompt", seed: 12345);

      // Assert
      Assert.IsNotNull(handler.CapturedBody);
      Assert.That(handler.CapturedBody, Does.Contain("\"seed\":12345"));
    }

    [Test]
    public async Task SendPromptAsync_WithNullSeed_OmitsSeedFromRequest()
    {
      // Arrange
      var handler = new CaptureHttpMessageHandler(CreateSuccessResponse("OK"));
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act
      await client.SendPromptAsync("Test prompt", seed: null);

      // Assert
      Assert.IsNotNull(handler.CapturedBody);
      Assert.That(handler.CapturedBody, Does.Not.Contain("\"seed\":"));
    }

    #endregion

    #region SendStructuredPromptAsync Seed Tests

    [Test]
    public async Task SendStructuredPromptAsync_WithSeed_IncludesSeedInRequest()
    {
      // Arrange
      var handler = new CaptureHttpMessageHandler(CreateSuccessResponse("{\"result\":\"ok\"}"));
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      var schema = @"{""type"":""object"",""properties"":{""result"":{""type"":""string""}}}";

      // Act
      await client.SendStructuredPromptAsync("Test prompt", schema, seed: 999);

      // Assert
      Assert.IsNotNull(handler.CapturedBody);
      Assert.That(handler.CapturedBody, Does.Contain("\"seed\":999"));
    }

    [Test]
    public async Task SendStructuredPromptAsync_WithNullSeed_OmitsSeedFromRequest()
    {
      // Arrange
      var handler = new CaptureHttpMessageHandler(CreateSuccessResponse("{\"result\":\"ok\"}"));
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      var schema = @"{""type"":""object"",""properties"":{""result"":{""type"":""string""}}}";

      // Act
      await client.SendStructuredPromptAsync("Test prompt", schema, seed: null);

      // Assert
      Assert.IsNotNull(handler.CapturedBody);
      Assert.That(handler.CapturedBody, Does.Not.Contain("\"seed\":"));
    }

    #endregion

    #region SendStructuredPromptWithMetricsAsync Seed Tests

    [Test]
    public async Task SendStructuredPromptWithMetricsAsync_WithSeed_IncludesSeedInRequest()
    {
      // Arrange
      var handler = new CaptureHttpMessageHandler(CreateSuccessResponse("{\"result\":\"ok\"}"));
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);
      var schema = @"{""type"":""object"",""properties"":{""result"":{""type"":""string""}}}";

      // Act
      await client.SendStructuredPromptWithMetricsAsync("Test prompt", schema, seed: 777);

      // Assert
      Assert.IsNotNull(handler.CapturedBody);
      Assert.That(handler.CapturedBody, Does.Contain("\"seed\":777"));
    }

    #endregion

    #region Seed Parameter Ordering Tests

    [Test]
    public async Task SendPromptWithMetricsAsync_SeedParameter_WorksWithOtherParameters()
    {
      // Arrange
      var handler = new CaptureHttpMessageHandler(CreateSuccessResponse("OK"));
      var mockClient = new HttpClient(handler);
      var client = new ApiClient("localhost", 5000, "test-model", null, 30, mockClient);

      // Act - test that seed works correctly alongside other parameters
      await client.SendPromptWithMetricsAsync("Test prompt", maxTokens: 100, temperature: 0.5f, seed: 42, cachePrompt: true);

      // Assert
      Assert.IsNotNull(handler.CapturedBody);
      Assert.That(handler.CapturedBody, Does.Contain("\"seed\":42"));
      Assert.That(handler.CapturedBody, Does.Contain("\"n_predict\":100"));
      Assert.That(handler.CapturedBody, Does.Contain("\"temperature\":0.5"));
      Assert.That(handler.CapturedBody, Does.Contain("\"cache_prompt\":true"));
    }

    #endregion
  }
}
