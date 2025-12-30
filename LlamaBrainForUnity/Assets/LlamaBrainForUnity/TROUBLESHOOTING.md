# LlamaBrain for Unity Troubleshooting Guide

This guide helps you resolve common issues when using LlamaBrain for Unity.

## 🚨 Common Issues

### Server Issues

#### Server Won't Start
**Symptoms**: BrainServer fails to initialize or start

**Possible Causes:**
- Incorrect executable path
- Missing or invalid model file
- Port already in use
- Insufficient permissions
- Invalid configuration

**Solutions:**
1. **Check Executable Path**
   ```
   - Verify llama-server.exe exists at the specified path
   - Ensure the path uses forward slashes (/) or escaped backslashes (\\)
   - Check file permissions
   ```

2. **Verify Model File**
   ```
   - Ensure the GGUF model file exists
   - Check file permissions
   - Verify the model is compatible with your llama.cpp version
   ```

3. **Check Port Availability**
   ```
   - Ensure port 5000 (default) is not in use
   - Try a different port in BrainSettings
   - Check firewall settings
   ```

4. **Review Configuration**
   ```
   - Verify all BrainSettings fields are set
   - Check for invalid characters in paths
   - Ensure context size is reasonable (2048-4096 recommended)
   ```

#### Server Starts But No Response
**Symptoms**: Server starts successfully but API calls fail

**Possible Causes:**
- Server not fully initialized
- Network connectivity issues
- Model loading problems
- Configuration mismatch

**Solutions:**
1. **Wait for Initialization**
   ```
   - Server takes 2-3 seconds to fully start
   - Check server logs for initialization messages
   - Wait for "Server ready" message
   ```

2. **Check Network**
   ```
   - Verify localhost:5000 is accessible
   - Test with curl: curl -X POST http://localhost:5000/completion
   - Check firewall settings
   ```

3. **Review Server Logs**
   ```
   - Check Unity Console for error messages
   - Look for model loading errors
   - Verify server process is running
   ```

### API Client Issues

#### Rate Limiting Errors
**Symptoms**: "Rate limit exceeded" or slow responses

**Solutions:**
1. **Reduce Request Frequency**
   ```
   - Limit to 60 requests per minute
   - Implement request queuing
   - Add delays between requests
   ```

2. **Optimize Usage**
   ```
   - Cache responses where possible
   - Batch requests when possible
   - Use appropriate timeouts
   ```

#### Timeout Errors
**Symptoms**: Requests timeout after 30 seconds

**Solutions:**
1. **Reduce Context Size**
   ```
   - Lower context size in BrainSettings
   - Use smaller models
   - Limit conversation history
   ```

2. **Optimize Prompts**
   ```
   - Shorter, more focused prompts
   - Reduce max tokens
   - Use appropriate temperature settings
   ```

3. **Check System Resources**
   ```
   - Ensure sufficient RAM
   - Check CPU usage
   - Close unnecessary applications
   ```

### Unity Integration Issues

#### UnityBrainAgent Not Responding
**Symptoms**: Agent exists but doesn't respond to messages

**Possible Causes:**
- Agent not initialized
- Missing client or memory provider
- Configuration issues

**Solutions:**
1. **Check Initialization**
   ```csharp
   // Ensure agent is properly initialized
   brainAgent.Initialize(client, memoryProvider);
   ```

2. **Verify Components**
   ```csharp
   // Check all required components
   if (brainAgent.RuntimeProfile == null)
   {
       brainAgent.ConvertConfigToProfile();
   }
   ```

3. **Review Configuration**
   ```
   - Verify PersonaConfig is assigned
   - Check PromptComposerSettings
   - Ensure BrainServer is running
   ```

#### NPC Responses Include Personality Traits
**Symptoms**: NPC responses include trait information like "Your personality traits: - Knowledgeable: Has extensive knowledge..."

**Solutions:**
1. **Disable Trait Inclusion**
   ```
   - Set PromptComposerSettings.includePersonalityTraits = false
   - This completely removes traits from prompts
   ```

2. **Use Trait Instructions**
   ```
   - Keep includePersonalityTraits = true
   - The system automatically adds instructions to prevent trait mention
   - This provides context while preventing pollution
   ```

3. **Check Trait Configuration**
   ```
   - Verify PersonaTrait.IncludeInPrompts settings
   - Review PersonaTraitAssignment.ShouldIncludeInPrompts()
   - Ensure traits are properly configured
   ```

#### UI Issues
**Symptoms**: Dialogue panel not working or displaying incorrectly

**Solutions:**
1. **Check Prefab References**
   ```
   - Verify all UI prefabs are assigned
   - Check for missing components
   - Ensure event connections are set up
   ```

