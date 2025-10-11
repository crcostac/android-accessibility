using SkiaSharp;

namespace Subzy.Models;

/// <summary>
/// Represents a color profile for subtitle detection in a specific app.
/// </summary>
public class SubtitleColorProfile
{
    /// <summary>
    /// Android package name of the app (e.g., "com.netflix.mediaclient")
    /// </summary>
    public string AppPackageName { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly display name (e.g., "Netflix")
    /// </summary>
    public string AppDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// List of subtitle colors in MRU (Most Recently Used) order.
    /// New colors are inserted at position 0, list is trimmed to max 5 colors.
    /// </summary>
    public List<SKColor> SubtitleColors { get; set; } = new();

    /// <summary>
    /// Color matching tolerance (RGB distance). Default is 30.
    /// </summary>
    public int ColorTolerance { get; set; } = 30;

    /// <summary>
    /// Timestamp of when this profile was last used
    /// </summary>
    public DateTime LastUsed { get; set; } = DateTime.Now;

    /// <summary>
    /// Timestamp of when this profile was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Helper property for JSON serialization - stores colors as hex strings
    /// </summary>
    public List<string> ColorHexStrings
    {
        get => SubtitleColors.Select(c => $"#{c.Red:X2}{c.Green:X2}{c.Blue:X2}").ToList();
        set => SubtitleColors = value.Select(ParseColorFromHex).ToList();
    }

    private static SKColor ParseColorFromHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            var r = Convert.ToByte(hex.Substring(0, 2), 16);
            var g = Convert.ToByte(hex.Substring(2, 2), 16);
            var b = Convert.ToByte(hex.Substring(4, 2), 16);
            return new SKColor(r, g, b);
        }
        return SKColors.White;
    }
}
