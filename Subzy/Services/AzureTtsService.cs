using Microsoft.CognitiveServices.Speech;
using Subzy.Services.Interfaces;

namespace Subzy.Services;

/// <summary>
/// Text-to-Speech service implementation using Azure Cognitive Services.
/// </summary>
public class AzureTtsService : ITtsService
{
    private readonly ILoggingService _logger;
    private readonly SettingsService _settingsService;
    private SpeechSynthesizer? _synthesizer;
    private bool _isSpeaking;

    public bool IsSpeaking => _isSpeaking;
    public bool IsConfigured { get; private set; }

    public AzureTtsService(ILoggingService logger, SettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
        InitializeSynthesizer();
    }

    private void InitializeSynthesizer()
    {
        try
        {
            var settings = _settingsService.LoadSettings();
            
            if (string.IsNullOrWhiteSpace(settings.AzureSpeechKey))
            {
                _logger.Warning("Azure Speech key not configured");
                IsConfigured = false;
                return;
            }

            var config = SpeechConfig.FromSubscription(settings.AzureSpeechKey, settings.AzureSpeechRegion);
            config.SpeechSynthesisVoiceName = settings.TtsVoice;
            
            _synthesizer = new SpeechSynthesizer(config);
            IsConfigured = true;
            _logger.Info("Azure TTS service initialized");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to initialize Azure TTS", ex);
            IsConfigured = false;
        }
    }

    public async Task SpeakAsync(string text, string voice, string language = "ro-RO")
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.Debug("No text to speak");
            return;
        }

        if (!IsConfigured)
        {
            _logger.Warning("TTS service not configured");
            return;
        }

        if (_isSpeaking)
        {
            await StopAsync();
        }

        try
        {
            _isSpeaking = true;
            
            // Build SSML for better control
            var ssml = $@"
                <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{language}'>
                    <voice name='{voice}'>
                        {System.Security.SecurityElement.Escape(text)}
                    </voice>
                </speak>";

            var result = await _synthesizer!.SpeakSsmlAsync(ssml);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                _logger.Info($"Speech synthesis completed: {text.Length} characters");
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                _logger.Warning($"Speech synthesis canceled: {cancellation.Reason} - {cancellation.ErrorDetails}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to synthesize speech", ex);
        }
        finally
        {
            _isSpeaking = false;
        }
    }

    public async Task StopAsync()
    {
        try
        {
            // Azure TTS doesn't have a direct stop method
            // We can dispose and recreate the synthesizer
            if (_synthesizer != null)
            {
                _synthesizer.Dispose();
                InitializeSynthesizer();
            }
            _isSpeaking = false;
            _logger.Debug("TTS stopped");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to stop TTS", ex);
        }
    }

    public void Dispose()
    {
        _synthesizer?.Dispose();
        _synthesizer = null;
        IsConfigured = false;
    }

    /// <summary>
    /// Reinitializes the TTS synthesizer with updated settings.
    /// Use this after changing API keys to apply the new credentials.
    /// </summary>
    public void Reinitialize()
    {
        _logger.Info("Reinitializing Azure TTS service");
        if (_synthesizer != null)
        {
            _synthesizer.Dispose();
            _synthesizer = null;
        }
        InitializeSynthesizer();
    }
}
