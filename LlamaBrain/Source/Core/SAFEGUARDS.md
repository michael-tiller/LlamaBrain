# LlamaBrain API Client Safeguards

This document outlines the comprehensive safeguards implemented in the LlamaBrain API client to ensure security, stability, and reliability.

## Overview

The LlamaBrain codebase has been enhanced with multiple layers of protection across all critical areas to prevent common issues and security vulnerabilities.

## Safeguards Implemented

### 1. API Client Safeguards (`ApiClient.cs`)

#### Input Validation
- **Host validation**: Ensures host is not null, empty, or whitespace
- **Port validation**: Ensures port is within valid range (1-65535)
- **Model validation**: Ensures model name is not null, empty, or whitespace
- **Prompt validation**: Prevents sending empty prompts, limits length to 10,000 characters
- **Parameter validation**: Clamps all LLM parameters to safe ranges

#### Rate Limiting
- **Maximum requests**: 60 requests per minute with sliding window
- **Thread-safe implementation**: Uses `SemaphoreSlim` and `ConcurrentQueue`
- **Automatic cleanup**: Request history cleaned up automatically
- **Graceful queuing**: Requests delayed when limits exceeded

#### Timeout & Error Handling
- **Request timeout**: 30 seconds per request
- **Cancellation support**: Supports `CancellationToken` for manual cancellation
- **Comprehensive exception handling**: Categorizes different error types
- **Graceful degradation**: Returns descriptive error messages

#### Resource Management
- **IDisposable implementation**: Proper cleanup of HTTP client and semaphores
- **Disposed state tracking**: Prevents usage after disposal
- **Memory management**: Efficient collections and automatic cleanup

#### Security Measures
- **Input sanitization**: Removes dangerous characters from hosts and prompts
- **Response validation**: Limits response length to 50,000 characters
- **JSON validation**: Validates response format before processing

### 2. Configuration Safeguards (`LlmConfig.cs`)

#### Property-Level Validation
- **Automatic clamping**: All properties validate and clamp values on assignment
- **Safe ranges**: Values automatically constrained to safe ranges
- **Validation methods**: `Validate()` method for comprehensive validation
- **Clone support**: Safe copying of configuration objects

#### Safe Defaults
- All configuration values have safe default values
- Validation prevents dangerous configurations
- Automatic correction of out-of-range values

### 3. File System Safeguards

#### PersonaProfileManager (`PersonaProfileManager.cs`)
- **Path validation**: Validates and sanitizes all file paths
- **File size limits**: Maximum 1MB per profile file
- **JSON size limits**: Maximum 500KB JSON string length
- **Atomic writes**: Uses temporary files for safe writes
- **Path traversal prevention**: Blocks `../` and other traversal attempts
- **Persona ID sanitization**: Removes dangerous characters from IDs
- **File count limits**: Maximum 1,000 profiles loaded at once
- **Comprehensive error handling**: Graceful handling of file operations

#### PersonaMemoryFileStore (`PersonaMemoryFileStore.cs`)
- **Memory size limits**: Maximum 5MB per memory file
- **JSON size limits**: Maximum 2MB JSON string length
- **Memory entry limits**: Maximum 10,000 entries per persona
- **Entry validation**: Validates individual memory entries (max 10KB each)
- **Atomic operations**: Safe file writes with temporary files
- **Path security**: Validates all file paths and prevents traversal
- **Memory truncation**: Automatically truncates oversized memories

### 4. Process Management Safeguards (`ServerManager.cs`)

#### Process Security
- **Executable validation**: Validates executable paths and extensions
- **Model validation**: Validates model file paths and extensions
- **Argument sanitization**: Removes dangerous characters from arguments
- **Path traversal prevention**: Blocks path traversal in all paths
- **Working directory**: Sets safe working directory for processes

#### Process Control
- **Startup validation**: Verifies process starts successfully
- **Graceful shutdown**: Attempts graceful shutdown before force kill
- **Timeout management**: Configurable timeouts for startup/shutdown
- **Output sanitization**: Sanitizes process output before logging
- **Resource cleanup**: Proper disposal of process resources

#### Configuration Validation
- **Port validation**: Ensures port is within valid range (1-65535)
- **Context size validation**: Limits context size to 32,768
- **Path length limits**: Maximum 260 characters for paths
- **Argument length limits**: Maximum 1,000 characters for arguments

### 5. JSON Operation Safeguards (`JsonUtils.cs`)

#### Serialization Safety
- **Size limits**: Maximum 1MB JSON string length
- **Null validation**: Validates objects before serialization
- **Error handling**: Comprehensive exception handling
- **Formatting support**: Safe serialization with formatting options

#### Deserialization Safety
- **Size validation**: Validates JSON string size before parsing
- **Depth limits**: Maximum 64 levels of object depth
- **Security settings**: Disables type name handling for security
- **Error recovery**: Graceful handling of deserialization errors
- **Safe deserialization**: Fallback methods for failed deserialization

#### JSON Utilities
- **Validation methods**: `IsValidJson()` for format validation
- **Size calculation**: `GetJsonSize()` for byte size calculation
- **Truncation support**: `TruncateJson()` for oversized JSON
- **Safe parsing**: Methods that don't throw exceptions

