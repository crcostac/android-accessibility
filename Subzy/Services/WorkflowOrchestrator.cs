using Android.Graphics;
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
    private readonly ForegroundAppDetector _appDetector;
    private readonly ColorProfileManager _colorProfileManager;

    public WorkflowOrchestrator(
        ILoggingService logger,
        SettingsService settingsService,
        IImageProcessor imageProcessor,
        ChangeDetectorService changeDetector,
        IOcrService ocrService,
        ITranslationService translationService,
        ITtsService ttsService,
        ForegroundAppDetector appDetector,
        ColorProfileManager colorProfileManager)
    {
        _logger = logger;
        _settingsService = settingsService;
        _imageProcessor = imageProcessor;
        _changeDetector = changeDetector;
        _ocrService = ocrService;
        _translationService = translationService;
        _ttsService = ttsService;
        _appDetector = appDetector;
        _colorProfileManager = colorProfileManager;
    }

    /// <summary>
    /// Processes a captured screenshot through the complete 5-stage pipeline:
    /// Stage 1: Detect Foreground App
    /// Stage 2: Color Filter + Noise Removal
    /// Stage 3: Perceptual Hashing
    /// Stage 4: Run OCR (if changed)
    /// Stage 5: Translation & TTS
    /// </summary>
    /// <param name="screenshotBytes">Raw screenshot data</param>
    /// <returns>Processing result with timing information</returns>
    public async Task<ProcessingResult> ProcessScreenshotAsync(Bitmap bitmapScreenshot)
    {
        var overallStopwatch = Stopwatch.StartNew();
        var result = new ProcessingResult { Success = true };

        try
        {
            var settings = _settingsService.LoadSettings();
            var stopwatch = Stopwatch.StartNew();

            // Stage 1: Detect Foreground App (~1ms)
            stopwatch.Restart();
            var appPackageName = _appDetector.GetForegroundAppPackageName();
            var appDisplayName = appPackageName != null 
                ? _appDetector.GetAppDisplayName(appPackageName) 
                : "Unknown";
            
            var colorProfile = _colorProfileManager.GetActiveProfile(
                appPackageName ?? "default", 
                appDisplayName);
            
            _logger.Debug($"Foreground app: {appDisplayName} ({stopwatch.ElapsedMilliseconds}ms)");

            // Stage 2: Color Filter + Noise Removal (~20-25ms)
            stopwatch.Restart();
            var bitmapFiltered = _imageProcessor.FilterAndCleanSubtitlePixels(
                bitmapScreenshot,
                colorProfile.SubtitleColors,
                settings.SubtitleColorTolerance,
                settings.MinSameColorNeighbors
            );
            result.ProcessingTime = stopwatch.Elapsed;
            _logger.Debug($"Color filtering completed in {stopwatch.ElapsedMilliseconds}ms");

            LoggingService.SaveBitmapToPNG(bitmapFiltered, "filtered");

            // Stage 3: Perceptual Hashing (~10ms)
            stopwatch.Restart();
            var hasChanged = settings.UsePerceptualHashing 
                ? _changeDetector.HasChanged(bitmapFiltered)
                : true; // Always run OCR if perceptual hashing disabled
            
            result.ContentChanged = hasChanged;

            if (!hasChanged)
            {
                _logger.Debug("No content change detected, skipping OCR");
                result.ProcessingDuration = overallStopwatch.Elapsed;
                _logger.Info($"Total pipeline time (no OCR): {result.ProcessingDuration.TotalMilliseconds}ms");
                return result;
            }

            // Stage 4: Run OCR (~200-500ms)
            stopwatch.Restart();
            var extractedText = await _ocrService.ExtractTextAsync(bitmapFiltered);
            result.OcrTime = stopwatch.Elapsed;
            _logger.Debug($"OCR completed in {stopwatch.ElapsedMilliseconds}ms, extracted text: {extractedText}");

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.Debug("No text extracted from image");
                result.ProcessingDuration = overallStopwatch.Elapsed;
                _logger.Info($"Total pipeline time (no text): {result.ProcessingDuration.TotalMilliseconds}ms");
                return result;
            }

            // Create subtitle data
            var subtitleData = new SubtitleData
            {
                OriginalText = extractedText,
                Timestamp = DateTime.Now
            };

            // Stage 5: Translation & TTS
            // Translate if enabled
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

            // Speak text if TTS enabled
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

    /// <summary>
    /// Resets the workflow state (e.g., change detector).
    /// </summary>
    public void Reset()
    {
        _changeDetector.Reset();
        _logger.Info("Workflow orchestrator reset");
    }
}
