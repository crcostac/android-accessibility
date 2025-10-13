using Subzy.Services.Interfaces;
using SkiaSharp;
using Android.Graphics;
using System.Runtime.CompilerServices;

namespace Subzy.Services;

public class ImageProcessorService : IImageProcessor
{
    private readonly ILoggingService _logger;

    public ImageProcessorService(ILoggingService logger)
    {
        _logger = logger;
    }

    public Bitmap FilterAndCleanSubtitlePixels(
        Bitmap bitmap,
        List<SKColor> subtitleColors,
        int colorTolerance = 30,
        int minNeighbors = 2)
    {
        if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));
        try
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            int length = width * height;

            // Extract pixels
            var source = new int[length];
            bitmap.GetPixels(source, 0, width, 0, 0, width, height);

            // Output buffer (avoid in-place until after noise removal)
            var output = new int[length];

            // Pre-extract target colors (RGB) for faster loop
            var targetColors = new (byte R, byte G, byte B)[subtitleColors.Count];
            for (int i = 0; i < subtitleColors.Count; i++)
            {
                var c = subtitleColors[i];
                targetColors[i] = (c.Red, c.Green, c.Blue);
            }

            // First pass: color mask
            var mask = new bool[length];

            for (int i = 0; i < length; i++)
            {
                int argb = source[i];
                // Android int pixel: AARRGGBB
                byte r = (byte)((argb >> 16) & 0xFF);
                byte g = (byte)((argb >> 8) & 0xFF);
                byte b = (byte)(argb & 0xFF);

                if (MatchesAny(targetColors, r, g, b, colorTolerance))
                {
                    mask[i] = true;
                    output[i] = argb; // keep original color
                }
                else
                {
                    output[i] = unchecked((int)0xFF000000); // opaque black
                }
            }

            if (minNeighbors > 0)
            {
                // Second pass: prune isolated pixels
                var pruned = new int[length];
                Array.Copy(output, pruned, length);

                for (int y = 0; y < height; y++)
                {
                    int rowOffset = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        int idx = rowOffset + x;
                        if (!mask[idx]) continue;

                        int neighbors = CountSameMaskNeighbors(mask, width, height, x, y);
                        if (neighbors < minNeighbors)
                        {
                            pruned[idx] = unchecked((int)0xFF000000);
                            mask[idx] = false;
                        }
                    }
                }

                output = pruned;
            }

            _logger.Debug($"Filtered (fast) bitmap: {width}x{height}, colors={subtitleColors.Count}, tol={colorTolerance}, minNeighbors={minNeighbors}");

            // Write the pixels back into a new Bitmap
            Bitmap outputBitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
            outputBitmap.SetPixels(output, 0, width, 0, 0, width, height);
            return outputBitmap;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to filter subtitle pixels (Android Bitmap), returning original image", ex);
            return bitmap;
        }
    }

    private static bool MatchesAny(List<SKColor> subtitleColors, SKColor pixel, int tolerance)
    {
        foreach (var c in subtitleColors)
            if (IsColorMatch(pixel, c, tolerance))
                return true;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MatchesAny((byte R, byte G, byte B)[] targets, byte r, byte g, byte b, int tolerance)
    {
        foreach (var (R, G, B) in targets)
        {
            int dr = Math.Abs(r - R);
            int dg = Math.Abs(g - G);
            int db = Math.Abs(b - B);
            if ((dr + dg + db) <= tolerance) return true;
        }
        return false;
    }

    private static bool IsColorMatch(SKColor c1, SKColor c2, int tolerance)
    {
        var dr = Math.Abs(c1.Red - c2.Red);
        var dg = Math.Abs(c1.Green - c2.Green);
        var db = Math.Abs(c1.Blue - c2.Blue);
        return (dr + dg + db) <= tolerance;
    }

    private static int CountSameMaskNeighbors(bool[] mask, int width, int height, int x, int y)
    {
        int count = 0;
        for (int dy = -1; dy <= 1; dy++)
        {
            int ny = y + dy;
            if (ny < 0 || ny >= height) continue;
            int rowOffset = ny * width;
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx;
                if (nx < 0 || nx >= width) continue;
                if (dx == 0 && dy == 0) continue;
                if (mask[rowOffset + nx]) count++;
            }
        }
        return count;
    }
}