### 6. Path Operation Safeguards (`PathUtils.cs`)

#### Path Validation
- **Length limits**: Maximum 260 characters for paths, 255 for filenames
- **Depth limits**: Maximum 32 directory levels
- **Traversal prevention**: Blocks `../` and other traversal patterns
- **Character validation**: Removes invalid path characters
- **Security checks**: Validates paths are within allowed directories

#### Path Utilities
- **Safe combination**: `CombinePath()` with full validation
- **Relative path handling**: Safe relative path operations
- **Filename sanitization**: `CreateSafeFilename()` for safe filenames
- **Path safety checks**: `IsPathSafe()` for validation
- **Directory containment**: `IsPathWithinDirectory()` for security

## Configuration Constants

### API Client
| Constant | Value | Purpose |
|----------|-------|---------|
| `MaxRequestsPerMinute` | 60 | Rate limiting threshold |
| `MaxPromptLength` | 10,000 | Maximum prompt characters |
| `MaxResponseLength` | 50,000 | Maximum response characters |
| `RequestTimeoutSeconds` | 30 | Request timeout in seconds |

### File Operations
| Constant | Value | Purpose |
|----------|-------|---------|
| `MaxFileSizeBytes` | 1MB/5MB | Maximum file size (profiles/memory) |
| `MaxJsonLength` | 500KB/2MB | Maximum JSON length (profiles/memory) |
| `MaxProfilesToLoad` | 1,000 | Maximum profiles to load |
| `MaxMemoryEntries` | 10,000 | Maximum memory entries per persona |

### Process Management
| Constant | Value | Purpose |
|----------|-------|---------|
| `MaxStartupTimeoutSeconds` | 30 | Process startup timeout |
| `MaxShutdownTimeoutSeconds` | 10 | Process shutdown timeout |
| `MaxExecutablePathLength` | 260 | Maximum executable path length |
| `MaxArgumentsLength` | 1,000 | Maximum arguments length |

### JSON Operations
| Constant | Value | Purpose |
|----------|-------|---------|
| `MaxJsonLength` | 1MB | Maximum JSON string length |
| `MaxDepth` | 64 | Maximum object depth |
| `MaxProperties` | 1,000 | Maximum properties per object |

### Path Operations
| Constant | Value | Purpose |
|----------|-------|---------|
| `MaxPathLength` | 260 | Maximum path length |
| `MaxFilenameLength` | 255 | Maximum filename length |
| `MaxDirectoryDepth` | 32 | Maximum directory depth |

## Usage Examples

### Safe File Operations
```csharp
var manager = new PersonaProfileManager("C:/Profiles");
var profile = PersonaProfile.Create("safe-id", "Safe Profile");
manager.SaveProfile(profile); // All safeguards applied automatically
```

### Safe Process Management
```csharp
var config = new ProcessConfig
{
    ExecutablePath = "C:/llama-server.exe",
    Model = "C:/models/llama-2.gguf",
    Port = 8080,
    ContextSize = 2048
};
var server = new ServerManager(config);
server.StartServer(); // All security checks applied
```

### Safe JSON Operations
```csharp
var json = JsonUtils.Serialize(profile); // Size and validation checks
var profile = JsonUtils.Deserialize<PersonaProfile>(json); // Safe deserialization
```

### Safe Path Operations
```csharp
var safePath = PathUtils.CombinePath("C:/Base", "relative/path"); // Full validation
var safeFilename = PathUtils.CreateSafeFilename("unsafe<>filename"); // Sanitization
```

## Best Practices

1. **Always use the provided utility classes** for file, JSON, and path operations
2. **Validate configurations** before using them in production
3. **Handle exceptions gracefully** and log errors appropriately
4. **Use appropriate timeouts** for long-running operations
5. **Monitor resource usage** and implement cleanup
6. **Test with malicious inputs** to verify safeguards work
7. **Keep dependencies updated** to avoid security vulnerabilities

## Security Considerations

- **Path traversal prevention**: All file operations prevent `../` attacks
- **Input sanitization**: All user inputs are sanitized before use
- **Resource limits**: File sizes, memory usage, and process limits enforced
- **Process isolation**: Process execution with proper security settings
- **JSON injection prevention**: Safe JSON handling with size and depth limits
- **Memory exhaustion prevention**: Limits on collections and data structures

## Performance Considerations

- **Rate limiting**: May impact high-frequency applications
- **File size limits**: May affect applications with large data
- **Validation overhead**: Minimal performance impact from safeguards
- **Resource cleanup**: Automatic cleanup prevents memory leaks
- **Caching**: Consider implementing caching for frequently accessed data

## Testing Safeguards

### Unit Tests
- Test with malicious inputs (path traversal, oversized data)
- Test boundary conditions (maximum sizes, edge cases)
- Test error conditions (network failures, file system errors)
- Test resource cleanup (disposal, memory management)

### Integration Tests
- Test end-to-end workflows with safeguards
- Test performance under load with rate limiting
- Test error recovery and graceful degradation
- Test security scenarios (malicious files, invalid JSON)

## Monitoring and Logging

- **Error logging**: All safeguards log errors and warnings
- **Performance monitoring**: Track resource usage and timing
- **Security events**: Log suspicious activities and violations
- **Health checks**: Monitor system health and resource availability 