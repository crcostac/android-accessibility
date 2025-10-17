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
    private readonly ISpeechToSpeechService _speechToSpeechService;

#if ANDROID
    private readonly Platforms.Android.AudioPlaybackService? _audioPlaybackService;
#endif

    [ObservableProperty]
    private string _statusText = "Welcome to Subzy";

    [ObservableProperty]
    private bool _isServiceRunning;

    [ObservableProperty]
    private bool _isSpeechToSpeechRunning;

    [ObservableProperty]
    private SubtitleData? _lastSubtitle;

    [ObservableProperty]
    private string _lastProcessedText = "No subtitles processed yet";

    [ObservableProperty]
    private string _speechToSpeechStatus = "Speech-to-Speech: Not active";

    public MainViewModel(
        ILoggingService logger, 
        SettingsService settingsService,
        ISpeechToSpeechService speechToSpeechService
#if ANDROID
        , Platforms.Android.AudioPlaybackService audioPlaybackService
#endif
    )
    {
        _logger = logger;
        _settingsService = settingsService;
        _speechToSpeechService = speechToSpeechService;

#if ANDROID
        _audioPlaybackService = audioPlaybackService;
#endif
        
        // Subscribe to Speech-to-Speech events
        _speechToSpeechService.TranslatedTextReceived += OnTranslatedTextReceived;
        _speechToSpeechService.AudioResponseReceived += OnAudioResponseReceived;
        _speechToSpeechService.ErrorOccurred += OnSpeechToSpeechError;
        
        Initialize();
    }

    private void Initialize()
    {
        var settings = _settingsService.LoadSettings();
        IsServiceRunning = settings.IsServiceEnabled;
        IsSpeechToSpeechRunning = _speechToSpeechService.IsActive;
        
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
                StatusText = "Service starting...";
                _logger.Info("Screen capture service start requested");
                StartPlatformService();
            }
            else
            {
                StatusText = "Service stopped";
                _logger.Info("Screen capture service stop requested");
                StopPlatformService();
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
    private async Task ToggleSpeechToSpeechAsync()
    {
        try
        {
            var settings = _settingsService.LoadSettings();
            
            if (!settings.IsSpeechToSpeechEnabled)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Speech-to-Speech Disabled",
                    "Speech-to-Speech is disabled in settings. Please enable it first.",
                    "OK"
                );
                return;
            }

            if (!_speechToSpeechService.IsConfigured)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Not Configured",
                    "Speech-to-Speech service is not configured. Please add Azure OpenAI settings.",
                    "OK"
                );
                return;
            }

            if (_speechToSpeechService.IsActive)
            {
                // Stop the service
                await _speechToSpeechService.StopAsync();
#if ANDROID
                // Stop audio playback
                if (_audioPlaybackService != null)
                {
                    await _audioPlaybackService.StopPlaybackAsync();
                }
#endif
                IsSpeechToSpeechRunning = false;
                SpeechToSpeechStatus = "Speech-to-Speech: Stopped";
                _logger.Info("Speech-to-Speech service stopped");
            }
            else
            {
                // Start audio playback first
#if ANDROID
                if (_audioPlaybackService != null)
                {
                    await _audioPlaybackService.StartPlaybackAsync();
                    _logger.Info("Audio playback service started");
                }
#endif
                // Start the service
                SpeechToSpeechStatus = "Speech-to-Speech: Starting...";
                await _speechToSpeechService.StartAsync(
                    settings.SpeechToSpeechSourceLanguage,
                    settings.SpeechToSpeechTargetLanguage
                );
                IsSpeechToSpeechRunning = true;
                SpeechToSpeechStatus = "Speech-to-Speech: Active - Listening...";
                _logger.Info("Speech-to-Speech service started");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to toggle Speech-to-Speech", ex);
            IsSpeechToSpeechRunning = false;
            SpeechToSpeechStatus = $"Speech-to-Speech Error: {ex.Message}";
            
            await Application.Current!.MainPage!.DisplayAlert(
                "Error",
                $"Failed to toggle Speech-to-Speech: {ex.Message}",
                "OK"
            );
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

    private void OnTranslatedTextReceived(object? sender, string translatedText)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LastProcessedText = $"[Speech-to-Speech] {translatedText}";
            SpeechToSpeechStatus = $"Speech-to-Speech: Translated - {translatedText.Substring(0, Math.Min(30, translatedText.Length))}...";
        });
    }

    private void OnAudioResponseReceived(object? sender, byte[] audioData)
    {
#if ANDROID
        // Enqueue audio data to the playback service
        if (_audioPlaybackService != null && audioData != null && audioData.Length > 0)
        {
            _audioPlaybackService.EnqueueAudio(audioData);
            _logger.Debug($"Enqueued {audioData.Length} bytes of audio for playback");
        }
#endif
    }

    private void OnSpeechToSpeechError(object? sender, Exception ex)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _logger.Error("Speech-to-Speech error", ex);
            SpeechToSpeechStatus = $"Speech-to-Speech Error: {ex.Message}";
        });
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

        // Update Speech-to-Speech status
        if (_speechToSpeechService.IsActive)
        {
            IsSpeechToSpeechRunning = true;
            SpeechToSpeechStatus = "Speech-to-Speech: Active - Listening...";
        }
        else
        {
            IsSpeechToSpeechRunning = false;
            SpeechToSpeechStatus = "Speech-to-Speech: Not active";
        }
    }

    /// <summary>
    /// Platform-specific implementation for starting the service.
    /// </summary>
    partial void StartPlatformService();

    /// <summary>
    /// Platform-specific implementation for stopping the service.
    /// </summary>
    partial void StopPlatformService();
}
