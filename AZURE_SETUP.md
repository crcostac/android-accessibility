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

## Step 3: Create a Speech Services Resource

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

## Step 4: Configure Subzy

1. Open the Subzy app on your Android device
2. Complete the onboarding flow
3. Navigate to **Settings**
4. Scroll down to "Azure Configuration"
5. Enter your API keys:
   - **Translator API Key**: Paste the key from Step 2
   - **Speech API Key**: Paste the key from Step 3
6. Verify the region matches your Azure resources (e.g., "westeurope")
7. Tap "Save Settings"

## Step 5: Test Your Configuration

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

### Cost Estimates

For typical usage (2 hours of video per day with subtitles):
- Translation: ~10,000 characters/hour = 600,000 characters/month
- TTS: ~10,000 characters/hour = 600,000 characters/month

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

- [Azure Translator Documentation](https://docs.microsoft.com/azure/cognitive-services/translator/)
- [Azure Speech Services Documentation](https://docs.microsoft.com/azure/cognitive-services/speech-service/)
- [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/)
- [Supported Languages](https://docs.microsoft.com/azure/cognitive-services/translator/language-support)

## Support

If you encounter issues:
1. Check the Subzy debug console for error messages
2. Review logs in the debug page
3. Verify Azure resource status
4. Create an issue on GitHub with details

---

For more help, see the [README](README.md) or create an issue on GitHub.
