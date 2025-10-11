using Android.App;
using Android.Content;
using Android.Media.Projection;
using Android.OS;
using Subzy.Helpers;
using Subzy.Services;
using Subzy.Services.Interfaces;

namespace Subzy.Platforms.Android;

/// <summary>
/// Lightweight activity that requests MediaProjection permission and starts the ScreenCaptureService.
/// This activity is transparent and finishes immediately after handling the permission flow.
/// </summary>
[Activity(
    Label = "Screen Capture Permission",
    Theme = "@android:style/Theme.Translucent.NoTitleBar",
    LaunchMode = global::Android.Content.PM.LaunchMode.SingleTop,
    NoHistory = true)]
public class ScreenCapturePermissionActivity : Activity
{
    private ILoggingService? _logger;
    private SettingsService? _settingsService;
    private MediaProjectionManager? _mediaProjectionManager;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        try
        {
            // Get services
            _logger = MauiApplication.Current.Services.GetService<ILoggingService>();
            _settingsService = MauiApplication.Current.Services.GetService<SettingsService>();

            _logger?.Info("ScreenCapturePermissionActivity created");

            // Get MediaProjectionManager
            _mediaProjectionManager = (MediaProjectionManager?)GetSystemService(MediaProjectionService);
            if (_mediaProjectionManager == null)
            {
                _logger?.Error("Failed to get MediaProjectionManager");
                RevertServiceState();
                Finish();
                return;
            }

            // Launch permission intent
            var permissionIntent = _mediaProjectionManager.CreateScreenCaptureIntent();
            StartActivityForResult(permissionIntent, Constants.MediaProjectionRequestCode);

            _logger?.Info("Screen capture permission requested");
        }
        catch (Exception ex)
        {
            _logger?.Error("Failed to request screen capture permission", ex);
            RevertServiceState();
            Finish();
        }
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode == Constants.MediaProjectionRequestCode)
        {
            if (resultCode == Result.Ok && data != null)
            {
                _logger?.Info("Screen capture permission granted");
                StartScreenCaptureService((int)resultCode, data);
            }
            else
            {
                _logger?.Warning("Screen capture permission denied");
                RevertServiceState();
            }
        }

        // Finish the activity after handling the result
        Finish();
    }

    private void StartScreenCaptureService(int resultCode, Intent data)
    {
        try
        {
            var serviceIntent = new Intent(this, typeof(Services.ScreenCaptureService));
            serviceIntent.SetAction(Services.ScreenCaptureService.ActionStart);
            serviceIntent.PutExtra(Services.ScreenCaptureService.ExtraResultCode, resultCode);
            serviceIntent.PutExtra(Services.ScreenCaptureService.ExtraData, data);

            // Use StartForegroundService for API 26+ (Android Oreo and above)
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                StartForegroundService(serviceIntent);
                _logger?.Info("Started foreground service (API >= 26)");
            }
            else
            {
                StartService(serviceIntent);
                _logger?.Info("Started service (API < 26)");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error("Failed to start ScreenCaptureService", ex);
            RevertServiceState();
        }
    }

    private void RevertServiceState()
    {
        try
        {
            if (_settingsService != null)
            {
                var settings = _settingsService.LoadSettings();
                settings.IsServiceEnabled = false;
                _settingsService.SaveSettings(settings);
                _logger?.Info("Reverted service state to disabled");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error("Failed to revert service state", ex);
        }
    }

    protected override void OnDestroy()
    {
        _logger?.Info("ScreenCapturePermissionActivity destroyed");
        base.OnDestroy();
    }
}
