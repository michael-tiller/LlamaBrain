#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using LlamaBrain.Core;
using LlamaBrain.Core.Metrics;

namespace LlamaBrain.Runtime.RedRoom.Interaction
{
  /// <summary>
  /// Collects and exports dialogue interaction metrics for LLM testing
  /// </summary>
  public class DialogueMetricsCollector : MonoBehaviour
  {
    private DialogueMetricsCollection? currentSession;
    private int currentFileIndex = 0;
    
    [Header("Export Settings")]
    [SerializeField] private bool autoExportOnQuit = true;
    [SerializeField] private string exportDirectory = "DialogueMetrics";
    
    [Header("Rolling File Settings")]
    [Tooltip("Enable rolling file system to prevent files from growing too large")]
    [SerializeField] private bool enableRollingFiles = true;
    
    [Tooltip("Maximum number of interactions per file before rolling to a new file (0 = disabled)")]
    [SerializeField] private int maxInteractionsPerFile = 1000;
    
    [Tooltip("Maximum file size in MB before rolling to a new file (0 = disabled)")]
    [SerializeField] private float maxFileSizeMB = 10f;
    
    [Tooltip("Maximum time in minutes per file before rolling to a new file (0 = disabled)")]
    [SerializeField] private float maxTimePerFileMinutes = 60f;
    
    [Tooltip("Maximum number of files to keep. Oldest files are deleted when exceeded (0 = keep all)")]
    [SerializeField] private int maxFilesToKeep = 50;
    
    /// <summary>
    /// Gets the singleton instance of the DialogueMetricsCollector.
    /// </summary>
    public static DialogueMetricsCollector? Instance { get; private set; }
    
    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(this.gameObject);
        return;
      }
      
