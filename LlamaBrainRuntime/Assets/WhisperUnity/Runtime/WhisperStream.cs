using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Whisper.Utils;
// ReSharper disable once RedundantUsingDirective
using System.Linq;

namespace Whisper
{
    public delegate void OnStreamResultUpdatedDelegate(string updatedResult);
    public delegate void OnStreamSegmentUpdatedDelegate(WhisperResult segment);
    public delegate void OnStreamSegmentFinishedDelegate(WhisperResult segment);
    public delegate void OnStreamFinishedDelegate(string finalResult);

    /// <summary>
    /// Parameters of whisper streaming processing.
    /// </summary>
    public class WhisperStreamParams
    {
        /// <summary>
        /// Regular whisper inference params.
        /// </summary>
        public readonly WhisperParams InferenceParam;

        /// <summary>
        /// Audio stream frequency. Can't change during transcription.
        /// </summary>
        public readonly int Frequency;

        /// <summary>
        /// Audio stream channels count. Can't change during transcription.
        /// </summary>
        public readonly int Channels;

        /// <summary>
        /// Minimal portions of audio that will be processed by whisper stream in seconds.
        /// </summary>
        public readonly float StepSec;

        /// <summary>
        /// Minimal portions of audio that will be processed by whisper in audio samples.
        /// </summary>
        public readonly int StepSamples;

        /// <summary>
        /// How many seconds of previous segment will be used for current segment.
        /// </summary>
        public readonly float KeepSec;

        /// <summary>
        /// How many samples of previous audio chunk will be used for current chunk.
        /// </summary>
        public readonly int KeepSamples;

        /// <summary>
        /// How many seconds of audio will be recurrently transcribe until context update.
        /// </summary>
        public readonly float LengthSec;

        /// <summary>
        /// How many samples of audio will be recurrently transcribe until context update.
        /// </summary>
        public readonly int LengthSamples;

        /// <summary>
        /// Should stream modify whisper prompt for better context handling?
        /// </summary>
        public readonly bool UpdatePrompt;

        /// <summary>
        /// How many recurrent iterations will be used for one chunk?
        /// </summary>
        public readonly int StepsCount;

        /// <summary>
        /// If false stream will use all information from previous iteration.
        /// </summary>
        public readonly bool DropOldBuffer;

        /// <summary>
        /// If true stream will ignore audio chunks with no detected speech.
        /// </summary>
        public readonly bool UseVad;

        public WhisperStreamParams(WhisperParams inferenceParam,
            int frequency, int channels,
            float stepSec = 3f, float keepSec = 0.2f, float lengthSec = 10f,
            bool updatePrompt = true, bool dropOldBuffer = false,
            bool useVad = false)
        {
            InferenceParam = inferenceParam;
            Frequency = frequency;
            Channels = channels;

            StepSec = stepSec;
            StepSamples = (int)(StepSec * Frequency * Channels);

            KeepSec = keepSec;
            KeepSamples = (int)(KeepSec * frequency * channels);

            LengthSec = lengthSec;
            LengthSamples = (int)(LengthSec * frequency * channels);

            StepsCount = Math.Max(1, (int)(LengthSec / StepSec) - 1);

            UpdatePrompt = updatePrompt;
            DropOldBuffer = dropOldBuffer;
            UseVad = useVad;
        }
    }

    /// <summary>
    /// Handling all streaming logic (sliding-window, VAD, etc).
    /// Fixed version: moves heavy buffer operations off main thread.
    /// </summary>
    public class WhisperStream
    {
        /// <summary>
        /// Raised when whisper updated stream transcription.
        /// Result contains a full stream transcript from the stream beginning.
        /// </summary>
        public event OnStreamResultUpdatedDelegate OnResultUpdated;
        /// <summary>
        /// Raised when whisper updated current segment transcript.
        /// </summary>
        public event OnStreamSegmentUpdatedDelegate OnSegmentUpdated;
        /// <summary>
        /// Raised when whisper finished current segment transcript.
        /// </summary>
        public event OnStreamSegmentFinishedDelegate OnSegmentFinished;
        /// <summary>
        /// Raised when whisper finished stream transcription and can start another one.
        /// </summary>
        public event OnStreamFinishedDelegate OnStreamFinished;

        private readonly WhisperWrapper _wrapper;
        private readonly WhisperStreamParams _param;
        private readonly string _originalPrompt;
        private readonly MicrophoneRecord _microphone;

