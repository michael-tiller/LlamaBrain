using System;
using UnityEngine;
using LlamaBrain.Core;
using LlamaBrain.Persona;
using LlamaBrain.Unity.Runtime.Core;
using LlamaBrain.Utilities;
using Cysharp.Threading.Tasks;

#nullable enable

namespace LlamaBrain.Unity.Runtime.Demo
{
  /// <summary>
  /// Example demonstrating structured JSON output with LlamaBrain
  /// </summary>
  public class StructuredOutputExample : MonoBehaviour
  {
    [Header("Configuration")]
    [SerializeField] private BrainSettings brainSettings = null!;
    [SerializeField] private PersonaConfig personaConfig = null!;

    [Header("Structured Output Schemas")]
    [TextArea(5, 10)]
    [SerializeField] private string dialogueResponseSchema = @"{
      ""type"": ""object"",
      ""properties"": {
        ""response"": {
          ""type"": ""string"",
          ""description"": ""The character's response to the player""
        },
        ""emotion"": {
          ""type"": ""string"",
          ""enum"": [""happy"", ""sad"", ""angry"", ""neutral"", ""excited"", ""worried""],
          ""description"": ""The emotional state of the character""
        },
        ""action"": {
          ""type"": ""string"",
          ""description"": ""Any physical action the character takes""
        }
      },
      ""required"": [""response""]
    }";

