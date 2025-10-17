namespace Subzy.Models;

/// <summary>
/// Configuration for Speech-to-Speech translation service using Azure OpenAI.
/// </summary>
public class SpeechToSpeechConfig
{
    /// <summary>
    /// Azure OpenAI endpoint URL (e.g., "https://your-resource.openai.azure.com/")
    /// </summary>
    public string AzureOpenAISpeechEndpoint { get; set; }

    /// <summary>
    /// Azure OpenAI API key
    /// </summary>
    public string AzureOpenAISpeechKey { get; set; }

    /// <summary>
    /// Model deployment name (default: "gpt-4o-mini-realtime")
    /// </summary>
    public string AzureOpenAISpeechDeployment { get; set; }

    /// <summary>
    /// Source language code (null for auto-detect)
    /// </summary>
    public string? SourceLanguage { get; set; }

    /// <summary>
    /// Target language code (default: "ro" for Romanian)
    /// </summary>
    public string TargetLanguage { get; set; }

    /// <summary>
    /// Audio sample rate in Hz (default: 16000)
    /// </summary>
    public int AudioSampleRate { get; set; } = 16000;

    /// <summary>
    /// Number of audio channels (default: 1 for mono)
    /// </summary>
    public int AudioChannels { get; set; } = 1;

    /// <summary>
    /// Audio buffer size in bytes (default: 3200 = 100ms at 16kHz, 16-bit)
    /// </summary>
    public int BufferSizeInBytes { get; set; } = 3200;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>True if the configuration is valid</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(AzureOpenAISpeechEndpoint) &&
               !string.IsNullOrWhiteSpace(AzureOpenAISpeechKey) &&
               !string.IsNullOrWhiteSpace(AzureOpenAISpeechDeployment) &&
               !string.IsNullOrWhiteSpace(TargetLanguage) &&
               AudioSampleRate > 0 &&
               AudioChannels > 0 &&
               BufferSizeInBytes > 0;
    }
}