        // Thread-safe buffer using lock
        private readonly object _bufferLock = new object();
        private readonly List<float> _newBuffer = new List<float>();
        private float[] _oldBuffer = Array.Empty<float>();
        private string _output = "";
        private int _step;
        private volatile bool _isStreaming;
        private volatile bool _isProcessing;

        private Task _processingTask;

        /// <summary>
        /// Create a new instance of Whisper streaming transcription.
        /// </summary>
        /// <param name="wrapper">Loaded Whisper model which will be used for transcription.</param>
        /// <param name="param">Whisper streaming parameters.</param>
        /// <param name="microphone">Optional microphone input for stream.</param>
        public WhisperStream(WhisperWrapper wrapper, WhisperStreamParams param,
            MicrophoneRecord microphone = null)
        {
            _wrapper = wrapper;
            _param = param;
            _originalPrompt = _param.InferenceParam.InitialPrompt;
            _microphone = microphone;
        }

        /// <summary>
        /// Start a new streaming transcription. Must be called before
        /// you start adding new audio chunks.
        /// </summary>
        /// <remarks>
        /// If you set microphone into constructor, it will start listening to it.
        /// Make sure you started microphone by <see cref="MicrophoneRecord.StartRecord"/>.
        /// There is no need to add audio chunks manually using <see cref="AddToStream"/>.
        /// </remarks>
        public void StartStream()
        {
            if (_isStreaming)
            {
                LogUtils.Warning("Stream is already working!");
                return;
            }
            _isStreaming = true;

            // if we set microphone - streaming works in auto mode
            if (_microphone != null)
            {
                _microphone.OnChunkReady += MicrophoneOnChunkReady;
                _microphone.OnRecordStop += MicrophoneOnRecordStop;
            }
        }

        /// <summary>
        /// Manually add a new chunk of audio to streaming.
        /// Make sure to call <see cref="StartStream"/> first.
        /// This method is now non-blocking - it queues the chunk for processing.
        /// </summary>
        /// <remarks>
        /// If you set microphone into constructor, it will be called automatically.
        /// </remarks>
        public void AddToStream(AudioChunk chunk)
        {
            if (!_isStreaming)
            {
                LogUtils.Warning("Start streaming first!");
                return;
            }

            // Quick path: if using VAD and no voice detected at start, just store reference
            if (_param.UseVad && !chunk.IsVoiceDetected && _step <= 0)
            {
                lock (_bufferLock)
                {
                    _oldBuffer = chunk.Data;
                }
                return;
            }

            // Add chunk data to buffer (fast operation on main thread)
            lock (_bufferLock)
            {
                _newBuffer.AddRange(chunk.Data);
            }

            // Trigger processing on thread pool if not already running
            TryStartProcessing(forceSegmentEnd: _param.UseVad && !chunk.IsVoiceDetected);
        }

