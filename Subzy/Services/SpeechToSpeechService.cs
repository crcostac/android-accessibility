using Subzy.Models;
using Subzy.Services.Interfaces;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

#if ANDROID
using Subzy.Platforms.Android;
#endif

namespace Subzy.Services;

/// <summary>
/// Speech-to-Speech translation service using Azure OpenAI gpt-4o-realtime model.
/// Captures audio from other apps (Netflix, HBO, etc.) using AudioPlaybackCapture and provides real-time speech translation via WebSocket.
/// Note: Requires Android 10+ and MediaProjection permission (same as screen capture).
/// </summary>
public class SpeechToSpeechService : ISpeechToSpeechService
{
    private readonly ILoggingService _logger;
    private readonly SettingsService _settingsService;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _connectionCts;
    private Task? _receiveTask;
    private bool _isActive;
    private bool _disposed;
    private SpeechToSpeechConfig? _config;

#if ANDROID
    private AudioCaptureService? _audioCapture;
#endif

    // Adaptive commit strategy fields
    private Timer? _commitTimer;
    private int _currentCommitIntervalMs = 2000; // Start with 2 seconds
    private DateTime _lastCommitTime;
    private DateTime _lastResponseTime;
    private int _pendingResponseCount = 0;
    private readonly object _commitLock = new object();
    
    // Audio activity tracking
    private bool _hasNewAudioSinceLastCommit = false;
    private DateTime _lastAudioReceivedTime = DateTime.MinValue;
    private long _audioBytesSinceLastCommit = 0;
    private const long MinAudioBytesForCommit = 1600; // ~50ms of audio at 16kHz 16-bit mono
    private const int AudioSilenceThresholdMs = 3000; // Consider silent if no audio for 3 seconds
    
    // Configuration constants
    private const int MinCommitIntervalMs = 1000;  // Minimum 1 second
    private const int MaxCommitIntervalMs = 5000;  // Maximum 5 seconds
    private const int MaxPendingResponses = 2;     // Skip commits if more than 2 pending
    private const int IntervalAdjustmentMs = 500;  // Adjustment step size

    /// <summary>
    /// Event raised when translated text is received.
    /// </summary>
    public event EventHandler<string>? TranslatedTextReceived;

    /// <summary>
    /// Event raised when audio response is received for playback.
    /// </summary>
    public event EventHandler<byte[]>? AudioResponseReceived;

    /// <summary>
    /// Event raised when an error occurs.
    /// </summary>
    public event EventHandler<Exception>? ErrorOccurred;

    /// <summary>
    /// Gets whether the service is currently active.
    /// </summary>
    public bool IsActive => _isActive;

    /// <summary>
    /// Gets whether the service is configured.
    /// </summary>
    public bool IsConfigured { get; private set; }

    public SpeechToSpeechService(ILoggingService logger, SettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
        InitializeConfiguration();
    }

