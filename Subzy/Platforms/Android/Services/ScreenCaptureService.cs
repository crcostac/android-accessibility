using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Display;
using Android.Media;
using Android.Media.Projection;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.Core.App;
using Subzy.Helpers;
using Subzy.Services;
using Subzy.Services.Interfaces;
using System.IO;

namespace Subzy.Platforms.Android.Services;

/// <summary>
/// Android foreground service that periodically captures screenshots for subtitle processing.
/// </summary>
[Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeMediaProjection)]
public class ScreenCaptureService : Service
{
    private ILoggingService? _logger;
    private SettingsService? _settingsService;
    private WorkflowOrchestrator? _orchestrator;
    private Timer? _captureTimer;
    private MediaProjectionManager? _projectionManager;
    private MediaProjection? _mediaProjection;
    private ImageReader? _imageReader;
    private VirtualDisplay? _virtualDisplay;
    private MediaProjectionCallback? _projectionCallback;
    private bool _isRunning;

    public const string ActionStart = "com.accessibility.subzy.START_CAPTURE";
    public const string ActionStop = "com.accessibility.subzy.STOP_CAPTURE";
    public const string ActionPickColor = "com.accessibility.subzy.PICK_COLOR";
    public const string ExtraResultCode = "result_code";
    public const string ExtraData = "data";

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    public override void OnCreate()
    {
        base.OnCreate();
        
        try
        {
            // Get services from the application's service provider
            _logger = MauiApplication.Current.Services.GetService<ILoggingService>();
            _settingsService = MauiApplication.Current.Services.GetService<SettingsService>();
            _orchestrator = MauiApplication.Current.Services.GetService<WorkflowOrchestrator>();
            
            _logger?.Info("ScreenCaptureService created");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize ScreenCaptureService: {ex.Message}");
        }
    }

    [return: GeneratedEnum]
    public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
    {
        if (intent == null)
            return StartCommandResult.NotSticky;

        var action = intent.Action;
        _logger?.Info($"ScreenCaptureService received action: {action}");

        switch (action)
        {
            case ActionStart:
                var resultCode = intent.GetIntExtra(ExtraResultCode, -1);
                var data = intent.GetParcelableExtra(ExtraData) as Intent;
                StartCapture(resultCode, data);
                break;

            case ActionStop:
                StopCapture();
                break;

            case ActionPickColor:
                LaunchColorPicker();
                break;
        }

        return StartCommandResult.Sticky;
    }

