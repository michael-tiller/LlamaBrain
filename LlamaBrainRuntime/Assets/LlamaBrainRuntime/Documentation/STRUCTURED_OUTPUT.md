# Structured Output Guide

LlamaBrain now supports perfect structured JSON output for reliable, parseable responses from your AI agents.

## Overview

Structured output ensures that your AI responses are always in a predictable JSON format, making them easy to parse and integrate into your applications. This is especially useful for:

- Game mechanics that need specific data fields
- Analysis and decision-making systems
- Memory storage with categorized data
- Multi-step reasoning processes

## Key Features

- **Schema Validation**: Define exact JSON schemas for responses
- **Type Safety**: Automatic deserialization to C# objects
- **Error Handling**: Robust validation and error recovery
- **Conversation Context**: Structured output with full conversation history
- **Memory Integration**: Structured memory storage and retrieval
- **Unity Integration**: UniTask support for better Unity performance

## Basic Usage

### 1. Simple Structured Response

```csharp
// Define your JSON schema
var schema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""response"": {
      ""type"": ""string"",
      ""description"": ""The character's response""
    },
    ""emotion"": {
      ""type"": ""string"",
      ""enum"": [""happy"", ""sad"", ""angry"", ""neutral""]
    }
  },
  ""required"": [""response""]
}";

// Send structured message (Unity with UniTask)
var jsonResponse = await brainAgent.SendStructuredMessageAsync("Hello!", schema);

// Or for non-Unity projects (regular Task)
var jsonResponse = await brainAgent.SendStructuredMessageAsync("Hello!", schema);
```

### 2. Typed Responses

```csharp
[System.Serializable]
public class DialogueResponse
{
    public string response;
    public string emotion;
    public string action;
}

// Get typed response directly (Unity with UniTask)
var response = await brainAgent.SendStructuredMessageAsync<DialogueResponse>("Hello!", schema);
Debug.Log($"Response: {response.response}, Emotion: {response.emotion}");

// Or for non-Unity projects (regular Task)
var response = await brainAgent.SendStructuredMessageAsync<DialogueResponse>("Hello!", schema);
```

### 3. Instruction-Based Structured Output

```csharp
var instruction = "Analyze the sentiment of this message";
var analysisSchema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""sentiment"": {
      ""type"": ""string"",
      ""enum"": [""positive"", ""negative"", ""neutral""]
    },
    ""confidence"": {
      ""type"": ""number"",
      ""minimum"": 0,
      ""maximum"": 1
    }
  },
  ""required"": [""sentiment"", ""confidence""]
}";

// Unity with UniTask
var analysis = await brainAgent.SendStructuredInstructionAsync<AnalysisResult>(instruction, analysisSchema);

// Non-Unity with regular Task
var analysis = await brainAgent.SendStructuredInstructionAsync<AnalysisResult>(instruction, analysisSchema);
```

## Unity-Specific Usage

For Unity projects, use the extension methods that provide UniTask support:

```csharp
using LlamaBrain.Unity.Runtime.Core;

// These methods automatically use UniTask for better Unity performance
var response = await brainAgent.SendStructuredMessageAsync<DialogueResponse>(message, schema);
var analysis = await brainAgent.SendStructuredInstructionAsync<AnalysisResult>(instruction, schema);
var regularResponse = await brainAgent.SendMessageAsync(message);
```

### Unity Context Menu Testing

```csharp
[ContextMenu("Test Structured Dialogue")]
public async UniTaskVoid TestStructuredDialogue()
{
    var response = await brainAgent.SendStructuredMessageAsync<DialogueResponse>(testMessage, dialogueResponseSchema);
    Debug.Log($"Response: {response.response}, Emotion: {response.emotion}");
}
```

## Schema Design Best Practices

### 1. Use Clear Descriptions

```json
{
  "type": "object",
  "properties": {
    "decision": {
      "type": "string",
      "description": "The decision made by the character"
    },
    "reasoning": {
      "type": "string", 
      "description": "Why this decision was made"
    }
  }
}
```

### 2. Use Enums for Constrained Values

```json
{
  "type": "object",
  "properties": {
    "action": {
      "type": "string",
      "enum": ["attack", "defend", "flee", "negotiate"],
      "description": "The action to take"
    }
  }
}
```

