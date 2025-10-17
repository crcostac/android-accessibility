using Android.Media;
using Android.Media.Projection;
using Subzy.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Subzy.Platforms.Android;

/// <summary>
/// Android-specific audio capture service using AudioRecord.
/// Supports both microphone capture and audio playback capture (from other apps).
/// </summary>
public class AudioCaptureService : IDisposable
{
    private readonly ILoggingService _logger;
    private AudioRecord? _audioRecord;
    private bool _isCapturing;
    private CancellationTokenSource? _captureCts;
    private Task? _captureTask;
    private bool _disposed;

    // Audio configuration
    private readonly int _sampleRate;
    private readonly ChannelIn _channelConfig;
    private readonly Encoding _audioFormat;
    private readonly int _bufferSize;
    private readonly MediaProjection? _mediaProjection;
    private readonly bool _capturePlayback;

    /// <summary>
    /// Event raised when audio data is captured.
    /// </summary>
    public event EventHandler<byte[]>? AudioDataCaptured;

    /// <summary>
    /// Event raised when an error occurs during capture.
    /// </summary>
    public event EventHandler<Exception>? ErrorOccurred;

    /// <summary>
    /// Gets whether audio capture is currently active.
    /// </summary>
    public bool IsCapturing => _isCapturing;

    /// <summary>
    /// Creates an audio capture service for microphone input.
    /// </summary>
    public AudioCaptureService(ILoggingService logger, int sampleRate = 16000, int channels = 1, int bufferSize = 3200)
    {
        _logger = logger;
        _sampleRate = sampleRate;
        _channelConfig = channels == 1 ? ChannelIn.Mono : ChannelIn.Stereo;
        _audioFormat = Encoding.Pcm16bit;
        _bufferSize = bufferSize;
        _mediaProjection = null;
        _capturePlayback = false;
    }

    /// <summary>
    /// Creates an audio capture service for audio playback capture (from other apps).
    /// Requires Android 10+ (API 29+) and MediaProjection permission.
    /// </summary>
    public AudioCaptureService(
        ILoggingService logger, 
        MediaProjection mediaProjection,
        int sampleRate = 16000, 
        int channels = 1, 
        int bufferSize = 3200)
    {
        _logger = logger;
        _sampleRate = sampleRate;
        _channelConfig = channels == 1 ? ChannelIn.Mono : ChannelIn.Stereo;
        _audioFormat = Encoding.Pcm16bit;
        _bufferSize = bufferSize;
        _mediaProjection = mediaProjection ?? throw new ArgumentNullException(nameof(mediaProjection));
        _capturePlayback = true;
    }

    /// <summary>
    /// Starts audio capture from the microphone or from app playback.
    /// </summary>
    public Task StartCaptureAsync()
    {
        if (_isCapturing)
        {
            _logger.Warning("Audio capture is already active");
            return Task.CompletedTask;
        }

        try
        {
            // Get minimum buffer size
            var minBufferSize = AudioRecord.GetMinBufferSize(_sampleRate, _channelConfig, _audioFormat);
            if (minBufferSize < 0)
            {
                throw new InvalidOperationException("Unable to get minimum buffer size for audio recording");
            }

            // Use the larger of requested buffer size or minimum buffer size
            var actualBufferSize = Math.Max(_bufferSize, minBufferSize);

            if (_capturePlayback)
            {
                _logger.Info($"Starting audio playback capture: {_sampleRate}Hz, buffer size: {actualBufferSize} bytes (min: {minBufferSize})");
                StartPlaybackCapture(actualBufferSize);
            }
            else
            {
                _logger.Info($"Starting microphone capture: {_sampleRate}Hz, buffer size: {actualBufferSize} bytes (min: {minBufferSize})");
                StartMicrophoneCapture(actualBufferSize);
            }

            if (_audioRecord?.State != State.Initialized)
            {
                throw new InvalidOperationException("Failed to initialize AudioRecord");
            }

            // Start recording
            _audioRecord.StartRecording();
            _isCapturing = true;

            // Start capture loop
            _captureCts = new CancellationTokenSource();
            _captureTask = Task.Run(() => CaptureLoop(_captureCts.Token), _captureCts.Token);

            _logger.Info($"Audio capture started successfully ({(_capturePlayback ? "playback" : "microphone")} mode)");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to start audio capture", ex);
            ErrorOccurred?.Invoke(this, ex);
            CleanupAudioRecord();
            throw;
        }
    }

