using Android.Media;
using Subzy.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Subzy.Platforms.Android;

/// <summary>
/// Android-specific audio capture service using AudioRecord.
/// Captures audio from the device microphone for speech-to-speech translation.
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

    public AudioCaptureService(ILoggingService logger, int sampleRate = 16000, int channels = 1, int bufferSize = 3200)
    {
        _logger = logger;
        _sampleRate = sampleRate;
        _channelConfig = channels == 1 ? ChannelIn.Mono : ChannelIn.Stereo;
        _audioFormat = Encoding.Pcm16bit;
        _bufferSize = bufferSize;
    }

    /// <summary>
    /// Starts audio capture from the microphone.
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

            _logger.Info($"Starting audio capture: {_sampleRate}Hz, buffer size: {actualBufferSize} bytes (min: {minBufferSize})");

            // Create AudioRecord instance
            _audioRecord = new AudioRecord(
                AudioSource.Mic,
                _sampleRate,
                _channelConfig,
                _audioFormat,
                actualBufferSize);

            if (_audioRecord.State != State.Initialized)
            {
                throw new InvalidOperationException("Failed to initialize AudioRecord");
            }

            // Start recording
            _audioRecord.StartRecording();
            _isCapturing = true;

            // Start capture loop
            _captureCts = new CancellationTokenSource();
            _captureTask = Task.Run(() => CaptureLoop(_captureCts.Token), _captureCts.Token);

            _logger.Info("Audio capture started successfully");
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