    private void StartCapture(int resultCode, Intent? data)
    {
        if (_isRunning)
        {
            _logger?.Warning("Screen capture already running");
            return;
        }

        try
        {
            // Start foreground service with notification
            StartForeground(Constants.ForegroundServiceNotificationId, CreateNotification());

            // Initialize media projection
            _projectionManager = (MediaProjectionManager?)GetSystemService(MediaProjectionService);
            if (_projectionManager == null || data == null)
            {
                _logger?.Error("Failed to get MediaProjectionManager or intent data");
                StopSelf();
                return;
            }

            _mediaProjection = _projectionManager.GetMediaProjection(resultCode, data);
            if (_mediaProjection == null)
            {
                _logger?.Error("Failed to get MediaProjection");
                StopSelf();
                return;
            }

            // Register callback BEFORE starting capture (required for Android 14+)
            _projectionCallback = new MediaProjectionCallback(this, _logger);
            _mediaProjection.RegisterCallback(_projectionCallback, new Handler(Looper.MainLooper));

            // Set up screen capture
            SetupScreenCapture();

            // Start periodic capture timer
            var settings = _settingsService?.LoadSettings();
            var frequency = settings?.SnapshotFrequencySeconds ?? Constants.DefaultSnapshotFrequencySeconds;
            
            _captureTimer = new Timer(
                async _ => await CaptureScreenshotAsync(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(frequency)
            );

            // Mark as running only after successful setup
            _isRunning = true;
            _logger?.Info($"Screen capture started with {frequency}s frequency");
        }
        catch (Exception ex)
        {
            _logger?.Error("Failed to start screen capture", ex);
            StopSelf();
        }
    }

    private void SetupScreenCapture()
    {
        try
        {
            var metrics = Resources?.DisplayMetrics;
            if (metrics == null)
            {
                _logger?.Error("Failed to get display metrics");
                return;
            }

            var width = metrics.WidthPixels;
            var height = metrics.HeightPixels;
            var density = (int)metrics.DensityDpi;

            _imageReader = ImageReader.NewInstance(width, height, (ImageFormatType)0x1, 2);
            
            try
            {
                _virtualDisplay = _mediaProjection?.CreateVirtualDisplay(
                    "SubzyCapture",
                    width,
                    height,
                    density,
                    DisplayFlags.None,
                    _imageReader?.Surface,
                    new VirtualDisplayCallback(_logger),
                    new Handler(Looper.MainLooper)
                );
            }
            catch (Java.Lang.IllegalStateException ex)
            {
                _logger?.Error("IllegalStateException during VirtualDisplay creation - callback may not be registered", ex);
                throw;
            }

            _logger?.Info($"Screen capture setup completed: {width}x{height} @ {density}dpi");
        }
        catch (Exception ex)
        {
            _logger?.Error("Failed to setup screen capture", ex);
            throw;
        }
    }

    bool IsProcessingScreenCapture = false;
    private async Task CaptureScreenshotAsync()
    {
        if (IsProcessingScreenCapture)
        {
            _logger?.Debug("Previous screenshot processing still ongoing, skipping this cycle");
            return;
        }
        IsProcessingScreenCapture = true;

        if (_imageReader == null)
        {
            _logger?.Warning("ImageReader not initialized");
            return;
        }

        try
        {
            var image = _imageReader.AcquireLatestImage();
            if (image == null)
            {
                _logger?.Debug("No image available");
                return;
            }

            using (image)
            {
                var planes = image.GetPlanes();
                if (planes == null || planes.Length == 0)
                {
                    _logger?.Warning("No image planes available");
                    return;
                }

                var buffer = planes[0].Buffer;
                if (buffer == null)
                {
                    _logger?.Warning("No buffer in image plane");
                    return;
                }

                var bytes = new byte[buffer.Remaining()];
                buffer.Get(bytes);

                // Convert to bitmap and then to byte array
                var bitmap = Bitmap.CreateBitmap(image.Width, image.Height, Bitmap.Config.Argb8888!);
                buffer.Rewind();
                bitmap.CopyPixelsFromBuffer(buffer);

                using var stream = new MemoryStream();
                await bitmap.CompressAsync(Bitmap.CompressFormat.Png!, 100, stream);
                var imageBytes = stream.ToArray();

                // Process through workflow
                if (_orchestrator != null)
                {
                    await _orchestrator.ProcessScreenshotAsync(imageBytes);
                }

                bitmap.Recycle();
            }
        }
        catch (Exception ex)
        {
            _logger?.Error("Failed to capture screenshot", ex);
        }
        finally
        {
            IsProcessingScreenCapture = false;
        }
    }

    private void LaunchColorPicker()
    {
        try
        {
            _logger?.Info("Launching color picker");

            // Capture current screenshot
            if (_imageReader == null)
            {
                _logger?.Warning("Cannot launch color picker: ImageReader not initialized");
                return;
            }

            var image = _imageReader.AcquireLatestImage();
            if (image == null)
            {
                _logger?.Warning("Cannot launch color picker: No image available");
                return;
            }

            byte[] screenshotBytes;
            using (image)
            {
                var planes = image.GetPlanes();
                if (planes == null || planes.Length == 0)
                {
                    _logger?.Warning("No image planes available for color picker");
                    return;
                }

                var buffer = planes[0].Buffer;
                if (buffer == null)
                {
                    _logger?.Warning("No buffer in image plane for color picker");
                    return;
                }

                var bitmap = Bitmap.CreateBitmap(image.Width, image.Height, Bitmap.Config.Argb8888!);
                buffer.Rewind();
                bitmap.CopyPixelsFromBuffer(buffer);

                using var stream = new MemoryStream();
                bitmap.Compress(Bitmap.CompressFormat.Png!, 100, stream);
                screenshotBytes = stream.ToArray();
                bitmap.Recycle();
            }

            // Launch ColorPickerActivity with screenshot
            var intent = new Intent(this, typeof(ColorPickerActivity));
            intent.SetAction(ColorPickerActivity.ActionPickColor);
            intent.PutExtra(ColorPickerActivity.ExtraScreenshotData, screenshotBytes);
            intent.SetFlags(ActivityFlags.NewTask);
            
            StartActivity(intent);
        }
        catch (Exception ex)
        {
            _logger?.Error("Failed to launch color picker", ex);
        }
    }

    private void StopCapture()
    {
        // Defensive early-return if not running
        if (!_isRunning)
        {
            _logger?.Debug("StopCapture called but service is not running");
            return;
        }

        try
        {
            _isRunning = false;
            
            // Dispose and null out timer
            _captureTimer?.Dispose();
            _captureTimer = null;

            // Release virtual display
            _virtualDisplay?.Release();
            _virtualDisplay = null;

            // Close image reader
            _imageReader?.Close();
            _imageReader = null;

            // Unregister callback before stopping projection
            if (_mediaProjection != null && _projectionCallback != null)
            {
                try
                {
                    _mediaProjection.UnregisterCallback(_projectionCallback);
                }
                catch (Exception ex)
                {
                    _logger?.Warning("Failed to unregister MediaProjection callback", ex);
                }
                _projectionCallback = null;
            }

            // Stop and dispose projection
            _mediaProjection?.Stop();
            _mediaProjection = null;

            // Stop foreground service
            StopForeground(true);
            StopSelf();

            _logger?.Info("Screen capture stopped");
        }
        catch (Exception ex)
        {
            _logger?.Error("Error stopping screen capture", ex);
        }
    }

    private Notification CreateNotification()
    {
        var channelId = Constants.ForegroundServiceChannelId;
        
        // Create notification channel for Android O and above
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(
                channelId,
                Constants.ForegroundServiceChannelName,
                NotificationImportance.Low
            )
            {
                Description = "Shows when Subzy is actively reading subtitles"
            };

            var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
            notificationManager?.CreateNotificationChannel(channel);
        }

        var notificationIntent = new Intent(this, typeof(MainActivity));
        var pendingIntent = PendingIntent.GetActivity(
            this,
            0,
            notificationIntent,
            PendingIntentFlags.Immutable
        );

        // Create "Pick Color" action
        var pickColorIntent = new Intent(this, typeof(ScreenCaptureService));
        pickColorIntent.SetAction(ActionPickColor);
        var pickColorPendingIntent = PendingIntent.GetService(
            this,
            1,
            pickColorIntent,
            PendingIntentFlags.Immutable
        );

        var notification = new NotificationCompat.Builder(this, channelId)
            .SetContentTitle("Subzy Active")
            .SetContentText("Reading subtitles in the background")
            .SetSmallIcon(global::Android.Resource.Drawable.IcMenuCamera)
            .SetContentIntent(pendingIntent)
            .AddAction(
                global::Android.Resource.Drawable.IcMenuEdit,
                "Pick Color",
                pickColorPendingIntent)
            .SetOngoing(true)
            .Build();

        return notification!;
    }

    public override void OnDestroy()
    {
        StopCapture();
        base.OnDestroy();
    }

    /// <summary>
    /// Callback for MediaProjection lifecycle events.
    /// </summary>
    private class MediaProjectionCallback : MediaProjection.Callback
    {
        private readonly ScreenCaptureService _service;
        private readonly ILoggingService? _logger;

        public MediaProjectionCallback(ScreenCaptureService service, ILoggingService? logger)
        {
            _service = service;
            _logger = logger;
        }

        public override void OnStop()
        {
            _logger?.Info("MediaProjection stopped by system");
            _service.StopCapture();
        }
    }

    /// <summary>
    /// Callback for VirtualDisplay lifecycle events (for debugging and telemetry).
    /// </summary>
    private class VirtualDisplayCallback : VirtualDisplay.Callback
    {
        private readonly ILoggingService? _logger;

        public VirtualDisplayCallback(ILoggingService? logger)
        {
            _logger = logger;
        }

        public override void OnPaused()
        {
            _logger?.Info("VirtualDisplay paused");
        }

        public override void OnResumed()
        {
            _logger?.Info("VirtualDisplay resumed");
        }

        public override void OnStopped()
        {
            _logger?.Info("VirtualDisplay stopped");
        }
    }
}
