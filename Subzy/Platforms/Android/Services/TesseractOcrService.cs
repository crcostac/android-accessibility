using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using Subzy.Services.Interfaces;
using Tesseract.Droid;

namespace Subzy.Platforms.Android.Services;

/// <summary>
/// OCR service implementation using Xamarin.Tesseract.
/// </summary>
public class TesseractOcrService : IOcrService, IDisposable
{
    private readonly ILoggingService _logger;
    private TesseractApi? _tesseract;
    private bool _isInitialized;
    private string _currentLanguage = "eng";
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _disposed;

    public bool IsInitialized => _isInitialized;
    public string LoadedLanguage => _currentLanguage;

    public TesseractOcrService(ILoggingService logger)
    {
        _logger = logger;
    }

    public Task InitializeAsync() => InitializeInternalAsync(_currentLanguage);

    private async Task InitializeInternalAsync(string language)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TesseractOcrService));

        if (_isInitialized && _tesseract != null && _currentLanguage == language)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized && _tesseract != null && _currentLanguage == language)
                return;

            _tesseract?.Dispose();
            _tesseract = null;
            _isInitialized = false;

            _logger.Info($"Initializing Tesseract OCR (language={language})");

            // looks for traineddata under Assets/tessdata/
            _tesseract = new TesseractApi(global::Android.App.Application.Context, AssetsDeployment.OncePerInitialization);

            var ok = await _tesseract.Init(language);
            if (!ok)
            {
                _logger.Error($"Tesseract initialization failed for language '{language}'. Ensure traineddata file exists in Assets/tessdata.");
                return;
            }

            _currentLanguage = language;
            _isInitialized = true;
            _logger.Info($"Tesseract OCR initialized successfully (language={language})");
        }
        catch (Exception ex)
        {
            _logger.Error("Exception during Tesseract initialization", ex);
            _isInitialized = false;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<string> ExtractTextAsync(Bitmap bitmap, string language = "eng")
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TesseractOcrService));

        if (!_isInitialized || language != _currentLanguage)
        {
            _logger.Debug(!_isInitialized
                ? "Tesseract not initialized; attempting initialization."
                : $"Language change requested: {_currentLanguage} -> {language}");
            await InitializeInternalAsync(language);
            if (!_isInitialized)
                return "[OCR not available - Tesseract initialization failed]";
        }

        try
        {
            if (_tesseract == null)
            {
                _logger.Error("TesseractApi instance is null after initialization");
                return string.Empty;
            }

            var setOk = await _tesseract.Recognise(bitmap);
            if (!setOk)
            {
                _logger.Warning("TesseractApi.Recognise returned false");
                return string.Empty;
            }

            var text = _tesseract.Text?.Trim() ?? string.Empty;
            _logger.Debug($"Tesseract extracted {text.Length} chars (lang={_currentLanguage})");
            return text;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to extract text using Tesseract", ex);
            return string.Empty;
        }
        finally
        {
            bitmap?.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            _tesseract?.Dispose();
            _initLock.Dispose();
        }
        catch { /* ignore */ }
    }
}