2. **Review Event Setup**
   ```csharp
   // Ensure events are properly connected
   dialoguePanel.onPlayerMessageSubmitted.AddListener(HandleMessage);
   ```

3. **Check Canvas Settings**
   ```
   - Verify Canvas is set to Screen Space - Overlay
   - Check Canvas Scaler settings
   - Ensure UI elements are visible
   ```

### Memory and Performance Issues

#### High Memory Usage
**Symptoms**: Application uses excessive memory

**Solutions:**
1. **Limit Conversation History**
   ```csharp
   // Clear history periodically
   brainAgent.ClearDialogueHistory();
   ```

2. **Optimize Memory Settings**
   ```
   - Reduce max memory entries (default: 10,000)
   - Limit memory file size (default: 5MB)
   - Implement memory cleanup
   ```

3. **Monitor Usage**
   ```csharp
   // Check memory usage
   var memories = brainAgent.Memories;
   Debug.Log($"Memory entries: {memories.Split('\n').Length}");
   ```

#### Slow Performance
**Symptoms**: Slow response times or frame drops

**Solutions:**
1. **Enable GPU Acceleration (CRITICAL)**
   ```
   - Set GpuLayers to 35+ in BrainSettings (requires CUDA-enabled llama.cpp)
   - Without GPU: Expect 5-10 tokens/sec (very slow)
   - With GPU (RTX 4070 Ti): Expect 100-200 tokens/sec (fast)
   - Check Unity Console for "[llama-server] load_tensors: offloaded X/X layers to GPU"
   ```

2. **Optimize HttpClient Configuration**
   ```
   - The system automatically uses optimized settings:
     * UseProxy = false (disables Windows proxy detection - saves 2+ seconds!)
     * ExpectContinue = false (removes HTTP round-trip delay)
     * Uses 127.0.0.1 instead of localhost (avoids DNS resolution)
   - These optimizations reduce latency from ~2200ms to ~200ms
   ```

3. **Configure llama.cpp Server Settings**
   ```
   - Parallel Slots: Set to 1 for lowest latency (default)
     * 1 slot = ~200ms latency (best for single NPC)
     * 4 slots = ~2000ms latency (better for multiple concurrent NPCs)
   - Batch Size: 512 (default, good for most GPUs)
   - UBatch Size: 128 (default, good for token generation)
   - Use Mlock: Enabled (prevents swapping, improves stability)
   ```

4. **Optimize Model Settings**
   ```
   - Use smaller models for real-time applications (3B-7B parameters)
   - Reduce context size (384-1024 for NPCs, 2048-4096 for complex dialogue)
   - Set appropriate max tokens:
     * Barks: 8-12 tokens (~50-100ms with GPU)
     * Normal replies: 20-30 tokens (~150-250ms with GPU)
     * Dialogue screen: 30-50 tokens (~250-400ms with GPU)
   - Adjust temperature and top-p for faster generation
   ```

5. **Monitor Performance Metrics**
   ```
   - Check Unity Console for performance logs:
     * TTFT (Time To First Token): Should be <50ms with GPU
     * Decode Speed: Should be 100-200 tps with GPU, <10 tps indicates CPU-only
     * Total Wall Time: Should be <300ms for typical NPC responses
   - Warning logs appear if decode speed <50 tps (suggests GPU not being used)
   ```

6. **Implement Caching**
   ```csharp
   // Cache common responses
   private Dictionary<string, string> responseCache = new();
   ```

7. **Use Async Operations**
   ```csharp
   // Don't block the main thread
   var response = await brainAgent.SendPlayerInputAsync(message);
   ```

#### High Latency (>2 seconds per request)
**Symptoms**: Requests take 2+ seconds even with GPU acceleration

**Possible Causes:**
- Windows proxy detection enabled (adds 2+ second delay)
- Too many parallel slots (scheduling overhead)
- CPU-only inference (GPU not being used)
- Network/DNS resolution delays

**Solutions:**
1. **Verify HttpClient Optimizations**
   ```
   - Check that UseProxy = false is set (automatic in latest version)
   - Verify 127.0.0.1 is used instead of localhost (automatic)
   - Ensure ExpectContinue = false (automatic)
   ```

2. **Check GPU Usage**
   ```
   - Look for "[llama-server] load_tensors: offloaded X/X layers to GPU" in logs
   - If you see "CPU only" or decode speed <50 tps, GPU is not being used
   - Verify CUDA-enabled llama.cpp build
   - Check GpuLayers setting (should be 35+)
   ```

3. **Reduce Parallel Slots**
   ```
   - Set ParallelSlots = 1 in BrainSettings (default)
   - Only increase if you need multiple concurrent NPCs
   ```

