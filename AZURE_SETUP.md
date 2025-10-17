# Azure Cognitive Services Setup Guide

Subzy requires Azure Cognitive Services API keys for translation and text-to-speech functionality. This guide will walk you through setting up your Azure account and obtaining the necessary API keys.

## Prerequisites

- A Microsoft account (create one at [microsoft.com](https://www.microsoft.com))
- An Azure subscription (free tier available)

## Step 1: Create an Azure Account

1. Go to [https://portal.azure.com](https://portal.azure.com)
2. Sign in with your Microsoft account or create a new one
3. If you don't have an Azure subscription, you can create a free account:
   - Visit [https://azure.microsoft.com/free/](https://azure.microsoft.com/free/)
   - Click "Start free"
   - Follow the registration process
   - You'll get $200 credit for 30 days and free services for 12 months

## Step 2: Create a Translator Resource

1. In the Azure Portal, click "Create a resource"
2. Search for "Translator"
3. Click "Create"
4. Fill in the required information:
   - **Subscription**: Select your subscription
   - **Resource Group**: Create new or select existing
   - **Region**: Choose a region close to you (e.g., West Europe)
   - **Name**: Enter a unique name (e.g., "subzy-translator")
   - **Pricing Tier**: Select "Free F0" (500K chars/month free) or "Standard S1"
5. Click "Review + create"
6. Click "Create"

### Get Translator API Key

1. Once deployment is complete, click "Go to resource"
2. In the left menu, click "Keys and Endpoint"
3. Copy one of the keys (KEY 1 or KEY 2)
4. Note the region (e.g., "westeurope")

## Step 3: Create an Azure OpenAI Resource (Recommended)

Azure OpenAI provides enhanced translation with context awareness, OCR artifact cleanup, and consistent entity translation. It also powers the experimental speech-to-speech translation feature.

1. In the Azure Portal, click "Create a resource"
2. Search for "Azure OpenAI"
3. Click "Create"
4. Fill in the required information:
   - **Subscription**: Select your subscription
   - **Resource Group**: Use the same as Translator or create new
   - **Region**: Choose a supported region (e.g., East US 2, Sweden Central for Realtime API)
   - **Name**: Enter a unique name (e.g., "subzy-openai")
   - **Pricing Tier**: Select "Standard S0"
5. Click "Review + create"
6. Click "Create"

### Get Azure OpenAI Configuration

1. Once deployment is complete, click "Go to resource"
2. In the left menu, click "Keys and Endpoint"
3. Copy one of the keys (KEY 1 or KEY 2)
4. **Copy BOTH endpoint URLs** - you'll need both:
   - **OpenAI Endpoint**: `https://your-resource.openai.azure.com/` (for text translation)
   - **Cognitive Services Endpoint**: `https://your-resource.cognitiveservices.azure.com` (for realtime API)
5. Go to "Model deployments" in the left menu
6. Click "Manage Deployments" or "Create"
7. Deploy a GPT-4 model (e.g., "gpt-4o" or "gpt-4") for translation
8. Note the deployment name you created

**Note**: Azure OpenAI requires approval. If you don't have access, you can still use the traditional Azure Translator service (see Step 2).

### Deploy gpt-4o-realtime-preview Model (Optional - For Speech-to-Speech Translation)

For the experimental speech-to-speech translation feature:

1. In Azure OpenAI Studio, go to "Deployments"
2. Click "Create new deployment"
3. Select "gpt-4o-realtime-preview" model (Note: "mini" version not yet available)
4. Enter a deployment name (e.g., "gpt-4o-realtime-preview")
5. Configure deployment settings:
   - **Model version**: Latest available
   - **Deployment type**: Standard
6. Click "Create"
7. Note the deployment name for use in the app

**Important**: The gpt-4o-realtime-preview model is required for real-time speech-to-speech translation via WebSocket. This feature enables:
- Real-time audio capture from streaming apps
- Streaming speech-to-speech translation
- Low-latency audio responses

**Availability**: The realtime API is currently in preview and may not be available in all regions. Supported regions include:
- **East US 2** (recommended for US)
- **Sweden Central** (recommended for Europe)

**Endpoint Clarification**:
- **Text Translation**: Uses OpenAI endpoint (`https://your-resource.openai.azure.com/`)
- **Realtime API**: Uses Cognitive Services endpoint (`https://your-resource.cognitiveservices.azure.com`)

### Usage Notes

- When using the Realtime API, ensure your app is configured to use the Cognitive Services endpoint for WebSocket communication.
- The Cognitive Services endpoint is different from the OpenAI endpoint used for standard text translation.
- Both endpoints are necessary if you are using features from both the Translator and Azure OpenAI services.

## Step 4: Create a Speech Services Resource

1. In the Azure Portal, click "Create a resource"
2. Search for "Speech Services" or "Speech"
3. Click "Create"
4. Fill in the required information:
   - **Subscription**: Select your subscription
   - **Resource Group**: Use the same as Translator or create new
   - **Region**: Choose the same region as Translator (e.g., West Europe)
   - **Name**: Enter a unique name (e.g., "subzy-speech")
   - **Pricing Tier**: Select "Free F0" (5 hours/month free) or "Standard S0"
5. Click "Review + create"
6. Click "Create"

### Get Speech API Key

1. Once deployment is complete, click "Go to resource"
2. In the left menu, click "Keys and Endpoint"
3. Copy one of the keys (KEY 1 or KEY 2)
4. Note the region (e.g., "westeurope")

## Step 5: Configure Subzy

1. Open the Subzy app on your Android device
2. Complete the onboarding flow
3. Navigate to **Settings**
4. Scroll down to "Azure Configuration"
5. Enter your API keys:
   - **Azure OpenAI Endpoint**: Paste the **OpenAI endpoint** URL from Step 3 (e.g., `https://your-resource.openai.azure.com/`)
   - **Azure OpenAI Key**: Paste the key from Step 3 (if using Azure OpenAI)
   - **Azure OpenAI Translation Deployment**: Enter the deployment name for text translation (e.g., "gpt-4o")
   - **Azure OpenAI Speech Endpoint**: Paste the **Cognitive Services endpoint** URL (e.g., `https://your-resource.cognitiveservices.azure.com`)
   - **Azure OpenAI Speech Deployment**: Enter the realtime deployment name (e.g., "gpt-4o-realtime-preview")
   - **Speech API Key**: Paste the key from Step 4 (for TTS)
6. Verify the region matches your Azure resources (e.g., "eastus2")
7. Tap "Save Settings"

**Important**: Make sure to use the correct endpoint for each feature:
- Translation uses the **OpenAI endpoint** (`.openai.azure.com`)
- Speech-to-Speech uses the **Cognitive Services endpoint** (`.cognitiveservices.azure.com`)

**Note**: If you're using traditional Azure Translator instead of Azure OpenAI, enter the Translator API Key instead.

## Step 6: Test Your Configuration

1. Go to the **Debug** page in Subzy
2. Enter some test text (e.g., "Hello, this is a test")
3. Tap "Test Translation" to verify translation works
4. Tap "Test TTS" to verify text-to-speech works

If both tests succeed, you're all set!

## Pricing Information

### Free Tier Limits

**Translator**:
- Free F0: 2 million characters per month
- Sufficient for casual use

**Speech Services**:
- Free F0: 5 audio hours per month
- Approximately 150,000 characters of speech
- Neural voices: 0.5 million characters per month

**Azure OpenAI**:
- Standard S0: Pay-per-use
- GPT-4: ~$0.03 per 1K tokens (~750 words)
- For subtitle translation: ~$0.10-0.20 per hour of video

### Cost Estimates

For typical usage (2 hours of video per day with subtitles):
- **Using Azure Translator**: ~10,000 characters/hour = 600,000 characters/month
- **Using Azure OpenAI**: ~$0.10-0.20/hour = $6-12/month for translation
- TTS: ~10,000 characters/hour = 600,000 characters/month

**Recommendation**: Use Azure OpenAI for better quality translation with context awareness and OCR cleanup. The extra cost is minimal compared to the improved accuracy.

**Monthly Cost** (if exceeding free tier):
- Translation: $10 per million characters
- Speech (Neural TTS): $16 per million characters
- **Estimated**: $10-15/month for heavy use

## Monitoring Usage

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Translator or Speech resource
3. Click "Metrics" in the left menu
4. View usage statistics and set up alerts

## Security Best Practices

- Keep your API keys confidential
- Don't share screenshots showing your keys
- Regenerate keys periodically from the Azure Portal
- Use separate keys for testing and production
- Monitor usage to detect unauthorized access

## Troubleshooting

### "Invalid API Key" Error
- Verify you copied the entire key correctly
- Check that the region matches (e.g., westeurope)
- Ensure the resource is not disabled in Azure Portal

### "Quota Exceeded" Error
- Check your usage in Azure Portal
- Upgrade to paid tier if needed
- Wait for monthly quota to reset

### Translation Not Working
- Verify internet connection
- Check Azure service status at [status.azure.com](https://status.azure.com)
- Ensure source text is in a supported language

### TTS Not Speaking
- Check device volume
- Verify Speech API key is correct
- Ensure selected voice is available for your region

## Additional Resources

- [Azure OpenAI Documentation](https://learn.microsoft.com/azure/ai-services/openai/)
- [Azure Translator Documentation](https://docs.microsoft.com/azure/cognitive-services/translator/)
- [Azure Speech Services Documentation](https://docs.microsoft.com/azure/cognitive-services/speech-service/)
- [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/)
- [Supported Languages](https://docs.microsoft.com/azure/cognitive-services/translator/language-support)

## Speech-to-Speech Translation Setup (Experimental)

The speech-to-speech translation feature allows real-time audio translation using Azure OpenAI's gpt-4o-mini-realtime model.

### Prerequisites

1. Azure OpenAI resource with gpt-4o-mini-realtime deployment (see Step 3 above)
2. Android device with microphone (RECORD_AUDIO permission already configured)
3. Stable internet connection for WebSocket communication

### Configuration

The speech-to-speech service uses Azure OpenAI credentials with the Cognitive Services endpoint:

```csharp
// Configuration is loaded from AppSettings
var config = new SpeechToSpeechConfig
{
    // IMPORTANT: Use Cognitive Services endpoint for Realtime API
    AzureOpenAIEndpoint = "https://your-resource.cognitiveservices.azure.com", // NOT .openai.azure.com
    AzureOpenAIKey = "your-api-key",
    ModelDeploymentName = "gpt-4o-realtime-preview", // Note: not "mini" version
    SourceLanguage = null,  // Auto-detect
    TargetLanguage = "ro",  // Romanian
    AudioSampleRate = 16000,
    AudioChannels = 1,
    BufferSizeInBytes = 3200
};
```

**Critical**: The Realtime API requires the **Cognitive Services endpoint** (`.cognitiveservices.azure.com`), not the OpenAI endpoint (`.openai.azure.com`). This is different from the text translation API.

### Usage Example

```csharp
// Inject the service
public class MyService
{
    private readonly ISpeechToSpeechService _speechService;

    public MyService(ISpeechToSpeechService speechService)
    {
        _speechService = speechService;
        
        // Subscribe to events
        _speechService.TranslatedTextReceived += OnTranslatedText;
        _speechService.AudioResponseReceived += OnAudioResponse;
        _speechService.ErrorOccurred += OnError;
    }

    public async Task StartTranslation()
    {
        // Start speech-to-speech translation
        // Source: auto-detect, Target: Romanian
        await _speechService.StartAsync(null, "ro");
    }

    public async Task StopTranslation()
    {
        await _speechService.StopAsync();
    }

    private void OnTranslatedText(object sender, string text)
    {
        // Handle translated text
        Console.WriteLine($"Translation: {text}");
    }

    private void OnAudioResponse(object sender, byte[] audioData)
    {
        // Handle audio response for playback
        // Audio format: PCM 16-bit, 16kHz, mono
    }

    private void OnError(object sender, Exception ex)
    {
        // Handle errors
        Console.WriteLine($"Error: {ex.Message}");
    }
}
```

### Audio Specifications

- **Input Format**: PCM 16-bit, 16kHz, mono
- **Output Format**: PCM 16-bit, 16kHz, mono
- **Buffer Size**: 3200 bytes (100ms at 16kHz)
- **Voice Activity Detection**: Server-side VAD with 500ms silence detection

### Performance Considerations

- **Latency**: ~500-1000ms for speech detection + translation
- **Battery Impact**: Moderate (continuous microphone and network usage)
- **Network Usage**: ~32 KB/s upload + variable download depending on response length
- **Cost**: Usage-based pricing through Azure OpenAI (more expensive than text translation)

### Troubleshooting

#### "Speech-to-Speech service not configured"
- Verify Azure OpenAI endpoint, key, and deployment name are set
- Ensure gpt-4o-mini-realtime model is deployed
- Check that the deployment name matches exactly

#### Audio capture fails
- Verify RECORD_AUDIO permission is granted
- Check microphone is not in use by another app
- Test on a physical device (emulator microphone may not work)

#### WebSocket connection fails
- Verify internet connection is stable
- Check firewall/proxy settings allow WebSocket connections
- Ensure Azure OpenAI endpoint URL is correct (should use wss:// protocol internally)

#### No translation output
- Speak clearly into the microphone
- Check microphone volume and placement
- Verify source language is supported
- Monitor logs for API errors or rate limiting

### Limitations

1. **Preview Feature**: The realtime API is in preview and subject to changes
2. **Regional Availability**: Not all Azure regions support the realtime model
3. **Cost**: Higher cost than text-based translation
4. **Platform Support**: Currently Android-only (microphone access)
5. **Network Dependency**: Requires stable internet connection

### Cost Estimation

For speech-to-speech translation:
- **Input Audio**: ~$0.06 per hour of audio processing
- **Output Audio**: ~$0.24 per hour of generated audio
- **Tokens**: Additional charges for text processing

For typical usage (30 minutes/day):
- Monthly cost: ~$4-8 depending on usage patterns

Compare to text translation: ~$0.10-0.20/hour

## Support

If you encounter issues:
1. Check the Subzy debug console for error messages
2. Review logs in the debug page
3. Verify Azure resource status
4. Create an issue on GitHub with details

---

For more help, see the [README](README.md) or create an issue on GitHub.

cd C:\Temp\screenshots
adb shell ls -l /storage/emulated/0/Download
adb pull /storage/emulated/0/Download/
adb shell rm /storage/emulated/0/Download/*.png
