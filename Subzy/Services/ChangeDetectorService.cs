using Subzy.Services.Interfaces;
using SkiaSharp;
using Android.Graphics;

namespace Subzy.Services;

/// <summary>
/// Service for detecting changes between consecutive frames using perceptual hashing.
/// Uses dHash algorithm for fast and accurate change detection.
/// </summary>
public class ChangeDetectorService
{
    private readonly ILoggingService _logger;
    private ulong? _previousPerceptualHash;
    private const int HammingDistanceThreshold = 8; // Distance >= 8 means change detected

    public ChangeDetectorService(ILoggingService logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detects if the current image has changed significantly from the previous one.
    /// Uses perceptual hashing (dHash) for fast and accurate change detection.
    /// </summary>
    /// <param name="bitmap">Current image data</param>
    /// <returns>True if image has changed, false if similar to previous</returns>
    public bool HasChanged(Bitmap bitmap)
    {
        try
        {
            var currentHash = ComputePerceptualHash(bitmap);
            
            if (_previousPerceptualHash == null)
            {
                _previousPerceptualHash = currentHash;
                _logger.Debug("First frame captured, assuming change");
                return true;
            }

            var hammingDistance = HammingDistance(_previousPerceptualHash.Value, currentHash);
            var hasChanged = hammingDistance >= HammingDistanceThreshold;

            if (hasChanged)
            {
                _previousPerceptualHash = currentHash;
                _logger.Debug($"Frame changed detected (Hamming distance: {hammingDistance})");
            }
            else
            {
                _logger.Debug($"No significant change (Hamming distance: {hammingDistance})");
            }

            return hasChanged;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to detect changes", ex);
            return true; // Assume change on error to avoid missing content
        }
    }

    /// <summary>
    /// Resets the change detector state.
    /// </summary>
    public void Reset()
    {
        _previousPerceptualHash = null;
        _logger.Debug("Change detector reset");
    }

    /// <summary>
    /// Computes perceptual hash using dHash algorithm.
    /// Resizes image to 9x8, compares horizontal adjacent pixels.
    /// </summary>
    private ulong ComputePerceptualHash(Bitmap bitmap)
    {
        var skBitmap = new SKBitmap(bitmap.Width, bitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

        // Lock Android Bitmap pixels
        var intPtr = bitmap.LockPixels();
        try
        {
            // Install pixels directly into SKBitmap
            skBitmap.InstallPixels(
                new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul),
                intPtr,
                bitmap.RowBytes,
                null, null);
        }
        finally
        {
            bitmap.UnlockPixels();
        }

        // Resize to 9x8 pixels using low quality filter (fast)
        using var resizedBitmap = skBitmap.Resize(new SKImageInfo(9, 8), SKFilterQuality.Low);
        
        if (resizedBitmap == null)
            return 0;

        ulong hash = 0;
        int bitIndex = 0;

        // Compare each pixel with its right neighbor (horizontal gradients)
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                var leftPixel = resizedBitmap.GetPixel(x, y);
                var rightPixel = resizedBitmap.GetPixel(x + 1, y);

                // Calculate brightness: 0.299*R + 0.587*G + 0.114*B
                var leftBrightness = 0.299 * leftPixel.Red + 0.587 * leftPixel.Green + 0.114 * leftPixel.Blue;
                var rightBrightness = 0.299 * rightPixel.Red + 0.587 * rightPixel.Green + 0.114 * rightPixel.Blue;

                // Set bit if left pixel is brighter than right
                if (leftBrightness > rightBrightness)
                {
                    hash |= (1UL << bitIndex);
                }

                bitIndex++;
            }
        }

        return hash;
    }

    /// <summary>
    /// Calculates Hamming distance between two hashes.
    /// Returns the number of differing bits.
    /// </summary>
    private int HammingDistance(ulong hash1, ulong hash2)
    {
        var xor = hash1 ^ hash2;
        int distance = 0;

        // Count the number of 1 bits in the XOR result
        while (xor != 0)
        {
            distance++;
            xor &= xor - 1; // Clear the least significant bit
        }

        return distance;
    }
}