        /// <summary>
        /// Stop current streaming transcription. It will process last
        /// audio chunks and raise <see cref="OnStreamFinished"/> when it's done.
        /// </summary>
        public void StopStream()
        {
            if (!_isStreaming)
            {
                // Stream already stopped (e.g., by VAD timeout) - not an error
                LogUtils.Verbose("StopStream called but stream already stopped");
                return;
            }
            _isStreaming = false;

            // unsubscribe from microphone events for now
            if (_microphone != null)
            {
                _microphone.OnChunkReady -= MicrophoneOnChunkReady;
                _microphone.OnRecordStop -= MicrophoneOnRecordStop;
            }

            // Start final processing on thread pool
            Task.Run(async () =>
            {
                try
                {
                    // Wait for any current processing to complete
                    if (_processingTask != null)
                        await _processingTask;

                    // Process remaining audio
                    await ProcessSlidingWindowAsync(forceSegmentEnd: true);

                    // Capture output value before Reset() clears it
                    // (closure would capture by reference otherwise)
                    var finalOutput = _output;

                    // Invoke on main thread via dispatcher
                    MainThreadDispatcher.Enqueue(() => OnStreamFinished?.Invoke(finalOutput));

                    // Reset state
                    Reset();
                }
                catch (Exception ex)
                {
                    LogUtils.Error($"Error in StopStream: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Tries to start processing if not already running.
        /// </summary>
        private void TryStartProcessing(bool forceSegmentEnd)
        {
            // Quick check without lock
            if (_isProcessing)
                return;

            // Double-check with lock
            lock (_bufferLock)
            {
                if (_isProcessing)
                    return;

                // Check if we have enough data
                if (!forceSegmentEnd && _newBuffer.Count < _param.StepSamples)
                    return;

                _isProcessing = true;
            }

            // Start processing on thread pool
            _processingTask = Task.Run(async () =>
            {
                try
                {
                    await ProcessSlidingWindowAsync(forceSegmentEnd);
                }
                catch (Exception ex)
                {
                    LogUtils.Error($"Error in ProcessSlidingWindowAsync: {ex.Message}");
                }
                finally
                {
                    _isProcessing = false;
                }
            });
        }

        /// <summary>
        /// Processes the sliding window entirely on thread pool.
        /// </summary>
        private async Task ProcessSlidingWindowAsync(bool forceSegmentEnd)
        {
            float[] buffer;
            int newBufferLen;

            // Snapshot and clear buffer under lock
            lock (_bufferLock)
            {
                newBufferLen = _newBuffer.Count;
                if (!forceSegmentEnd && newBufferLen < _param.StepSamples)
                    return;

                // Calculate how much we can get from _oldBuffer
                var oldBufferLen = _oldBuffer.Length;
                int nSamplesTake;
                if (_param.DropOldBuffer)
                {
                    nSamplesTake = Math.Min(oldBufferLen,
                        Math.Max(0, _param.KeepSamples + _param.LengthSamples - newBufferLen));
                }
                else
                {
                    nSamplesTake = oldBufferLen;
                }

                // Create combined buffer
                var bufferLen = nSamplesTake + newBufferLen;
                if (bufferLen == 0)
                    return;

                buffer = new float[bufferLen];
                var oldBufferStart = oldBufferLen - nSamplesTake;

                if (nSamplesTake > 0)
                    Array.Copy(_oldBuffer, oldBufferStart, buffer, 0, nSamplesTake);

                if (newBufferLen > 0)
                    _newBuffer.CopyTo(0, buffer, nSamplesTake, newBufferLen);

                _newBuffer.Clear();
            }

            // Run whisper inference (already on thread pool)
            var res = await Task.Run(() => _wrapper.GetText(buffer, _param.Frequency,
                _param.Channels, _param.InferenceParam));

            if (res == null)
                return;

            var currentSegment = res.Result;

            // Filter out Whisper artifacts that indicate no actual speech
            if (IsWhisperArtifact(currentSegment))
                currentSegment = "";

            var currentOutput = _output + currentSegment;

            // Dispatch events to main thread
            MainThreadDispatcher.Enqueue(() =>
            {
                OnSegmentUpdated?.Invoke(res);
                OnResultUpdated?.Invoke(currentOutput);
            });

            // Update state
            _step++;
            if (forceSegmentEnd || _step >= _param.StepsCount)
            {
                LogUtils.Verbose($"Stream finished an old segment with total steps of {_step}");
                _output = currentOutput;

                if (_param.UpdatePrompt)
                    _param.InferenceParam.InitialPrompt = _originalPrompt + _output;

                // Trim old buffer
                lock (_bufferLock)
                {
                    var updBufferLen = Math.Min(_param.KeepSamples, buffer.Length);
                    if (updBufferLen > 0)
                    {
                        var segment = new ArraySegment<float>(buffer, buffer.Length - updBufferLen, updBufferLen);
                        _oldBuffer = segment.ToArray();
                    }
                    else
                    {
                        _oldBuffer = Array.Empty<float>();
                    }
                }
                _step = 0;

                MainThreadDispatcher.Enqueue(() => OnSegmentFinished?.Invoke(res));
            }
            else
            {
                LogUtils.Verbose("Stream continues current segment");
                lock (_bufferLock)
                {
                    _oldBuffer = buffer;
                }
            }
        }

        private void Reset()
        {
            lock (_bufferLock)
            {
                _output = "";
                _step = 0;
                _oldBuffer = Array.Empty<float>();
                _newBuffer.Clear();
            }
        }

        private void MicrophoneOnChunkReady(AudioChunk chunk)
        {
            AddToStream(chunk);
        }

        private void MicrophoneOnRecordStop(AudioChunk recordedAudio)
        {
            StopStream();
        }

        /// <summary>
        /// Check if the text is a Whisper artifact (not actual speech).
        /// </summary>
        private static bool IsWhisperArtifact(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return true;

            var trimmed = text.Trim();

            // Common Whisper artifacts indicating no speech or non-speech audio
            return trimmed.Equals("[BLANK_AUDIO]", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("[MUSIC]", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("[APPLAUSE]", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("[LAUGHTER]", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("[SILENCE]", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("[NOISE]", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("[INAUDIBLE]", StringComparison.OrdinalIgnoreCase);
        }

    }
}
