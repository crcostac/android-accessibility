namespace Subzy.Services.Interfaces;

/// <summary>
/// Interface for translation services.
/// Allows swapping between different translation providers.
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translates text from source to target language.
    /// </summary>
    /// <param name="text">Text to translate</param>
    /// <param name="targetLanguage">Target language code (e.g., "ro" for Romanian)</param>
    /// <param name="sourceLanguage">Source language code (optional, auto-detect if null)</param>
    /// <returns>Tuple of (translatedText, detectedSourceLanguage)</returns>
    Task<(string translatedText, string detectedLanguage)> TranslateAsync(
        string text, 
        string targetLanguage, 
        string? sourceLanguage = null);

    /// <summary>
    /// Checks if the translation service is configured and ready.
    /// </summary>
    bool IsConfigured { get; }
}
