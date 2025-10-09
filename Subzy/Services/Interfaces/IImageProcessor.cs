namespace Subzy.Services.Interfaces;

/// <summary>
/// Interface for image processing operations.
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// Crops an image to the specified region of interest.
    /// </summary>
    /// <param name="imageBytes">Original image data</param>
    /// <param name="x">X coordinate of the top-left corner</param>
    /// <param name="y">Y coordinate of the top-left corner</param>
    /// <param name="width">Width of the region</param>
    /// <param name="height">Height of the region</param>
    /// <returns>Cropped image data</returns>
    Task<byte[]> CropImageAsync(byte[] imageBytes, int x, int y, int width, int height);

    /// <summary>
    /// Enhances image contrast for better OCR results.
    /// </summary>
    /// <param name="imageBytes">Image data</param>
    /// <param name="contrastFactor">Contrast adjustment factor (1.0 = no change)</param>
    /// <returns>Enhanced image data</returns>
    Task<byte[]> AdjustContrastAsync(byte[] imageBytes, float contrastFactor);

    /// <summary>
    /// Adjusts image brightness for better OCR results.
    /// </summary>
    /// <param name="imageBytes">Image data</param>
    /// <param name="brightnessFactor">Brightness adjustment factor (1.0 = no change)</param>
    /// <returns>Enhanced image data</returns>
    Task<byte[]> AdjustBrightnessAsync(byte[] imageBytes, float brightnessFactor);

    /// <summary>
    /// Applies all configured enhancements to an image.
    /// </summary>
    /// <param name="imageBytes">Original image data</param>
    /// <param name="brightness">Brightness factor</param>
    /// <param name="contrast">Contrast factor</param>
    /// <returns>Enhanced image data</returns>
    Task<byte[]> EnhanceImageAsync(byte[] imageBytes, float brightness = 1.0f, float contrast = 1.0f);
}
