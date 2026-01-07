// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Michael Tiller and contributors

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LlamaBrain.Core.StructuredOutput;
using LlamaBrain.Core.Validation;
using LlamaBrain.Persona;
using LlamaBrain.Utilities;

namespace LlamaBrain.Core
{
  /// <summary>
  /// High-level agent for managing persona interactions with LLM integration
  /// </summary>
  public sealed class BrainAgent : IDisposable
  {
    /// <summary>
    /// The persona profile for this agent
    /// </summary>
    private readonly PersonaProfile _profile;

    /// <summary>
    /// The API client for LLM communication
    /// </summary>
    private readonly IApiClient _apiClient;

    /// <summary>
    /// The dialogue session for conversation tracking
    /// </summary>
    private readonly DialogueSession _dialogueSession;

    /// <summary>
    /// The memory store for persistent memory
    /// </summary>
    private readonly PersonaMemoryStore _memoryStore;

    /// <summary>
    /// The prompt composer for building LLM prompts
    /// </summary>
    private readonly PromptComposer _promptComposer;

    /// <summary>
    /// Whether the agent has been disposed
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// Gets the persona profile
    /// </summary>
    public PersonaProfile Profile => _profile;

    /// <summary>
    /// Gets the dialogue session
    /// </summary>
    public DialogueSession DialogueSession => _dialogueSession;

    /// <summary>
    /// Gets the memory store
    /// </summary>
    public PersonaMemoryStore MemoryStore => _memoryStore;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="profile">The persona profile</param>
    /// <param name="apiClient">The API client for LLM communication</param>
    /// <param name="memoryStore">Optional memory store (will create one if not provided)</param>
    public BrainAgent(PersonaProfile profile, IApiClient apiClient, PersonaMemoryStore? memoryStore = null)
    {
      _profile = profile ?? throw new ArgumentNullException(nameof(profile));
      _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
      _memoryStore = memoryStore ?? new PersonaMemoryStore();
      _dialogueSession = new DialogueSession(profile.PersonaId, _memoryStore);
      _promptComposer = new PromptComposer();
    }

    /// <summary>
    /// Sends a message to the persona and gets a response
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <param name="seed">Optional seed for deterministic generation (e.g., InteractionCount)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The persona's response</returns>
    public async Task<string> SendMessageAsync(string message, int? seed = null, CancellationToken cancellationToken = default)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(BrainAgent));

      if (string.IsNullOrWhiteSpace(message))
        throw new ArgumentException("Message cannot be null or empty", nameof(message));

      try
      {
        // Add the user message to the dialogue session
        _dialogueSession.AppendPlayer(message);

        // Compose the prompt using the persona profile and dialogue history
        var prompt = _promptComposer.ComposePrompt(_profile, _dialogueSession, message);

        // Send the prompt to the LLM
        var response = await _apiClient.SendPromptAsync(prompt, seed: seed, cancellationToken: cancellationToken);

        // Add the response to the dialogue session
        _dialogueSession.AppendNpc(response);

        return response;
      }
      catch (Exception ex)
      {
        Logger.Error($"Error in BrainAgent.SendMessageAsync: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Sends a simple message without conversation history
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <param name="seed">Optional seed for deterministic generation (e.g., InteractionCount)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The persona's response</returns>
    public async Task<string> SendSimpleMessageAsync(string message, int? seed = null, CancellationToken cancellationToken = default)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(BrainAgent));

      if (string.IsNullOrWhiteSpace(message))
        throw new ArgumentException("Message cannot be null or empty", nameof(message));

      try
      {
        // Compose a simple prompt without conversation history
        var prompt = _promptComposer.ComposeSimplePrompt(_profile, message);

        // Send the prompt to the LLM
        var response = await _apiClient.SendPromptAsync(prompt, seed: seed, cancellationToken: cancellationToken);

        return response;
      }
      catch (Exception ex)
      {
        Logger.Error($"Error in BrainAgent.SendSimpleMessageAsync: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Sends an instruction to the persona
    /// </summary>
    /// <param name="instruction">The instruction to send</param>
    /// <param name="context">Optional additional context</param>
    /// <param name="seed">Optional seed for deterministic generation (e.g., InteractionCount)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The persona's response</returns>
    public async Task<string> SendInstructionAsync(string instruction, string? context = null, int? seed = null, CancellationToken cancellationToken = default)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(BrainAgent));

      if (string.IsNullOrWhiteSpace(instruction))
        throw new ArgumentException("Instruction cannot be null or empty", nameof(instruction));

      try
      {
        // Compose an instruction prompt
        var prompt = _promptComposer.ComposeInstructionPrompt(_profile, instruction, context);

        // Send the prompt to the LLM
        var response = await _apiClient.SendPromptAsync(prompt, seed: seed, cancellationToken: cancellationToken);

        return response;
      }
      catch (Exception ex)
      {
        Logger.Error($"Error in BrainAgent.SendInstructionAsync: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Adds a memory entry for the persona
    /// </summary>
    /// <param name="memory">The memory to add</param>
    public void AddMemory(string memory)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(BrainAgent));

      if (string.IsNullOrWhiteSpace(memory))
        throw new ArgumentException("Memory cannot be null or empty", nameof(memory));

      _memoryStore.AddMemory(_profile, memory);
    }

    /// <summary>
    /// Gets all episodic memories for the persona (raw content without formatting prefixes)
    /// </summary>
    /// <returns>The persona's episodic memories</returns>
    public IReadOnlyList<string> GetMemories()
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(BrainAgent));

      return _memoryStore.GetEpisodicMemories(_profile);
    }

