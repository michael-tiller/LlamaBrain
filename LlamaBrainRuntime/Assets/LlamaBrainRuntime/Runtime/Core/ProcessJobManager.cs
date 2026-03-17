#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LlamaBrain.Runtime.Core
{
    /// <summary>
    /// Manages Windows Job Objects to ensure child processes (like llama-server) are
    /// automatically terminated when the parent process exits, even on crash.
    ///
    /// Uses JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE which causes Windows to kill all
    /// processes in the job when the job handle is closed (which happens automatically
    /// when the parent process terminates for any reason).
    /// </summary>
    public static class ProcessJobManager
    {
        private static IntPtr _jobHandle = IntPtr.Zero;
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        #region P/Invoke Declarations

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObjectW(IntPtr lpJobAttributes, string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(
            IntPtr hJob,
            JobObjectInfoType infoType,
            ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInfo,
            int cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern int GetLastError();

        private enum JobObjectInfoType
        {
            BasicLimitInformation = 2,
            BasicUIRestrictions = 4,
            SecurityLimitInformation = 5,
            EndOfJobTimeInformation = 6,
            AssociateCompletionPortInformation = 7,
            ExtendedLimitInformation = 9,
            GroupInformation = 11
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        // Limit flags
        private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

        #endregion

        /// <summary>
        /// Whether the job object is initialized and ready to accept processes.
        /// </summary>
        public static bool IsInitialized => _initialized && _jobHandle != IntPtr.Zero;

        /// <summary>
        /// Initializes the job object. Call this early in application startup.
        /// Safe to call multiple times (idempotent).
        /// </summary>
        /// <returns>True if initialization succeeded, false otherwise.</returns>
        public static bool Initialize()
        {
            // Only works on Windows
            if (Application.platform != RuntimePlatform.WindowsPlayer &&
                Application.platform != RuntimePlatform.WindowsEditor)
            {
                UnityEngine.Debug.Log("[ProcessJobManager] Skipping initialization - not on Windows");
                return false;
            }

            lock (_lock)
            {
                if (_initialized)
                {
                    return _jobHandle != IntPtr.Zero;
                }

                try
                {
                    // Create an anonymous job object
                    _jobHandle = CreateJobObjectW(IntPtr.Zero, null);
                    if (_jobHandle == IntPtr.Zero)
                    {
                        var error = GetLastError();
                        UnityEngine.Debug.LogError($"[ProcessJobManager] CreateJobObject failed with error: {error}");
                        _initialized = true; // Mark as initialized to prevent retries
                        return false;
                    }

                    // Configure the job to kill all processes when the job handle is closed
                    var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
                    info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

                    if (!SetInformationJobObject(
                        _jobHandle,
                        JobObjectInfoType.ExtendedLimitInformation,
                        ref info,
                        Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION))))
                    {
                        var error = GetLastError();
                        UnityEngine.Debug.LogError($"[ProcessJobManager] SetInformationJobObject failed with error: {error}");
                        CloseHandle(_jobHandle);
                        _jobHandle = IntPtr.Zero;
                        _initialized = true;
                        return false;
                    }

                    _initialized = true;
                    UnityEngine.Debug.Log("[ProcessJobManager] Job object created successfully - child processes will be killed on crash");
                    return true;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[ProcessJobManager] Initialization failed: {ex.Message}");
                    _initialized = true;
                    return false;
                }
            }
        }

        /// <summary>
        /// Assigns a process to the job object by its Process instance.
        /// The process will be automatically killed when the parent process exits.
        /// </summary>
        /// <param name="process">The process to assign to the job.</param>
        /// <returns>True if assignment succeeded, false otherwise.</returns>
        public static bool AssignProcess(Process process)
        {
            if (process == null)
            {
                UnityEngine.Debug.LogWarning("[ProcessJobManager] Cannot assign null process");
                return false;
            }

            if (!_initialized)
            {
                Initialize();
            }

            if (_jobHandle == IntPtr.Zero)
            {
                UnityEngine.Debug.LogWarning("[ProcessJobManager] Job object not available - process not assigned");
                return false;
            }

            try
            {
                if (process.HasExited)
                {
                    UnityEngine.Debug.LogWarning($"[ProcessJobManager] Process {process.Id} has already exited");
                    return false;
                }

                if (!AssignProcessToJobObject(_jobHandle, process.Handle))
                {
                    var error = GetLastError();
                    // Error 5 = ACCESS_DENIED (process may already be in a job)
                    // Error 6 = INVALID_HANDLE (process already exited)
                    if (error == 5)
                    {
                        UnityEngine.Debug.LogWarning($"[ProcessJobManager] Process {process.Id} may already be in a job object (error 5)");
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"[ProcessJobManager] AssignProcessToJobObject failed with error: {error}");
                    }
                    return false;
                }

                UnityEngine.Debug.Log($"[ProcessJobManager] Process {process.Id} ({process.ProcessName}) assigned to job - will be killed on parent exit");
                return true;
            }
            catch (InvalidOperationException)
            {
                // Process has exited
                UnityEngine.Debug.LogWarning($"[ProcessJobManager] Process exited before assignment");
                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ProcessJobManager] Failed to assign process: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Finds and assigns a process to the job object by name.
        /// Useful when you don't have direct access to the Process handle.
        /// </summary>
        /// <param name="processName">The process name without .exe extension.</param>
        /// <param name="matchNewest">If multiple processes match, assign the newest one.</param>
        /// <returns>True if at least one process was assigned, false otherwise.</returns>
        public static bool AssignProcessByName(string processName, bool matchNewest = true)
        {
            if (string.IsNullOrEmpty(processName))
            {
                UnityEngine.Debug.LogWarning("[ProcessJobManager] Cannot find process with empty name");
                return false;
            }

            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0)
                {
                    UnityEngine.Debug.LogWarning($"[ProcessJobManager] No process found with name: {processName}");
                    return false;
                }

                if (matchNewest && processes.Length > 1)
                {
                    // Sort by start time descending and take the newest
                    Array.Sort(processes, (a, b) =>
                    {
                        try
                        {
                            return b.StartTime.CompareTo(a.StartTime);
                        }
                        catch
                        {
                            return 0;
                        }
                    });
                }

                bool anyAssigned = false;
                foreach (var process in processes)
                {
                    try
                    {
                        if (AssignProcess(process))
                        {
                            anyAssigned = true;
                            if (matchNewest)
                            {
                                break; // Only assign the newest
                            }
                        }
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                return anyAssigned;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ProcessJobManager] Failed to find process by name: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Finds and assigns a process to the job object by PID.
        /// </summary>
        /// <param name="processId">The process ID.</param>
        /// <returns>True if assignment succeeded, false otherwise.</returns>
        public static bool AssignProcessById(int processId)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                return AssignProcess(process);
            }
            catch (ArgumentException)
            {
                UnityEngine.Debug.LogWarning($"[ProcessJobManager] No process found with ID: {processId}");
                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ProcessJobManager] Failed to find process by ID: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Manually closes the job handle. Normally not needed as the OS
        /// will close it when the process exits.
        /// </summary>
        public static void Shutdown()
        {
            lock (_lock)
            {
                if (_jobHandle != IntPtr.Zero)
                {
                    CloseHandle(_jobHandle);
                    _jobHandle = IntPtr.Zero;
                    UnityEngine.Debug.Log("[ProcessJobManager] Job object closed");
                }
                _initialized = false;
            }
        }
    }
}