      Instance = this;
      DontDestroyOnLoad(gameObject);
      StartNewSession();
    }
    
    /// <summary>
    /// Starts a new metrics collection session
    /// </summary>
    public void StartNewSession()
    {
      if (currentSession != null && currentSession.TotalInteractions > 0)
      {
        currentSession.EndSession();
        ExportSession(currentSession, currentFileIndex);
      }
      
      // Reset file index for new session sequence
      currentFileIndex = 0;
      currentSession = new DialogueMetricsCollection();
      Debug.Log($"[DialogueMetricsCollector] Started new session: {currentSession.SessionId}");
    }
    
    /// <summary>
    /// Records a dialogue interaction with metrics.
    /// </summary>
    /// <param name="metrics">The completion metrics from the LLM response</param>
    /// <param name="trigger">The trigger that initiated this interaction</param>
    /// <param name="npcName">The name of the NPC</param>
    /// <param name="wasTruncated">Whether the response was truncated</param>
    public void RecordInteraction(
      CompletionMetrics metrics,
      NpcDialogueTrigger trigger,
      string npcName,
      bool wasTruncated = false)
    {
      RecordInteraction(metrics, trigger, npcName, wasTruncated, null);
    }

    /// <summary>
    /// Records a dialogue interaction with metrics and architectural data.
    /// </summary>
    /// <param name="metrics">The completion metrics from the LLM response</param>
    /// <param name="trigger">The trigger that initiated this interaction</param>
    /// <param name="npcName">The name of the NPC</param>
    /// <param name="wasTruncated">Whether the response was truncated</param>
    /// <param name="agent">The LlamaBrainAgent that performed the inference (optional, for architectural metrics)</param>
    public void RecordInteraction(
      CompletionMetrics metrics,
      NpcDialogueTrigger trigger,
      string npcName,
      bool wasTruncated,
      LlamaBrain.Runtime.Core.LlamaBrainAgent? agent)
    {
      if (currentSession == null)
      {
        StartNewSession();
      }
      
      var interaction = DialogueInteraction.FromMetrics(metrics, trigger, npcName);
      interaction.WasTruncated = wasTruncated;
      
      // Populate architectural metrics if agent is provided
      if (agent != null)
      {
        interaction.PopulateArchitecturalMetrics(agent);
      }
      
      if (currentSession != null)
      {
        currentSession.AddInteraction(interaction);
        
        var validationStatus = interaction.ValidationPassed ? "PASS" : "FAIL";
        var retryInfo = interaction.RetryCount > 0 ? $" ({interaction.RetryCount} retries)" : "";
        var fallbackInfo = interaction.FallbackUsed ? " [FALLBACK]" : "";
        
        Debug.Log($"[DialogueMetricsCollector] Recorded interaction #{currentSession.TotalInteractions} " +
                  $"from trigger '{trigger.GetTriggerName()}' - " +
                  $"TTFT: {metrics.TtftMs}ms, Tokens: {metrics.GeneratedTokenCount}, " +
                  $"Speed: {metrics.TokensPerSecond:F1} tps, " +
                  $"Validation: {validationStatus}{retryInfo}{fallbackInfo}");
      }
      
      // Check if we need to roll over to a new file
      if (enableRollingFiles && currentSession != null && ShouldRollFile())
      {
        Debug.Log($"[DialogueMetricsCollector] Rolling to new file. Current: {currentSession.TotalInteractions} interactions");
        RollToNewFile();
      }
    }
    
    /// <summary>
    /// Checks if the current file should be rolled over based on configured thresholds
    /// </summary>
    private bool ShouldRollFile()
    {
      if (currentSession == null || currentSession.TotalInteractions == 0)
        return false;
      
      // Check interaction count threshold
      if (maxInteractionsPerFile > 0 && currentSession.TotalInteractions >= maxInteractionsPerFile)
      {
        Debug.Log($"[DialogueMetricsCollector] Rollover triggered: Interaction count ({currentSession.TotalInteractions}) >= {maxInteractionsPerFile}");
        return true;
      }
      
      // Check time threshold
      if (maxTimePerFileMinutes > 0)
      {
        var sessionDuration = DateTime.Now - currentSession.SessionStart;
        if (sessionDuration.TotalMinutes >= maxTimePerFileMinutes)
        {
          Debug.Log($"[DialogueMetricsCollector] Rollover triggered: Session duration ({sessionDuration.TotalMinutes:F1} min) >= {maxTimePerFileMinutes} min");
          return true;
        }
      }
      
      // Check file size threshold (estimate based on average interaction size)
      if (maxFileSizeMB > 0)
      {
        // Estimate file size: average ~500 bytes per interaction (conservative estimate)
        // This is approximate - actual size depends on response text length
        double estimatedSizeMB = (currentSession.TotalInteractions * 500.0) / (1024.0 * 1024.0);
        if (estimatedSizeMB >= maxFileSizeMB)
        {
          Debug.Log($"[DialogueMetricsCollector] Rollover triggered: Estimated file size ({estimatedSizeMB:F2} MB) >= {maxFileSizeMB} MB");
          return true;
        }
      }
      
      return false;
    }
    
    /// <summary>
    /// Rolls over to a new file by exporting the current session and starting a new one
    /// </summary>
    private void RollToNewFile()
    {
      if (currentSession == null || currentSession.TotalInteractions == 0)
        return;
      
      // Export current session
      currentSession.EndSession();
      ExportSession(currentSession, currentFileIndex);
      
      // Clean up old files if needed
      CleanupOldFiles();
      
      // Start new session with incremented file index
      currentFileIndex++;
      currentSession = new DialogueMetricsCollection();
      Debug.Log($"[DialogueMetricsCollector] Started new file session #{currentFileIndex}: {currentSession.SessionId}");
    }
    
    /// <summary>
    /// Exports the current session to CSV and JSON
    /// </summary>
    public void ExportCurrentSession()
    {
      if (currentSession == null || currentSession.TotalInteractions == 0)
      {
        Debug.LogWarning("[DialogueMetricsCollector] No interactions to export");
        return;
      }
      
      currentSession.EndSession();
      ExportSession(currentSession, currentFileIndex);
      CleanupOldFiles();
    }
    
    /// <summary>
    /// Exports a session to both CSV and JSON formats
    /// </summary>
    private void ExportSession(DialogueMetricsCollection session, int fileIndex = 0)
    {
      string basePath = Path.Combine(Application.persistentDataPath, exportDirectory);
      
      if (!Directory.Exists(basePath))
      {
        Directory.CreateDirectory(basePath);
      }
      
      string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
      string baseFileName = fileIndex > 0 
        ? $"DialogueMetrics_{session.SessionId}_{timestamp}_part{fileIndex:D3}"
        : $"DialogueMetrics_{session.SessionId}_{timestamp}";
      
      // Export CSV
      string csvPath = Path.Combine(basePath, $"{baseFileName}.csv");
      ExportToCsv(session, csvPath);
      
      // Export JSON
      string jsonPath = Path.Combine(basePath, $"{baseFileName}.json");
      ExportToJson(session, jsonPath);
      
      // Export validation statistics
      ExportValidationStatistics(session, fileIndex);
      
      // Export constraint violations
      ExportConstraintViolations(session, fileIndex);
      
      Debug.Log($"[DialogueMetricsCollector] Exported session with {session.TotalInteractions} interactions (file #{fileIndex}):");
      Debug.Log($"  CSV: {csvPath}");
      Debug.Log($"  JSON: {jsonPath}");
      Debug.Log($"  Validation Statistics: {Path.Combine(basePath, $"ValidationStats_{session.SessionId}_{DateTime.Now:yyyyMMdd_HHmmss}{(fileIndex > 0 ? $"_part{fileIndex:D3}" : "")}.csv")}");
      Debug.Log($"  Constraint Violations: {Path.Combine(basePath, $"ConstraintViolations_{session.SessionId}_{DateTime.Now:yyyyMMdd_HHmmss}{(fileIndex > 0 ? $"_part{fileIndex:D3}" : "")}.csv")}");
    }
    
    /// <summary>
    /// Cleans up old files, keeping only the most recent N files
    /// </summary>
    private void CleanupOldFiles()
    {
      if (maxFilesToKeep <= 0)
        return; // Keep all files
      
      string basePath = Path.Combine(Application.persistentDataPath, exportDirectory);
      if (!Directory.Exists(basePath))
        return;
      
      try
      {
        // Get all CSV files (we'll delete both CSV and JSON pairs)
        var csvFiles = Directory.GetFiles(basePath, "DialogueMetrics_*.csv")
          .Select(f => new FileInfo(f))
          .OrderByDescending(f => f.CreationTime)
          .ToList();
        
        if (csvFiles.Count <= maxFilesToKeep)
          return; // Within limit, no cleanup needed
        
        // Delete oldest files beyond the limit
        int filesToDelete = csvFiles.Count - maxFilesToKeep;
        int deletedCount = 0;
        
        for (int i = csvFiles.Count - 1; i >= maxFilesToKeep; i--)
        {
          var csvFile = csvFiles[i];
          var jsonFile = csvFile.FullName.Replace(".csv", ".json");
          
          try
          {
            File.Delete(csvFile.FullName);
            if (File.Exists(jsonFile))
            {
              File.Delete(jsonFile);
            }
            deletedCount++;
          }
          catch (Exception ex)
          {
            Debug.LogWarning($"[DialogueMetricsCollector] Failed to delete file {csvFile.Name}: {ex.Message}");
          }
        }
        
        if (deletedCount > 0)
        {
          Debug.Log($"[DialogueMetricsCollector] Cleaned up {deletedCount} old file(s). Keeping {maxFilesToKeep} most recent files.");
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"[DialogueMetricsCollector] Error during file cleanup: {ex.Message}");
      }
    }
    
    /// <summary>
    /// Exports metrics to CSV format
    /// </summary>
    private void ExportToCsv(DialogueMetricsCollection session, string filePath)
    {
      var csv = new StringBuilder();
      
      // Header
      csv.AppendLine("InteractionId,Timestamp,TriggerId,TriggerName,PromptText,PromptCount,NpcName," +
                     "ResponseText,ResponseLength,TtftMs,PrefillTimeMs,DecodeTimeMs,TotalTimeMs," +
                     "PromptTokenCount,GeneratedTokenCount,CachedTokenCount,TokensPerSecond,WasTruncated," +
                     "ValidationPassed,ValidationFailureCount,ConstraintViolationTypes,HasCriticalFailure," +
                     "RetryCount,TotalAttempts,FallbackUsed,FallbackTriggerReason," +
                     "ConstraintCount,ProhibitionCount,RequirementCount,PermissionCount");
      
      // Data rows
      foreach (var interaction in session.Interactions)
      {
        // Escape CSV special characters in text fields
        string escapedPrompt = EscapeCsvField(interaction.PromptText);
        string escapedResponse = EscapeCsvField(interaction.ResponseText);
        
        csv.AppendLine($"{interaction.InteractionId}," +
                      $"{interaction.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                      $"{interaction.TriggerId}," +
                      $"{EscapeCsvField(interaction.TriggerName)}," +
                      $"\"{escapedPrompt}\"," +
                      $"{interaction.PromptCount}," +
                      $"{EscapeCsvField(interaction.NpcName)}," +
                      $"\"{escapedResponse}\"," +
                      $"{interaction.ResponseLength}," +
                      $"{interaction.TtftMs}," +
                      $"{interaction.PrefillTimeMs}," +
                      $"{interaction.DecodeTimeMs}," +
                      $"{interaction.TotalTimeMs}," +
                      $"{interaction.PromptTokenCount}," +
                      $"{interaction.GeneratedTokenCount}," +
                      $"{interaction.CachedTokenCount}," +
                      $"{interaction.TokensPerSecond:F2}," +
                      $"{interaction.WasTruncated}," +
                      $"{interaction.ValidationPassed}," +
                      $"{interaction.ValidationFailureCount}," +
                      $"\"{EscapeCsvField(interaction.ConstraintViolationTypes)}\"," +
                      $"{interaction.HasCriticalFailure}," +
                      $"{interaction.RetryCount}," +
                      $"{interaction.TotalAttempts}," +
                      $"{interaction.FallbackUsed}," +
                      $"\"{EscapeCsvField(interaction.FallbackTriggerReason)}\"," +
                      $"{interaction.ConstraintCount}," +
                      $"{interaction.ProhibitionCount}," +
                      $"{interaction.RequirementCount}," +
                      $"{interaction.PermissionCount}");
      }
      
      File.WriteAllText(filePath, csv.ToString());
    }
    
    /// <summary>
    /// Exports metrics to JSON format
    /// </summary>
    private void ExportToJson(DialogueMetricsCollection session, string filePath)
    {
      var json = new StringBuilder();
      json.AppendLine("{");
      json.AppendLine($"  \"SessionId\": \"{session.SessionId}\",");
      json.AppendLine($"  \"SessionStart\": \"{session.SessionStart:yyyy-MM-dd HH:mm:ss.fff}\",");
      json.AppendLine($"  \"SessionEnd\": \"{session.SessionEnd?.ToString("yyyy-MM-dd HH:mm:ss.fff") ?? "null"}\",");
      json.AppendLine($"  \"TotalInteractions\": {session.TotalInteractions},");
      json.AppendLine("  \"Interactions\": [");
      
      for (int i = 0; i < session.Interactions.Count; i++)
      {
        var interaction = session.Interactions[i];
        json.AppendLine("    {");
        json.AppendLine($"      \"InteractionId\": \"{interaction.InteractionId}\",");
        json.AppendLine($"      \"Timestamp\": \"{interaction.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\",");
        json.AppendLine($"      \"TriggerId\": \"{interaction.TriggerId}\",");
        json.AppendLine($"      \"TriggerName\": \"{EscapeJsonString(interaction.TriggerName)}\",");
        json.AppendLine($"      \"PromptText\": \"{EscapeJsonString(interaction.PromptText)}\",");
        json.AppendLine($"      \"PromptCount\": {interaction.PromptCount},");
        json.AppendLine($"      \"NpcName\": \"{EscapeJsonString(interaction.NpcName)}\",");
        json.AppendLine($"      \"ResponseText\": \"{EscapeJsonString(interaction.ResponseText)}\",");
        json.AppendLine($"      \"ResponseLength\": {interaction.ResponseLength},");
        json.AppendLine($"      \"TtftMs\": {interaction.TtftMs},");
        json.AppendLine($"      \"PrefillTimeMs\": {interaction.PrefillTimeMs},");
        json.AppendLine($"      \"DecodeTimeMs\": {interaction.DecodeTimeMs},");
        json.AppendLine($"      \"TotalTimeMs\": {interaction.TotalTimeMs},");
        json.AppendLine($"      \"PromptTokenCount\": {interaction.PromptTokenCount},");
        json.AppendLine($"      \"GeneratedTokenCount\": {interaction.GeneratedTokenCount},");
        json.AppendLine($"      \"CachedTokenCount\": {interaction.CachedTokenCount},");
        json.AppendLine($"      \"TokensPerSecond\": {interaction.TokensPerSecond:F2},");
        json.AppendLine($"      \"WasTruncated\": {interaction.WasTruncated.ToString().ToLower()},");
        json.AppendLine($"      \"ValidationPassed\": {interaction.ValidationPassed.ToString().ToLower()},");
        json.AppendLine($"      \"ValidationFailureCount\": {interaction.ValidationFailureCount},");
        json.AppendLine($"      \"ConstraintViolationTypes\": \"{EscapeJsonString(interaction.ConstraintViolationTypes)}\",");
        json.AppendLine($"      \"HasCriticalFailure\": {interaction.HasCriticalFailure.ToString().ToLower()},");
        json.AppendLine($"      \"RetryCount\": {interaction.RetryCount},");
        json.AppendLine($"      \"TotalAttempts\": {interaction.TotalAttempts},");
        json.AppendLine($"      \"FallbackUsed\": {interaction.FallbackUsed.ToString().ToLower()},");
        json.AppendLine($"      \"FallbackTriggerReason\": \"{EscapeJsonString(interaction.FallbackTriggerReason)}\",");
        json.AppendLine($"      \"ConstraintCount\": {interaction.ConstraintCount},");
        json.AppendLine($"      \"ProhibitionCount\": {interaction.ProhibitionCount},");
        json.AppendLine($"      \"RequirementCount\": {interaction.RequirementCount},");
        json.AppendLine($"      \"PermissionCount\": {interaction.PermissionCount}");
        json.Append(i < session.Interactions.Count - 1 ? "    }," : "    }");
      }
      
      json.AppendLine("  ]");
      json.AppendLine("}");
      
      File.WriteAllText(filePath, json.ToString());
    }
    
    private string EscapeCsvField(string field)
    {
      if (string.IsNullOrEmpty(field)) return string.Empty;
      // Replace quotes with double quotes and wrap in quotes if contains comma, newline, or quote
      if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
      {
        return field.Replace("\"", "\"\"");
      }
      return field;
    }
    
    private string EscapeJsonString(string str)
    {
      if (string.IsNullOrEmpty(str)) return string.Empty;
      return str.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
    }
    
    /// <summary>
    /// Gets summary statistics for the current session
    /// </summary>
    /// <summary>
    /// Gets a formatted summary of the current session metrics.
    /// </summary>
    /// <returns>A formatted string containing session statistics</returns>
    public string GetSessionSummary()
    {
      if (currentSession == null || currentSession.TotalInteractions == 0)
      {
        return "No interactions recorded yet.";
      }
      
      var interactions = currentSession.Interactions;
      var avgTtft = interactions.Average(i => i.TtftMs);
      var avgDecode = interactions.Average(i => i.DecodeTimeMs);
      var avgTotal = interactions.Average(i => i.TotalTimeMs);
      var avgTokens = interactions.Average(i => i.GeneratedTokenCount);
      var avgTps = interactions.Average(i => i.TokensPerSecond);
      var truncatedCount = interactions.Count(i => i.WasTruncated);
      
      var summary = new StringBuilder();
      summary.AppendLine($"Session: {currentSession.SessionId}");
      summary.AppendLine($"Total Interactions: {currentSession.TotalInteractions}");
      summary.AppendLine($"Average TTFT: {avgTtft:F1}ms");
      summary.AppendLine($"Average Decode Time: {avgDecode:F1}ms");
      summary.AppendLine($"Average Total Time: {avgTotal:F1}ms");
      summary.AppendLine($"Average Tokens Generated: {avgTokens:F1}");
      summary.AppendLine($"Average Tokens/Second: {avgTps:F1}");
      summary.AppendLine($"Truncated Responses: {truncatedCount}/{currentSession.TotalInteractions}");
      
      // Architectural pattern metrics
      var validationPassedCount = interactions.Count(i => i.ValidationPassed);
      var validationPassRate = currentSession.TotalInteractions > 0 
        ? (double)validationPassedCount / currentSession.TotalInteractions * 100.0 
        : 0.0;
      var fallbackUsedCount = interactions.Count(i => i.FallbackUsed);
      var fallbackRate = currentSession.TotalInteractions > 0 
        ? (double)fallbackUsedCount / currentSession.TotalInteractions * 100.0 
        : 0.0;
      var avgRetries = interactions.Average(i => i.RetryCount);
      var criticalFailureCount = interactions.Count(i => i.HasCriticalFailure);
      
      summary.AppendLine("\nArchitectural Pattern Metrics:");
      summary.AppendLine($"  Validation Pass Rate: {validationPassRate:F1}% ({validationPassedCount}/{currentSession.TotalInteractions})");
      summary.AppendLine($"  Average Retries per Interaction: {avgRetries:F2}");
      summary.AppendLine($"  Fallback Usage Rate: {fallbackRate:F1}% ({fallbackUsedCount}/{currentSession.TotalInteractions})");
      summary.AppendLine($"  Critical Failures: {criticalFailureCount}");
      
      // Per-trigger statistics
      var triggerGroups = interactions.GroupBy(i => i.TriggerName);
      summary.AppendLine("\nPer-Trigger Statistics:");
      foreach (var group in triggerGroups)
      {
        summary.AppendLine($"  {group.Key}: {group.Count()} interactions");
      }
      
      return summary.ToString();
    }

    /// <summary>
    /// Exports validation statistics to a separate CSV file.
    /// </summary>
    /// <param name="session">The session to export statistics for</param>
    /// <param name="fileIndex">The file index for rolling files</param>
    public void ExportValidationStatistics(DialogueMetricsCollection session, int fileIndex = 0)
    {
      string basePath = Path.Combine(Application.persistentDataPath, exportDirectory);
      if (!Directory.Exists(basePath))
      {
        Directory.CreateDirectory(basePath);
      }

      string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
      string baseFileName = fileIndex > 0 
        ? $"ValidationStats_{session.SessionId}_{timestamp}_part{fileIndex:D3}"
        : $"ValidationStats_{session.SessionId}_{timestamp}";
      
      string csvPath = Path.Combine(basePath, $"{baseFileName}.csv");
      
      var csv = new StringBuilder();
      csv.AppendLine("ValidationPassed,ValidationFailureCount,ConstraintViolationTypes,HasCriticalFailure," +
                     "RetryCount,TotalAttempts,FallbackUsed,ConstraintCount,ProhibitionCount,RequirementCount,PermissionCount," +
                     "InteractionId,TriggerName,NpcName");
      
      foreach (var interaction in session.Interactions)
      {
        csv.AppendLine($"{interaction.ValidationPassed}," +
                      $"{interaction.ValidationFailureCount}," +
                      $"\"{EscapeCsvField(interaction.ConstraintViolationTypes)}\"," +
                      $"{interaction.HasCriticalFailure}," +
                      $"{interaction.RetryCount}," +
                      $"{interaction.TotalAttempts}," +
                      $"{interaction.FallbackUsed}," +
                      $"{interaction.ConstraintCount}," +
                      $"{interaction.ProhibitionCount}," +
                      $"{interaction.RequirementCount}," +
                      $"{interaction.PermissionCount}," +
                      $"{interaction.InteractionId}," +
                      $"{EscapeCsvField(interaction.TriggerName)}," +
                      $"{EscapeCsvField(interaction.NpcName)}");
      }
      
      File.WriteAllText(csvPath, csv.ToString());
      Debug.Log($"[DialogueMetricsCollector] Exported validation statistics to: {csvPath}");
    }

    /// <summary>
    /// Exports constraint violation details to a separate CSV file.
    /// </summary>
    /// <param name="session">The session to export violations for</param>
    /// <param name="fileIndex">The file index for rolling files</param>
    public void ExportConstraintViolations(DialogueMetricsCollection session, int fileIndex = 0)
    {
      // Filter to only interactions with violations
      var violations = session.Interactions
        .Where(i => !i.ValidationPassed && !string.IsNullOrEmpty(i.ConstraintViolationTypes))
        .ToList();
      
      if (violations.Count == 0)
      {
        Debug.Log("[DialogueMetricsCollector] No constraint violations to export");
        return;
      }

      string basePath = Path.Combine(Application.persistentDataPath, exportDirectory);
      if (!Directory.Exists(basePath))
      {
        Directory.CreateDirectory(basePath);
      }

      string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
      string baseFileName = fileIndex > 0 
        ? $"ConstraintViolations_{session.SessionId}_{timestamp}_part{fileIndex:D3}"
        : $"ConstraintViolations_{session.SessionId}_{timestamp}";
      
      string csvPath = Path.Combine(basePath, $"{baseFileName}.csv");
      
      var csv = new StringBuilder();
      csv.AppendLine("InteractionId,Timestamp,TriggerName,NpcName,ConstraintViolationTypes," +
                     "ValidationFailureCount,HasCriticalFailure,RetryCount,TotalAttempts," +
                     "ConstraintCount,ProhibitionCount,RequirementCount,PermissionCount,ResponseText");
      
      foreach (var interaction in violations)
      {
        csv.AppendLine($"{interaction.InteractionId}," +
                      $"{interaction.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                      $"{EscapeCsvField(interaction.TriggerName)}," +
                      $"{EscapeCsvField(interaction.NpcName)}," +
                      $"\"{EscapeCsvField(interaction.ConstraintViolationTypes)}\"," +
                      $"{interaction.ValidationFailureCount}," +
                      $"{interaction.HasCriticalFailure}," +
                      $"{interaction.RetryCount}," +
                      $"{interaction.TotalAttempts}," +
                      $"{interaction.ConstraintCount}," +
                      $"{interaction.ProhibitionCount}," +
                      $"{interaction.RequirementCount}," +
                      $"{interaction.PermissionCount}," +
                      $"\"{EscapeCsvField(interaction.ResponseText)}\"");
      }
      
      File.WriteAllText(csvPath, csv.ToString());
      Debug.Log($"[DialogueMetricsCollector] Exported {violations.Count} constraint violations to: {csvPath}");
    }
    
    private void OnApplicationQuit()
    {
      if (autoExportOnQuit && currentSession != null && currentSession.TotalInteractions > 0)
      {
        ExportCurrentSession();
      }
    }
    
    [ContextMenu("Export Current Session")]
    private void ExportCurrentSessionMenu()
    {
      ExportCurrentSession();
    }
    
    [ContextMenu("Print Session Summary")]
    private void PrintSessionSummary()
    {
      Debug.Log(GetSessionSummary());
    }
  }
}

