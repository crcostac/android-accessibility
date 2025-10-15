namespace Subzy.Services.Interfaces;

/// <summary>
/// Interface for Speech-to-Speech translation services.
/// Captures audio from the microphone and provides real-time speech translation.
/// </summary>
public interface ISpeechToSpeechService : IDisposable
{
    /// <summary>
    /// Starts audio capture and translation.
    /// </summary>
    /// <param name="sourceLanguage">Source language code (e.g., "en" for English, null for auto-detect)</param>
    /// <param name="targetLanguage">Target language code (e.g., "ro" for Romanian)</param>
    /// <returns>Task representing the async operation</returns>
    Task StartAsync(string? sourceLanguage = null, string targetLanguage = "ro");

    /// <summary>
    /// Stops audio capture and translation.
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task StopAsync();

    /// <summary>
    /// Checks if the service is currently capturing and translating audio.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Checks if the service is configured and ready.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Event raised when translated text is received.
    /// </summary>
    event EventHandler<string>? TranslatedTextReceived;

    /// <summary>
    /// Event raised when audio response is received for playback.
    /// </summary>
    event EventHandler<byte[]>? AudioResponseReceived;

    /// <summary>
    /// Event raised when an error occurs.
    /// </summary>
    event EventHandler<Exception>? ErrorOccurred;
}
