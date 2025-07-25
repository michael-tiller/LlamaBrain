using System;
using System.Threading;
using LlamaBrain.Core;
using LlamaBrain.Utilities;
using Cysharp.Threading.Tasks;

#nullable enable

namespace LlamaBrain.Unity.Runtime.Core
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
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The structured JSON response as a string</returns>
    public static async UniTask<string> SendStructuredMessageAsync(this BrainAgent brainAgent, string message, string jsonSchema, CancellationToken cancellationToken = default)
    {
      return await brainAgent.SendStructuredMessageAsync(message, jsonSchema, cancellationToken).AsUniTask();
    }

    /// <summary>
    /// Sends an instruction and expects a structured JSON response using UniTask
    /// </summary>
    /// <param name="brainAgent">The brain agent</param>
    /// <param name="instruction">The instruction to send</param>
    /// <param name="jsonSchema">The JSON schema the response should follow</param>
    /// <param name="context">Optional additional context</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The structured JSON response as a string</returns>
    public static async UniTask<string> SendStructuredInstructionAsync(this BrainAgent brainAgent, string instruction, string jsonSchema, string? context = null, CancellationToken cancellationToken = default)
    {
      return await brainAgent.SendStructuredInstructionAsync(instruction, jsonSchema, context, cancellationToken).AsUniTask();
    }

    /// <summary>
    /// Sends a message and deserializes the response to a specific type using UniTask
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="brainAgent">The brain agent</param>
    /// <param name="message">The message to send</param>
    /// <param name="jsonSchema">The JSON schema the response should follow</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The deserialized response object</returns>
    public static async UniTask<T?> SendStructuredMessageAsync<T>(this BrainAgent brainAgent, string message, string jsonSchema, CancellationToken cancellationToken = default) where T : class
    {
      return await brainAgent.SendStructuredMessageAsync<T>(message, jsonSchema, cancellationToken).AsUniTask();
    }

    /// <summary>
    /// Sends an instruction and deserializes the response to a specific type using UniTask
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="brainAgent">The brain agent</param>
    /// <param name="instruction">The instruction to send</param>
    /// <param name="jsonSchema">The JSON schema the response should follow</param>
    /// <param name="context">Optional additional context</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The deserialized response object</returns>
    public static async UniTask<T?> SendStructuredInstructionAsync<T>(this BrainAgent brainAgent, string instruction, string jsonSchema, string? context = null, CancellationToken cancellationToken = default) where T : class
    {
      return await brainAgent.SendStructuredInstructionAsync<T>(instruction, jsonSchema, context, cancellationToken).AsUniTask();
    }

    /// <summary>
    /// Sends a message to the persona and gets a response using UniTask
    /// </summary>
    /// <param name="brainAgent">The brain agent</param>
    /// <param name="message">The message to send</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The persona's response</returns>
    public static async UniTask<string> SendMessageAsync(this BrainAgent brainAgent, string message, CancellationToken cancellationToken = default)
    {
      return await brainAgent.SendMessageAsync(message, cancellationToken).AsUniTask();
    }

    /// <summary>
    /// Sends a simple message to the persona and gets a response using UniTask
    /// </summary>
    /// <param name="brainAgent">The brain agent</param>
    /// <param name="message">The message to send</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The persona's response</returns>
    public static async UniTask<string> SendSimpleMessageAsync(this BrainAgent brainAgent, string message, CancellationToken cancellationToken = default)
    {
      return await brainAgent.SendSimpleMessageAsync(message, cancellationToken).AsUniTask();
    }

    /// <summary>
    /// Sends an instruction to the persona and gets a response using UniTask
    /// </summary>
    /// <param name="brainAgent">The brain agent</param>
    /// <param name="instruction">The instruction to send</param>
    /// <param name="context">Optional additional context</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The persona's response</returns>
    public static async UniTask<string> SendInstructionAsync(this BrainAgent brainAgent, string instruction, string? context = null, CancellationToken cancellationToken = default)
    {
      return await brainAgent.SendInstructionAsync(instruction, context, cancellationToken).AsUniTask();
    }
  }
}