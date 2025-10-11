using Subzy.Services.Interfaces;

namespace Subzy.Services;

/// <summary>
/// Service for detecting the currently active foreground app.
/// Platform-specific implementations are in the Platforms folder.
/// </summary>
public partial class ForegroundAppDetector
{
    private readonly ILoggingService _logger;

    public ForegroundAppDetector(ILoggingService logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the package name of the currently active app.
    /// </summary>
    /// <returns>Package name (e.g., "com.netflix.mediaclient") or null if unavailable</returns>
    public partial string? GetForegroundAppPackageName();

    /// <summary>
    /// Gets a user-friendly display name for the given package.
    /// </summary>
    /// <param name="packageName">Android package name</param>
    /// <returns>Display name (e.g., "Netflix") or the package name if unavailable</returns>
    public partial string GetAppDisplayName(string packageName);

    /// <summary>
    /// Checks if the necessary permissions are granted.
    /// </summary>
    /// <returns>True if permissions are available</returns>
    public partial bool HasPermission();
}
