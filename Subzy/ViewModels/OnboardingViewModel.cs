using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Subzy.Helpers;
using Subzy.Models;
using Subzy.Services;
using Subzy.Services.Interfaces;

namespace Subzy.ViewModels;

/// <summary>
/// ViewModel for the onboarding flow.
/// </summary>
public partial class OnboardingViewModel : ObservableObject
{
    private readonly ILoggingService _logger;
    private readonly SettingsService _settingsService;
    private readonly PermissionHelper _permissionHelper;

    [ObservableProperty]
    private int _currentStep = 0;

    [ObservableProperty]
    private string _currentStepTitle = "Welcome to Subzy";

    [ObservableProperty]
    private string _currentStepDescription = "Enhance your viewing experience with real-time subtitle translation and text-to-speech";

    [ObservableProperty]
    private bool _canProceed = true;

    [ObservableProperty]
    private Models.PermissionStatus _permissionStatus = new();

    private readonly List<(string title, string description)> _steps = new()
    {
        ("Welcome to Subzy", "Enhance your viewing experience with real-time subtitle translation and text-to-speech"),
        ("How It Works", "Subzy captures your screen, reads subtitles using OCR, translates them, and speaks them aloud"),
        ("Privacy & Data", "Screenshots are processed locally and sent securely to Azure for translation and TTS. No data is permanently stored."),
        ("Permissions Required", "Subzy needs screen capture, internet access, and notification permissions to function"),
        ("Azure Configuration", "You'll need Azure Cognitive Services API keys for translation and text-to-speech"),
        ("Get Started", "You're all set! Configure your preferences in settings and start using Subzy")
    };

    public int TotalSteps => _steps.Count;

    public OnboardingViewModel(
        ILoggingService logger,
        SettingsService settingsService,
        PermissionHelper permissionHelper)
    {
        _logger = logger;
        _settingsService = settingsService;
        _permissionHelper = permissionHelper;

        UpdateStepContent();
        _logger.Info("OnboardingViewModel initialized");
    }

    [RelayCommand]
    private async Task NextStepAsync()
    {
        if (CurrentStep < TotalSteps - 1)
        {
            CurrentStep++;
            UpdateStepContent();

            // Check permissions on the permissions step
            if (CurrentStep == 3)
            {
                await CheckPermissionsAsync();
            }
        }
        else
        {
            await CompleteOnboardingAsync();
        }
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep > 0)
        {
            CurrentStep--;
            UpdateStepContent();
        }
    }

    [RelayCommand]
    private async Task RequestPermissionsAsync()
    {
        try
        {
            _logger.Info("Requesting permissions");
            await _permissionHelper.RequestPermissionsAsync();
            await CheckPermissionsAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to request permissions", ex);
        }
    }

    [RelayCommand]
    private async Task SkipOnboardingAsync()
    {
        var result = await Application.Current!.MainPage!.DisplayAlert(
            "Skip Onboarding",
            "Are you sure you want to skip the onboarding? You can always access help from the settings.",
            "Yes, Skip",
            "No"
        );

        if (result)
        {
            await CompleteOnboardingAsync();
        }
    }

    private async Task CheckPermissionsAsync()
    {
        try
        {
            PermissionStatus = await _permissionHelper.CheckPermissionsAsync();
            CanProceed = PermissionStatus.AllPermissionsGranted;
            _logger.Info($"Permission check: All granted = {CanProceed}");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to check permissions", ex);
        }
    }

    private async Task CompleteOnboardingAsync()
    {
        try
        {
            var settings = _settingsService.LoadSettings();
            settings.HasCompletedOnboarding = true;
            _settingsService.SaveSettings(settings);

            _logger.Info("Onboarding completed");
            
            // Navigate to main page
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to complete onboarding", ex);
        }
    }

    private void UpdateStepContent()
    {
        if (CurrentStep >= 0 && CurrentStep < _steps.Count)
        {
            var step = _steps[CurrentStep];
            CurrentStepTitle = step.title;
            CurrentStepDescription = step.description;
        }
    }
}
