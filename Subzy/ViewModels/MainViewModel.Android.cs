using Android.Content;
using Subzy.Platforms.Android;

namespace Subzy.ViewModels;

/// <summary>
/// Android-specific implementation of MainViewModel partial methods.
/// </summary>
public partial class MainViewModel
{
    /// <summary>
    /// Starts the screen capture service by launching the permission activity.
    /// Also starts Speech-to-Speech if enabled.
    /// </summary>
    partial void StartPlatformService()
    {
        try
        {
            var context = global::Android.App.Application.Context;
            var intent = new Intent(context, typeof(ScreenCapturePermissionActivity));
            intent.SetFlags(ActivityFlags.NewTask);
            context.StartActivity(intent);

            _logger.Info("Launched ScreenCapturePermissionActivity");

            // Start Speech-to-Speech if enabled
            var settings = _settingsService.LoadSettings();
            if (settings.IsSpeechToSpeechEnabled && _speechToSpeechService.IsConfigured)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _speechToSpeechService.StartAsync(
                            settings.SpeechToSpeechSourceLanguage,
                            settings.SpeechToSpeechTargetLanguage
                        );
                        
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            IsSpeechToSpeechRunning = true;
                            SpeechToSpeechStatus = "Speech-to-Speech: Active - Listening...";
                        });

                        _logger.Info("Speech-to-Speech service started automatically");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Failed to auto-start Speech-to-Speech", ex);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to start platform service", ex);
            
            // Revert state on failure
            var settings = _settingsService.LoadSettings();
            settings.IsServiceEnabled = false;
            _settingsService.SaveSettings(settings);
            IsServiceRunning = false;
            StatusText = "Failed to start service";
        }
    }

    /// <summary>
    /// Stops the screen capture service by sending a stop intent.
    /// Also stops Speech-to-Speech if running.
    /// </summary>
    partial void StopPlatformService()
    {
        try
        {
            var context = global::Android.App.Application.Context;
            var intent = new Intent(context, typeof(Subzy.Platforms.Android.Services.ScreenCaptureService));
            intent.SetAction(Subzy.Platforms.Android.Services.ScreenCaptureService.ActionStop);
            context.StartService(intent);

            _logger.Info("Sent stop intent to ScreenCaptureService");

            // Stop Speech-to-Speech if running
            if (_speechToSpeechService.IsActive)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _speechToSpeechService.StopAsync();
                        
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            IsSpeechToSpeechRunning = false;
                            SpeechToSpeechStatus = "Speech-to-Speech: Stopped";
                        });

                        _logger.Info("Speech-to-Speech service stopped automatically");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Failed to auto-stop Speech-to-Speech", ex);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to stop platform service", ex);
        }
    }
}
