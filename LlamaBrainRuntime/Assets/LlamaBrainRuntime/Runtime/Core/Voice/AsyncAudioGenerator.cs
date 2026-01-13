using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.InferenceEngine;
using UnityEngine;
using uPiper.Core;
using uPiper.Core.AudioGeneration;

namespace LlamaBrain.Runtime.Core.Voice
{
  /// <summary>
  /// Async audio generator that runs inference in a way that doesn't block the main thread.
  /// Wraps Unity.InferenceEngine to execute inference operations off the main thread where possible.
  /// </summary>
  public sealed class AsyncAudioGenerator : IDisposable
  {
    private Worker _worker;
    private Model _model;
    private PiperVoiceConfig _voiceConfig;
    private bool _isInitialized;
    private bool _disposed;
    private BackendType _backendType;

    public bool IsInitialized => _isInitialized;
    public int SampleRate => _voiceConfig?.SampleRate ?? 22050;

    /// <summary>
    /// Initialize the generator. Must be called from main thread.
    /// </summary>
    public async UniTask InitializeAsync(ModelAsset modelAsset, PiperVoiceConfig config, CancellationToken ct = default)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(AsyncAudioGenerator));

      if (modelAsset == null)
        throw new ArgumentNullException(nameof(modelAsset));

      if (config == null)
        throw new ArgumentNullException(nameof(config));

      // Quick check without lock
      if (_isInitialized)
        return;

      // Model loading must be on main thread
      await UniTask.SwitchToMainThread(ct);

      // Double-check after switching threads, but don't use lock across async boundaries
      if (_isInitialized)
        return;

      try
      {
        _voiceConfig = config;
        Debug.Log("[AsyncAudioGenerator] Loading model...");
        _model = ModelLoader.Load(modelAsset);

        if (_model == null)
          throw new InvalidOperationException("ModelLoader.Load returned null");

        // Determine backend - prefer CPU for better background thread compatibility
        _backendType = DetermineBackendType();

        Debug.Log("[AsyncAudioGenerator] Creating worker...");
        _worker = new Worker(_model, _backendType);
        _isInitialized = true;

        Debug.Log($"[AsyncAudioGenerator] Initialized with backend: {_backendType}");
      }
      catch (Exception ex)
      {
        Debug.LogError($"[AsyncAudioGenerator] Initialization failed: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Generate audio from phoneme IDs. Runs inference spread across multiple frames.
    /// </summary>
    public async UniTask<float[]> GenerateAudioAsync(
        int[] phonemeIds,
        float lengthScale = 1.0f,
        float noiseScale = 0.667f,
        float noiseW = 0.8f,
        CancellationToken ct = default)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(AsyncAudioGenerator));

      if (!_isInitialized)
        throw new InvalidOperationException("Generator not initialized");

      if (phonemeIds == null || phonemeIds.Length == 0)
        throw new ArgumentException("Phoneme IDs cannot be null or empty");

      ct.ThrowIfCancellationRequested();

      // Must be on main thread for Sentis operations
      await UniTask.SwitchToMainThread(ct);

      // Prepare tensors - track all for cleanup
      Tensor<int> inputTensor = null;
      Tensor<int> inputLengthsTensor = null;
      Tensor<float> scalesTensor = null;
      Tensor<float> outputTensor = null;
      Tensor<float> readableTensor = null;

      try
      {
        inputTensor = new Tensor<int>(new TensorShape(1, phonemeIds.Length), phonemeIds);
        inputLengthsTensor = new Tensor<int>(new TensorShape(1), new[] { phonemeIds.Length });
        scalesTensor = new Tensor<float>(new TensorShape(3), new[] { noiseScale, lengthScale, noiseW });

        // Set inputs
        if (_model.inputs.Count >= 3)
        {
          _worker.SetInput(_model.inputs[0].name, inputTensor);
          _worker.SetInput(_model.inputs[1].name, inputLengthsTensor);
          _worker.SetInput(_model.inputs[2].name, scalesTensor);
        }

        // Use ScheduleIterable to spread inference across frames
        Debug.Log("[AsyncAudioGenerator] Starting inference...");
        var enumerator = _worker.ScheduleIterable();
        var iterationCount = 0;
        const int iterationsPerYield = 50;
        const int maxIterations = 100000; // Safety limit

        while (enumerator.MoveNext())
        {
          iterationCount++;
          if (iterationCount > maxIterations)
          {
            Debug.LogError($"[AsyncAudioGenerator] Exceeded max iterations ({maxIterations}), aborting");
            throw new InvalidOperationException("Inference exceeded maximum iterations");
          }

          if (iterationCount % iterationsPerYield == 0)
          {
            ct.ThrowIfCancellationRequested();
            await UniTask.Yield();
          }
        }

        Debug.Log($"[AsyncAudioGenerator] Inference complete after {iterationCount} iterations");

        ct.ThrowIfCancellationRequested();

        // Get output
        var outputName = _model.outputs.Count > 0 ? _model.outputs[0].name : null;

        try
        {
          outputTensor = outputName != null
              ? _worker.PeekOutput(outputName) as Tensor<float>
              : _worker.PeekOutput() as Tensor<float>;
        }
        catch
        {
          outputTensor = _worker.PeekOutput() as Tensor<float>;
        }

        if (outputTensor == null)
          throw new InvalidOperationException("Failed to get output from model");

        // ReadbackAndClone - yield before to let UI breathe
        await UniTask.Yield();
        readableTensor = outputTensor.ReadbackAndClone();

        // Copy data on thread pool
        await UniTask.SwitchToThreadPool();

        var length = readableTensor.shape.length;
        var result = new float[length];
        for (int i = 0; i < length; i++)
        {
          result[i] = readableTensor[i];
        }

        return result;
      }
      finally
      {
        // Always cleanup on main thread
        try
        {
          // Use a linked token that won't throw if original is cancelled
          await UniTask.SwitchToMainThread(CancellationToken.None);
        }
        catch
        {
          // Already on main thread or can't switch - try cleanup anyway
        }

        try { inputTensor?.Dispose(); } catch { }
        try { inputLengthsTensor?.Dispose(); } catch { }
        try { scalesTensor?.Dispose(); } catch { }
        try { outputTensor?.Dispose(); } catch { }
        try { readableTensor?.Dispose(); } catch { }
      }
    }

    private BackendType DetermineBackendType()
    {
      // CPU backend is most compatible with threading
      // GPU backends may have issues with async execution
      if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Metal)
      {
        Debug.Log("[AsyncAudioGenerator] Using CPU backend (Metal compatibility)");
        return BackendType.CPU;
      }

      // For now, prefer CPU for stability with async operations
      // GPU can be faster but introduces synchronization complexity
      Debug.Log("[AsyncAudioGenerator] Using CPU backend (async compatibility)");
      return BackendType.CPU;
    }

    public void Dispose()
    {
      if (_disposed)
        return;

      _disposed = true;

      try
      {
        _worker?.Dispose();
      }
      catch
      {
        // Ignore disposal errors
      }

      _worker = null;
      _model = null;
      _voiceConfig = null;
      _isInitialized = false;
    }
  }
}
