namespace Subzy.Helpers;

/// <summary>
/// Application-wide constants.
/// </summary>
public static class Constants
{
    // Application
    public const string AppName = "Subzy";
    public const string AppVersion = "1.0.0";

    // Permissions
    public const int ScreenCaptureRequestCode = 1000;
    public const int MediaProjectionRequestCode = 1001;

    // Service
    public const int ForegroundServiceNotificationId = 1;
    public const string ForegroundServiceChannelId = "SubzyServiceChannel";
    public const string ForegroundServiceChannelName = "Subzy Background Service";

    // Defaults
    public const int DefaultSnapshotFrequencySeconds = 2;
    public const int MinSnapshotFrequencySeconds = 1;
    public const int MaxSnapshotFrequencySeconds = 10;

    // OCR Languages
    public const string DefaultOcrLanguage = "eng";
    
    // Translation Languages
    public const string DefaultTargetLanguage = "ro";
    public const string RomanianLanguageCode = "ro";
    public const string EnglishLanguageCode = "en";

    // TTS Voices
    public const string DefaultTtsVoice = "ro-RO-AlinaNeural";
    public const string DefaultTtsLanguage = "ro-RO";

    // Available Romanian neural voices
    public static readonly string[] RomanianVoices = new[]
    {
        "ro-RO-AlinaNeural",
        "ro-RO-EmilNeural"
    };
    
    // Azure OpenAI
    public const string DefaultAzureOpenAIDeployment = "gpt-4o";

    // Resource Management
    public const int DefaultLowBatteryThreshold = 20;
    public const int LowBatterySnapshotFrequencySeconds = 5;

    // URLs
    public const string AzurePortalUrl = "https://portal.azure.com";
    public const string PrivacyPolicyUrl = "https://github.com/crcostac/android-accessibility";
    public const string SupportEmail = "support@subzy.app";

    // Debug
    public const string DebugGesture = "LongPress"; // For accessing debug UI
}
