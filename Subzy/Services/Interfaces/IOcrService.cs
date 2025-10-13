using Android.Graphics;

namespace Subzy.Services.Interfaces;

/// <summary>
/// Interface for OCR (Optical Character Recognition) services.
/// Allows swapping between different OCR providers.
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Initializes the OCR service with necessary resources.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Extracts text from an image.
    /// </summary>
    /// <param name="bitmap">Image data as Bitmap</param>
    /// <param name="language">Language code for OCR (e.g., "eng" for English)</param>
    /// <returns>Extracted text</returns>
    Task<string> ExtractTextAsync(Bitmap bitmap, string language = "eng");

    /// <summary>
    /// Checks if the OCR service is initialized and ready.
    /// </summary>
    bool IsInitialized { get; }
}
