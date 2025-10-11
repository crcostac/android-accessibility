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
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to stop platform service", ex);
        }
    }
}
