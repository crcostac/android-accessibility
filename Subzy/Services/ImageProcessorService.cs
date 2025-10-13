using Subzy.Services.Interfaces;
using SkiaSharp;

namespace Subzy.Services;

/// <summary>
/// Service for image processing operations including cropping and enhancement.
/// Uses SkiaSharp for image manipulation and OCR optimization.
/// </summary>
public class ImageProcessorService : IImageProcessor
{
    private readonly ILoggingService _logger;

    public ImageProcessorService(ILoggingService logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> CropImageAsync(byte[] imageBytes, int x, int y, int width, int height)
    {
        return await Task.Run(() =>
        {
            try
            {
                // For now, return original image as cropping requires platform-specific implementation
                // This would typically use SkiaSharp or similar library for actual cropping
                _logger.Debug($"Crop requested: x={x}, y={y}, width={width}, height={height}");
                return imageBytes;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to crop image", ex);
                return imageBytes;
            }
        });
    }

    public async Task<byte[]> AdjustContrastAsync(byte[] imageBytes, float contrastFactor)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Contrast adjustment would require image manipulation library
                // For now, return original image
                _logger.Debug($"Contrast adjustment requested: factor={contrastFactor}");
                return imageBytes;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to adjust contrast", ex);
                return imageBytes;
            }
        });
    }

    public async Task<byte[]> AdjustBrightnessAsync(byte[] imageBytes, float brightnessFactor)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Brightness adjustment would require image manipulation library
                // For now, return original image
                _logger.Debug($"Brightness adjustment requested: factor={brightnessFactor}");
                return imageBytes;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to adjust brightness", ex);
                return imageBytes;
            }
        });
    }

    public async Task<byte[]> EnhanceImageAsync(byte[] imageBytes, float brightness = 1.0f, float contrast = 1.0f)
    {
        return await Task.Run(() =>
        {
            try
            {
                var result = imageBytes;
                
                if (Math.Abs(brightness - 1.0f) > 0.01f)
                {
                    result = AdjustBrightnessAsync(result, brightness).Result;
                }
                
                if (Math.Abs(contrast - 1.0f) > 0.01f)
                {
                    result = AdjustContrastAsync(result, contrast).Result;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to enhance image", ex);
                return imageBytes;
            }
        });
    }

    public async Task<byte[]> FilterAndCleanSubtitlePixelsAsync(
        byte[] imageBytes, 
        List<SKColor> subtitleColors,
        int colorTolerance = 30,
        int minNeighbors = 2)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var inputStream = new MemoryStream(imageBytes);
                using var bitmap = SKBitmap.Decode(inputStream);
                
                if (bitmap == null)
                {
                    _logger.Error("Failed to decode image for filtering");
                    return imageBytes;
                }

                var width = bitmap.Width;
                var height = bitmap.Height;
                
                // Create output bitmap
                using var outputBitmap = new SKBitmap(width, height);
                
                // Single pass: check color match and count neighbors
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var pixel = bitmap.GetPixel(x, y);
                        
                        // Check if pixel matches any subtitle color
                        var matchesColor = false;
                        foreach (var targetColor in subtitleColors)
                        {
                            if (IsColorMatch(pixel, targetColor, colorTolerance))
                            {
                                matchesColor = true;
                                break;
                            }
                        }

                        if (matchesColor)
                        {
                            outputBitmap.SetPixel(x, y, pixel);
                            // // Count neighbors with the SAME color as current pixel
                            // var sameColorNeighbors = CountSameColorNeighbors(bitmap, x, y, pixel, colorTolerance);
                            // 
                            // if (sameColorNeighbors >= minNeighbors)
                            // {
                            //     // Keep the pixel
                            //     outputBitmap.SetPixel(x, y, pixel);
                            // }
                            // else
                            // {
                            //     // Remove noise - set to black/transparent
                            //     outputBitmap.SetPixel(x, y, SKColors.Black);
                            // }
                        }
                        else
                        {
                            // Not a subtitle color - set to black/transparent
                            outputBitmap.SetPixel(x, y, SKColors.Black);
                        }
                    }
                }

                // Encode to PNG
                using var outputStream = new MemoryStream();
                using var image = SKImage.FromBitmap(outputBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                data.SaveTo(outputStream);
                
                _logger.Debug($"Filtered image: {width}x{height}, {subtitleColors.Count} colors, tolerance={colorTolerance}");
                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to filter subtitle pixels", ex);
                return imageBytes;
            }
        });
    }

    /// <summary>
    /// Checks if two colors match within tolerance.
    /// </summary>
    private static bool IsColorMatch(SKColor c1, SKColor c2, int tolerance)
    {
        var dr = Math.Abs(c1.Red - c2.Red);
        var dg = Math.Abs(c1.Green - c2.Green);
        var db = Math.Abs(c1.Blue - c2.Blue);
        return (dr + dg + db) <= tolerance;
    }

    /// <summary>
    /// Counts 8-connected neighbors with the same color as the reference pixel.
    /// </summary>
    private static int CountSameColorNeighbors(SKBitmap bitmap, int x, int y, SKColor refColor, int tolerance)
    {
        var count = 0;
        var width = bitmap.Width;
        var height = bitmap.Height;

        // Check all 8 surrounding pixels
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue; // Skip center pixel
                
                var nx = x + dx;
                var ny = y + dy;
                
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    var neighborColor = bitmap.GetPixel(nx, ny);
                    if (IsColorMatch(neighborColor, refColor, tolerance))
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }
}
