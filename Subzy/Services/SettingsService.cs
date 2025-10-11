using Subzy.Models;
using Subzy.Services.Interfaces;

namespace Subzy.Services;

/// <summary>
/// Service for persisting and loading user preferences using MAUI Preferences API.
/// </summary>
public class SettingsService
{
    private readonly ILoggingService _logger;
    private AppSettings? _cachedSettings;

    public SettingsService(ILoggingService logger)
    {
        _logger = logger;
    }

    public AppSettings LoadSettings()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        try
        {
            _cachedSettings = new AppSettings
            {
                IsServiceEnabled = Preferences.Get(nameof(AppSettings.IsServiceEnabled), false),
                SnapshotFrequencySeconds = Preferences.Get(nameof(AppSettings.SnapshotFrequencySeconds), 2),
                
                Brightness = Preferences.Get(nameof(AppSettings.Brightness), 1.0f),
                Contrast = Preferences.Get(nameof(AppSettings.Contrast), 1.0f),
                
                IsTranslationEnabled = Preferences.Get(nameof(AppSettings.IsTranslationEnabled), true),
                TargetLanguage = Preferences.Get(nameof(AppSettings.TargetLanguage), "ro"),
                
                IsTtsEnabled = Preferences.Get(nameof(AppSettings.IsTtsEnabled), true),
                TtsVoice = Preferences.Get(nameof(AppSettings.TtsVoice), "ro-RO-AlinaNeural"),
                
                RoiX = Preferences.Get(nameof(AppSettings.RoiX), 0),
                RoiY = Preferences.Get(nameof(AppSettings.RoiY), 0),
                RoiWidth = Preferences.Get(nameof(AppSettings.RoiWidth), 0),
                RoiHeight = Preferences.Get(nameof(AppSettings.RoiHeight), 0),
                
                AzureTranslatorKey = Preferences.Get(nameof(AppSettings.AzureTranslatorKey), ""),
                AzureTranslatorRegion = Preferences.Get(nameof(AppSettings.AzureTranslatorRegion), "westeurope"),
                AzureSpeechKey = Preferences.Get(nameof(AppSettings.AzureSpeechKey), ""),
                AzureSpeechRegion = Preferences.Get(nameof(AppSettings.AzureSpeechRegion), "westeurope"),
                
                AdaptiveScheduling = Preferences.Get(nameof(AppSettings.AdaptiveScheduling), true),
                LowBatteryThreshold = Preferences.Get(nameof(AppSettings.LowBatteryThreshold), 20),
                
                HasCompletedOnboarding = Preferences.Get(nameof(AppSettings.HasCompletedOnboarding), false),
                
                MaxColorsPerApp = Preferences.Get(nameof(AppSettings.MaxColorsPerApp), 5),
                SubtitleColorTolerance = Preferences.Get(nameof(AppSettings.SubtitleColorTolerance), 30),
                MinSameColorNeighbors = Preferences.Get(nameof(AppSettings.MinSameColorNeighbors), 2),
                UsePerceptualHashing = Preferences.Get(nameof(AppSettings.UsePerceptualHashing), true),
                HashSimilarityThreshold = Preferences.Get(nameof(AppSettings.HashSimilarityThreshold), 8)
            };

            _logger.Info("Settings loaded successfully");
            return _cachedSettings;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load settings", ex);
            _cachedSettings = new AppSettings();
            return _cachedSettings;
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            Preferences.Set(nameof(AppSettings.IsServiceEnabled), settings.IsServiceEnabled);
            Preferences.Set(nameof(AppSettings.SnapshotFrequencySeconds), settings.SnapshotFrequencySeconds);
            
            Preferences.Set(nameof(AppSettings.Brightness), settings.Brightness);
            Preferences.Set(nameof(AppSettings.Contrast), settings.Contrast);
            
            Preferences.Set(nameof(AppSettings.IsTranslationEnabled), settings.IsTranslationEnabled);
            Preferences.Set(nameof(AppSettings.TargetLanguage), settings.TargetLanguage);
            
            Preferences.Set(nameof(AppSettings.IsTtsEnabled), settings.IsTtsEnabled);
            Preferences.Set(nameof(AppSettings.TtsVoice), settings.TtsVoice);
            
            Preferences.Set(nameof(AppSettings.RoiX), settings.RoiX);
            Preferences.Set(nameof(AppSettings.RoiY), settings.RoiY);
            Preferences.Set(nameof(AppSettings.RoiWidth), settings.RoiWidth);
            Preferences.Set(nameof(AppSettings.RoiHeight), settings.RoiHeight);
            
            Preferences.Set(nameof(AppSettings.AzureTranslatorKey), settings.AzureTranslatorKey);
            Preferences.Set(nameof(AppSettings.AzureTranslatorRegion), settings.AzureTranslatorRegion);
            Preferences.Set(nameof(AppSettings.AzureSpeechKey), settings.AzureSpeechKey);
            Preferences.Set(nameof(AppSettings.AzureSpeechRegion), settings.AzureSpeechRegion);
            
            Preferences.Set(nameof(AppSettings.AdaptiveScheduling), settings.AdaptiveScheduling);
            Preferences.Set(nameof(AppSettings.LowBatteryThreshold), settings.LowBatteryThreshold);
            
            Preferences.Set(nameof(AppSettings.HasCompletedOnboarding), settings.HasCompletedOnboarding);
            
            Preferences.Set(nameof(AppSettings.MaxColorsPerApp), settings.MaxColorsPerApp);
            Preferences.Set(nameof(AppSettings.SubtitleColorTolerance), settings.SubtitleColorTolerance);
            Preferences.Set(nameof(AppSettings.MinSameColorNeighbors), settings.MinSameColorNeighbors);
            Preferences.Set(nameof(AppSettings.UsePerceptualHashing), settings.UsePerceptualHashing);
            Preferences.Set(nameof(AppSettings.HashSimilarityThreshold), settings.HashSimilarityThreshold);

            _cachedSettings = settings;
            _logger.Info("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to save settings", ex);
        }
    }

    public void ResetSettings()
    {
        try
        {
            Preferences.Clear();
            _cachedSettings = null;
            _logger.Info("Settings reset to defaults");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to reset settings", ex);
        }
    }
}
