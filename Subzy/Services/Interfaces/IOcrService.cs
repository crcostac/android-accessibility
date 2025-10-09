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
    /// <param name="imageBytes">Image data as byte array</param>
    /// <param name="language">Language code for OCR (e.g., "eng" for English)</param>
    /// <returns>Extracted text</returns>
    Task<string> ExtractTextAsync(byte[] imageBytes, string language = "eng");

    /// <summary>
    /// Checks if the OCR service is initialized and ready.
    /// </summary>
    bool IsInitialized { get; }
}