    /// <summary>
    /// Clears all memories for the persona
    /// </summary>
    public void ClearMemories()
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(BrainAgent));

      _memoryStore.ClearMemory(_profile);
    }

    /// <summary>
    /// Clears the dialogue history
    /// </summary>
    public void ClearDialogueHistory()
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(BrainAgent));

      _dialogueSession.Clear();
    }

    /// <summary>
    /// Gets the conversation history
    /// </summary>
    /// <returns>The conversation history</returns>
    public IReadOnlyList<string> GetConversationHistory()
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(BrainAgent));

      return _dialogueSession.GetHistory();
    }

    /// <summary>
    /// Gets the recent conversation history
    /// </summary>
    /// <param name="count">Number of recent entries to retrieve</param>
    /// <returns>The recent conversation history</returns>
    public IReadOnlyList<DialogueEntry> GetRecentHistory(int count)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(BrainAgent));

      return _dialogueSession.GetRecentHistory(count);
    }

    /// <summary>
    /// Updates the persona profile
    /// </summary>
    /// <param name="newProfile">The new profile</param>
    public void UpdateProfile(PersonaProfile newProfile)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(BrainAgent));

      if (newProfile == null)
        throw new ArgumentNullException(nameof(newProfile));

      // Validate the new profile
      if (string.IsNullOrWhiteSpace(newProfile.Name))
        throw new ArgumentException("Profile name cannot be null or empty", nameof(newProfile));

      if (string.IsNullOrWhiteSpace(newProfile.PersonaId))
        throw new ArgumentException("Profile PersonaId cannot be null or empty", nameof(newProfile));

      // Validate that the persona ID matches
      if (newProfile.PersonaId != _profile.PersonaId)
        throw new ArgumentException("New profile must have the same PersonaId", nameof(newProfile));

      // Update the profile properties directly
      // Note: In a production environment, you'd want proper thread synchronization
      lock (this)
      {
        var oldName = _profile.Name;

        // Update the profile properties
        _profile.Name = newProfile.Name;
        _profile.Description = newProfile.Description;
        _profile.Background = newProfile.Background;
        _profile.SystemPrompt = newProfile.SystemPrompt;
        _profile.UseMemory = newProfile.UseMemory;

        // Update traits
        _profile.Traits.Clear();
        foreach (var trait in newProfile.Traits)
        {
          _profile.Traits[trait.Key] = trait.Value;
        }

        // Update metadata
        _profile.Metadata.Clear();
        foreach (var metadata in newProfile.Metadata)
        {
          _profile.Metadata[metadata.Key] = metadata.Value;
        }

        // Log the update
        Logger.Info($"Updated profile for persona {_profile.PersonaId}: {oldName} -> {_profile.Name}");

        // Optionally, you could trigger events or notifications here
        // OnProfileUpdated?.Invoke(oldProfile, _profile);
      }
    }

    /// <summary>
    /// Sends a message and expects a structured JSON response
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <param name="jsonSchema">The JSON schema the response should follow</param>
    /// <param name="seed">Optional seed for deterministic generation (e.g., InteractionCount)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The structured JSON response as a string</returns>
    public async Task<string> SendStructuredMessageAsync(string message, string jsonSchema, int? seed = null, CancellationToken cancellationToken = default)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(BrainAgent));

      if (string.IsNullOrWhiteSpace(message))
        throw new ArgumentException("Message cannot be null or empty", nameof(message));

      if (string.IsNullOrWhiteSpace(jsonSchema))
        throw new ArgumentException("JSON schema cannot be null or empty", nameof(jsonSchema));

      try
      {
        // Add the user message to the dialogue session
        _dialogueSession.AppendPlayer(message);

        // Compose the structured prompt using the persona profile and dialogue history
        var prompt = _promptComposer.ComposeStructuredJsonConversationPrompt(_profile, _dialogueSession, message, jsonSchema);

        // Send the prompt to the LLM
        var response = await _apiClient.SendPromptAsync(prompt, seed: seed, cancellationToken: cancellationToken);

        // Validate and clean the JSON response
        var cleanedResponse = CleanAndValidateJsonResponse(response);

        // Add the response to the dialogue session (as plain text for conversation flow)
        _dialogueSession.AppendNpc(cleanedResponse);

        return cleanedResponse;
      }
      catch (Exception ex)
      {
        Logger.Error($"Error in SendStructuredMessageAsync: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Sends an instruction and expects a structured JSON response
    /// </summary>
    /// <param name="instruction">The instruction to send</param>
    /// <param name="jsonSchema">The JSON schema the response should follow</param>
    /// <param name="context">Optional additional context</param>
    /// <param name="seed">Optional seed for deterministic generation (e.g., InteractionCount)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The structured JSON response as a string</returns>
    public async Task<string> SendStructuredInstructionAsync(string instruction, string jsonSchema, string? context = null, int? seed = null, CancellationToken cancellationToken = default)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(BrainAgent));

      if (string.IsNullOrWhiteSpace(instruction))
        throw new ArgumentException("Instruction cannot be null or empty", nameof(instruction));

      if (string.IsNullOrWhiteSpace(jsonSchema))
        throw new ArgumentException("JSON schema cannot be null or empty", nameof(jsonSchema));

      try
      {
        // Compose the structured prompt using the persona profile
        var prompt = _promptComposer.ComposeStructuredJsonPrompt(_profile, instruction, jsonSchema, context);

        // Send the prompt to the LLM
        var response = await _apiClient.SendPromptAsync(prompt, seed: seed, cancellationToken: cancellationToken);

        // Validate and clean the JSON response
        var cleanedResponse = CleanAndValidateJsonResponse(response);

        return cleanedResponse;
      }
      catch (Exception ex)
      {
        Logger.Error($"Error in SendStructuredInstructionAsync: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Sends a message and deserializes the response to a specific type
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="message">The message to send</param>
    /// <param name="jsonSchema">The JSON schema the response should follow</param>
    /// <param name="seed">Optional seed for deterministic generation (e.g., InteractionCount)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The deserialized response object</returns>
    public async Task<T?> SendStructuredMessageAsync<T>(string message, string jsonSchema, int? seed = null, CancellationToken cancellationToken = default) where T : class
    {
      var jsonResponse = await SendStructuredMessageAsync(message, jsonSchema, seed, cancellationToken);
      return JsonUtils.Deserialize<T>(jsonResponse);
    }

    /// <summary>
    /// Sends an instruction and deserializes the response to a specific type
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="instruction">The instruction to send</param>
    /// <param name="jsonSchema">The JSON schema the response should follow</param>
    /// <param name="context">Optional additional context</param>
    /// <param name="seed">Optional seed for deterministic generation (e.g., InteractionCount)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The deserialized response object</returns>
    public async Task<T?> SendStructuredInstructionAsync<T>(string instruction, string jsonSchema, string? context = null, int? seed = null, CancellationToken cancellationToken = default) where T : class
    {
      var jsonResponse = await SendStructuredInstructionAsync(instruction, jsonSchema, context, seed, cancellationToken);
      return JsonUtils.Deserialize<T>(jsonResponse);
    }

    // =========================================================================
    // Native Structured Output Methods (llama.cpp json_schema support)
    // =========================================================================

    /// <summary>
    /// Sends a message using native structured output (llama.cpp json_schema).
    /// The LLM is constrained to output valid JSON matching the schema.
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <param name="jsonSchema">The JSON schema the response must conform to</param>
    /// <param name="format">The structured output format (default: JsonSchema)</param>
    /// <param name="seed">Optional seed for deterministic generation (e.g., InteractionCount)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The structured JSON response</returns>
    public async Task<string> SendNativeStructuredMessageAsync(
      string message,
      string jsonSchema,
      StructuredOutputFormat format = StructuredOutputFormat.JsonSchema,
      int? seed = null,
      CancellationToken cancellationToken = default)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(BrainAgent));

      if (string.IsNullOrWhiteSpace(message))
        throw new ArgumentException("Message cannot be null or empty", nameof(message));

      if (string.IsNullOrWhiteSpace(jsonSchema))
        throw new ArgumentException("JSON schema cannot be null or empty", nameof(jsonSchema));

      try
      {
        // Add the user message to the dialogue session
        _dialogueSession.AppendPlayer(message);

        // Compose the prompt (without structured output instructions - the API enforces structure)
        var prompt = _promptComposer.ComposePrompt(_profile, _dialogueSession, message);

        // Send with native structured output
        var response = await _apiClient.SendStructuredPromptAsync(
          prompt,
          jsonSchema,
          format,
          seed: seed,
          cancellationToken: cancellationToken);

        // Check for null/empty response
        if (string.IsNullOrWhiteSpace(response))
        {
          Logger.Error("Unexpected null or empty response from SendStructuredPromptAsync");
          throw new InvalidOperationException("Received null or empty response from LLM");
        }

        // Check for errors
        if (response.StartsWith("Error:"))
          throw new InvalidOperationException($"LLM returned an error: {response}");

        // Add the response to the dialogue session
        _dialogueSession.AppendNpc(response);

        return response;
      }
      catch (Exception ex)
      {
        Logger.Error($"Error in SendNativeStructuredMessageAsync: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Sends a message using native structured output and parses the result.
    /// Uses the ParsedOutput schema for dialogue with mutations and intents.
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <param name="format">The structured output format (default: JsonSchema)</param>
    /// <param name="seed">Optional seed for deterministic generation (e.g., InteractionCount)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Parsed output with dialogue, mutations, and intents</returns>
    public async Task<ParsedOutput> SendNativeDialogueAsync(
      string message,
      StructuredOutputFormat format = StructuredOutputFormat.JsonSchema,
      int? seed = null,
      CancellationToken cancellationToken = default)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(BrainAgent));

      if (string.IsNullOrWhiteSpace(message))
        throw new ArgumentException("Message cannot be null or empty", nameof(message));

      try
      {
        // Use the pre-built ParsedOutput schema
        var schema = JsonSchemaBuilder.ParsedOutputSchema;

        // Send with native structured output
        var jsonResponse = await SendNativeStructuredMessageAsync(
          message,
          schema,
          format,
          seed,
          cancellationToken);

        // Parse the structured response
        var parser = new OutputParser(OutputParserConfig.NativeStructured);
        var result = parser.ParseStructured(jsonResponse);

        return result;
      }
      catch (Exception ex)
      {
        Logger.Error($"Error in SendNativeDialogueAsync: {ex.Message}");
        return ParsedOutput.Failed($"Error: {ex.Message}", "");
      }
    }

    /// <summary>
    /// Sends a message using native structured output and deserializes to a specific type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="message">The message to send</param>
    /// <param name="jsonSchema">The JSON schema the response must conform to</param>
    /// <param name="format">The structured output format (default: JsonSchema)</param>
    /// <param name="seed">Optional seed for deterministic generation (e.g., InteractionCount)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The deserialized response object</returns>
    public async Task<T?> SendNativeStructuredMessageAsync<T>(
      string message,
      string jsonSchema,
      StructuredOutputFormat format = StructuredOutputFormat.JsonSchema,
      int? seed = null,
      CancellationToken cancellationToken = default) where T : class
    {
      var jsonResponse = await SendNativeStructuredMessageAsync(message, jsonSchema, format, seed, cancellationToken);
      return JsonUtils.Deserialize<T>(jsonResponse);
    }

    /// <summary>
    /// Sends a simple instruction using native structured output.
    /// Uses the provided schema to constrain the LLM output.
    /// </summary>
    /// <param name="instruction">The instruction to send</param>
    /// <param name="jsonSchema">The JSON schema the response must conform to</param>
    /// <param name="context">Optional additional context</param>
    /// <param name="format">The structured output format (default: JsonSchema)</param>
    /// <param name="seed">Optional seed for deterministic generation (e.g., InteractionCount)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The structured JSON response</returns>
    public async Task<string> SendNativeStructuredInstructionAsync(
      string instruction,
      string jsonSchema,
      string? context = null,
      StructuredOutputFormat format = StructuredOutputFormat.JsonSchema,
      int? seed = null,
      CancellationToken cancellationToken = default)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(BrainAgent));

      if (string.IsNullOrWhiteSpace(instruction))
        throw new ArgumentException("Instruction cannot be null or empty", nameof(instruction));

      if (string.IsNullOrWhiteSpace(jsonSchema))
        throw new ArgumentException("JSON schema cannot be null or empty", nameof(jsonSchema));

      try
      {
        // Compose the instruction prompt (without structured output instructions)
        var prompt = _promptComposer.ComposeInstructionPrompt(_profile, instruction, context);

        // Send with native structured output
        var response = await _apiClient.SendStructuredPromptAsync(
          prompt,
          jsonSchema,
          format,
          seed: seed,
          cancellationToken: cancellationToken);

        // Check for null/empty response
        if (string.IsNullOrWhiteSpace(response))
        {
          Logger.Error("Unexpected null or empty response from SendStructuredPromptAsync");
          throw new InvalidOperationException("Received null or empty response from LLM");
        }

        // Check for errors
        if (response.StartsWith("Error:"))
          throw new InvalidOperationException($"LLM returned an error: {response}");

        return response;
      }
      catch (Exception ex)
      {
        Logger.Error($"Error in SendNativeStructuredInstructionAsync: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Sends an instruction using native structured output and deserializes to a specific type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="instruction">The instruction to send</param>
    /// <param name="jsonSchema">The JSON schema the response must conform to</param>
    /// <param name="context">Optional additional context</param>
    /// <param name="format">The structured output format (default: JsonSchema)</param>
    /// <param name="seed">Optional seed for deterministic generation (e.g., InteractionCount)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The deserialized response object</returns>
    public async Task<T?> SendNativeStructuredInstructionAsync<T>(
      string instruction,
      string jsonSchema,
      string? context = null,
      StructuredOutputFormat format = StructuredOutputFormat.JsonSchema,
      int? seed = null,
      CancellationToken cancellationToken = default) where T : class
    {
      var jsonResponse = await SendNativeStructuredInstructionAsync(instruction, jsonSchema, context, format, seed, cancellationToken);
      return JsonUtils.Deserialize<T>(jsonResponse);
    }

    /// <summary>
    /// Cleans and validates a JSON response from the LLM
    /// </summary>
    /// <param name="response">The raw response from the LLM</param>
    /// <returns>The cleaned and validated JSON string</returns>
    private string CleanAndValidateJsonResponse(string response)
    {
      if (string.IsNullOrWhiteSpace(response))
        throw new InvalidOperationException("Empty response from LLM");

      // Remove any error prefixes
      if (response.StartsWith("Error:"))
        throw new InvalidOperationException($"LLM returned an error: {response}");

      // Try to extract JSON from the response
      var cleanedResponse = ExtractJsonFromResponse(response);

      // Validate that it's valid JSON
      if (!JsonUtils.IsValidJson(cleanedResponse))
        throw new InvalidOperationException($"Invalid JSON response: {cleanedResponse}");

      return cleanedResponse;
    }

    /// <summary>
    /// Extracts JSON from a potentially mixed response
    /// </summary>
    /// <param name="response">The raw response that may contain text before/after JSON</param>
    /// <returns>The extracted JSON string</returns>
    private string ExtractJsonFromResponse(string response)
    {
      // Trim whitespace
      response = response.Trim();

      // Look for JSON object start
      var startIndex = response.IndexOf('{');
      if (startIndex == -1)
        throw new InvalidOperationException("No JSON object found in response");

      // Look for JSON object end (handle nested braces)
      var braceCount = 0;
      var endIndex = -1;

      for (int i = startIndex; i < response.Length; i++)
      {
        if (response[i] == '{')
          braceCount++;
        else if (response[i] == '}')
        {
          braceCount--;
          if (braceCount == 0)
          {
            endIndex = i;
            break;
          }
        }
      }

      if (endIndex == -1)
        throw new InvalidOperationException("Incomplete JSON object in response");

      return response.Substring(startIndex, endIndex - startIndex + 1);
    }

    /// <summary>
    /// Disposes the agent and its resources
    /// </summary>
    public void Dispose()
    {
      if (!_disposed)
      {
        if (_apiClient is IDisposable disposable)
          disposable.Dispose();
        _disposed = true;
      }
    }
  }
}