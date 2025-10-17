using Android.Media;
using Subzy.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Subzy.Platforms.Android;

/// <summary>
/// Android-specific audio playback service using AudioTrack.
/// Plays audio received from Azure OpenAI Realtime API.
/// </summary>
public class AudioPlaybackService : IDisposable
{
    private readonly ILoggingService _logger;
    private AudioTrack? _audioTrack;
    private bool _isPlaying;
    private bool _disposed;
    private readonly ConcurrentQueue<byte[]> _audioQueue;
    private Task? _playbackTask;
    private CancellationTokenSource? _playbackCts;

    // Audio configuration (must match Azure OpenAI Realtime API output format)
    private readonly int _sampleRate = 24000; // Azure OpenAI outputs 24kHz
    private readonly ChannelOut _channelConfig = ChannelOut.Mono;
    private readonly Encoding _audioFormat = Encoding.Pcm16bit;

    /// <summary>
    /// Gets whether audio playback is currently active.
    /// </summary>
    public bool IsPlaying => _isPlaying;

    public AudioPlaybackService(ILoggingService logger)
    {
        _logger = logger;
        _audioQueue = new ConcurrentQueue<byte[]>();
    }

    /// <summary>
    /// Starts audio playback.
    /// </summary>
    public Task StartPlaybackAsync()
    {
        if (_isPlaying)
        {
            _logger.Warning("Audio playback is already active");
            return Task.CompletedTask;
        }

        try
        {
            // Get minimum buffer size
            var minBufferSize = AudioTrack.GetMinBufferSize(
                _sampleRate,
                _channelConfig,
                _audioFormat);

            if (minBufferSize < 0)
            {
                throw new InvalidOperationException("Unable to get minimum buffer size for audio playback");
            }

            _logger.Info($"Starting audio playback: {_sampleRate}Hz, buffer size: {minBufferSize} bytes");

            // Create AudioTrack instance
            _audioTrack = new AudioTrack.Builder()
                .SetAudioAttributes(new AudioAttributes.Builder()
                    .SetUsage(AudioUsageKind.Media)
                    .SetContentType(AudioContentType.Speech)
                    .Build())
                .SetAudioFormat(new AudioFormat.Builder()
                    .SetEncoding(_audioFormat)
                    .SetSampleRate(_sampleRate)
                    .SetChannelMask(_channelConfig)
                    .Build())
                .SetBufferSizeInBytes(minBufferSize * 2) // Double buffer for smoother playback
                .SetTransferMode(AudioTrackMode.Stream)
                .Build();

            if (_audioTrack.State != AudioTrackState.Initialized)
            {
                throw new InvalidOperationException("Failed to initialize AudioTrack");
            }

            // Start playback
            _audioTrack.Play();
            _isPlaying = true;

            // Start playback loop
            _playbackCts = new CancellationTokenSource();
            _playbackTask = Task.Run(() => PlaybackLoop(_playbackCts.Token), _playbackCts.Token);

            _logger.Info("Audio playback started successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to start audio playback", ex);
            CleanupAudioTrack();
            throw;
        }
    }

    /// <summary>
    /// Stops audio playback.
    /// </summary>
    public async Task StopPlaybackAsync()
    {
        if (!_isPlaying)
        {
            return;
        }

        _logger.Info("Stopping audio playback");

        try
        {
            _isPlaying = false;

            // Cancel the playback loop
            _playbackCts?.Cancel();

            // Wait for playback task to complete
            if (_playbackTask != null)
            {
                try
                {
                    await _playbackTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelling
                }
            }

            // Stop and flush AudioTrack
            _audioTrack?.Stop();
            _audioTrack?.Flush();

            // Clear the queue
            while (_audioQueue.TryDequeue(out _)) { }

            _logger.Info("Audio playback stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Error stopping audio playback", ex);
        }
        finally
        {
            CleanupAudioTrack();
        }
    }

    /// <summary>
    /// Queues audio data for playback.
    /// </summary>
    public void EnqueueAudio(byte[] audioData)
    {
        if (!_isPlaying)
        {
            _logger.Warning("Cannot enqueue audio: playback not started");
            return;
        }

        if (audioData == null || audioData.Length == 0)
        {
            return;
        }

        _audioQueue.Enqueue(audioData);
        _logger.Debug($"Queued {audioData.Length} bytes of audio. Queue size: {_audioQueue.Count}");
    }

    private void PlaybackLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _isPlaying)
            {
                // Try to dequeue audio data
                if (_audioQueue.TryDequeue(out var audioData))
                {
                    // Write audio data to AudioTrack
                    var written = _audioTrack?.Write(audioData, 0, audioData.Length) ?? 0;

                    if (written < 0)
                    {
                        _logger.Warning($"Error writing audio data: {written}");
                    }
                    else if (written < audioData.Length)
                    {
                        _logger.Warning($"Only wrote {written}/{audioData.Length} bytes");
                    }
                }
                else
                {
                    // No audio data available, sleep briefly
                    Thread.Sleep(10);
                }
            }
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _logger.Error("Error in audio playback loop", ex);
            }
        }
    }

    private void CleanupAudioTrack()
    {
        try
        {
            _audioTrack?.Stop();
            _audioTrack?.Release();
            _audioTrack?.Dispose();
            _audioTrack = null;

            _playbackCts?.Dispose();
            _playbackCts = null;

            _playbackTask = null;
        }
        catch (Exception ex)
        {
            _logger.Error("Error cleaning up AudioTrack", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            StopPlaybackAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.Error("Error disposing AudioPlaybackService", ex);
        }

        CleanupAudioTrack();
    }
}
