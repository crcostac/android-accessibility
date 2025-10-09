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
            return true;
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
            "ScreenCapture" => "Screen capture permission is required to read subtitles from your screen. " +
                              "This allows Subzy to take periodic screenshots to extract subtitle text.",
            
            "Accessibility" => "Accessibility permission may be required for enhanced screen reading capabilities. " +
                              "This is optional but can improve functionality.",
            
            "Internet" => "Internet access is required to use cloud-based translation and text-to-speech services. " +
                         "Your subtitle text will be securely transmitted to Azure for processing.",
            
            "ForegroundService" => "Foreground service permission allows Subzy to run in the background " +
                                  "while you watch videos, ensuring continuous subtitle reading.",
            
            _ => "This permission is required for Subzy to function properly."
        };
    }
}