    private void InitializeConfiguration()
    {
        try
        {
            var settings = _settingsService.LoadSettings();

            _config = new SpeechToSpeechConfig
            {
                AzureOpenAISpeechEndpoint = settings.AzureOpenAISpeechEndpoint,
                AzureOpenAISpeechKey = settings.AzureOpenAISpeechKey,
                AzureOpenAISpeechDeployment = settings.AzureOpenAISpeechDeployment,
                TargetLanguage = settings.TargetLanguage
            };

            IsConfigured = _config.IsValid();

            if (IsConfigured)
            {
                _logger.Info("Speech-to-Speech service configured successfully");
                _logger.Debug($"Using Cognitive Services endpoint: {_config.AzureOpenAISpeechEndpoint}");
            }
            else
            {
                _logger.Warning("Speech-to-Speech service not fully configured");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to initialize Speech-to-Speech service configuration", ex);
            IsConfigured = false;
        }
    }

    /// <summary>
    /// Starts audio capture and translation from app playback (Netflix, HBO, etc.).
    /// Requires that ScreenCaptureService is already running to provide MediaProjection.
    /// </summary>
    public async Task StartAsync(string? sourceLanguage = null, string targetLanguage = "ro")
    {
        if (_isActive)
        {
            _logger.Warning("Speech-to-Speech service is already active");
            return;
        }

        if (!IsConfigured || _config == null)
        {
            _logger.Warning("Speech-to-Speech service not configured");
            return;
        }

        try
        {
            _logger.Info($"Starting Speech-to-Speech translation (source: {sourceLanguage ?? "auto"}, target: {targetLanguage})");

            // Update configuration
            _config.SourceLanguage = sourceLanguage;
            _config.TargetLanguage = targetLanguage;

            // Reset audio activity tracking
            _hasNewAudioSinceLastCommit = false;
            _audioBytesSinceLastCommit = 0;
            _lastAudioReceivedTime = DateTime.MinValue;

            // Connect to Azure OpenAI WebSocket
            await ConnectWebSocketAsync();

            // Start audio capture from app playback
#if ANDROID
            // Get MediaProjection from ScreenCaptureService
            var mediaProjection = Platforms.Android.Services.ScreenCaptureService.CurrentMediaProjection;
            if (mediaProjection == null)
            {
                throw new InvalidOperationException(
                    "MediaProjection not available. Please start the screen capture service first. " +
                    "Speech-to-Speech requires the same MediaProjection permission as screen capture.");
            }

            _logger.Info("Creating AudioCaptureService with AudioPlaybackCapture");
            
            _audioCapture = new AudioCaptureService(
                _logger,
                mediaProjection,
                _config.AudioSampleRate,
                _config.AudioChannels,
                _config.BufferSizeInBytes);

            _audioCapture.AudioDataCaptured += OnAudioDataCaptured;
            _audioCapture.ErrorOccurred += OnAudioCaptureError;

            await _audioCapture.StartCaptureAsync();
            _logger.Info("Audio playback capture started - capturing audio from streaming apps");
#else
            _logger.Warning("Audio playback capture is only supported on Android 10+");
            throw new PlatformNotSupportedException("Audio playback capture is only supported on Android 10+");
#endif

            _isActive = true;
            
            // Start adaptive commit timer
            StartAdaptiveCommitTimer();
            
            _logger.Info("Speech-to-Speech service started successfully with adaptive commit strategy");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to start Speech-to-Speech service", ex);
            await StopAsync();
            throw;
        }
    }

    /// <summary>
    /// Stops audio capture and translation.
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isActive)
        {
            return;
        }

        _logger.Info("Stopping Speech-to-Speech service");

