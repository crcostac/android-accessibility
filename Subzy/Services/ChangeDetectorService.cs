using Subzy.Services.Interfaces;
using System.Security.Cryptography;

namespace Subzy.Services;

/// <summary>
/// Service for detecting changes between consecutive frames to avoid redundant processing.
/// </summary>
public class ChangeDetectorService
{
    private readonly ILoggingService _logger;
    private byte[]? _previousImageHash;
    private const double SimilarityThreshold = 0.95; // 95% similar = no change

    public ChangeDetectorService(ILoggingService logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detects if the current image has changed significantly from the previous one.
    /// </summary>
    /// <param name="imageBytes">Current image data</param>
    /// <returns>True if image has changed, false if similar to previous</returns>
    public bool HasChanged(byte[] imageBytes)
    {
        try
        {
            var currentHash = ComputeHash(imageBytes);
            
            if (_previousImageHash == null)
            {
                _previousImageHash = currentHash;
                _logger.Debug("First frame captured, assuming change");
                return true;
            }

            var similarity = CalculateSimilarity(_previousImageHash, currentHash);
            var hasChanged = similarity < SimilarityThreshold;

            if (hasChanged)
            {
                _previousImageHash = currentHash;
                _logger.Debug($"Frame changed detected (similarity: {similarity:P})");
            }
            else
            {
                _logger.Debug($"No significant change (similarity: {similarity:P})");
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
        _previousImageHash = null;
        _logger.Debug("Change detector reset");
    }

    private byte[] ComputeHash(byte[] imageBytes)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(imageBytes);
    }

    private double CalculateSimilarity(byte[] hash1, byte[] hash2)
    {
        if (hash1.Length != hash2.Length)
            return 0.0;

        int matchingBytes = 0;
        for (int i = 0; i < hash1.Length; i++)
        {
            if (hash1[i] == hash2[i])
                matchingBytes++;
        }

        return (double)matchingBytes / hash1.Length;
    }
}
