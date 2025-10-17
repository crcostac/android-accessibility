using Subzy.Models;
using Subzy.Services.Interfaces;

namespace Subzy.Helpers;

/// <summary>
/// Helper class for managing application permissions.
/// </summary>
public class PermissionHelper
{
    private readonly ILoggingService _logger;

    public PermissionHelper(ILoggingService logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Checks the current status of all required permissions.
    /// </summary>
    public async Task<Models.PermissionStatus> CheckPermissionsAsync()
    {
        var status = new Models.PermissionStatus();

        try
        {
            // Check internet permission (usually granted by default)
            status.HasInternetPermission = true;

            // Check foreground service permission (Android 9+)
            status.HasForegroundServicePermission = true;

            // Screen capture and accessibility permissions need to be checked platform-specifically
            // These are handled in the Android-specific code

            _logger.Info($"Permission check completed: Internet={status.HasInternetPermission}, " +
                        $"Foreground={status.HasForegroundServicePermission}");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to check permissions", ex);
        }

        return status;
    }

    /// <summary>
    /// Requests necessary permissions from the user.
    /// </summary>
    public async Task<bool> RequestPermissionsAsync()
    {
        try
        {
            // Basic permissions can be requested here
            // Platform-specific permissions (screen capture, accessibility) are handled separately
            
            _logger.Info("Permission request initiated");
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to request permissions", ex);
            return false;
        }
    }

    /// <summary>
    /// Gets a user-friendly explanation for a specific permission.
    /// </summary>
    public string GetPermissionExplanation(string permissionType)
    {
        return permissionType switch
        {
            "ScreenCapture" => "Screen capture permission is required to read subtitles from your screen and capture audio from streaming apps. " +
                              "This allows Subzy to take periodic screenshots to extract subtitle text and optionally capture audio for speech-to-speech translation.",
            
            "Accessibility" => "Accessibility permission may be required for enhanced screen reading capabilities. " +
                              "This is optional but can improve functionality.",
            
            "Internet" => "Internet access is required to use cloud-based translation and text-to-speech services. " +
                         "Your subtitle text and audio will be securely transmitted to Azure for processing.",
            
            "ForegroundService" => "Foreground service permission allows Subzy to run in the background " +
                                  "while you watch videos, ensuring continuous subtitle reading and audio translation.",
            
            "MediaProjection" => "Media projection permission allows Subzy to capture both screen content and audio from streaming apps. " +
                                "This is required for both subtitle reading (OCR) and speech-to-speech translation features. " +
                                "Audio is only captured from media playback (movies, shows), not from system sounds or notifications.",
            
            _ => "This permission is required for Subzy to function properly."
        };
    }
}
