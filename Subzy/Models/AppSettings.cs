namespace Subzy.Models;

/// <summary>
/// Represents all user preferences and application settings.
/// </summary>
public class AppSettings
{
    // Service Control
    public bool IsServiceEnabled { get; set; }
    public int SnapshotFrequencySeconds { get; set; } = 2;

    // Image Processing
    public float Brightness { get; set; } = 1.0f;
    public float Contrast { get; set; } = 1.0f;

    // Translation Settings
    public bool IsTranslationEnabled { get; set; } = true;
    public string TargetLanguage { get; set; } = "ro"; // Romanian by default

    // Text-to-Speech Settings
    public bool IsTtsEnabled { get; set; } = true;
    public string TtsVoice { get; set; } = "ro-RO-AlinaNeural";

    // Region of Interest (ROI) for subtitles
    public int RoiX { get; set; } = 0;
    public int RoiY { get; set; } = 0;
    public int RoiWidth { get; set; } = 0;
    public int RoiHeight { get; set; } = 0;

    // Azure API Configuration
    public string AzureTranslatorKey { get; set; } = "";
    public string AzureTranslatorRegion { get; set; } = "westeurope";
    public string AzureSpeechKey { get; set; } = "";
    public string AzureSpeechRegion { get; set; } = "westeurope";

    // Resource Management
    public bool AdaptiveScheduling { get; set; } = true;
    public int LowBatteryThreshold { get; set; } = 20;

    // Onboarding
    public bool HasCompletedOnboarding { get; set; }
}
