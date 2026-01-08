using NUnit.Framework;
using LlamaBrain.Core;
using Newtonsoft.Json;

namespace LlamaBrain.Tests
{
  public class ApiContractsTests
  {
    #region CompletionRequest Tests

    [Test]
    public void CompletionRequest_DefaultValues_AreCorrect()
    {
      // Arrange & Act
      var request = new CompletionRequest();

      // Assert
      Assert.IsNull(request.prompt);
      Assert.AreEqual(128, request.n_predict);
      Assert.AreEqual(0.7f, request.temperature);
      Assert.AreEqual(0.9f, request.top_p);
      Assert.AreEqual(40, request.top_k);
      Assert.AreEqual(1.1f, request.repeat_penalty);
      Assert.IsNull(request.stop);
      Assert.IsFalse(request.stream);
      Assert.IsFalse(request.cache_prompt);
      Assert.IsNull(request.seed);
      Assert.IsNull(request.json_schema);
      Assert.IsNull(request.grammar);
      Assert.IsNull(request.response_format);
    }

    [Test]
    public void CompletionRequest_WithCustomValues_StoresCorrectly()
    {
      // Arrange
      var request = new CompletionRequest
      {
        prompt = "Hello, world!",
        n_predict = 256,
        temperature = 0.5f,
        top_p = 0.8f,
        top_k = 20,
        repeat_penalty = 1.2f,
        stop = new string[] { "END", "STOP" },
        stream = true
      };

      // Assert
      Assert.AreEqual("Hello, world!", request.prompt);
      Assert.AreEqual(256, request.n_predict);
      Assert.AreEqual(0.5f, request.temperature);
      Assert.AreEqual(0.8f, request.top_p);
      Assert.AreEqual(20, request.top_k);
      Assert.AreEqual(1.2f, request.repeat_penalty);
      Assert.AreEqual(2, request.stop.Length);
      Assert.AreEqual("END", request.stop[0]);
      Assert.AreEqual("STOP", request.stop[1]);
      Assert.IsTrue(request.stream);
    }

    [Test]
    public void CompletionRequest_Serialization_WorksCorrectly()
    {
      // Arrange
      var request = new CompletionRequest
      {
        prompt = "Test prompt",
        n_predict = 100,
        temperature = 0.8f,
        stream = false
      };

      // Act
      var json = JsonConvert.SerializeObject(request);
      var deserialized = JsonConvert.DeserializeObject<CompletionRequest>(json);

      // Assert
      Assert.IsNotNull(deserialized);
      Assert.AreEqual(request.prompt, deserialized!.prompt);
      Assert.AreEqual(request.n_predict, deserialized.n_predict);
      Assert.AreEqual(request.temperature, deserialized.temperature);
      Assert.AreEqual(request.stream, deserialized.stream);
    }

    [Test]
    public void CompletionResponse_DefaultValues_AreCorrect()
    {
      // Arrange & Act
      var response = new CompletionResponse();

      // Assert
      Assert.IsNull(response.content);
      Assert.IsFalse(response.stop);
      Assert.IsNull(response.timings);
      Assert.AreEqual(0, response.tokens_predicted);
      Assert.AreEqual(0, response.tokens_cached);
      Assert.AreEqual(0, response.tokens_evaluated);
    }

    [Test]
    public void CompletionResponse_WithCustomValues_StoresCorrectly()
    {
      // Arrange
      var response = new CompletionResponse
      {
        content = "Generated response",
        stop = true,
        timings = new Timings
        {
          predicted_ms = 100.0,
          prompt_ms = 50.0,
          predicted_n = 20,
          prompt_n = 100
        },
        tokens_predicted = 50,
        tokens_cached = 100,
        tokens_evaluated = 200
      };

      // Assert
      Assert.AreEqual("Generated response", response.content);
      Assert.IsTrue(response.stop);
      Assert.IsNotNull(response.timings);
      Assert.AreEqual(100.0, response.timings.predicted_ms);
      Assert.AreEqual(50.0, response.timings.prompt_ms);
      Assert.AreEqual(20, response.timings.predicted_n);
      Assert.AreEqual(100, response.timings.prompt_n);
      Assert.AreEqual(50, response.tokens_predicted);
      Assert.AreEqual(100, response.tokens_cached);
      Assert.AreEqual(200, response.tokens_evaluated);
    }

