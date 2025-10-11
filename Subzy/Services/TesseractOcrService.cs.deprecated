using Subzy.Services.Interfaces;
using Tesseract;

namespace Subzy.Services;

/// <summary>
/// OCR service implementation using Tesseract library.
/// </summary>
public class TesseractOcrService : IOcrService
{
    private readonly ILoggingService _logger;
    private TesseractEngine? _engine;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public TesseractOcrService(ILoggingService logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        await Task.Run(() =>
        {
            try
            {
                // Tesseract requires trained data files
                // These should be placed in the Assets folder and copied to the app directory
                var tessDataPath = Path.Combine(FileSystem.AppDataDirectory, "tessdata");
                
                if (!Directory.Exists(tessDataPath))
                {
                    Directory.CreateDirectory(tessDataPath);
                    _logger.Warning($"Tesseract data directory created at {tessDataPath}. Please ensure trained data files are present.");
                }

                // Initialize Tesseract engine with English language
                // In production, this path should point to the actual tessdata location
                try
                {
                    _engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
                    _isInitialized = true;
                    _logger.Info("Tesseract OCR initialized successfully");
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Tesseract initialization failed (tessdata may be missing): {ex.Message}");
                    _isInitialized = false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to initialize Tesseract OCR", ex);
                _isInitialized = false;
            }
        });
    }

    public async Task<string> ExtractTextAsync(byte[] imageBytes, string language = "eng")
    {
        if (!_isInitialized)
        {
            _logger.Warning("OCR service not initialized, attempting initialization");
            await InitializeAsync();
            
            if (!_isInitialized)
            {
                return "[OCR not available - Tesseract data files missing]";
            }
        }

        return await Task.Run(() =>
        {
            try
            {
                using var img = Pix.LoadFromMemory(imageBytes);
                using var page = _engine!.Process(img);
                var text = page.GetText();
                
                _logger.Debug($"OCR extracted text: {text?.Length ?? 0} characters");
                return text?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to extract text from image", ex);
                return string.Empty;
            }
        });
    }

    public void Dispose()
    {
        _engine?.Dispose();
        _engine = null;
        _isInitialized = false;
    }
}
