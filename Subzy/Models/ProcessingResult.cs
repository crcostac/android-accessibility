namespace Subzy.Models;

/// <summary>
/// Represents the result of the subtitle processing pipeline.
/// </summary>
public class ProcessingResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public SubtitleData? SubtitleData { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.Now;
    
    // Pipeline stage timings
    public TimeSpan CaptureTime { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public TimeSpan OcrTime { get; set; }
    public TimeSpan TranslationTime { get; set; }
    public TimeSpan TtsTime { get; set; }
    
    public bool ContentChanged { get; set; }
}
