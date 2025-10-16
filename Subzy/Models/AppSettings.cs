namespace Subzy.Models;

/// <summary>
/// Represents all user preferences and application settings.
/// </summary>
public class AppSettings
{
    // Service Control
    public bool IsServiceEnabled { get; set; }
    public int SnapshotFrequencySeconds { get; set; }

    // Image Processing
    public float Brightness { get; set; }
    public float Contrast { get; set; }

    // Translation Settings
    public bool IsTranslationEnabled { get; set; }
    public string TargetLanguage { get; set; }

    // Text-to-Speech Settings
    public bool IsTtsEnabled { get; set; }
    public string TtsVoice { get; set; }

    // Region of Interest (ROI) for subtitles
    public int RoiX { get; set; }
    public int RoiY { get; set; }
    public int RoiWidth { get; set; }
    public int RoiHeight { get; set; }

    // Azure API Configuration
    public string AzureTranslatorKey { get; set; }
    public string AzureTranslatorRegion { get; set; }
    public string AzureSpeechKey { get; set; }
    public string AzureSpeechRegion { get; set; }
    
    // Azure OpenAI Configuration
    public string AzureOpenAIEndpoint { get; set; }
    public string AzureOpenAIKey { get; set; }
    public string AzureOpenAITranslationDeployment { get; set; }
    public string AzureOpenAISpeechDeployment { get; set; }

    // Speech-to-Speech Translation Settings
    public bool IsSpeechToSpeechEnabled { get; set; }
    public string SpeechToSpeechSourceLanguage { get; set; } // null for auto-detect
    public string SpeechToSpeechTargetLanguage { get; set; }

    // Resource Management
    public bool AdaptiveScheduling { get; set; }
    public int LowBatteryThreshold { get; set; }

    // Onboarding
    public bool HasCompletedOnboarding { get; set; }

    // Color Detection
    public int MaxColorsPerApp { get; set; }
    public int SubtitleColorTolerance { get; set; }

    // Noise Removal
    public int MinSameColorNeighbors { get; set; }

    // Perceptual Hashing
    public bool UsePerceptualHashing { get; set; }
    public int HashSimilarityThreshold { get; set; }
}
