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
/// Speech-to-Speech translation service using Azure OpenAI gpt-4o-mini-realtime model.
/// Captures audio from the microphone and provides real-time speech translation via WebSocket.
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
                AzureOpenAIEndpoint = settings.AzureOpenAIEndpoint,
                AzureOpenAIKey = settings.AzureOpenAIKey,
                ModelDeploymentName = settings.AzureOpenAISpeechDeployment,
                TargetLanguage = settings.TargetLanguage
            };

            IsConfigured = _config.IsValid();

            if (IsConfigured)
            {
                _logger.Info("Speech-to-Speech service configured successfully");
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
    /// Starts audio capture and translation.
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

            // Connect to Azure OpenAI WebSocket
            await ConnectWebSocketAsync();

            // Start audio capture
#if ANDROID
            _audioCapture = new AudioCaptureService(
                _logger,
                _config.AudioSampleRate,
                _config.AudioChannels,
                _config.BufferSizeInBytes);

            _audioCapture.AudioDataCaptured += OnAudioDataCaptured;
            _audioCapture.ErrorOccurred += OnAudioCaptureError;

            await _audioCapture.StartCaptureAsync();
#else
            _logger.Warning("Audio capture is only supported on Android");
#endif

            _isActive = true;
            _logger.Info("Speech-to-Speech service started successfully");
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
            var endpoint = _config.AzureOpenAIEndpoint.TrimEnd('/');
            var wsUrl = $"{endpoint}/openai/realtime?deployment={_config.ModelDeploymentName}&api-version=2024-10-01-preview";
            wsUrl = wsUrl.Replace("https://", "wss://");

            // Add API key to headers
            _webSocket.Options.SetRequestHeader("api-key", _config.AzureOpenAIKey);

            _logger.Info($"Connecting to Azure OpenAI Realtime API: {endpoint}");

            // Connect to WebSocket
            await _webSocket.ConnectAsync(new Uri(wsUrl), _connectionCts.Token);

            // Send session configuration
            await SendSessionConfigAsync();

            // Start receiving messages
            _receiveTask = Task.Run(() => ReceiveLoop(_connectionCts.Token), _connectionCts.Token);

            _logger.Info("Connected to Azure OpenAI Realtime API");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to connect to WebSocket", ex);
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
            // Create session configuration message
            var configMessage = new
            {
                type = "session.update",
                session = new
                {
                    modalities = new[] { "text", "audio" },
                    instructions = $"You are a real-time speech translator. Translate speech from {_config.SourceLanguage ?? "any language"} to {_config.TargetLanguage}. Provide natural, accurate translations.",
                    voice = "alloy",
                    input_audio_format = "pcm16",
                    output_audio_format = "pcm16",
                    input_audio_transcription = new
                    {
                        model = "whisper-1"
                    },
                    turn_detection = new
                    {
                        type = "server_vad",
                        threshold = 0.5,
                        prefix_padding_ms = 300,
                        silence_duration_ms = 500
                    }
                }
            };

            var json = JsonSerializer.Serialize(configMessage);
            var buffer = Encoding.UTF8.GetBytes(json);

            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            _logger.Debug("Sent session configuration");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to send session configuration", ex);
            throw;
        }
    }

    private void OnAudioDataCaptured(object? sender, byte[] audioData)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open || !_isActive)
        {
            return;
        }

        try
        {
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
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleWebSocketMessageAsync(json);
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
                        _logger.Error($"API error: {errorMessage}");
                        ErrorOccurred?.Invoke(this, new Exception(errorMessage));
                    }
                    break;

                case "session.created":
                case "session.updated":
                    _logger.Info($"Session {messageType}");
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
