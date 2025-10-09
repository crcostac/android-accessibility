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
    private bool _adaptiveScheduling;

    [ObservableProperty]
    private int _lowBatteryThreshold;

    public List<string> AvailableVoices { get; } = new(Constants.RomanianVoices);

    public SettingsViewModel(ILoggingService logger, SettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
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
        AdaptiveScheduling = _settings.AdaptiveScheduling;
        LowBatteryThreshold = _settings.LowBatteryThreshold;

        _logger.Info("Settings loaded into ViewModel");
    }

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            _settings.SnapshotFrequencySeconds = SnapshotFrequency;
            _settings.Brightness = Brightness;
            _settings.Contrast = Contrast;
            _settings.IsTranslationEnabled = IsTranslationEnabled;
            _settings.TargetLanguage = TargetLanguage;
            _settings.IsTtsEnabled = IsTtsEnabled;
            _settings.TtsVoice = TtsVoice;
            _settings.AzureTranslatorKey = AzureTranslatorKey;
            _settings.AzureSpeechKey = AzureSpeechKey;
            _settings.AdaptiveScheduling = AdaptiveScheduling;
            _settings.LowBatteryThreshold = LowBatteryThreshold;

            _settingsService.SaveSettings(_settings);
            _logger.Info("Settings saved successfully");

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
            SaveSettings();
            await Application.Current!.MainPage!.DisplayAlert(
                "Test Translation",
                "Translation test would be performed here with current settings",
                "OK"
            );
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to test translation", ex);
        }
    }

    [RelayCommand]
    private async Task TestTtsAsync()
    {
        try
        {
            SaveSettings();
            await Application.Current!.MainPage!.DisplayAlert(
                "Test TTS",
                "Text-to-speech test would be performed here with current settings",
                "OK"
            );
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to test TTS", ex);
        }
    }
}
