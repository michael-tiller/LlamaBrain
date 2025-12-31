using NUnit.Framework;
using LlamaBrain.Core;
using Newtonsoft.Json;

namespace LlamaBrain.Tests
{
  public class ApiContractsTests
  {
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
  }
}

