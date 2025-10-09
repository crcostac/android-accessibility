using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Subzy.Models;
using Subzy.Services;
using Subzy.Services.Interfaces;
using System.Text;

namespace Subzy.ViewModels;

/// <summary>
/// ViewModel for the debug/testing page.
/// </summary>
public partial class DebugViewModel : ObservableObject
{
    private readonly ILoggingService _logger;
    private readonly SettingsService _settingsService;
    private readonly IOcrService _ocrService;
    private readonly ITranslationService _translationService;
    private readonly ITtsService _ttsService;
    private readonly WorkflowOrchestrator _orchestrator;

    [ObservableProperty]
    private string _debugOutput = "Debug output will appear here";

    [ObservableProperty]
    private string _testText = "Hello, this is a test subtitle";

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private ProcessingResult? _lastResult;

    public DebugViewModel(
        ILoggingService logger,
        SettingsService settingsService,
        IOcrService ocrService,
        ITranslationService translationService,
        ITtsService ttsService,
        WorkflowOrchestrator orchestrator)
    {
        _logger = logger;
        _settingsService = settingsService;
        _ocrService = ocrService;
        _translationService = translationService;
        _ttsService = ttsService;
        _orchestrator = orchestrator;

        LoadSystemInfo();
    }

    private void LoadSystemInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Subzy Debug Console ===");
        sb.AppendLine($"Device: {DeviceInfo.Model}");
        sb.AppendLine($"Platform: {DeviceInfo.Platform} {DeviceInfo.VersionString}");
        sb.AppendLine($"App Version: {AppInfo.VersionString}");
        sb.AppendLine();
        sb.AppendLine("=== Service Status ===");
        sb.AppendLine($"OCR Initialized: {_ocrService.IsInitialized}");
        sb.AppendLine($"Translation Configured: {_translationService.IsConfigured}");
        sb.AppendLine($"TTS Configured: {_ttsService.IsConfigured}");
        sb.AppendLine();
        sb.AppendLine("Tap buttons below to test individual components");

        DebugOutput = sb.ToString();
    }

    [RelayCommand]
    private async Task TestOcrAsync()
    {
        if (IsTesting) return;

        try
        {
            IsTesting = true;
            AppendOutput("\n=== Testing OCR ===");
            AppendOutput("OCR test requires actual image data");
            AppendOutput($"OCR Status: {(_ocrService.IsInitialized ? "Ready" : "Not Initialized")}");

            if (!_ocrService.IsInitialized)
            {
                AppendOutput("Initializing OCR...");
                await _ocrService.InitializeAsync();
                AppendOutput($"OCR Status after init: {(_ocrService.IsInitialized ? "Ready" : "Failed")}");
            }
        }
        catch (Exception ex)
        {
            AppendOutput($"OCR Test Error: {ex.Message}");
            _logger.Error("OCR test failed", ex);
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private async Task TestTranslationAsync()
    {
        if (IsTesting) return;

        try
        {
            IsTesting = true;
            AppendOutput("\n=== Testing Translation ===");
            
            if (!_translationService.IsConfigured)
            {
                AppendOutput("Translation service not configured. Please add Azure keys in Settings.");
                return;
            }

            AppendOutput($"Translating: \"{TestText}\"");
            var (translated, detected) = await _translationService.TranslateAsync(
                TestText,
                "ro",
                "en"
            );
            
            AppendOutput($"Detected Language: {detected}");
            AppendOutput($"Translated: {translated}");
        }
        catch (Exception ex)
        {
            AppendOutput($"Translation Test Error: {ex.Message}");
            _logger.Error("Translation test failed", ex);
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private async Task TestTtsAsync()
    {
        if (IsTesting) return;

        try
        {
            IsTesting = true;
            AppendOutput("\n=== Testing TTS ===");
            
            if (!_ttsService.IsConfigured)
            {
                AppendOutput("TTS service not configured. Please add Azure keys in Settings.");
                return;
            }

            var settings = _settingsService.LoadSettings();
            AppendOutput($"Speaking: \"{TestText}\"");
            AppendOutput($"Voice: {settings.TtsVoice}");
            
            await _ttsService.SpeakAsync(TestText, settings.TtsVoice);
            AppendOutput("TTS completed successfully");
        }
        catch (Exception ex)
        {
            AppendOutput($"TTS Test Error: {ex.Message}");
            _logger.Error("TTS test failed", ex);
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private async Task ViewLogsAsync()
    {
        try
        {
            var logPath = _logger.GetLogFilePath();
            AppendOutput($"\n=== Log File ===");
            AppendOutput($"Path: {logPath}");
            
            if (File.Exists(logPath))
            {
                var logContent = await File.ReadAllTextAsync(logPath);
                var lastLines = logContent.Split('\n').TakeLast(20);
                AppendOutput("\nLast 20 log entries:");
                foreach (var line in lastLines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        AppendOutput(line);
                }
            }
            else
            {
                AppendOutput("Log file not found");
            }
        }
        catch (Exception ex)
        {
            AppendOutput($"Error reading logs: {ex.Message}");
            _logger.Error("Failed to read logs", ex);
        }
    }

    [RelayCommand]
    private async Task ClearLogsAsync()
    {
        try
        {
            await _logger.ClearLogsAsync();
            AppendOutput("\n=== Logs Cleared ===");
            AppendOutput("All log files have been deleted");
        }
        catch (Exception ex)
        {
            AppendOutput($"Error clearing logs: {ex.Message}");
            _logger.Error("Failed to clear logs", ex);
        }
    }

    [RelayCommand]
    private void ClearOutput()
    {
        LoadSystemInfo();
    }

    private void AppendOutput(string text)
    {
        DebugOutput += $"\n{text}";
    }
}
