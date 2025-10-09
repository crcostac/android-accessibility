using Subzy.Models;
using Subzy.Services.Interfaces;
using System.Diagnostics;

namespace Subzy.Services;

/// <summary>
/// Orchestrates the complete subtitle processing workflow:
/// Capture -> Process -> Detect Changes -> OCR -> Translate -> TTS
/// </summary>
public class WorkflowOrchestrator
{
    private readonly ILoggingService _logger;
    private readonly SettingsService _settingsService;
    private readonly IImageProcessor _imageProcessor;
    private readonly ChangeDetectorService _changeDetector;
    private readonly IOcrService _ocrService;
    private readonly ITranslationService _translationService;
    private readonly ITtsService _ttsService;

    public WorkflowOrchestrator(
        ILoggingService logger,
        SettingsService settingsService,
        IImageProcessor imageProcessor,
        ChangeDetectorService changeDetector,
        IOcrService ocrService,
        ITranslationService translationService,
        ITtsService ttsService)
    {
        _logger = logger;
        _settingsService = settingsService;
        _imageProcessor = imageProcessor;
        _changeDetector = changeDetector;
        _ocrService = ocrService;
        _translationService = translationService;
        _ttsService = ttsService;
    }

    /// <summary>
    /// Processes a captured screenshot through the complete pipeline.
    /// </summary>
    /// <param name="screenshotBytes">Raw screenshot data</param>
    /// <returns>Processing result with timing information</returns>
    public async Task<ProcessingResult> ProcessScreenshotAsync(byte[] screenshotBytes)
    {
        var overallStopwatch = Stopwatch.StartNew();
        var result = new ProcessingResult { Success = true };

        try
        {
            var settings = _settingsService.LoadSettings();
            
            // Step 1: Pre-process image (crop ROI, enhance)
            var stopwatch = Stopwatch.StartNew();
            var processedImage = await PreProcessImageAsync(screenshotBytes, settings);
            result.ProcessingTime = stopwatch.Elapsed;
            _logger.Debug($"Image processing completed in {stopwatch.ElapsedMilliseconds}ms");

            // Step 2: Detect changes
            stopwatch.Restart();
            var hasChanged = _changeDetector.HasChanged(processedImage);
            result.ContentChanged = hasChanged;

            if (!hasChanged)
            {
                _logger.Debug("No content change detected, skipping OCR");
                result.ProcessingDuration = overallStopwatch.Elapsed;
                return result;
            }

            // Step 3: Extract text via OCR
            stopwatch.Restart();
            var extractedText = await _ocrService.ExtractTextAsync(processedImage);
            result.OcrTime = stopwatch.Elapsed;
            _logger.Debug($"OCR completed in {stopwatch.ElapsedMilliseconds}ms");

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.Debug("No text extracted from image");
                result.ProcessingDuration = overallStopwatch.Elapsed;
                return result;
            }

            // Create subtitle data
            var subtitleData = new SubtitleData
            {
                OriginalText = extractedText,
                Timestamp = DateTime.Now
            };

            // Step 4: Translate if enabled
            if (settings.IsTranslationEnabled && _translationService.IsConfigured)
            {
                stopwatch.Restart();
                var (translatedText, detectedLanguage) = await _translationService.TranslateAsync(
                    extractedText,
                    settings.TargetLanguage
                );
                result.TranslationTime = stopwatch.Elapsed;
                
                subtitleData.TranslatedText = translatedText;
                subtitleData.DetectedLanguage = detectedLanguage;
                subtitleData.WasTranslated = true;
                
                _logger.Info($"Translation completed in {stopwatch.ElapsedMilliseconds}ms: {detectedLanguage} -> {settings.TargetLanguage}");
            }
            else
            {
                subtitleData.TranslatedText = extractedText;
                subtitleData.DetectedLanguage = "none";
            }

            // Step 5: Speak text if TTS enabled
            if (settings.IsTtsEnabled && _ttsService.IsConfigured)
            {
                stopwatch.Restart();
                var textToSpeak = subtitleData.WasTranslated 
                    ? subtitleData.TranslatedText 
                    : subtitleData.OriginalText;
                    
                await _ttsService.SpeakAsync(textToSpeak, settings.TtsVoice);
                result.TtsTime = stopwatch.Elapsed;
                
                subtitleData.WasSpoken = true;
                _logger.Info($"TTS completed in {stopwatch.ElapsedMilliseconds}ms");
            }

            result.SubtitleData = subtitleData;
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.Error("Workflow processing failed", ex);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            result.ProcessingDuration = overallStopwatch.Elapsed;
            _logger.Info($"Total processing time: {result.ProcessingDuration.TotalMilliseconds}ms");
        }

        return result;
    }

    private async Task<byte[]> PreProcessImageAsync(byte[] imageBytes, AppSettings settings)
    {
        var processedImage = imageBytes;

        // Crop to ROI if configured
        if (settings.RoiWidth > 0 && settings.RoiHeight > 0)
        {
            processedImage = await _imageProcessor.CropImageAsync(
                processedImage,
                settings.RoiX,
                settings.RoiY,
                settings.RoiWidth,
                settings.RoiHeight
            );
        }

        // Enhance image for better OCR
        processedImage = await _imageProcessor.EnhanceImageAsync(
            processedImage,
            settings.Brightness,
            settings.Contrast
        );

        return processedImage;
    }

    /// <summary>
    /// Resets the workflow state (e.g., change detector).
    /// </summary>
    public void Reset()
    {
        _changeDetector.Reset();
        _logger.Info("Workflow orchestrator reset");
    }
}
