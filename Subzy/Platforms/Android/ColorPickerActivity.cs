using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using Subzy.Services;

namespace Subzy.Platforms.Android;

/// <summary>
/// Semi-transparent overlay activity for color picking.
/// Displays crosshair and instructions, handles tap to pick color.
/// </summary>
[Activity(
    Label = "Pick Subtitle Color",
    Theme = "@android:style/Theme.Translucent.NoTitleBar",
    LaunchMode = global::Android.Content.PM.LaunchMode.SingleTop)]
public class ColorPickerActivity : Activity
{
    public const string ActionPickColor = "com.accessibility.subzy.PICK_COLOR";
    public const string ExtraScreenshotData = "screenshot_data";

    private byte[]? _screenshotBytes;
    private ColorPickerService? _colorPickerService;
    private Subzy.Services.Interfaces.ILoggingService? _logger;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        try
        {
            // Get services
            _logger = MauiApplication.Current.Services.GetService<Subzy.Services.Interfaces.ILoggingService>();
            _colorPickerService = MauiApplication.Current.Services.GetService<ColorPickerService>();

            // Get screenshot data from intent
            if (Intent?.HasExtra(ExtraScreenshotData) == true)
            {
                _screenshotBytes = Intent.GetByteArrayExtra(ExtraScreenshotData);
            }

            if (_screenshotBytes == null || _colorPickerService == null)
            {
                _logger?.Error("ColorPickerActivity: Missing screenshot data or service");
                Finish();
                return;
            }

            // Set up overlay UI
            SetupOverlay();

            _logger?.Info("ColorPickerActivity started");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ColorPickerActivity OnCreate error: {ex.Message}");
            Finish();
        }
    }

    private void SetupOverlay()
    {
        // Create semi-transparent overlay
        var layout = new RelativeLayout(this)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };
        layout.SetBackgroundColor(global::Android.Graphics.Color.Argb(128, 0, 0, 0)); // 50% transparent black

        // Add instructions text
        var instructions = new TextView(this)
        {
            Text = "Tap on a subtitle letter to pick its color",
            TextSize = 20,
            Gravity = GravityFlags.Center
        };
        instructions.SetTextColor(global::Android.Graphics.Color.White);
        
        var instructionsParams = new RelativeLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);
        instructionsParams.AddRule(LayoutRules.CenterHorizontal);
        instructionsParams.TopMargin = 100;
        instructions.LayoutParameters = instructionsParams;

        layout.AddView(instructions);

        // Add cancel button
        var cancelButton = new global::Android.Widget.Button(this)
        {
            Text = "Cancel"
        };
        var cancelParams = new RelativeLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent);
        cancelParams.AddRule(LayoutRules.CenterHorizontal);
        cancelParams.AddRule(LayoutRules.AlignParentBottom);
        cancelParams.BottomMargin = 100;
        cancelButton.LayoutParameters = cancelParams;
        cancelButton.Click += (s, e) => Finish();

        layout.AddView(cancelButton);

        // Handle tap
        layout.Touch += OnOverlayTouch;

        SetContentView(layout);
    }

    private void OnOverlayTouch(object? sender, global::Android.Views.View.TouchEventArgs e)
    {
        if (e.Event?.Action == MotionEventActions.Down)
        {
            var x = (int)e.Event.GetX();
            var y = (int)e.Event.GetY();

            _logger?.Debug($"Color pick tap at ({x}, {y})");

            if (_screenshotBytes != null && _colorPickerService != null)
            {
                // Extract dominant color
                var color = _colorPickerService.ExtractDominantColor(_screenshotBytes, x, y);

                if (color.HasValue)
                {
                    // Add to foreground app profile
                    var message = _colorPickerService.AddColorToForegroundApp(color.Value);

                    if (message != null)
                    {
                        // Show toast
                        Toast.MakeText(this, message, ToastLength.Short)?.Show();
                    }
                    else
                    {
                        Toast.MakeText(this, "Failed to add color", ToastLength.Short)?.Show();
                    }
                }
                else
                {
                    Toast.MakeText(this, "Could not detect color", ToastLength.Short)?.Show();
                }

                // Close overlay after picking
                Finish();
            }

            e.Handled = true;
        }
    }

    protected override void OnDestroy()
    {
        _screenshotBytes = null;
        base.OnDestroy();
    }
}