    [Test]
    public void CompletionResponse_Serialization_WorksCorrectly()
    {
      // Arrange
      var response = new CompletionResponse
      {
        content = "Test response",
        stop = true,
        timings = new Timings
        {
          predicted_ms = 25.0,
          prompt_ms = 10.0,
          predicted_n = 10,
          prompt_n = 50
        },
        tokens_predicted = 25
      };

      // Act
      var json = JsonConvert.SerializeObject(response);
      var deserialized = JsonConvert.DeserializeObject<CompletionResponse>(json);

      // Assert
      Assert.IsNotNull(deserialized);
      Assert.AreEqual(response.content, deserialized!.content);
      Assert.AreEqual(response.stop, deserialized.stop);
      Assert.IsNotNull(deserialized.timings);
      Assert.AreEqual(response.timings.predicted_ms, deserialized.timings!.predicted_ms);
      Assert.AreEqual(response.timings.prompt_ms, deserialized.timings.prompt_ms);
      Assert.AreEqual(response.timings.predicted_n, deserialized.timings.predicted_n);
      Assert.AreEqual(response.timings.prompt_n, deserialized.timings.prompt_n);
      Assert.AreEqual(response.tokens_predicted, deserialized.tokens_predicted);
    }

    #endregion

    #region CompletionRequest Structured Output Tests

    [Test]
    public void CompletionRequest_WithStructuredOutputParams_StoresCorrectly()
    {
      // Arrange & Act
      var request = new CompletionRequest
      {
        prompt = "Generate JSON",
        json_schema = @"{""type"":""object""}",
        grammar = "root ::= object",
        response_format = ResponseFormat.JsonObject,
        cache_prompt = true,
        seed = 42
      };

      // Assert
      Assert.AreEqual(@"{""type"":""object""}", request.json_schema);
      Assert.AreEqual("root ::= object", request.grammar);
      Assert.IsNotNull(request.response_format);
      Assert.IsTrue(request.cache_prompt);
      Assert.AreEqual(42, request.seed);
    }

    [Test]
    public void CompletionRequest_SeedWithNull_OmitsFromJson()
    {
      // Arrange
      var request = new CompletionRequest { prompt = "Test", seed = null };

      // Act
      var json = JsonConvert.SerializeObject(request);

      // Assert - seed should be omitted due to NullValueHandling.Ignore attribute
      Assert.That(json, Does.Not.Contain("\"seed\":"));
    }

    [Test]
    public void CompletionRequest_SeedWithValue_IncludesInJson()
    {
      // Arrange
      var request = new CompletionRequest { prompt = "Test", seed = 123 };

      // Act
      var json = JsonConvert.SerializeObject(request);

      // Assert
      Assert.That(json, Does.Contain("\"seed\":123"));
    }

    #endregion

    #region CompletionRequest n_keep Tests

    [Test]
    public void CompletionRequest_NKeep_DefaultIsNull()
    {
      // Arrange & Act
      var request = new CompletionRequest();

      // Assert
      Assert.That(request.n_keep, Is.Null);
    }

    [Test]
    public void CompletionRequest_NKeep_CanBeSet()
    {
      // Arrange & Act
      var request = new CompletionRequest { n_keep = 500 };

      // Assert
      Assert.That(request.n_keep, Is.EqualTo(500));
    }

    [Test]
    public void CompletionRequest_NKeepWithNull_OmitsFromJson()
    {
      // Arrange
      var request = new CompletionRequest { prompt = "Test", n_keep = null };

      // Act
      var json = JsonConvert.SerializeObject(request);

      // Assert - n_keep should be omitted due to NullValueHandling.Ignore attribute
      Assert.That(json, Does.Not.Contain("\"n_keep\":"));
    }

    [Test]
    public void CompletionRequest_NKeepWithValue_IncludesInJson()
    {
      // Arrange
      var request = new CompletionRequest { prompt = "Test", n_keep = 256 };

      // Act
      var json = JsonConvert.SerializeObject(request);

      // Assert
      Assert.That(json, Does.Contain("\"n_keep\":256"));
    }

