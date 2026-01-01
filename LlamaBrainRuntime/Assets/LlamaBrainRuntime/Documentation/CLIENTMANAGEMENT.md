# Client Management Sample

This sample demonstrates the client management features of LlamaBrain for Unity, showing how to monitor and control the connection to the llama.cpp server.

## 🔌 Features Demonstrated

### Server Health Monitoring
- **IsRunningAsync()**: Check if the server is currently running and responding
- **Connection Status**: Real-time monitoring of server connection state
- **Health Checks**: Manual and automatic server health verification
- **Status Indicators**: Visual indicators for connection status

### Server Control
- **WaitForServerAsync()**: Wait for server to be ready with configurable timeout
- **RestartServer()**: Force restart the llama.cpp server
- **Server Startup**: Monitor server startup time and attempts
- **Connection Management**: Handle connection failures and timeouts

### Status Information
- **Connection Status**: Current connection state (Connected, Not Responding, etc.)
- **Server Startup Time**: Time taken for server to become ready
- **Connection Attempts**: Number of connection attempts made
- **Error Messages**: Detailed error information for troubleshooting
- **Detailed Status**: Comprehensive server status information

### UI Features
- **TextMeshPro Integration**: Modern UI with TMP components
- **Real-time Updates**: Auto-refresh server status displays
- **Interactive Controls**: Buttons for health checks and server control
- **Status Indicators**: Color-coded status indicators
- **Configurable Timeouts**: Adjustable timeout settings
- **Debug Tools**: Context menu tools for development

## 🎮 How to Use

### Setup
1. Open the `ClientManagement.unity` scene
2. Configure the BrainServer with your llama.cpp settings
3. Set up the UI references in the ClientManagerUI component
4. Ensure TextMeshPro is installed for UI components

### Basic Usage
1. **Monitor Status**: Watch the real-time connection status and indicators
2. **Check Health**: Use the "Check Health" button to verify server status
3. **Wait for Server**: Use the "Wait for Server" button to wait for server readiness
4. **Restart Server**: Use the "Restart Server" button to force a server restart
5. **View Details**: Monitor startup time, connection attempts, and error messages

### Advanced Usage
- **Custom Timeouts**: Adjust timeout values for different scenarios
- **Status Monitoring**: Use the detailed status information for debugging
- **Server Control**: Manage server lifecycle during development
- **Error Handling**: Monitor and respond to connection issues

## 🔧 Implementation Details

### BrainServer Extensions
The sample adds these methods to BrainServer:

```csharp
// Server Health
public async Task<bool> IsServerRunningAsync()
public bool IsServerRunning { get; }
public string ConnectionStatus { get; }
public string LastErrorMessage { get; }

// Server Control
public async Task<bool> WaitForServerAsync(int timeoutSeconds = 30)
public void RestartServer()
public void CheckServerHealth()

// Status Information
public float ServerStartupTime { get; }
public int ConnectionAttempts { get; }
public string GetServerStatus()
```

### ClientManagerUI Component
An advanced UI component that provides:
- TextMeshPro integration for modern UI
- Real-time server status monitoring
- Interactive server control buttons
- Color-coded status indicators
- Configurable timeout settings
- Error message display
- Debug tools via context menu
- Auto-update functionality

## 📊 Connection States

### Status Types
- **Connected**: Server is running and responding
- **Not Responding**: Server is not responding to health checks
- **Checking**: Currently checking server status
- **Waiting**: Waiting for server to become ready
- **Timeout**: Server startup timed out
- **Connection Failed**: Failed to establish connection
- **Restarting**: Server is being restarted
- **Not Initialized**: BrainServer not properly initialized

### Status Indicators
- **Green**: Server is connected and healthy
- **Yellow**: Checking server status or waiting
- **Red**: Server is disconnected or has errors

## 🎯 Best Practices

### Server Monitoring
- **Regular Health Checks**: Monitor server health periodically
- **Timeout Management**: Use appropriate timeouts for your environment
- **Error Handling**: Monitor error messages for troubleshooting
- **Status Tracking**: Keep track of connection attempts and startup times

### Server Control
- **Graceful Restarts**: Use restart functionality when needed
- **Wait for Readiness**: Always wait for server to be ready before use
- **Timeout Configuration**: Adjust timeouts based on server performance
- **Error Recovery**: Handle connection failures appropriately

### UI Design
- **Real-time Updates**: Keep displays current with auto-refresh
- **Clear Status Indicators**: Use color coding for quick status recognition
- **Interactive Controls**: Make server control actions obvious
- **Error Display**: Show error messages clearly for troubleshooting
- **Debug Tools**: Provide tools for development and debugging

## 🔍 Debugging

### Common Issues
- **Server not starting**: Check executable path and model file
- **Connection timeouts**: Increase timeout values or check server performance
- **Health check failures**: Verify server is running and accessible
- **Restart failures**: Check server process and configuration
- **UI not updating**: Verify auto-update is enabled and BrainServer is found

### Debug Information
- **Console logs**: Check for server operations and errors
- **Status displays**: Monitor connection status and error messages
- **Context menu tools**: Use "Debug UI State" and "Force Find Server" tools
- **Detailed status**: Review comprehensive server status information
- **Connection attempts**: Monitor connection attempt counts

## 🚀 Next Steps

### Enhancements
- **Advanced Monitoring**: Add performance metrics and resource usage
- **Connection Pooling**: Implement connection pooling for multiple clients
- **Load Balancing**: Add support for multiple server instances
- **Health Alerts**: Implement notification system for connection issues
- **Server Analytics**: Add detailed server performance analytics

### Integration
- **Game Events**: Trigger server operations from game events
- **Save System**: Integrate with game save/load functionality
- **Multiplayer**: Support for multiplayer server management
- **Cloud Integration**: Add cloud-based server management
- **Automated Recovery**: Implement automatic server recovery

## 📋 Requirements

### Unity Requirements
- **Unity 2021.3 or later**: For TextMeshPro integration
- **TextMeshPro Package**: Must be installed via Package Manager
- **Cysharp.Threading.Tasks**: For async operations (included with package)

### Setup Requirements
- **BrainServer**: Properly configured with llama.cpp
- **llama.cpp Server**: Running llama.cpp server instance
- **UI References**: All TMP components properly assigned
- **Network Access**: Access to the server port and host

## 📚 Related Documentation
- [SAMPLES.md](SAMPLES.md) - Overview of all samples
- [Troubleshooting](TROUBLESHOOTING.md) - Common issues and solutions 