    private void StartMicrophoneCapture(int bufferSize)
    {
        // Create AudioRecord instance for microphone
        _audioRecord = new AudioRecord(
            AudioSource.Mic,
            _sampleRate,
            _channelConfig,
            _audioFormat,
            bufferSize);
    }

    private void StartPlaybackCapture(int bufferSize)
    {
        if (_mediaProjection == null)
        {
            throw new InvalidOperationException("MediaProjection is required for audio playback capture");
        }

        // Check Android version
        if (global::Android.OS.Build.VERSION.SdkInt < global::Android.OS.BuildVersionCodes.Q)
        {
            throw new NotSupportedException("Audio playback capture requires Android 10 (API 29) or higher");
        }

        try
        {
            // Configure audio playback capture
            // IMPORTANT: Cannot mix AddMatchingUsage (inclusive) with ExcludeUsage (exclusive)
            // Use ONLY AddMatchingUsage to specify what audio to capture
            var captureConfig = new AudioPlaybackCaptureConfiguration.Builder(_mediaProjection)
                // Capture only media audio (movies, music, games)
                .AddMatchingUsage(AudioUsageKind.Media)
                .AddMatchingUsage(AudioUsageKind.Game)
                .Build();

            _logger.Info("Audio playback capture config: Capturing Media and Game audio only");

            // Configure audio format  
            // For AudioPlaybackCapture, AudioFormat expects ChannelOut for capture sources
            // Map ChannelIn to appropriate ChannelOut value
            var channelOut = _channelConfig == ChannelIn.Mono ? ChannelOut.Mono : ChannelOut.Stereo;
            
            var audioFormat = new AudioFormat.Builder()
                .SetEncoding(_audioFormat)
                .SetSampleRate(_sampleRate)
                .SetChannelMask(channelOut)
                .Build();

            // Create AudioRecord with playback capture configuration
            _audioRecord = new AudioRecord.Builder()
                .SetAudioPlaybackCaptureConfig(captureConfig)
                .SetAudioFormat(audioFormat)
                .SetBufferSizeInBytes(bufferSize)
                .Build();

            _logger.Info("Audio playback capture configured successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to configure audio playback capture", ex);
            throw;
        }
    }

    /// <summary>
    /// Stops audio capture.
    /// </summary>
    public async Task StopCaptureAsync()
    {
        if (!_isCapturing)
        {
            return;
        }

        _logger.Info("Stopping audio capture");

        try
        {
            _isCapturing = false;

            // Cancel the capture loop
            _captureCts?.Cancel();

            // Wait for capture task to complete
            if (_captureTask != null)
            {
                try
                {
                    await _captureTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelling
                }
            }

            // Stop recording
            _audioRecord?.Stop();

            _logger.Info("Audio capture stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Error stopping audio capture", ex);
        }
        finally
        {
            CleanupAudioRecord();
        }
    }

    private void CaptureLoop(CancellationToken cancellationToken)
    {
        var buffer = new byte[_bufferSize];

        try
        {
            while (!cancellationToken.IsCancellationRequested && _isCapturing)
            {
                // Read audio data
                var readBytes = _audioRecord?.Read(buffer, 0, buffer.Length) ?? 0;

                if (readBytes > 0)
                {
                    // Create a copy of the buffer to avoid race conditions
                    var audioData = new byte[readBytes];
                    Array.Copy(buffer, audioData, readBytes);

                    // Raise event with captured audio data
                    AudioDataCaptured?.Invoke(this, audioData);
                }
                else if (readBytes < 0)
                {
                    _logger.Warning($"Error reading audio data: {readBytes}");
                    
                    if (readBytes == (int)RecordStatus.ErrorInvalidOperation)
                    {
                        throw new InvalidOperationException("AudioRecord is in invalid state");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _logger.Error("Error in audio capture loop", ex);
                ErrorOccurred?.Invoke(this, ex);
            }
        }
    }

    private void CleanupAudioRecord()
    {
        try
        {
            _audioRecord?.Release();
            _audioRecord?.Dispose();
            _audioRecord = null;

            _captureCts?.Dispose();
            _captureCts = null;

            _captureTask = null;
        }
        catch (Exception ex)
        {
            _logger.Error("Error cleaning up AudioRecord", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            StopCaptureAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.Error("Error disposing AudioCaptureService", ex);
        }

        CleanupAudioRecord();
    }
}