    [Test]
    public void CompletionRequest_NKeepWithNegativeOne_IncludesInJson()
    {
      // Arrange - n_keep of -1 means "keep all tokens" in llama.cpp
      var request = new CompletionRequest { prompt = "Test", n_keep = -1 };

      // Act
      var json = JsonConvert.SerializeObject(request);

      // Assert
      Assert.That(json, Does.Contain("\"n_keep\":-1"));
    }

    [Test]
    public void CompletionRequest_NKeepWithZero_IncludesInJson()
    {
      // Arrange - n_keep of 0 means "keep no tokens"
      var request = new CompletionRequest { prompt = "Test", n_keep = 0 };

      // Act
      var json = JsonConvert.SerializeObject(request);

      // Assert
      Assert.That(json, Does.Contain("\"n_keep\":0"));
    }

    [Test]
    public void CompletionRequest_FullKvCacheConfig_SerializesCorrectly()
    {
      // Arrange - full KV cache configuration scenario
      var request = new CompletionRequest
      {
        prompt = "You are a helpful assistant.\n[Fact] The sky is blue.\nUser: Hello",
        cache_prompt = true,
        n_keep = 50, // Protect first 50 tokens (static prefix)
        seed = 42
      };

      // Act
      var json = JsonConvert.SerializeObject(request);
      var deserialized = JsonConvert.DeserializeObject<CompletionRequest>(json);

      // Assert
      Assert.That(deserialized, Is.Not.Null);
      Assert.That(deserialized!.cache_prompt, Is.True);
      Assert.That(deserialized.n_keep, Is.EqualTo(50));
      Assert.That(deserialized.seed, Is.EqualTo(42));
    }

    #endregion

    #region ResponseFormat Tests

    [Test]
    public void ResponseFormat_DefaultType_IsJsonObject()
    {
      // Arrange & Act
      var format = new ResponseFormat();

      // Assert
      Assert.AreEqual("json_object", format.type);
      Assert.IsNull(format.schema);
    }

    [Test]
    public void ResponseFormat_JsonObject_CreatesCorrectInstance()
    {
      // Act
      var format = ResponseFormat.JsonObject;

      // Assert
      Assert.IsNotNull(format);
      Assert.AreEqual("json_object", format.type);
      Assert.IsNull(format.schema);
    }

    [Test]
    public void ResponseFormat_WithSchema_CreatesCorrectInstance()
    {
      // Arrange
      var schema = new { type = "object", properties = new { name = new { type = "string" } } };

      // Act
      var format = ResponseFormat.WithSchema(schema);

      // Assert
      Assert.IsNotNull(format);
      Assert.AreEqual("json_object", format.type);
      Assert.IsNotNull(format.schema);
      Assert.AreEqual(schema, format.schema);
    }

    [Test]
    public void ResponseFormat_Serialization_WorksCorrectly()
    {
      // Arrange
      var format = new ResponseFormat
      {
        type = "json_object",
        schema = new { type = "object" }
      };

      // Act
      var json = JsonConvert.SerializeObject(format);
      var deserialized = JsonConvert.DeserializeObject<ResponseFormat>(json);

      // Assert
      Assert.IsNotNull(deserialized);
      Assert.AreEqual("json_object", deserialized!.type);
      Assert.IsNotNull(deserialized.schema);
    }

    #endregion

    #region Timings Tests

    [Test]
    public void Timings_DefaultValues_AreZero()
    {
      // Arrange & Act
      var timings = new Timings();

      // Assert
      Assert.AreEqual(0, timings.prompt_n);
      Assert.AreEqual(0.0, timings.prompt_ms);
      Assert.AreEqual(0.0, timings.prompt_per_token_ms);
      Assert.AreEqual(0.0, timings.prompt_per_second);
      Assert.AreEqual(0, timings.predicted_n);
      Assert.AreEqual(0.0, timings.predicted_ms);
      Assert.AreEqual(0.0, timings.predicted_per_token_ms);
      Assert.AreEqual(0.0, timings.predicted_per_second);
    }

