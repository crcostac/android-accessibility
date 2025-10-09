namespace Subzy.Models;

/// <summary>
/// Represents extracted subtitle information.
/// </summary>
public class SubtitleData
{
    public string OriginalText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public string DetectedLanguage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool WasTranslated { get; set; }
    public bool WasSpoken { get; set; }
    public double ConfidenceScore { get; set; }
}