### 3. Set Appropriate Constraints

```json
{
  "type": "object",
  "properties": {
    "confidence": {
      "type": "number",
      "minimum": 0,
      "maximum": 1,
      "description": "Confidence level (0-1)"
    },
    "priority": {
      "type": "integer",
      "minimum": 1,
      "maximum": 10,
      "description": "Priority level (1-10)"
    }
  }
}
```

### 4. Make Required Fields Explicit

```json
{
  "type": "object",
  "properties": {
    "response": { "type": "string" },
    "emotion": { "type": "string" },
    "optional_note": { "type": "string" }
  },
  "required": ["response", "emotion"]
}
```

## Common Use Cases

### 1. Dialogue Responses

```csharp
var dialogueSchema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""response"": {
      ""type"": ""string"",
      ""description"": ""The character's response to the player""
    },
    ""emotion"": {
      ""type"": ""string"",
      ""enum"": [""happy"", ""sad"", ""angry"", ""neutral"", ""excited"", ""worried""]
    },
    ""action"": {
      ""type"": ""string"",
      ""description"": ""Any physical action the character takes""
    }
  },
  ""required"": [""response""]
}";

// Unity with UniTask
var response = await brainAgent.SendStructuredMessageAsync<DialogueResponse>(playerMessage, dialogueSchema);

// Non-Unity with regular Task
var response = await brainAgent.SendStructuredMessageAsync<DialogueResponse>(playerMessage, dialogueSchema);
```

### 2. Decision Making

```csharp
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
      ""maximum"": 1
    },
    ""alternatives"": {
      ""type"": ""array"",
      ""items"": { ""type"": ""string"" }
    }
  },
  ""required"": [""decision"", ""reasoning"", ""confidence""]
}";
```

### 3. Memory Storage

```csharp
var memorySchema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""memory"": {
      ""type"": ""string"",
      ""description"": ""The memory to store""
    },
    ""category"": {
      ""type"": ""string"",
      ""enum"": [""conversation"", ""event"", ""fact"", ""emotion"", ""relationship""]
    },
    ""importance"": {
      ""type"": ""number"",
      ""minimum"": 1,
      ""maximum"": 10
    },
    ""tags"": {
      ""type"": ""array"",
      ""items"": { ""type"": ""string"" }
    }
  },
  ""required"": [""memory"", ""category"", ""importance""]
}";
```

### 4. Analysis and Sentiment

```csharp
var analysisSchema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""sentiment"": {
      ""type"": ""string"",
      ""enum"": [""positive"", ""negative"", ""neutral""]
    },
    ""topics"": {
      ""type"": ""array"",
      ""items"": { ""type"": ""string"" }
    },
    ""intent"": {
      ""type"": ""string"",
      ""description"": ""Player's likely intent""
    }
  },
  ""required"": [""sentiment"", ""topics""]
}";
```

## Error Handling

The structured output system includes robust error handling:

```csharp
try
{
    // Unity with UniTask
    var response = await brainAgent.SendStructuredMessageAsync<MyResponse>(message, schema);
    // Process response
}
catch (InvalidOperationException ex)
{
    // Handle JSON validation errors
    Debug.LogError($"Invalid JSON response: {ex.Message}");
}
catch (JsonException ex)
{
    // Handle JSON parsing errors
    Debug.LogError($"JSON parsing error: {ex.Message}");
}
catch (Exception ex)
{
    // Handle other errors
    Debug.LogError($"Unexpected error: {ex.Message}");
}
```

## Performance Considerations

### 1. Schema Complexity
- Keep schemas as simple as possible
- Avoid deeply nested objects when not necessary
- Use enums instead of free-form strings when possible

### 2. Response Size
- Set appropriate `maxTokens` in your LLM config
- Use compact schemas for high-frequency interactions
- Consider splitting complex responses into multiple calls

### 3. Caching
- Cache frequently used schemas
- Reuse schema objects instead of creating new strings
- Consider pre-compiling common response types

### 4. Unity Performance
- Use UniTask for better Unity integration
- Avoid blocking the main thread with `.Result` or `.Wait()`
- Use `UniTaskVoid` for fire-and-forget operations

## Integration with Unity

### 1. Inspector Configuration

