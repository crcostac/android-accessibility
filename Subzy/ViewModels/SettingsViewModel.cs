using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Subzy.Helpers;
using Subzy.Models;
using Subzy.Services;
using Subzy.Services.Interfaces;

namespace Subzy.ViewModels;

/// <summary>
/// ViewModel for the settings page.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ILoggingService _logger;
    private readonly SettingsService _settingsService;
    private readonly ITranslationService _translationService;
    private readonly ITtsService _ttsService;
    private readonly ISpeechToSpeechService _speechToSpeechService;
    private AppSettings _settings;

    [ObservableProperty]
    private int _snapshotFrequency;

    [ObservableProperty]
    private float _brightness;

    [ObservableProperty]
    private float _contrast;

    [ObservableProperty]
    private bool _isTranslationEnabled;

    [ObservableProperty]
    private string _targetLanguage;

    [ObservableProperty]
    private bool _isTtsEnabled;

    [ObservableProperty]
    private string _ttsVoice;

    [ObservableProperty]
    private string _azureTranslatorKey;

    [ObservableProperty]
    private string _azureSpeechKey;

    [ObservableProperty]
    private string _azureOpenAIEndpoint;

    [ObservableProperty]
    private string _azureOpenAIKey;

    [ObservableProperty]
    private string _azureOpenAITranslationDeployment;

    [ObservableProperty]
    private string _azureOpenAISpeechDeployment;

    [ObservableProperty]
    private bool _isSpeechToSpeechEnabled;

    [ObservableProperty]
    private string _speechToSpeechSourceLanguage;

    [ObservableProperty]
    private string _speechToSpeechTargetLanguage;

    [ObservableProperty]
    private bool _adaptiveScheduling;

    [ObservableProperty]
    private int _lowBatteryThreshold;

    public List<string> AvailableVoices { get; } = new(Constants.RomanianVoices);

    public SettingsViewModel(
        ILoggingService logger, 
        SettingsService settingsService,
        ITranslationService translationService,
        ITtsService ttsService,
        ISpeechToSpeechService speechToSpeechService)
    {
        _logger = logger;
        _settingsService = settingsService;
        _translationService = translationService;
        _ttsService = ttsService;
        _speechToSpeechService = speechToSpeechService;
        _settings = _settingsService.LoadSettings();
        
        LoadSettings();
    }

    private void LoadSettings()
    {
        SnapshotFrequency = _settings.SnapshotFrequencySeconds;
        Brightness = _settings.Brightness;
        Contrast = _settings.Contrast;
        IsTranslationEnabled = _settings.IsTranslationEnabled;
        TargetLanguage = _settings.TargetLanguage;
        IsTtsEnabled = _settings.IsTtsEnabled;
        TtsVoice = _settings.TtsVoice;
        AzureTranslatorKey = _settings.AzureTranslatorKey;
        AzureSpeechKey = _settings.AzureSpeechKey;
        AzureOpenAIEndpoint = _settings.AzureOpenAIEndpoint;
        AzureOpenAIKey = _settings.AzureOpenAIKey;
        AzureOpenAITranslationDeployment = _settings.AzureOpenAITranslationDeployment;
        AzureOpenAISpeechDeployment = _settings.AzureOpenAISpeechDeployment;
        IsSpeechToSpeechEnabled = _settings.IsSpeechToSpeechEnabled;
        SpeechToSpeechSourceLanguage = _settings.SpeechToSpeechSourceLanguage ?? string.Empty;
        SpeechToSpeechTargetLanguage = _settings.SpeechToSpeechTargetLanguage;
        AdaptiveScheduling = _settings.AdaptiveScheduling;
        LowBatteryThreshold = _settings.LowBatteryThreshold;

        _logger.Info("Settings loaded into ViewModel");
    }

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            InternalSaveSettings();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Success",
                    "Settings saved successfully",
                    "OK"
                );
            });
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to save settings", ex);
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Error",
                    $"Failed to save settings: {ex.Message}",
                    "OK"
                );
            });
        }
    }

    private void InternalSaveSettings()
    {
        // Store original API keys to detect changes
        var originalTranslatorKey = _settings.AzureTranslatorKey;
        var originalSpeechKey = _settings.AzureSpeechKey;
        var originalOpenAIKey = _settings.AzureOpenAIKey;
        var originalOpenAIEndpoint = _settings.AzureOpenAIEndpoint;

        _settings.SnapshotFrequencySeconds = SnapshotFrequency;
        _settings.Brightness = Brightness;
        _settings.Contrast = Contrast;
        _settings.IsTranslationEnabled = IsTranslationEnabled;
        _settings.TargetLanguage = TargetLanguage;
        _settings.IsTtsEnabled = IsTtsEnabled;
        _settings.TtsVoice = TtsVoice;
        _settings.AzureTranslatorKey = AzureTranslatorKey;
        _settings.AzureSpeechKey = AzureSpeechKey;
        _settings.AzureOpenAIEndpoint = AzureOpenAIEndpoint;
        _settings.AzureOpenAIKey = AzureOpenAIKey;
        _settings.AzureOpenAITranslationDeployment = AzureOpenAITranslationDeployment;
        _settings.AzureOpenAISpeechDeployment = AzureOpenAISpeechDeployment;
        _settings.IsSpeechToSpeechEnabled = IsSpeechToSpeechEnabled;
        _settings.SpeechToSpeechSourceLanguage = string.IsNullOrWhiteSpace(SpeechToSpeechSourceLanguage) 
            ? null 
            : SpeechToSpeechSourceLanguage;
        _settings.SpeechToSpeechTargetLanguage = SpeechToSpeechTargetLanguage;
        _settings.AdaptiveScheduling = AdaptiveScheduling;
        _settings.LowBatteryThreshold = LowBatteryThreshold;

        _settingsService.SaveSettings(_settings);
        _logger.Info("Settings saved successfully");

        // Reinitialize services if API keys have changed
        if (originalTranslatorKey != AzureTranslatorKey)
        {
            _logger.Info("Azure Translator key changed, reinitializing translation service");
            if (_translationService is AzureTranslatorService translatorService)
            {
                translatorService.Reinitialize();
                _logger.Info("Translation service reinitialized with new API key");
            }
        }

        if (originalSpeechKey != AzureSpeechKey)
        {
            _logger.Info("Azure Speech key changed, reinitializing TTS service");
            if (_ttsService is AzureTtsService ttsService)
            {
                ttsService.Reinitialize();
                _logger.Info("TTS service reinitialized with new API key");
            }
        }

        if (originalOpenAIKey != AzureOpenAIKey || originalOpenAIEndpoint != AzureOpenAIEndpoint)
        {
            _logger.Info("Azure OpenAI configuration changed, reinitializing services");
            if (_translationService is AzureOpenAITranslationService openAITranslationService)
            {
                openAITranslationService.Reinitialize();
                _logger.Info("OpenAI Translation service reinitialized");
            }
            
            _speechToSpeechService.Reinitialize();
            _logger.Info("Speech-to-Speech service reinitialized");
        }
    }

    [RelayCommand]
    private async Task ResetSettingsAsync()
    {
        var result = await Application.Current!.MainPage!.DisplayAlert(
            "Reset Settings",
            "Are you sure you want to reset all settings to defaults?",
            "Yes",
            "No"
        );

        if (result)
        {
            _settingsService.ResetSettings();
            _settings = _settingsService.LoadSettings();
            LoadSettings();
            _logger.Info("Settings reset to defaults");
        }
    }

    [RelayCommand]
    private async Task OpenAzurePortalAsync()
    {
        try
        {
            await Browser.OpenAsync(Constants.AzurePortalUrl, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to open Azure Portal", ex);
        }
    }

    [RelayCommand]
    private async Task TestTranslationAsync()
    {
        try
        {
            // Save current settings before testing
            InternalSaveSettings();
            
            // Check if translation service is configured
            if (!_translationService.IsConfigured)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Translation Not Configured",
                    "Translation service not configured. Please add Azure Translator key in Settings.",
                    "OK"
                );
                return;
            }

            // Use sample text
            var testText = "This is a test of subtitle translation.";
            var settings = _settingsService.LoadSettings();

            // Perform translation
            var (translated, detected) = await _translationService.TranslateAsync(
                testText,
                settings.TargetLanguage,
                "en"
            );
            
            // Show result
            await Application.Current!.MainPage!.DisplayAlert(
                "Translation Test Result",
                $"Original: {testText}\n\nTranslated: {translated}",
                "OK"
            );
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to test translation", ex);
            await Application.Current!.MainPage!.DisplayAlert(
                "Translation Test Error",
                $"Error: {ex.Message}",
                "OK"
            );
        }
    }

    [RelayCommand]
    private async Task TestTtsAsync()
    {
        try
        {
            // Save current settings before testing
            InternalSaveSettings();
            
            // Check if TTS service is configured
            if (!_ttsService.IsConfigured)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "TTS Not Configured",
                    "TTS service not configured. Please add Azure Speech key in Settings.",
                    "OK"
                );
                return;
            }

            // Use sample text
            var testText = "Acesta este un test de traducere a subtitrărilor.";
            var settings = _settingsService.LoadSettings();
            
            // Show what will be spoken
            await Application.Current!.MainPage!.DisplayAlert(
                "Testing TTS",
                $"Speaking: \"{testText}\"\nVoice: {settings.TtsVoice}",
                "OK"
            );

            // Actually speak the text
            await _ttsService.SpeakAsync(testText, settings.TtsVoice);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to test TTS", ex);
            await Application.Current!.MainPage!.DisplayAlert(
                "TTS Test Error",
                $"Error: {ex.Message}",
                "OK"
            );
        }
    }

    [RelayCommand]
    private async Task TestSpeechToSpeechAsync()
    {
        try
        {
            // Save current settings before testing
            InternalSaveSettings();
            
            // Check if Speech-to-Speech service is configured
            if (!_speechToSpeechService.IsConfigured)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Speech-to-Speech Not Configured",
                    "Speech-to-Speech service not configured. Please add Azure OpenAI settings.",
                    "OK"
                );
                return;
            }

            if (_speechToSpeechService.IsActive)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Already Active",
                    "Speech-to-Speech service is already running. Stop it first.",
                    "OK"
                );
                return;
            }

            // Start the service for testing
            var settings = _settingsService.LoadSettings();
            await _speechToSpeechService.StartAsync(
                settings.SpeechToSpeechSourceLanguage,
                settings.SpeechToSpeechTargetLanguage
            );

            await Application.Current!.MainPage!.DisplayAlert(
                "Testing Speech-to-Speech",
                "Speech-to-Speech service started. Speak into your microphone to test translation.",
                "Stop"
            );

            // Stop the service
            await _speechToSpeechService.StopAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to test Speech-to-Speech", ex);
            await Application.Current!.MainPage!.DisplayAlert(
                "Speech-to-Speech Test Error",
                $"Error: {ex.Message}",
                "OK"
            );
            
            // Ensure service is stopped on error
            if (_speechToSpeechService.IsActive)
            {
                await _speechToSpeechService.StopAsync();
            }
        }
    }
}
