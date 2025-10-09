namespace Subzy.Services.Interfaces;

/// <summary>
/// Interface for Text-to-Speech services.
/// Allows swapping between different TTS providers.
/// </summary>
public interface ITtsService
{
    /// <summary>
    /// Synthesizes text to speech and plays it.
    /// </summary>
    /// <param name="text">Text to speak</param>
    /// <param name="voice">Voice identifier (e.g., "ro-RO-AlinaNeural")</param>
    /// <param name="language">Language code (e.g., "ro-RO")</param>
    Task SpeakAsync(string text, string voice, string language = "ro-RO");

    /// <summary>
    /// Stops any currently playing speech.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Checks if TTS is currently speaking.
    /// </summary>
    bool IsSpeaking { get; }

    /// <summary>
    /// Checks if the TTS service is configured and ready.
    /// </summary>
    bool IsConfigured { get; }
}
