using System;
using System.Threading;
using System.Diagnostics;
using LlamaBrain.Core;
using LlamaBrain.Utilities;
using Cysharp.Threading.Tasks;

#nullable enable

namespace LlamaBrain.Runtime.Core
{
  /// <summary>
  /// Unity-specific extensions for BrainAgent that provide UniTask support
  /// </summary>
  public static class BrainAgentExtensions
  {
    /// <summary>
    /// Sends a message and expects a structured JSON response using UniTask
    /// </summary>
    /// <param name="brainAgent">The brain agent</param>
    /// <param name="message">The message to send</param>
    /// <param name="jsonSchema">The JSON schema the response should follow</param>
    /// <param name="seed">Optional seed for deterministic generation (Feature 14)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The structured JSON response as a string</returns>
    public static async UniTask<string> SendStructuredMessageAsync(this BrainAgent brainAgent, string message, string jsonSchema, int? seed = null, CancellationToken cancellationToken = default)
    {
      var stopwatch = Stopwatch.StartNew();
      var result = await brainAgent.SendStructuredMessageAsync(message, jsonSchema, seed, cancellationToken).AsUniTask();
      stopwatch.Stop();
      // Note: Detailed metrics are logged via ApiClient.OnMetricsAvailable event
      UnityEngine.Debug.Log($"[BrainAgentExtensions] Structured message generation completed in {stopwatch.ElapsedMilliseconds}ms");
      return result;
    }

    /// <summary>
    /// Sends an instruction and expects a structured JSON response using UniTask
    /// </summary>
    /// <param name="brainAgent">The brain agent</param>
    /// <param name="instruction">The instruction to send</param>
    /// <param name="jsonSchema">The JSON schema the response should follow</param>
    /// <param name="context">Optional additional context</param>
    /// <param name="seed">Optional seed for deterministic generation (Feature 14)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The structured JSON response as a string</returns>
    public static async UniTask<string> SendStructuredInstructionAsync(this BrainAgent brainAgent, string instruction, string jsonSchema, string? context = null, int? seed = null, CancellationToken cancellationToken = default)
    {
      var stopwatch = Stopwatch.StartNew();
      var result = await brainAgent.SendStructuredInstructionAsync(instruction, jsonSchema, context, seed, cancellationToken).AsUniTask();
      stopwatch.Stop();
      // Note: Detailed metrics are logged via ApiClient.OnMetricsAvailable event
      UnityEngine.Debug.Log($"[BrainAgentExtensions] Structured instruction generation completed in {stopwatch.ElapsedMilliseconds}ms");
      return result;
    }

    /// <summary>
    /// Sends a message and deserializes the response to a specific type using UniTask
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="brainAgent">The brain agent</param>
    /// <param name="message">The message to send</param>
    /// <param name="jsonSchema">The JSON schema the response should follow</param>
    /// <param name="seed">Optional seed for deterministic generation (Feature 14)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The deserialized response object</returns>
    public static async UniTask<T?> SendStructuredMessageAsync<T>(this BrainAgent brainAgent, string message, string jsonSchema, int? seed = null, CancellationToken cancellationToken = default) where T : class
    {
      var stopwatch = Stopwatch.StartNew();
      var result = await brainAgent.SendStructuredMessageAsync<T>(message, jsonSchema, seed, cancellationToken).AsUniTask();
      stopwatch.Stop();
      // Note: Detailed metrics are logged via ApiClient.OnMetricsAvailable event
      UnityEngine.Debug.Log($"[BrainAgentExtensions] Structured message generation (typed) completed in {stopwatch.ElapsedMilliseconds}ms");
      return result;
    }

    /// <summary>
    /// Sends an instruction and deserializes the response to a specific type using UniTask
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="brainAgent">The brain agent</param>
    /// <param name="instruction">The instruction to send</param>
    /// <param name="jsonSchema">The JSON schema the response should follow</param>
    /// <param name="context">Optional additional context</param>
    /// <param name="seed">Optional seed for deterministic generation (Feature 14)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The deserialized response object</returns>
    public static async UniTask<T?> SendStructuredInstructionAsync<T>(this BrainAgent brainAgent, string instruction, string jsonSchema, string? context = null, int? seed = null, CancellationToken cancellationToken = default) where T : class
    {
      var stopwatch = Stopwatch.StartNew();
      var result = await brainAgent.SendStructuredInstructionAsync<T>(instruction, jsonSchema, context, seed, cancellationToken).AsUniTask();
      stopwatch.Stop();
      // Note: Detailed metrics are logged via ApiClient.OnMetricsAvailable event
      UnityEngine.Debug.Log($"[BrainAgentExtensions] Structured instruction generation (typed) completed in {stopwatch.ElapsedMilliseconds}ms");
      return result;
    }

    /// <summary>
    /// Sends a message to the persona and gets a response using UniTask
    /// </summary>
    /// <param name="brainAgent">The brain agent</param>
    /// <param name="message">The message to send</param>
    /// <param name="seed">Optional seed for deterministic generation (Feature 14)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The persona's response</returns>
    public static async UniTask<string> SendMessageAsync(this BrainAgent brainAgent, string message, int? seed = null, CancellationToken cancellationToken = default)
    {
      var stopwatch = Stopwatch.StartNew();
      var result = await brainAgent.SendMessageAsync(message, seed, cancellationToken).AsUniTask();
      stopwatch.Stop();
      // Note: Detailed metrics are logged via ApiClient.OnMetricsAvailable event
      UnityEngine.Debug.Log($"[BrainAgentExtensions] Message generation completed in {stopwatch.ElapsedMilliseconds}ms");
      return result;
    }

    /// <summary>
    /// Sends a simple message to the persona and gets a response using UniTask
    /// </summary>
    /// <param name="brainAgent">The brain agent</param>
    /// <param name="message">The message to send</param>
    /// <param name="seed">Optional seed for deterministic generation (Feature 14)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The persona's response</returns>
    public static async UniTask<string> SendSimpleMessageAsync(this BrainAgent brainAgent, string message, int? seed = null, CancellationToken cancellationToken = default)
    {
      var stopwatch = Stopwatch.StartNew();
      var result = await brainAgent.SendSimpleMessageAsync(message, seed, cancellationToken).AsUniTask();
      stopwatch.Stop();
      // Note: Detailed metrics are logged via ApiClient.OnMetricsAvailable event
      UnityEngine.Debug.Log($"[BrainAgentExtensions] Simple message generation completed in {stopwatch.ElapsedMilliseconds}ms");
      return result;
    }

    /// <summary>
    /// Sends an instruction to the persona and gets a response using UniTask
    /// </summary>
    /// <param name="brainAgent">The brain agent</param>
    /// <param name="instruction">The instruction to send</param>
    /// <param name="context">Optional additional context</param>
    /// <param name="seed">Optional seed for deterministic generation (Feature 14)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The persona's response</returns>
    public static async UniTask<string> SendInstructionAsync(this BrainAgent brainAgent, string instruction, string? context = null, int? seed = null, CancellationToken cancellationToken = default)
    {
      var stopwatch = Stopwatch.StartNew();
      var result = await brainAgent.SendInstructionAsync(instruction, context, seed, cancellationToken).AsUniTask();
      stopwatch.Stop();
      // Note: Detailed metrics are logged via ApiClient.OnMetricsAvailable event
      UnityEngine.Debug.Log($"[BrainAgentExtensions] Instruction generation completed in {stopwatch.ElapsedMilliseconds}ms");
      return result;
    }
  }
}