        try
        {
            _isActive = false;

            // Stop commit timer
            _commitTimer?.Dispose();
            _commitTimer = null;

            // Stop audio capture
#if ANDROID
            if (_audioCapture != null)
            {
                _audioCapture.AudioDataCaptured -= OnAudioDataCaptured;
                _audioCapture.ErrorOccurred -= OnAudioCaptureError;
                await _audioCapture.StopCaptureAsync();
                _audioCapture.Dispose();
                _audioCapture = null;
            }
#endif

            // Close WebSocket connection
            await DisconnectWebSocketAsync();

            _logger.Info("Speech-to-Speech service stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Error stopping Speech-to-Speech service", ex);
        }
    }

    private async Task ConnectWebSocketAsync()
    {
        try
        {
            if (_config == null)
            {
                throw new InvalidOperationException("Configuration is null");
            }

            _webSocket = new ClientWebSocket();
            _connectionCts = new CancellationTokenSource();

            // Build WebSocket URL for Azure OpenAI Realtime API
            var endpoint = _config.AzureOpenAISpeechEndpoint.TrimEnd('/');
            
            // Ensure we're using HTTPS endpoint (will be converted to WSS)
            if (!endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                endpoint = endpoint.Replace("http://", "https://");
            }

            // Build the WebSocket URL according to Azure Cognitive Services Realtime API specification
            // Format for Cognitive Services: wss://{endpoint}/openai/realtime?api-version=2024-10-01-preview&deployment={deployment}
            // The Cognitive Services endpoint already includes the base path
            var wsUrl = $"{endpoint}/openai/realtime?api-version=2024-10-01-preview&deployment={Uri.EscapeDataString(_config.AzureOpenAISpeechDeployment)}&api-key={_config.AzureOpenAISpeechKey}";
            
            // Convert HTTPS to WSS for WebSocket
            wsUrl = wsUrl.Replace("https://", "wss://");

            // Configure WebSocket options for Azure OpenAI
            _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            
            // Set user agent to identify the client
            _webSocket.Options.SetRequestHeader("User-Agent", "Subzy-Android/1.0");
            
            // Add OpenAI-specific headers
            _webSocket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");

            // Set a reasonable timeout
            var connectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _connectionCts.Token, 
                connectTimeout.Token);

            try
            {
                // Connect to WebSocket
                await _webSocket.ConnectAsync(new Uri(wsUrl), linkedCts.Token);
                
                _logger.Info("Successfully connected to Azure OpenAI Realtime API");
            }
            catch (TaskCanceledException) when (connectTimeout.IsCancellationRequested)
            {
                throw new TimeoutException("Connection to Azure OpenAI Realtime API timed out after 30 seconds");
            }

            // Send session configuration
            await SendSessionConfigAsync();

            // Start receiving messages
            _receiveTask = Task.Run(() => ReceiveLoop(_connectionCts.Token), _connectionCts.Token);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to connect to WebSocket: {ex.Message}", ex);
            
            // Provide more specific error information
            if (ex is WebSocketException wsEx)
            {
                _logger.Error($"WebSocket error code: {wsEx.WebSocketErrorCode}, Native error: {wsEx.NativeErrorCode}");
                
                if (wsEx.WebSocketErrorCode == WebSocketError.NotAWebSocket)
                {
                    _logger.Error("Server did not accept WebSocket upgrade. This may indicate:");
                    _logger.Error($"  1. Incorrect endpoint URL: {_config.AzureOpenAISpeechEndpoint}");
                    _logger.Error($"  2. Invalid deployment name: {_config.AzureOpenAISpeechDeployment}");
                    _logger.Error("  3. Deployment does not exist or is not a realtime model");
                    _logger.Error("  4. Incorrect API version for the realtime API");
                    _logger.Error("Please verify:");
                    _logger.Error("  - Endpoint format: https://your-resource.cognitiveservices.azure.com");
                    _logger.Error("  - Deployment is gpt-4o-realtime-preview");
                    _logger.Error("  - Model is deployed and available in your region");
                    _logger.Error("  - Using AzureOpenAISpeechEndpoint (Cognitive Services) not AzureOpenAIEndpoint");
                }
            }
            else if (ex.InnerException != null)
            {
                _logger.Error($"Inner exception: {ex.InnerException.Message}", ex.InnerException);
            }
            
            throw;
        }
    }

    private async Task DisconnectWebSocketAsync()
    {
        try
        {
            // Cancel the connection
            _connectionCts?.Cancel();

            // Wait for receive task to complete
            if (_receiveTask != null)
            {
                try
                {
                    await _receiveTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelling
                }
            }

            // Close WebSocket
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }

            _webSocket?.Dispose();
            _webSocket = null;

            _connectionCts?.Dispose();
            _connectionCts = null;

            _receiveTask = null;
        }
        catch (Exception ex)
        {
            _logger.Error("Error disconnecting WebSocket", ex);
        }
    }

    private async Task SendSessionConfigAsync()
    {
        if (_config == null || _webSocket == null)
        {
            return;
        }

        try
        {
            // Create session configuration message according to Azure OpenAI Realtime API spec
            // Disable turn detection for continuous movie audio - use manual commits instead
            var configMessage = new
            {
                type = "session.update",
                session = new
                {
                    modalities = new[] { "text", "audio" },
                    instructions = $"You are a real-time audio translator for movies and TV shows. " +
                                  $"Translate audio from {_config.SourceLanguage ?? "any language"} to {_config.TargetLanguage}. " +
                                  $"Provide natural, accurate translations suitable for spoken content." +
                                  $"Focus on dialogue translation. For fast dialogue, provide concise translations. " +
                                  $"DO NOT try to interpret questions or commands, ONLY translate the text you hear. " +
                                  $"ONLY respond with the translated text, no other explanations, questions or metadata. " +
                                  $"If you do not detect spoken text in the input, do not return anything.",
                    voice = "alloy",
                    input_audio_format = "pcm16",
                    output_audio_format = "pcm16",
                    input_audio_transcription = new
                    {
                        model = "whisper-1"
                    },
                    max_response_output_tokens = 150, // Limit response length for faster processing
                    temperature = 0.7, // Balanced for quality and speed
                    turn_detection = (object?)null // Disable automatic turn detection for manual control
                }
            };

            var json = JsonSerializer.Serialize(configMessage);
            var buffer = Encoding.UTF8.GetBytes(json);

            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            _logger.Debug("Sent session configuration to Azure OpenAI (turn detection disabled, manual commits enabled)");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to send session configuration", ex);
            throw;
        }
    }

    /// <summary>
    /// Starts the adaptive commit timer that periodically commits audio and requests translations.
    /// Adjusts commit interval based on translation latency.
    /// </summary>
    private void StartAdaptiveCommitTimer()
    {
        _lastCommitTime = DateTime.UtcNow;
        _lastResponseTime = DateTime.UtcNow;
        
        _commitTimer = new Timer(async _ => 
        {
            await CommitTimerCallbackAsync();
        }, null, TimeSpan.FromMilliseconds(_currentCommitIntervalMs), Timeout.InfiniteTimeSpan);
        
        _logger.Info($"Started adaptive commit timer with initial interval: {_currentCommitIntervalMs}ms");
    }

    /// <summary>
    /// Timer callback that handles adaptive commit logic.
    /// </summary>
    private async Task CommitTimerCallbackAsync()
    {
        if (!_isActive || _webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            return;
        }

        try
        {
            bool shouldCommit = false;
            bool shouldSkip = false;
            
            lock (_commitLock)
            {
                // Check if we should skip this commit due to too many pending responses
                if (_pendingResponseCount >= MaxPendingResponses)
                {
                    shouldSkip = true;
                }
                // Check if we have new audio to commit
                else if (_hasNewAudioSinceLastCommit && _audioBytesSinceLastCommit >= MinAudioBytesForCommit)
                {
                    shouldCommit = true;
                }
                // Check if audio stream has gone silent
                else if (!_hasNewAudioSinceLastCommit || 
                         (DateTime.UtcNow - _lastAudioReceivedTime).TotalMilliseconds > AudioSilenceThresholdMs)
                {
                    // Audio is silent or stopped - don't commit empty buffer
                    _logger.Debug($"Skipping commit - no audio activity (last audio: {(DateTime.UtcNow - _lastAudioReceivedTime).TotalMilliseconds:F0}ms ago, bytes: {_audioBytesSinceLastCommit})");
                }
            }

            if (shouldSkip)
            {
                _logger.Warning($"Skipping commit - {_pendingResponseCount} responses pending (max: {MaxPendingResponses})");
                
                // Clear the audio buffer to prevent overflow
                await ClearInputBufferAsync();
                
                // Reset audio tracking
                lock (_commitLock)
                {
                    _hasNewAudioSinceLastCommit = false;
                    _audioBytesSinceLastCommit = 0;
                }
            }
            else if (shouldCommit)
            {
                // Perform commit and request response
                await CommitAudioAndRequestResponseAsync();
                
                // Reset audio tracking
                lock (_commitLock)
                {
                    _hasNewAudioSinceLastCommit = false;
                    _audioBytesSinceLastCommit = 0;
                }
                
                // Adjust commit interval based on latency
                AdjustCommitInterval();
            }
            
            // Schedule next commit with adjusted interval
            _commitTimer?.Change(TimeSpan.FromMilliseconds(_currentCommitIntervalMs), Timeout.InfiniteTimeSpan);
        }
        catch (Exception ex)
        {
            _logger.Error("Error in commit timer callback", ex);
            
            // Retry with current interval
            _commitTimer?.Change(TimeSpan.FromMilliseconds(_currentCommitIntervalMs), Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Commits the current audio buffer and requests a translation response.
    /// </summary>
    private async Task CommitAudioAndRequestResponseAsync()
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            return;
        }

        try
        {
            _lastCommitTime = DateTime.UtcNow;
            
            lock (_commitLock)
            {
                _pendingResponseCount++;
            }

            // Commit the current audio buffer
            var commitMessage = new
            {
                type = "input_audio_buffer.commit"
            };

            await SendWebSocketMessageAsync(commitMessage);
            _logger.Debug($"Committed audio buffer ({_audioBytesSinceLastCommit} bytes)");

            // Request a response
            var responseMessage = new
            {
                type = "response.create",
                response = new
                {
                    modalities = new[] { "text", "audio" }
                }
            };

            await SendWebSocketMessageAsync(responseMessage);
            _logger.Debug($"Requested translation response (pending: {_pendingResponseCount})");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to commit audio and request response", ex);
            
            lock (_commitLock)
            {
                _pendingResponseCount = Math.Max(0, _pendingResponseCount - 1);
            }
        }
    }

    /// <summary>
    /// Clears the input audio buffer (used when skipping commits).
    /// </summary>
    private async Task ClearInputBufferAsync()
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            return;
        }

        try
        {
            var clearMessage = new
            {
                type = "input_audio_buffer.clear"
            };

            await SendWebSocketMessageAsync(clearMessage);
            _logger.Debug("Cleared input audio buffer");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to clear input audio buffer", ex);
        }
    }

    /// <summary>
    /// Adjusts the commit interval based on observed translation latency.
    /// </summary>
    private void AdjustCommitInterval()
    {
        var latency = (_lastResponseTime - _lastCommitTime).TotalMilliseconds;
        
        // If latency > commit interval * 1.2, increase interval to reduce queue buildup
        if (latency > _currentCommitIntervalMs * 1.2)
        {
            var newInterval = Math.Min(_currentCommitIntervalMs + IntervalAdjustmentMs, MaxCommitIntervalMs);
            
            if (newInterval != _currentCommitIntervalMs)
            {
                _currentCommitIntervalMs = newInterval;
                _logger.Warning($"Increased commit interval to {_currentCommitIntervalMs}ms (latency: {latency:F0}ms)");
            }
        }
        // If latency < commit interval * 0.8, decrease interval for lower end-to-end delay
        else if (latency < _currentCommitIntervalMs * 0.8 && _currentCommitIntervalMs > MinCommitIntervalMs)
        {
            var newInterval = Math.Max(_currentCommitIntervalMs - IntervalAdjustmentMs, MinCommitIntervalMs);
            
            if (newInterval != _currentCommitIntervalMs)
            {
                _currentCommitIntervalMs = newInterval;
                _logger.Info($"Decreased commit interval to {_currentCommitIntervalMs}ms (latency: {latency:F0}ms)");
            }
        }
    }

    /// <summary>
    /// Sends a JSON message over the WebSocket.
    /// </summary>
    private async Task SendWebSocketMessageAsync(object message)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            return;
        }

        var json = JsonSerializer.Serialize(message);
        var buffer = Encoding.UTF8.GetBytes(json);

        await _webSocket.SendAsync(
            new ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }

    private void OnAudioDataCaptured(object? sender, byte[] audioData)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open || !_isActive)
        {
            return;
        }

        try
        {
            // Track audio activity
            lock (_commitLock)
            {
                _hasNewAudioSinceLastCommit = true;
                _audioBytesSinceLastCommit += audioData.Length;
                _lastAudioReceivedTime = DateTime.UtcNow;
            }

            // Convert audio data to base64
            var base64Audio = Convert.ToBase64String(audioData);

            // Create audio append message
            var audioMessage = new
            {
                type = "input_audio_buffer.append",
                audio = base64Audio
            };

            var json = JsonSerializer.Serialize(audioMessage);
            var buffer = Encoding.UTF8.GetBytes(json);

            // Send audio data (fire and forget, as this is called frequently)
            _ = _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to send audio data", ex);
            ErrorOccurred?.Invoke(this, ex);
        }
    }

    private void OnAudioCaptureError(object? sender, Exception ex)
    {
        _logger.Error("Audio capture error", ex);
        ErrorOccurred?.Invoke(this, ex);
    }

    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var messageBuffer = new List<byte>();

        try
        {
            while (!cancellationToken.IsCancellationRequested && _webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.Info("WebSocket closed by server");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    // Accumulate message fragments
                    messageBuffer.AddRange(buffer.Take(result.Count));

                    // Check if this is the end of the message
                    if (result.EndOfMessage)
                    {
                        // We have a complete message, convert to string and process
                        var json = Encoding.UTF8.GetString(messageBuffer.ToArray());
                        await HandleWebSocketMessageAsync(json);
                        
                        // Clear the buffer for the next message
                        messageBuffer.Clear();
                    }
                    // If not EndOfMessage, continue accumulating in the next iteration
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _logger.Error("Error in WebSocket receive loop", ex);
                ErrorOccurred?.Invoke(this, ex);
            }
        }
    }

    private async Task HandleWebSocketMessageAsync(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
            {
                return;
            }

            var messageType = typeElement.GetString();
            _logger.Debug($"Received message type: {messageType}");

            switch (messageType)
            {
                case "response.audio.delta":
                    // Audio chunk received
                    if (root.TryGetProperty("delta", out var deltaElement))
                    {
                        var base64Audio = deltaElement.GetString();
                        if (!string.IsNullOrEmpty(base64Audio))
                        {
                            var audioData = Convert.FromBase64String(base64Audio);
                            AudioResponseReceived?.Invoke(this, audioData);
                        }
                    }
                    break;

                case "response.text.delta":
                    // Text chunk received
                    if (root.TryGetProperty("delta", out var textDelta))
                    {
                        var text = textDelta.GetString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            TranslatedTextReceived?.Invoke(this, text);
                        }
                    }
                    break;

                case "response.done":
                    // Response completed - update timing and decrement pending count
                    _lastResponseTime = DateTime.UtcNow;
                    
                    lock (_commitLock)
                    {
                        _pendingResponseCount = Math.Max(0, _pendingResponseCount - 1);
                    }
                    
                    var latency = (_lastResponseTime - _lastCommitTime).TotalMilliseconds;
                    _logger.Info($"Translation completed in {latency:F0}ms (pending: {_pendingResponseCount}, interval: {_currentCommitIntervalMs}ms)");
                    break;

                case "conversation.item.input_audio_transcription.completed":
                    // Input transcription completed
                    if (root.TryGetProperty("transcript", out var transcript))
                    {
                        var text = transcript.GetString();
                        _logger.Info($"Input transcription: {text}");
                    }
                    break;

                case "error":
                    // Error message
                    if (root.TryGetProperty("error", out var errorElement))
                    {
                        var errorMessage = errorElement.GetProperty("message").GetString();
                        var errorCode = errorElement.TryGetProperty("code", out var codeElement) 
                            ? codeElement.GetString() 
                            : "unknown";
                        
                        _logger.Error($"API error [{errorCode}]: {errorMessage}");
                        
                        // Decrement pending count on error
                        lock (_commitLock)
                        {
                            _pendingResponseCount = Math.Max(0, _pendingResponseCount - 1);
                        }
                        
                        ErrorOccurred?.Invoke(this, new Exception($"[{errorCode}] {errorMessage}"));
                    }
                    break;

                case "session.created":
                case "session.updated":
                    _logger.Info($"Session {messageType}");
                    break;

                case "rate_limits_updated":
                    // Log rate limit information for monitoring
                    _logger.Debug($"Rate limits updated: {json}");
                    break;

                default:
                    // Log other message types for debugging
                    _logger.Debug($"Unhandled message type: {messageType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error handling WebSocket message: {json}", ex);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Reinitializes the service with updated settings.
    /// </summary>
    public void Reinitialize()
    {
        _logger.Info("Reinitializing Speech-to-Speech service");
        InitializeConfiguration();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            // Stop commit timer first
            _commitTimer?.Dispose();
            _commitTimer = null;
            
            StopAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.Error("Error disposing Speech-to-Speech service", ex);
        }

#if ANDROID
        _audioCapture?.Dispose();
#endif
        _webSocket?.Dispose();
        _connectionCts?.Dispose();
    }
}
