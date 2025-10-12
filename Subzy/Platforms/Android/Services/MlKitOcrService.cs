using Android.Graphics;
using Android.Runtime;
using Subzy.Services.Interfaces;
using Subzy.Platforms.Android.Helpers;
using Xamarin.Google.MLKit.Vision.Common;
using Xamarin.Google.MLKit.Vision.Text;
using Xamarin.Google.MLKit.Vision.Text.Latin;
using System.Text;

namespace Subzy.Platforms.Android.Services;

/// <summary>
/// OCR service implementation using Google ML Kit Text Recognition v2.
/// Provides fast, on-device text recognition with automatic language detection.
/// </summary>
public class MlKitOcrService : IOcrService
{
    private readonly ILoggingService _logger;
    private ITextRecognizer? _textRecognizer;
    private bool _isInitialized;

    /// <summary>
    /// Gets the last detected predominant language code from OCR results.
    /// </summary>
    public string? LastDetectedLanguage { get; private set; }

    public bool IsInitialized => _isInitialized;

    public MlKitOcrService(ILoggingService logger)
    {
        _logger = logger;
    }

    public Task InitializeAsync()
    {
        if (_isInitialized)
            return Task.CompletedTask;

        try
        {
            // Initialize ML Kit Text Recognizer (Latin script)
            _textRecognizer = TextRecognition.GetClient(TextRecognizerOptions.DefaultOptions);
            _isInitialized = true;
            _logger.Info("ML Kit Text Recognizer initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to initialize ML Kit Text Recognizer", ex);
            _isInitialized = false;
        }

        return Task.CompletedTask;
    }

    public async Task<string> ExtractTextAsync(byte[] imageBytes, string language = "eng")
    {
        if (!_isInitialized)
        {
            _logger.Warning("OCR service not initialized, attempting initialization");
            await InitializeAsync();

            if (!_isInitialized)
            {
                return "[OCR not available - ML Kit Text Recognizer initialization failed]";
            }
        }

        try
        {
            // Convert byte[] to Bitmap
            var bitmap = await BitmapFactory.DecodeByteArrayAsync(imageBytes, 0, imageBytes.Length);
            if (bitmap == null)
            {
                _logger.Error("Failed to decode image bytes to Bitmap");
                return string.Empty;
            }

            // Convert Bitmap to InputImage
            var inputImage = InputImage.FromBitmap(bitmap, 0);

            // Process image with ML Kit (convert Task to Task<Text>)
            var result = await _textRecognizer!.Process(inputImage).AsAsync<Text>();

            // Dispose bitmap after use
            bitmap.Dispose();

            if (result == null)
            {
                _logger.Warning("ML Kit returned null result");
                return string.Empty;
            }

            // Infer predominant language from blocks
            LastDetectedLanguage = InferPredominantLanguage(result);

            // Aggregate text from all recognized lines, preserving diacritics
            var textBuilder = new StringBuilder();
            var blocks = result.TextBlocks;
            
            if (blocks != null && blocks.Count > 0)
            {
                foreach (var block in blocks)
                {
                    if (block?.Lines != null)
                    {
                        foreach (var line in block.Lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line?.Text))
                            {
                                textBuilder.AppendLine(line.Text);
                            }
                        }
                    }
                }
            }

            var extractedText = textBuilder.ToString().Trim();
            _logger.Debug($"ML Kit extracted text: {extractedText.Length} characters, detected language: {LastDetectedLanguage ?? "unknown"}");
            
            return extractedText;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to extract text from image using ML Kit", ex);
            return string.Empty;
        }
    }

    /// <summary>
    /// Infers the predominant language from recognized text blocks by counting language codes.
    /// </summary>
    private string? InferPredominantLanguage(Text result)
    {
        try
        {
            var languageCounts = new Dictionary<string, int>();

            var blocks = result.TextBlocks;
            if (blocks != null && blocks.Count > 0)
            {
                foreach (var block in blocks)
                {
                    var recognizedLanguages = block?.RecognizedLanguages;
                    if (recognizedLanguages != null && recognizedLanguages.Count > 0)
                    {
                        foreach (var recognizedLang in recognizedLanguages)
                        {
                            var langCode = recognizedLang?.LanguageCode;
                            if (!string.IsNullOrEmpty(langCode))
                            {
                                if (!languageCounts.ContainsKey(langCode))
                                {
                                    languageCounts[langCode] = 0;
                                }
                                languageCounts[langCode]++;
                            }
                        }
                    }
                }
            }

            // Return the most frequent language code
            if (languageCounts.Count > 0)
            {
                var predominant = languageCounts.OrderByDescending(kvp => kvp.Value).First();
                return predominant.Key;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to infer predominant language: {ex.Message}");
            return null;
        }
    }
}
