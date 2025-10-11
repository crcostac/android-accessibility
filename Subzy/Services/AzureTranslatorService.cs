using Azure;
using Azure.AI.Translation.Text;
using Subzy.Services.Interfaces;

namespace Subzy.Services;

/// <summary>
/// Translation service implementation using Azure Translator.
/// </summary>
public class AzureTranslatorService : ITranslationService
{
    private readonly ILoggingService _logger;
    private readonly SettingsService _settingsService;
    private TextTranslationClient? _client;

    public bool IsConfigured { get; private set; }

    public AzureTranslatorService(ILoggingService logger, SettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
        InitializeClient();
    }

    private void InitializeClient()
    {
        try
        {
            var settings = _settingsService.LoadSettings();
            
            if (string.IsNullOrWhiteSpace(settings.AzureTranslatorKey))
            {
                _logger.Warning("Azure Translator key not configured");
                IsConfigured = false;
                return;
            }

            var credential = new AzureKeyCredential(settings.AzureTranslatorKey);
            _client = new TextTranslationClient(credential, settings.AzureTranslatorRegion);
            IsConfigured = true;
            _logger.Info("Azure Translator service initialized");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to initialize Azure Translator", ex);
            IsConfigured = false;
        }
    }

    public async Task<(string translatedText, string detectedLanguage)> TranslateAsync(
        string text,
        string targetLanguage,
        string? sourceLanguage = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (string.Empty, string.Empty);
        }

        if (!IsConfigured)
        {
            _logger.Warning("Translation service not configured");
            return (text, "unknown");
        }

        try
        {
            var response = await _client!.TranslateAsync(
                targetLanguages: new[] { targetLanguage },
                content: new[] { text },
                sourceLanguage: sourceLanguage
            );

            var translation = response.Value.FirstOrDefault();
            if (translation != null)
            {
                var translatedText = translation.Translations.FirstOrDefault()?.Text ?? text;
                var detectedLang = translation.DetectedLanguage?.Language ?? sourceLanguage ?? "unknown";

                _logger.Info($"Translated text from {detectedLang} to {targetLanguage}");
                return (translatedText, detectedLang);
            }

            _logger.Warning("No translation result received");
            return (text, sourceLanguage ?? "unknown");
        }
        catch (Exception ex)
        {
            _logger.Error("Translation failed", ex);
            return (text, sourceLanguage ?? "error");
        }
    }

    /// <summary>
    /// Reinitializes the translation client with updated settings.
    /// Use this after changing API keys to apply the new credentials.
    /// </summary>
    public void Reinitialize()
    {
        _logger.Info("Reinitializing Azure Translator service");
        InitializeClient();
    }
}
