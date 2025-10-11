using SkiaSharp;
using Subzy.Services.Interfaces;

namespace Subzy.Services;

/// <summary>
/// Service for interactive color picking from screen.
/// Provides histogram analysis to find dominant colors.
/// </summary>
public class ColorPickerService
{
    private readonly ILoggingService _logger;
    private readonly ColorProfileManager _colorProfileManager;
    private readonly ForegroundAppDetector _appDetector;

    public ColorPickerService(
        ILoggingService logger,
        ColorProfileManager colorProfileManager,
        ForegroundAppDetector appDetector)
    {
        _logger = logger;
        _colorProfileManager = colorProfileManager;
        _appDetector = appDetector;
    }

    /// <summary>
    /// Analyzes a region around tap coordinates to extract the dominant color.
    /// Uses histogram analysis and color quantization.
    /// </summary>
    /// <param name="screenshotBytes">Screenshot data</param>
    /// <param name="tapX">Tap X coordinate</param>
    /// <param name="tapY">Tap Y coordinate</param>
    /// <param name="regionSize">Size of region to analyze (default 100x100)</param>
    /// <returns>Dominant color or null if analysis fails</returns>
    public SKColor? ExtractDominantColor(
        byte[] screenshotBytes, 
        int tapX, 
        int tapY, 
        int regionSize = 100)
    {
        try
        {
            using var inputStream = new MemoryStream(screenshotBytes);
            using var bitmap = SKBitmap.Decode(inputStream);

            if (bitmap == null)
            {
                _logger.Error("Failed to decode screenshot for color picking");
                return null;
            }

            // Calculate region bounds
            var halfSize = regionSize / 2;
            var startX = Math.Max(0, tapX - halfSize);
            var startY = Math.Max(0, tapY - halfSize);
            var endX = Math.Min(bitmap.Width, tapX + halfSize);
            var endY = Math.Min(bitmap.Height, tapY + halfSize);

            // Sample 3x3 region around tap for anti-aliasing handling
            var centerColors = new List<SKColor>();
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    var px = tapX + dx;
                    var py = tapY + dy;
                    if (px >= 0 && px < bitmap.Width && py >= 0 && py < bitmap.Height)
                    {
                        centerColors.Add(bitmap.GetPixel(px, py));
                    }
                }
            }

            // Histogram for quantized colors
            var histogram = new Dictionary<SKColor, int>();

            // Analyze region with color quantization
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    
                    // Quantize color (round to nearest 16)
                    var quantized = new SKColor(
                        (byte)((pixel.Red / 16) * 16),
                        (byte)((pixel.Green / 16) * 16),
                        (byte)((pixel.Blue / 16) * 16)
                    );

                    if (!histogram.ContainsKey(quantized))
                        histogram[quantized] = 0;
                    
                    histogram[quantized]++;
                }
            }

            // Find dominant color (must appear > 100 times)
            var dominantColor = histogram
                .Where(kvp => kvp.Value > 100)
                .OrderByDescending(kvp => kvp.Value)
                .FirstOrDefault();

            if (dominantColor.Key.Alpha == 0)
            {
                _logger.Debug("No dominant color found with sufficient frequency");
                return null;
            }

            _logger.Info($"Dominant color: #{dominantColor.Key.Red:X2}{dominantColor.Key.Green:X2}{dominantColor.Key.Blue:X2} (count: {dominantColor.Value})");
            return dominantColor.Key;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to extract dominant color", ex);
            return null;
        }
    }

    /// <summary>
    /// Adds a picked color to the foreground app's profile.
    /// </summary>
    /// <param name="color">Color to add</param>
    /// <returns>Success message or null if failed</returns>
    public string? AddColorToForegroundApp(SKColor color)
    {
        try
        {
            var packageName = _appDetector.GetForegroundAppPackageName();
            if (packageName == null)
            {
                _logger.Warning("Could not detect foreground app");
                return null;
            }

            var displayName = _appDetector.GetAppDisplayName(packageName);
            _colorProfileManager.AddColorToApp(packageName, displayName, color);

            var colorName = GetColorName(color);
            var message = $"{colorName} added to {displayName}";
            _logger.Info(message);
            
            return message;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to add color to app profile", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets a user-friendly color name.
    /// </summary>
    private static string GetColorName(SKColor color)
    {
        // Simple color naming based on RGB values
        var r = color.Red;
        var g = color.Green;
        var b = color.Blue;

        if (r > 200 && g > 200 && b > 200)
            return "White";
        else if (r < 50 && g < 50 && b < 50)
            return "Black";
        else if (r > 200 && g > 200 && b < 100)
            return "Yellow";
        else if (r > 200 && g < 100 && b < 100)
            return "Red";
        else if (r < 100 && g > 200 && b < 100)
            return "Green";
        else if (r < 100 && g < 100 && b > 200)
            return "Blue";
        else if (r < 150 && g > 150 && b > 150)
            return "Cyan";
        else if (r > 150 && g < 150 && b > 150)
            return "Magenta";
        else
            return $"#{r:X2}{g:X2}{b:X2}";
    }
}
