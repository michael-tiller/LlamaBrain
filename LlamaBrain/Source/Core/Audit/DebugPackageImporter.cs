using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Imports debug packages from JSON or compressed bytes for replay.
  /// Performs validation of integrity, version, and model fingerprint.
  /// Supports automatic detection of compressed packages.
  /// </summary>
  /// <remarks>
  /// Performance targets:
  /// - Import 50 turns: &lt; 500ms
  ///
  /// Compression:
  /// - Compressed packages are auto-detected by magic header "LBPK"
  /// - Supports GZip decompression
  /// </remarks>
  public sealed class DebugPackageImporter
  {
    /// <summary>
    /// Imports a debug package from JSON.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <param name="validateIntegrity">Whether to validate the package integrity hash.</param>
    /// <returns>Import result containing the package or error information.</returns>
    public ImportResult FromJson(string json, bool validateIntegrity = false)
    {
      // Validate input
      if (string.IsNullOrWhiteSpace(json))
      {
        return ImportResult.Failed("JSON content is null or empty.");
      }

      // Parse JSON
      DebugPackage package;
      try
      {
        package = JsonConvert.DeserializeObject<DebugPackage>(json)!;
        if (package == null)
        {
          return ImportResult.Failed("Failed to parse JSON: result was null.");
        }
      }
      catch (JsonException ex)
      {
        return ImportResult.Failed($"Failed to parse JSON: {ex.Message}");
      }

      var result = ImportResult.Succeeded(package);

      // Validate version
      if (package.Version > DebugPackage.CurrentVersion)
      {
        result.VersionSupported = false;
        result.Warnings.Add($"Package version {package.Version} is newer than supported version {DebugPackage.CurrentVersion}. Some features may not work correctly.");
      }

      // Validate integrity if requested
      if (validateIntegrity)
      {
        if (string.IsNullOrEmpty(package.PackageIntegrityHash))
        {
          result.Success = false;
          result.IntegrityValid = false;
          result.ErrorMessage = "Package integrity validation failed: no integrity hash present.";
          return result;
        }

        var isValid = package.ValidateIntegrity();
        result.IntegrityValid = isValid;

        if (!isValid)
        {
          result.Success = false;
          result.ErrorMessage = "Package integrity validation failed: hash mismatch. The package may have been tampered with.";
          return result;
        }
      }

      return result;
    }

    /// <summary>
    /// Imports a debug package from bytes (auto-detects compression).
    /// </summary>
    /// <param name="data">The byte array containing the package (JSON or compressed).</param>
    /// <param name="validateIntegrity">Whether to validate the package integrity hash.</param>
    /// <returns>Import result containing the package or error information.</returns>
    public ImportResult FromBytes(byte[] data, bool validateIntegrity = false)
    {
      if (data == null || data.Length == 0)
      {
        return ImportResult.Failed("Data is null or empty.");
      }

      // Check if compressed
      if (DebugPackageExporter.IsCompressed(data))
      {
        return FromCompressedBytes(data, validateIntegrity);
      }
      else
      {
        // Assume UTF8 JSON
        var json = Encoding.UTF8.GetString(data);
        return FromJson(json, validateIntegrity);
      }
    }

    /// <summary>
    /// Imports a debug package from compressed bytes.
    /// </summary>
    /// <param name="compressedData">The compressed byte array with magic header.</param>
    /// <param name="validateIntegrity">Whether to validate the package integrity hash.</param>
    /// <returns>Import result containing the package or error information.</returns>
    public ImportResult FromCompressedBytes(byte[] compressedData, bool validateIntegrity = false)
    {
      if (compressedData == null || compressedData.Length == 0)
      {
        return ImportResult.Failed("Compressed data is null or empty.");
      }

      // Verify magic header
      if (!DebugPackageExporter.IsCompressed(compressedData))
      {
        return ImportResult.Failed("Invalid compressed package: missing magic header.");
      }

      try
      {
        // Skip magic header and decompress
        using (var inputStream = new MemoryStream(compressedData, DebugPackageExporter.CompressedMagicHeader.Length,
          compressedData.Length - DebugPackageExporter.CompressedMagicHeader.Length))
        using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        using (var outputStream = new MemoryStream())
        {
          gzipStream.CopyTo(outputStream);
          var json = Encoding.UTF8.GetString(outputStream.ToArray());
          var result = FromJson(json, validateIntegrity);

          // Mark as compressed in result
          if (result.Success)
          {
            result.WasCompressed = true;
            result.CompressedSize = compressedData.Length;
            result.UncompressedSize = outputStream.Length;
          }

          return result;
        }
      }
      catch (InvalidDataException ex)
      {
        return ImportResult.Failed($"Failed to decompress package: {ex.Message}");
      }
      catch (Exception ex)
      {
        return ImportResult.Failed($"Failed to import compressed package: {ex.Message}");
      }
    }

    /// <summary>
    /// Imports a debug package from a file (auto-detects compression by content).
    /// </summary>
    /// <param name="filePath">Path to the package file.</param>
    /// <param name="validateIntegrity">Whether to validate the package integrity hash.</param>
    /// <returns>Import result containing the package or error information.</returns>
    public ImportResult FromFile(string filePath, bool validateIntegrity = false)
    {
      if (string.IsNullOrWhiteSpace(filePath))
      {
        return ImportResult.Failed("File path is null or empty.");
      }

      if (!File.Exists(filePath))
      {
        return ImportResult.Failed($"File not found: {filePath}");
      }

      try
      {
        var data = File.ReadAllBytes(filePath);
        return FromBytes(data, validateIntegrity);
      }
      catch (Exception ex)
      {
        return ImportResult.Failed($"Failed to read file: {ex.Message}");
      }
    }

    /// <summary>
    /// Validates the model fingerprint from a package against the current model.
    /// </summary>
    /// <param name="package">The debug package containing the original fingerprint.</param>
    /// <param name="currentFingerprint">The fingerprint of the currently loaded model.</param>
    /// <returns>Validation result indicating match status.</returns>
    public ModelFingerprintValidationResult ValidateModelFingerprint(
      DebugPackage package,
      ModelFingerprint currentFingerprint)
    {
      if (package == null)
        throw new ArgumentNullException(nameof(package));

      if (currentFingerprint == null)
        throw new ArgumentNullException(nameof(currentFingerprint));

      var packageFingerprint = package.ModelFingerprint;

      var result = new ModelFingerprintValidationResult
      {
        PackageFingerprint = packageFingerprint,
        CurrentFingerprint = currentFingerprint,
        IsExactMatch = packageFingerprint.Matches(currentFingerprint),
        IsCompatible = packageFingerprint.IsCompatibleWith(currentFingerprint)
      };

      // Generate mismatch description
      if (!result.IsExactMatch)
      {
        if (!result.IsCompatible)
        {
          result.MismatchDescription = $"Model mismatch: package uses '{packageFingerprint.ModelFileName}' " +
            $"({packageFingerprint.ModelFileSizeBytes} bytes), " +
            $"but current model is '{currentFingerprint.ModelFileName}' " +
            $"({currentFingerprint.ModelFileSizeBytes} bytes).";
        }
        else
        {
          result.MismatchDescription = $"Model configuration differs: package used context length " +
            $"{packageFingerprint.ContextLength}, current is {currentFingerprint.ContextLength}. " +
            "Replay may produce slightly different outputs.";
        }
      }

      return result;
    }
  }
}