4. **Test Direct Server Access**
   ```powershell
   # Test if server itself is fast (should be <100ms)
   $body = @{ prompt = "Hello"; n_predict = 10 } | ConvertTo-Json
   Measure-Command { Invoke-RestMethod -Uri "http://127.0.0.1:5000/completion" -Method POST -Body $body -ContentType "application/json" }
   ```
   - If PowerShell test is fast (<100ms) but Unity is slow (>2000ms), check HttpClient configuration
   - If PowerShell test is also slow, check llama.cpp server settings

#### Low Decode Speed (<50 tokens/sec)
**Symptoms**: Performance metrics show low decode speed despite GPU

**Solutions:**
1. **Verify GPU Offload**
   ```
   - Check server startup logs for "offloaded X/X layers to GPU"
   - Ensure GpuLayers = 35+ (or -1 for all layers)
   - Verify CUDA-enabled llama.cpp build
   ```

2. **Check Model Quantization**
   ```
   - Q4_0 or Q4_K_M quantizations are good balance of speed/quality
   - Q8_0 or F16 are slower but higher quality
   - Use Q4_0 for real-time NPC dialogue
   ```

3. **Optimize Batch Sizes**
   ```
   - BatchSize: 512 (good default for most GPUs)
   - UBatchSize: 128 (good default for token generation)
   - Lower values = less VRAM usage but potentially slower
   ```

4. **Check System Resources**
   ```
   - Ensure GPU has sufficient VRAM (3B model needs ~2GB, 7B needs ~4GB)
   - Close other GPU-intensive applications
   - Check GPU temperature/throttling
   ```

### Configuration Issues

#### Invalid Settings
**Symptoms**: Configuration errors or validation failures

**Solutions:**
1. **Check Parameter Ranges**
   ```
   - MaxTokens: 1-2048
   - Temperature: 0.0-2.0
   - TopP: 0.0-1.0
   - TopK: 1-100
   - RepeatPenalty: 0.0-2.0
   ```

2. **Validate Paths**
   ```
   - Use absolute paths
   - Check for invalid characters
   - Ensure paths exist
   ```

3. **Review File Sizes**
   ```
   - Profile files: max 1MB
   - Memory files: max 5MB
   - JSON strings: max 500KB (profiles) / 2MB (memory)
   ```

## 🔧 Debugging Tools

### Unity Console
Check the Unity Console for error messages and warnings:
```
- Look for [LLM] prefixed messages
- Check for validation errors
- Monitor server status messages
```

### Server Logs
Enable detailed logging to debug server issues:
```csharp
// Add debug logging
Debug.Log($"[LLM] Server status: {server.IsInitialized}");
Debug.Log($"[LLM] Client created: {client != null}");
```

### Configuration Validation
Validate your configuration before use:
```csharp
// Check BrainSettings
var config = brainSettings.ToProcessConfig();
if (config == null)
{
    Debug.LogError("Invalid BrainSettings configuration");
}
```

## 📋 Checklist

### Before Starting
- [ ] llama.cpp server executable downloaded
- [ ] Compatible GGUF model file available
- [ ] Unity 2022.3 LTS or higher
- [ ] .NET Standard 2.1 support enabled

### Server Setup
- [ ] Executable path correctly set
- [ ] Model file path correctly set
- [ ] Port 5000 available
- [ ] Sufficient system resources

### Unity Configuration
- [ ] BrainSettings asset created and configured
- [ ] PersonaConfig assets created
- [ ] UnityBrainAgent components added
- [ ] UI prefabs assigned

### Testing
- [ ] Server starts without errors
- [ ] API client can connect
- [ ] Agent responds to messages
- [ ] UI displays correctly

## 🆘 Getting Help

### Before Asking for Help
1. **Check this troubleshooting guide**
2. **Review the main README**
3. **Check the SAFEGUARDS.md**
4. **Test with sample scenes**

### When Reporting Issues
Include the following information:
- Unity version
- LlamaBrain version
- Error messages (exact text)
- Steps to reproduce
- System specifications
- Configuration files (sanitized)

### Useful Commands
```bash
# Test server connectivity
curl -X POST http://localhost:5000/completion

# Check process status
tasklist | findstr llama-server

# Test model file
# (Use llama.cpp tools to validate GGUF file)
```

## 📚 Additional Resources

- [Main README](LLAMABRAINFORUNITY.md) - Complete documentation
- [Samples Guide](SAMPLES.md) - Sample scene explanations
- [Security Guide](../../../LlamaBrain/Source/Core/SAFEGUARDS.md) - Security information
- [llama.cpp Documentation](https://github.com/ggerganov/llama.cpp) - Server documentation

---

**Note**: Most issues can be resolved by following this guide. If you continue to have problems, ensure you're using the latest version of LlamaBrain and have properly configured your llama.cpp server. 