    [Test]
    public void Timings_WithCustomValues_StoresCorrectly()
    {
      // Arrange & Act
      var timings = new Timings
      {
        prompt_n = 100,
        prompt_ms = 50.5,
        prompt_per_token_ms = 0.505,
        prompt_per_second = 1980.2,
        predicted_n = 50,
        predicted_ms = 200.0,
        predicted_per_token_ms = 4.0,
        predicted_per_second = 250.0
      };

      // Assert
      Assert.AreEqual(100, timings.prompt_n);
      Assert.AreEqual(50.5, timings.prompt_ms);
      Assert.AreEqual(0.505, timings.prompt_per_token_ms);
      Assert.AreEqual(1980.2, timings.prompt_per_second);
      Assert.AreEqual(50, timings.predicted_n);
      Assert.AreEqual(200.0, timings.predicted_ms);
      Assert.AreEqual(4.0, timings.predicted_per_token_ms);
      Assert.AreEqual(250.0, timings.predicted_per_second);
    }

    #endregion

    #region CompletionMetrics Tests

    [Test]
    public void CompletionMetrics_DefaultValues_AreCorrect()
    {
      // Arrange & Act
      var metrics = new CompletionMetrics();

      // Assert
      Assert.AreEqual(string.Empty, metrics.Content);
      Assert.AreEqual(0, metrics.PromptTokenCount);
      Assert.AreEqual(0, metrics.PrefillTimeMs);
      Assert.AreEqual(0, metrics.DecodeTimeMs);
      Assert.AreEqual(0, metrics.TtftMs);
      Assert.AreEqual(0, metrics.GeneratedTokenCount);
      Assert.AreEqual(0, metrics.CachedTokenCount);
      Assert.AreEqual(0, metrics.TotalTimeMs);
    }

    [Test]
    public void CompletionMetrics_TokensPerSecond_CalculatesCorrectly()
    {
      // Arrange
      var metrics = new CompletionMetrics
      {
        GeneratedTokenCount = 100,
        DecodeTimeMs = 1000  // 1 second
      };

      // Act
      var tokensPerSecond = metrics.TokensPerSecond;

      // Assert - 100 tokens in 1000ms = 100 tokens/sec
      Assert.AreEqual(100.0, tokensPerSecond);
    }

    [Test]
    public void CompletionMetrics_TokensPerSecond_WithZeroDecodeTime_ReturnsZero()
    {
      // Arrange
      var metrics = new CompletionMetrics
      {
        GeneratedTokenCount = 100,
        DecodeTimeMs = 0
      };

      // Act
      var tokensPerSecond = metrics.TokensPerSecond;

      // Assert - division by zero should return 0
      Assert.AreEqual(0.0, tokensPerSecond);
    }

    [Test]
    public void CompletionMetrics_TokensPerSecond_WithFractionalValues_CalculatesCorrectly()
    {
      // Arrange
      var metrics = new CompletionMetrics
      {
        GeneratedTokenCount = 50,
        DecodeTimeMs = 200  // 200ms
      };

      // Act
      var tokensPerSecond = metrics.TokensPerSecond;

      // Assert - 50 tokens in 200ms = 250 tokens/sec
      Assert.AreEqual(250.0, tokensPerSecond);
    }

    [Test]
    public void CompletionMetrics_WithCustomValues_StoresCorrectly()
    {
      // Arrange & Act
      var metrics = new CompletionMetrics
      {
        Content = "Generated content",
        PromptTokenCount = 50,
        PrefillTimeMs = 100,
        DecodeTimeMs = 500,
        TtftMs = 100,
        GeneratedTokenCount = 25,
        CachedTokenCount = 10,
        TotalTimeMs = 600
      };

      // Assert
      Assert.AreEqual("Generated content", metrics.Content);
      Assert.AreEqual(50, metrics.PromptTokenCount);
      Assert.AreEqual(100, metrics.PrefillTimeMs);
      Assert.AreEqual(500, metrics.DecodeTimeMs);
      Assert.AreEqual(100, metrics.TtftMs);
      Assert.AreEqual(25, metrics.GeneratedTokenCount);
      Assert.AreEqual(10, metrics.CachedTokenCount);
      Assert.AreEqual(600, metrics.TotalTimeMs);
      Assert.AreEqual(50.0, metrics.TokensPerSecond); // 25 tokens in 500ms = 50 tokens/sec
    }

    #endregion
  }
}