```csharp
[Header("Structured Output Schemas")]
[TextArea(5, 10)]
[SerializeField] private string dialogueResponseSchema;

[TextArea(5, 10)]
[SerializeField] private string analysisSchema;
```

### 2. Context Menu Testing

```csharp
[ContextMenu("Test Structured Dialogue")]
public async UniTaskVoid TestStructuredDialogue()
{
    var response = await brainAgent.SendStructuredMessageAsync<DialogueResponse>(testMessage, dialogueResponseSchema);
    Debug.Log($"Response: {response.response}, Emotion: {response.emotion}");
}
```

### 3. Event-Driven Responses

```csharp
public async UniTaskVoid OnPlayerMessage(string message)
{
    var response = await brainAgent.SendStructuredMessageAsync<DialogueResponse>(message, dialogueResponseSchema);
    
    // Trigger Unity events based on structured data
    if (response.emotion == "angry")
    {
        OnAngryResponse?.Invoke(response);
    }
    else if (response.emotion == "happy")
    {
        OnHappyResponse?.Invoke(response);
    }
}
```

## Advanced Patterns

### 1. Multi-Step Reasoning

```csharp
// Step 1: Analyze the situation
var analysis = await brainAgent.SendStructuredInstructionAsync<AnalysisResult>(
    "Analyze this situation", analysisSchema);

// Step 2: Make a decision based on analysis
var decision = await brainAgent.SendStructuredInstructionAsync<DecisionResult>(
    $"Based on analysis: {JsonConvert.SerializeObject(analysis)}, make a decision", decisionSchema);

// Step 3: Generate response based on decision
var response = await brainAgent.SendStructuredInstructionAsync<DialogueResponse>(
    $"Based on decision: {decision.decision}, generate a response", responseSchema);
```

### 2. Conditional Logic

```csharp
var response = await brainAgent.SendStructuredMessageAsync<DialogueResponse>(message, schema);

switch (response.emotion)
{
    case "angry":
        // Handle angry response
        break;
    case "happy":
        // Handle happy response
        break;
    default:
        // Handle neutral response
        break;
}
```

### 3. Memory Integration

```csharp
// Create structured memory
var memory = await brainAgent.SendStructuredInstructionAsync<MemoryEntry>(
    "Create a memory about this conversation", memorySchema);

// Add to agent's memory store
brainAgent.AddMemory(JsonConvert.SerializeObject(memory));
```

## Troubleshooting

### Common Issues

1. **Invalid JSON Response**
   - Check that your schema is valid JSON
   - Ensure the LLM has enough tokens to complete the response
   - Try simplifying the schema

2. **Missing Required Fields**
   - Verify that all required fields are clearly described
   - Consider making some fields optional if the LLM struggles

3. **Enum Values Not Respected**
   - Make sure enum values are clearly defined
   - Provide examples in the schema description
   - Consider using a more specific model

4. **Response Too Long**
   - Reduce the `maxTokens` setting
   - Simplify the schema
   - Split complex responses into multiple calls

5. **Unity Performance Issues**
   - Use UniTask instead of Task for Unity projects
   - Avoid blocking the main thread
   - Use `UniTaskVoid` for fire-and-forget operations

### Debug Tips

1. **Log Raw Responses**
   ```csharp
   var rawResponse = await brainAgent.SendStructuredMessageAsync(message, schema);
   Debug.Log($"Raw response: {rawResponse}");
   ```

2. **Validate Schema**
   ```csharp
   if (!JsonUtils.IsValidJson(schema))
   {
       Debug.LogError("Invalid schema JSON");
   }
   ```

3. **Test with Simple Schema**
   ```csharp
   var simpleSchema = @"{
     ""type"": ""object"",
     ""properties"": {
       ""response"": { ""type"": ""string"" }
     },
     ""required"": [""response""]
   }";
   ```

## Conclusion

Structured output provides a reliable way to get predictable, parseable responses from your AI agents. By following these guidelines and best practices, you can create robust systems that integrate seamlessly with your Unity applications.

For Unity projects, use the UniTask extension methods for better performance and integration. For non-Unity projects, use the standard Task-based methods.

For more examples and advanced usage patterns, see the `StructuredOutputExample.cs` demo script included with the package. 