    [TextArea(5, 10)]
    [SerializeField] private string analysisSchema = @"{
      ""type"": ""object"",
      ""properties"": {
        ""sentiment"": {
          ""type"": ""string"",
          ""enum"": [""positive"", ""negative"", ""neutral""],
          ""description"": ""Overall sentiment analysis""
        },
        ""topics"": {
          ""type"": ""array"",
          ""items"": {
            ""type"": ""string""
          },
          ""description"": ""Key topics discussed""
        },
        ""intent"": {
          ""type"": ""string"",
          ""description"": ""Player's likely intent""
        }
      },
      ""required"": [""sentiment"", ""topics""]
    }";

    [Header("Test Input")]
    [SerializeField] private string testMessage = "Hello! How are you feeling today?";

    [Header("Output")]
    [SerializeField] private string lastResponse = "";
    [SerializeField] private string lastAnalysis = "";

    private BrainAgent? brainAgent;
    private bool isInitialized = false;

    [System.Serializable]
    public class DialogueResponse
    {
      public string response = "";
      public string emotion = "";
      public string action = "";
    }

    [System.Serializable]
    public class AnalysisResult
    {
      public string sentiment = "";
      public string[] topics = Array.Empty<string>();
      public string intent = "";
    }

    private void Start()
    {
      InitializeBrainAgent();
    }

    private void InitializeBrainAgent()
    {
      try
      {
        if (brainSettings == null)
        {
          Debug.LogError("BrainSettings not assigned!");
          return;
        }

        if (personaConfig == null)
        {
          Debug.LogError("PersonaConfig not assigned!");
          return;
        }

        // Create API client using BrainSettings properties
        var processConfig = brainSettings.ToProcessConfig();
        var apiClient = new ApiClient(
          processConfig.Host,
          processConfig.Port,
          processConfig.Model,
          processConfig.LlmConfig
        );

        // Create persona profile from config
        var profile = personaConfig.ToProfile();

        // Create brain agent
        brainAgent = new BrainAgent(profile, apiClient);

        isInitialized = true;
        Debug.Log("BrainAgent initialized successfully!");
      }
      catch (Exception ex)
      {
        Debug.LogError($"Failed to initialize BrainAgent: {ex.Message}");
      }
    }

    [ContextMenu("Test Structured Dialogue")]
    public async UniTaskVoid TestStructuredDialogue()
    {
      if (!isInitialized || brainAgent == null)
      {
        Debug.LogError("BrainAgent not initialized!");
        return;
      }

      try
      {
        Debug.Log($"Sending message: {testMessage}");

        // Send structured message using UniTask extension
        var jsonResponse = await brainAgent.SendStructuredMessageAsync(testMessage, dialogueResponseSchema);
        lastResponse = jsonResponse;

        Debug.Log($"Structured Response: {jsonResponse}");

        // Deserialize to typed object using UniTask extension
        var response = await brainAgent.SendStructuredMessageAsync<DialogueResponse>(testMessage, dialogueResponseSchema);
        if (response != null)
        {
          Debug.Log($"Parsed Response - Text: {response.response}, Emotion: {response.emotion}, Action: {response.action}");
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"Error in structured dialogue: {ex.Message}");
      }
    }

    [ContextMenu("Test Analysis")]
    public async UniTaskVoid TestAnalysis()
    {
      if (!isInitialized || brainAgent == null)
      {
        Debug.LogError("BrainAgent not initialized!");
        return;
      }

      try
      {
        var instruction = $"Analyze this message: '{testMessage}'";

        // Send structured instruction using UniTask extension
        var jsonResponse = await brainAgent.SendStructuredInstructionAsync(instruction, analysisSchema);
        lastAnalysis = jsonResponse;

        Debug.Log($"Analysis Response: {jsonResponse}");

        // Deserialize to typed object using UniTask extension
        var analysis = await brainAgent.SendStructuredInstructionAsync<AnalysisResult>(instruction, analysisSchema);
        if (analysis != null)
        {
          Debug.Log($"Parsed Analysis - Sentiment: {analysis.sentiment}, Topics: {string.Join(", ", analysis.topics)}, Intent: {analysis.intent}");
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"Error in analysis: {ex.Message}");
      }
    }

    [ContextMenu("Test Memory Storage")]
    public async UniTaskVoid TestMemoryStorage()
    {
      if (!isInitialized || brainAgent == null)
      {
        Debug.LogError("BrainAgent not initialized!");
        return;
      }

      try
      {
        var memorySchema = @"{
          ""type"": ""object"",
          ""properties"": {
            ""memory"": {
              ""type"": ""string"",
              ""description"": ""The memory to store""
            },
            ""category"": {
              ""type"": ""string"",
              ""enum"": [""conversation"", ""event"", ""fact"", ""emotion"", ""relationship""],
              ""description"": ""Category of the memory""
            },
            ""importance"": {
              ""type"": ""number"",
              ""minimum"": 1,
              ""maximum"": 10,
              ""description"": ""Importance level (1-10)""
            }
          },
          ""required"": [""memory"", ""category"", ""importance""]
        }";

        var instruction = $"Create a memory entry about this conversation: '{testMessage}'";

        var jsonResponse = await brainAgent.SendStructuredInstructionAsync(instruction, memorySchema);
        Debug.Log($"Memory Response: {jsonResponse}");

        // Add the memory to the agent's memory store
        brainAgent.AddMemory(jsonResponse);
        Debug.Log("Memory added to agent's memory store");
      }
      catch (Exception ex)
      {
        Debug.LogError($"Error in memory storage: {ex.Message}");
      }
    }

    [ContextMenu("Test Decision Making")]
    public async UniTaskVoid TestDecisionMaking()
    {
      if (!isInitialized || brainAgent == null)
      {
        Debug.LogError("BrainAgent not initialized!");
        return;
      }

      try
      {
        var decisionSchema = @"{
          ""type"": ""object"",
          ""properties"": {
            ""decision"": {
              ""type"": ""string"",
              ""description"": ""The decision made""
            },
            ""reasoning"": {
              ""type"": ""string"",
              ""description"": ""Why this decision was made""
            },
            ""confidence"": {
              ""type"": ""number"",
              ""minimum"": 0,
              ""maximum"": 1,
              ""description"": ""Confidence level (0-1)""
            }
          },
          ""required"": [""decision"", ""reasoning"", ""confidence""]
        }";

        var instruction = $"Make a decision about how to respond to: '{testMessage}'";

        var jsonResponse = await brainAgent.SendStructuredInstructionAsync(instruction, decisionSchema);
        Debug.Log($"Decision Response: {jsonResponse}");
      }
      catch (Exception ex)
      {
        Debug.LogError($"Error in decision making: {ex.Message}");
      }
    }

    [ContextMenu("Test Regular Dialogue")]
    public async UniTaskVoid TestRegularDialogue()
    {
      if (!isInitialized || brainAgent == null)
      {
        Debug.LogError("BrainAgent not initialized!");
        return;
      }

      try
      {
        Debug.Log($"Sending regular message: {testMessage}");

        // Send regular message using UniTask extension
        var response = await brainAgent.SendMessageAsync(testMessage);
        Debug.Log($"Regular Response: {response}");
      }
      catch (Exception ex)
      {
        Debug.LogError($"Error in regular dialogue: {ex.Message}");
      }
    }

    private void OnDestroy()
    {
      brainAgent?.Dispose();
    }
  }
}