using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Content.PM;

namespace Subzy.Services;

/// <summary>
/// Android-specific implementation for detecting the foreground app.
/// </summary>
public partial class ForegroundAppDetector
{
    public partial string? GetForegroundAppPackageName()
    {
        try
        {
            var context = Platform.AppContext;
            var usageStatsManager = context.GetSystemService(Context.UsageStatsService) as UsageStatsManager;

            if (usageStatsManager == null)
            {
                _logger.Warning("UsageStatsManager not available");
                return null;
            }

            // Query usage stats for the last second
            var endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var startTime = endTime - 1000; // 1 second ago

            var usageStats = usageStatsManager.QueryUsageStats(
                UsageStatsInterval.Daily,
                startTime,
                endTime
            );

            if (usageStats == null || !usageStats.Any())
            {
                _logger.Debug("No usage stats available");
                return null;
            }

            // Find the most recently used app
            var recentApp = usageStats
                .OrderByDescending(stats => stats.LastTimeUsed)
                .FirstOrDefault();

            if (recentApp != null)
            {
                _logger.Debug($"Foreground app detected: {recentApp.PackageName}");
                return recentApp.PackageName;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to detect foreground app", ex);
            return null;
        }
    }

    public partial string GetAppDisplayName(string packageName)
    {
        try
        {
            var context = Platform.AppContext;
            var packageManager = context.PackageManager;

            if (packageManager == null)
                return packageName;

            var appInfo = packageManager.GetApplicationInfo(packageName, 0);
            var appName = packageManager.GetApplicationLabel(appInfo)?.ToString();

            return appName ?? packageName;
        }
        catch (Exception ex)
        {
            _logger.Debug($"Failed to get app display name for {packageName}: {ex.Message}");
            return packageName;
        }
    }

    public partial bool HasPermission()
    {
        try
        {
            var context = Platform.AppContext;
            var appOpsManager = context.GetSystemService(Context.AppOpsService) as AppOpsManager;

            if (appOpsManager == null)
                return false;

            var mode = appOpsManager.CheckOpNoThrow(
                AppOpsManager.OpstrGetUsageStats,
                Android.OS.Process.MyUid(),
                context.PackageName
            );

            return mode == AppOpsManagerMode.Allowed;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to check usage stats permission", ex);
            return false;
        }
    }
}
