using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Subzy.Models;
using Subzy.Services;
using Subzy.Services.Interfaces;

namespace Subzy.ViewModels;

/// <summary>
/// ViewModel for the main application page.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILoggingService _logger;
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private string _statusText = "Welcome to Subzy";

    [ObservableProperty]
    private bool _isServiceRunning;

    [ObservableProperty]
    private SubtitleData? _lastSubtitle;

    [ObservableProperty]
    private string _lastProcessedText = "No subtitles processed yet";

    public MainViewModel(ILoggingService logger, SettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
        
        Initialize();
    }

    private void Initialize()
    {
        var settings = _settingsService.LoadSettings();
        IsServiceRunning = settings.IsServiceEnabled;
        
        UpdateStatusText();
        _logger.Info("MainViewModel initialized");
    }

    [RelayCommand]
    private async Task ToggleServiceAsync()
    {
        try
        {
            var settings = _settingsService.LoadSettings();
            settings.IsServiceEnabled = !settings.IsServiceEnabled;
            _settingsService.SaveSettings(settings);
            
            IsServiceRunning = settings.IsServiceEnabled;
            
            if (IsServiceRunning)
            {
                // TODO: Start the Android service
                StatusText = "Service starting...";
                _logger.Info("Screen capture service start requested");
            }
            else
            {
                // TODO: Stop the Android service
                StatusText = "Service stopped";
                _logger.Info("Screen capture service stop requested");
            }
            
            UpdateStatusText();
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to toggle service", ex);
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task NavigateToSettingsAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//SettingsPage");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to navigate to settings", ex);
        }
    }

    [RelayCommand]
    private async Task NavigateToDebugAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//DebugPage");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to navigate to debug page", ex);
        }
    }

    public void UpdateSubtitle(SubtitleData subtitle)
    {
        LastSubtitle = subtitle;
        LastProcessedText = subtitle.WasTranslated 
            ? $"Original: {subtitle.OriginalText}\nTranslated: {subtitle.TranslatedText}"
            : subtitle.OriginalText;
        
        _logger.Debug($"Updated subtitle display: {subtitle.OriginalText}");
    }

    private void UpdateStatusText()
    {
        if (IsServiceRunning)
        {
            StatusText = "Service is running - Reading subtitles";
        }
        else
        {
            StatusText = "Service is stopped - Tap to start";
        }
    }
}
