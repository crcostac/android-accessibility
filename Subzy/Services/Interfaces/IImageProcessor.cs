using Android.Graphics;

namespace Subzy.Services.Interfaces;

/// <summary>
/// Interface for image processing operations.
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// Filters and cleans subtitle pixels using color profile and noise removal.
    /// Combines color filtering and neighbor-based noise removal in a single pass.
    /// </summary>
    /// <param name="bitmap">Original image data</param>
    /// <param name="subtitleColors">List of subtitle colors to detect</param>
    /// <param name="colorTolerance">RGB distance tolerance for color matching (default 30)</param>
    /// <param name="minNeighbors">Minimum same-color neighbors required to keep pixel (default 2)</param>
    /// <returns>Filtered image with only subtitle pixels</returns>
    Bitmap FilterAndCleanSubtitlePixels(
        Bitmap bitmap, 
        List<SkiaSharp.SKColor> subtitleColors,
        int colorTolerance = 30,
        int minNeighbors = 2);
}
