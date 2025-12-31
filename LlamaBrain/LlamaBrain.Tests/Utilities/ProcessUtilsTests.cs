using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using LlamaBrain.Utilities;

namespace LlamaBrain.Tests.Utilities
{
  /// <summary>
  /// Tests for ProcessUtils utility class.
  /// Note: Some tests use the current process or known system processes for verification.
  /// </summary>
  public class ProcessUtilsTests
  {
    private string _currentProcessName = null!;

    [SetUp]
    public void SetUp()
    {
      // Get current process name for tests that need a guaranteed running process
      _currentProcessName = Process.GetCurrentProcess().ProcessName;
    }

    #region IsProcessRunning Tests

    [Test]
    public void IsProcessRunning_NullProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => ProcessUtils.IsProcessRunning(null!));
    }

    [Test]
    public void IsProcessRunning_EmptyProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => ProcessUtils.IsProcessRunning(""));
    }

    [Test]
    public void IsProcessRunning_WhitespaceProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => ProcessUtils.IsProcessRunning("   "));
    }

    [Test]
    public void IsProcessRunning_TooLongProcessName_ThrowsArgumentException()
    {
      // Arrange
      var longName = new string('a', 300);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => ProcessUtils.IsProcessRunning(longName));
    }

    [Test]
    public void IsProcessRunning_CurrentProcess_ReturnsTrue()
    {
      // Act
      var result = ProcessUtils.IsProcessRunning(_currentProcessName);

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public void IsProcessRunning_NonExistentProcess_ReturnsFalse()
    {
      // Arrange
      var nonExistentProcess = "definitely_not_a_real_process_xyz123";

      // Act
      var result = ProcessUtils.IsProcessRunning(nonExistentProcess);

      // Assert
      Assert.That(result, Is.False);
    }

    #endregion

    #region GetRunningProcesses Tests

    [Test]
    public void GetRunningProcesses_NullProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => ProcessUtils.GetRunningProcesses(null!));
    }

    [Test]
    public void GetRunningProcesses_EmptyProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => ProcessUtils.GetRunningProcesses(""));
    }

    [Test]
    public void GetRunningProcesses_WhitespaceProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => ProcessUtils.GetRunningProcesses("   "));
    }

    [Test]
    public void GetRunningProcesses_TooLongProcessName_ThrowsArgumentException()
    {
      // Arrange
      var longName = new string('a', 300);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => ProcessUtils.GetRunningProcesses(longName));
    }

    [Test]
    public void GetRunningProcesses_CurrentProcess_ReturnsNonEmptyList()
    {
      // Act
      var result = ProcessUtils.GetRunningProcesses(_currentProcessName);

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result.Count, Is.GreaterThan(0));

      // Clean up
      foreach (var process in result)
      {
        process.Dispose();
      }
    }

    [Test]
    public void GetRunningProcesses_NonExistentProcess_ReturnsEmptyList()
    {
      // Arrange
      var nonExistentProcess = "definitely_not_a_real_process_xyz123";

      // Act
      var result = ProcessUtils.GetRunningProcesses(nonExistentProcess);

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result.Count, Is.EqualTo(0));
    }

    #endregion

    #region KillProcess Tests

    [Test]
    public void KillProcess_NullProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => ProcessUtils.KillProcess(null!));
    }

    [Test]
    public void KillProcess_EmptyProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => ProcessUtils.KillProcess(""));
    }

    [Test]
    public void KillProcess_WhitespaceProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => ProcessUtils.KillProcess("   "));
    }

    [Test]
    public void KillProcess_NonExistentProcess_ReturnsFalse()
    {
      // Arrange
      var nonExistentProcess = "definitely_not_a_real_process_xyz123";

      // Act
      var result = ProcessUtils.KillProcess(nonExistentProcess);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void KillProcess_ForceKillParameter_AcceptsBothValues()
    {
      // Arrange
      var nonExistentProcess = "definitely_not_a_real_process_xyz123";

      // Act - Just verify the method accepts both parameter values
      var result1 = ProcessUtils.KillProcess(nonExistentProcess, forceKill: false);
      var result2 = ProcessUtils.KillProcess(nonExistentProcess, forceKill: true);

      // Assert
      Assert.That(result1, Is.False);
      Assert.That(result2, Is.False);
    }

    #endregion

    #region GetProcessInfo Tests

    [Test]
    public void GetProcessInfo_NullProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => ProcessUtils.GetProcessInfo(null!));
    }

    [Test]
    public void GetProcessInfo_EmptyProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => ProcessUtils.GetProcessInfo(""));
    }

    [Test]
    public void GetProcessInfo_WhitespaceProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.Throws<ArgumentException>(() => ProcessUtils.GetProcessInfo("   "));
    }

    [Test]
    public void GetProcessInfo_CurrentProcess_ReturnsValidInfo()
    {
      // Act
      var result = ProcessUtils.GetProcessInfo(_currentProcessName);

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result.Count, Is.GreaterThan(0));

      var info = result[0];
      Assert.That(info.Id, Is.GreaterThan(0));
      Assert.That(info.ProcessName, Is.EqualTo(_currentProcessName));
      Assert.That(info.ThreadCount, Is.GreaterThan(0));
    }

    [Test]
    public void GetProcessInfo_NonExistentProcess_ReturnsEmptyList()
    {
      // Arrange
      var nonExistentProcess = "definitely_not_a_real_process_xyz123";

      // Act
      var result = ProcessUtils.GetProcessInfo(nonExistentProcess);

      // Assert
      Assert.That(result, Is.Not.Null);
      Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public void GetProcessInfo_ReturnsCorrectMemoryValues()
    {
      // Act
      var result = ProcessUtils.GetProcessInfo(_currentProcessName);

      // Assert
      Assert.That(result.Count, Is.GreaterThan(0));
      var info = result[0];
      Assert.That(info.WorkingSet, Is.GreaterThan(0));
      Assert.That(info.VirtualMemorySize, Is.GreaterThan(0));
      Assert.That(info.PrivateMemorySize, Is.GreaterThan(0));
    }

    #endregion

    #region WaitForProcessStartAsync Tests

    [Test]
    public void WaitForProcessStartAsync_NullProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.ThrowsAsync<ArgumentException>(async () =>
        await ProcessUtils.WaitForProcessStartAsync(null!));
    }

    [Test]
    public void WaitForProcessStartAsync_EmptyProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.ThrowsAsync<ArgumentException>(async () =>
        await ProcessUtils.WaitForProcessStartAsync(""));
    }

    [Test]
    public void WaitForProcessStartAsync_WhitespaceProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.ThrowsAsync<ArgumentException>(async () =>
        await ProcessUtils.WaitForProcessStartAsync("   "));
    }

    [Test]
    public void WaitForProcessStartAsync_ZeroTimeout_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.ThrowsAsync<ArgumentException>(async () =>
        await ProcessUtils.WaitForProcessStartAsync("test", 0));
    }

    [Test]
    public void WaitForProcessStartAsync_NegativeTimeout_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.ThrowsAsync<ArgumentException>(async () =>
        await ProcessUtils.WaitForProcessStartAsync("test", -1));
    }

    [Test]
    public async Task WaitForProcessStartAsync_AlreadyRunningProcess_ReturnsImmediately()
    {
      // Act
      var result = await ProcessUtils.WaitForProcessStartAsync(_currentProcessName, 1);

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public async Task WaitForProcessStartAsync_NonExistentProcess_ReturnsFalseAfterTimeout()
    {
      // Arrange
      var nonExistentProcess = "definitely_not_a_real_process_xyz123";

      // Act
      var result = await ProcessUtils.WaitForProcessStartAsync(nonExistentProcess, 1);

      // Assert
      Assert.That(result, Is.False);
    }

    #endregion

    #region WaitForProcessExitAsync Tests

    [Test]
    public void WaitForProcessExitAsync_NullProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.ThrowsAsync<ArgumentException>(async () =>
        await ProcessUtils.WaitForProcessExitAsync(null!));
    }

    [Test]
    public void WaitForProcessExitAsync_EmptyProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.ThrowsAsync<ArgumentException>(async () =>
        await ProcessUtils.WaitForProcessExitAsync(""));
    }

    [Test]
    public void WaitForProcessExitAsync_WhitespaceProcessName_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.ThrowsAsync<ArgumentException>(async () =>
        await ProcessUtils.WaitForProcessExitAsync("   "));
    }

    [Test]
    public void WaitForProcessExitAsync_ZeroTimeout_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.ThrowsAsync<ArgumentException>(async () =>
        await ProcessUtils.WaitForProcessExitAsync("test", 0));
    }

    [Test]
    public void WaitForProcessExitAsync_NegativeTimeout_ThrowsArgumentException()
    {
      // Act & Assert
      Assert.ThrowsAsync<ArgumentException>(async () =>
        await ProcessUtils.WaitForProcessExitAsync("test", -1));
    }

    [Test]
    public async Task WaitForProcessExitAsync_NotRunningProcess_ReturnsImmediately()
    {
      // Arrange
      var nonExistentProcess = "definitely_not_a_real_process_xyz123";

      // Act
      var result = await ProcessUtils.WaitForProcessExitAsync(nonExistentProcess, 1);

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public async Task WaitForProcessExitAsync_RunningProcess_ReturnsFalseAfterTimeout()
    {
      // Act - Current process won't exit during test
      var result = await ProcessUtils.WaitForProcessExitAsync(_currentProcessName, 1);

      // Assert
      Assert.That(result, Is.False);
    }

    #endregion

    #region ValidateProcessConfig Tests

    [Test]
    public void ValidateProcessConfig_NullExecutablePath_ReturnsFalse()
    {
      // Act
      var result = ProcessUtils.ValidateProcessConfig(null!);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateProcessConfig_EmptyExecutablePath_ReturnsFalse()
    {
      // Act
      var result = ProcessUtils.ValidateProcessConfig("");

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateProcessConfig_WhitespaceExecutablePath_ReturnsFalse()
    {
      // Act
      var result = ProcessUtils.ValidateProcessConfig("   ");

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateProcessConfig_NonExistentFile_ReturnsFalse()
    {
      // Arrange
      var nonExistentPath = Path.Combine(Path.GetTempPath(), "definitely_not_a_file_xyz123.exe");

      // Act
      var result = ProcessUtils.ValidateProcessConfig(nonExistentPath);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateProcessConfig_ExistingFile_ReturnsTrue()
    {
      // Arrange - Create a temporary file to test with
      var tempFile = Path.GetTempFileName();
      try
      {
        File.WriteAllText(tempFile, "test content");

        // Act
        var result = ProcessUtils.ValidateProcessConfig(tempFile);

        // Assert
        Assert.That(result, Is.True);
      }
      finally
      {
        File.Delete(tempFile);
      }
    }

    [Test]
    public void ValidateProcessConfig_EmptyFile_ReturnsFalse()
    {
      // Arrange - Create an empty temporary file
      var tempFile = Path.GetTempFileName();
      try
      {
        // File.GetTempFileName creates a 0-byte file

        // Act
        var result = ProcessUtils.ValidateProcessConfig(tempFile);

        // Assert
        Assert.That(result, Is.False);
      }
      finally
      {
        File.Delete(tempFile);
      }
    }

    [Test]
    public void ValidateProcessConfig_ValidPathWithNullArguments_ReturnsTrue()
    {
      // Arrange
      var tempFile = Path.GetTempFileName();
      try
      {
        File.WriteAllText(tempFile, "test content");

        // Act
        var result = ProcessUtils.ValidateProcessConfig(tempFile, null);

        // Assert
        Assert.That(result, Is.True);
      }
      finally
      {
        File.Delete(tempFile);
      }
    }

    [Test]
    public void ValidateProcessConfig_ValidPathWithEmptyArguments_ReturnsTrue()
    {
      // Arrange
      var tempFile = Path.GetTempFileName();
      try
      {
        File.WriteAllText(tempFile, "test content");

        // Act
        var result = ProcessUtils.ValidateProcessConfig(tempFile, "");

        // Assert
        Assert.That(result, Is.True);
      }
      finally
      {
        File.Delete(tempFile);
      }
    }

    [Test]
    public void ValidateProcessConfig_ValidPathWithValidArguments_ReturnsTrue()
    {
      // Arrange
      var tempFile = Path.GetTempFileName();
      try
      {
        File.WriteAllText(tempFile, "test content");

        // Act
        var result = ProcessUtils.ValidateProcessConfig(tempFile, "--config test.json");

        // Assert
        Assert.That(result, Is.True);
      }
      finally
      {
        File.Delete(tempFile);
      }
    }

    [Test]
    public void ValidateProcessConfig_TooLongArguments_ReturnsFalse()
    {
      // Arrange
      var tempFile = Path.GetTempFileName();
      try
      {
        File.WriteAllText(tempFile, "test content");
        var longArguments = new string('a', 33000); // Over 32768 limit

        // Act
        var result = ProcessUtils.ValidateProcessConfig(tempFile, longArguments);

        // Assert
        Assert.That(result, Is.False);
      }
      finally
      {
        File.Delete(tempFile);
      }
    }

    [Test]
    public void ValidateProcessConfig_ArgumentsAtLimit_ReturnsTrue()
    {
      // Arrange
      var tempFile = Path.GetTempFileName();
      try
      {
        File.WriteAllText(tempFile, "test content");
        var limitArguments = new string('a', 32768); // Exactly at limit

        // Act
        var result = ProcessUtils.ValidateProcessConfig(tempFile, limitArguments);

        // Assert
        Assert.That(result, Is.True);
      }
      finally
      {
        File.Delete(tempFile);
      }
    }

    #endregion

    #region ProcessInfo Class Tests

    [Test]
    public void ProcessInfo_DefaultValues_AreCorrect()
    {
      // Arrange & Act
      var info = new ProcessInfo();

      // Assert
      Assert.That(info.Id, Is.EqualTo(0));
      Assert.That(info.ProcessName, Is.EqualTo(string.Empty));
      Assert.That(info.StartTime, Is.EqualTo(default(DateTime)));
      Assert.That(info.WorkingSet, Is.EqualTo(0L));
      Assert.That(info.VirtualMemorySize, Is.EqualTo(0L));
      Assert.That(info.PrivateMemorySize, Is.EqualTo(0L));
      Assert.That(info.ThreadCount, Is.EqualTo(0));
      Assert.That(info.Responding, Is.False);
      Assert.That(info.HasExited, Is.False);
    }

    [Test]
    public void ProcessInfo_CanSetAllProperties()
    {
      // Arrange
      var now = DateTime.Now;
      var info = new ProcessInfo
      {
        Id = 12345,
        ProcessName = "TestProcess",
        StartTime = now,
        WorkingSet = 1000000L,
        VirtualMemorySize = 2000000L,
        PrivateMemorySize = 500000L,
        ThreadCount = 10,
        Responding = true,
        HasExited = false
      };

      // Assert
      Assert.That(info.Id, Is.EqualTo(12345));
      Assert.That(info.ProcessName, Is.EqualTo("TestProcess"));
      Assert.That(info.StartTime, Is.EqualTo(now));
      Assert.That(info.WorkingSet, Is.EqualTo(1000000L));
      Assert.That(info.VirtualMemorySize, Is.EqualTo(2000000L));
      Assert.That(info.PrivateMemorySize, Is.EqualTo(500000L));
      Assert.That(info.ThreadCount, Is.EqualTo(10));
      Assert.That(info.Responding, Is.True);
      Assert.That(info.HasExited, Is.False);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Test]
    public void IsProcessRunning_ProcessNameWithSpaces_HandlesCorrectly()
    {
      // Arrange - Process names with spaces are unusual but valid input
      var processNameWithSpaces = "some process name";

      // Act
      var result = ProcessUtils.IsProcessRunning(processNameWithSpaces);

      // Assert - Should return false (process doesn't exist) without throwing
      Assert.That(result, Is.False);
    }

    [Test]
    public void IsProcessRunning_ProcessNameWithSpecialChars_HandlesCorrectly()
    {
      // Arrange
      var processNameWithSpecialChars = "test-process_v1.0";

      // Act
      var result = ProcessUtils.IsProcessRunning(processNameWithSpecialChars);

      // Assert - Should return false without throwing
      Assert.That(result, Is.False);
    }

    [Test]
    public void GetRunningProcesses_MaxLengthProcessName_DoesNotThrow()
    {
      // Arrange - 260 chars is the max
      var maxLengthName = new string('a', 260);

      // Act & Assert - Should not throw
      var result = ProcessUtils.GetRunningProcesses(maxLengthName);
      Assert.That(result, Is.Not.Null);
      Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public void IsProcessRunning_ExactlyMaxLength_DoesNotThrow()
    {
      // Arrange - 260 chars is the max
      var maxLengthName = new string('a', 260);

      // Act & Assert - Should not throw
      var result = ProcessUtils.IsProcessRunning(maxLengthName);
      Assert.That(result, Is.False);
    }

    [Test]
    public void IsProcessRunning_OneOverMaxLength_ThrowsArgumentException()
    {
      // Arrange - 261 chars is over the max
      var overMaxName = new string('a', 261);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => ProcessUtils.IsProcessRunning(overMaxName));
    }

    [Test]
    public async Task WaitForProcessStartAsync_DefaultTimeout_UsesThirtySeconds()
    {
      // This test verifies the default timeout parameter works
      // We use an already-running process so it returns immediately
      var result = await ProcessUtils.WaitForProcessStartAsync(_currentProcessName);
      Assert.That(result, Is.True);
    }

    [Test]
    public async Task WaitForProcessExitAsync_DefaultTimeout_UsesThirtySeconds()
    {
      // This test verifies the default timeout parameter works
      // We use a non-running process so it returns immediately
      var result = await ProcessUtils.WaitForProcessExitAsync("nonexistent_process_xyz123");
      Assert.That(result, Is.True);
    }

    #endregion
  }
}
