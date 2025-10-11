# Subzy - Android Subtitle Reader & Translator

Subzy is an Android accessibility application designed to enhance the viewing experience for users with low vision and print disabilities. It captures, translates, and speaks aloud on-screen subtitles in real-time from any streaming application.

## Features

- **Real-time Subtitle Capture**: Periodically captures screenshots to extract subtitle text
- **OCR Processing**: Uses Tesseract OCR to read subtitles from screen images (with Tesseract4Android integration support for native Android performance)
- **Translation**: Translates subtitles from English to Romanian (extensible to other languages)
- **Text-to-Speech**: Speaks subtitles aloud using Azure Neural TTS with natural inflection
- **Multi-Application Support**: Works across various streaming apps (Netflix, HBO, Prime Video, etc.)
- **Configurable Settings**: Adjust capture frequency, image processing, translation, and TTS preferences
- **Background Service**: Runs as a foreground service for continuous subtitle reading
- **Accessibility-First Design**: Large text, high contrast, and screen reader support

## Requirements

- Android device running Android 5.0 (API 21) or higher
- Internet connection for cloud-based translation and TTS services
- Azure Cognitive Services account with:
  - Azure Translator API key
  - Azure Speech Services API key

## Getting Started

### 1. Azure Account Setup

To use Subzy, you need to set up Azure Cognitive Services:

1. Create an Azure account at [https://portal.azure.com](https://portal.azure.com)
2. Create a **Translator** resource:
   - Go to "Create a resource" → "AI + Machine Learning" → "Translator"
   - Note the API key and region
3. Create a **Speech** resource:
   - Go to "Create a resource" → "AI + Machine Learning" → "Speech"
   - Note the API key and region

### 2. Install Subzy

1. Download the APK from the releases page
2. Install on your Android device or TV
3. Grant required permissions when prompted

### 3. Initial Configuration

1. Launch Subzy and complete the onboarding flow
2. Navigate to **Settings**
3. Enter your Azure API keys:
   - Translator API Key
   - Speech API Key
4. Configure your preferences:
   - Snapshot frequency (how often to capture)
   - Translation settings (target language)
   - TTS settings (voice selection)
5. Save settings

### 4. Using Subzy

1. Return to the **Home** screen
2. Tap the **Start Service** button
3. Grant screen capture permission when prompted
4. Subzy will now run in the background and read subtitles aloud
5. Open your streaming app and start watching with subtitles enabled

## Building from Source

### Prerequisites

- .NET 9.0 SDK
- .NET MAUI workload installed
- Android SDK

### Build Steps

```bash
# Clone the repository
git clone https://github.com/crcostac/android-accessibility.git
cd android-accessibility/Subzy

# Restore dependencies
dotnet restore

# Build the project
dotnet build -c Release -f net9.0-android

# Build with Tesseract4Android support (optional, requires AAR binding)
dotnet build -c Release -f net9.0-android -p:UseTesseract4Android=true

# Deploy to connected device
dotnet build -c Release -f net9.0-android -t:Run
```

For more information about Tesseract4Android integration, see [TESSERACT4ANDROID_INTEGRATION.md](TESSERACT4ANDROID_INTEGRATION.md).

## Configuration

### Settings Overview

- **Service Configuration**
  - Snapshot Frequency: 1-10 seconds (default: 2s)
  
- **Image Processing**
  - Brightness: 0.5-2.0 (default: 1.0)
  - Contrast: 0.5-2.0 (default: 1.0)
  
- **Translation**
  - Enable/Disable translation
  - Target language (default: Romanian - "ro")
  
- **Text-to-Speech**
  - Enable/Disable TTS
  - Voice selection (Romanian neural voices available)
  
- **Resource Management**
  - Adaptive scheduling based on battery level
  - Low battery threshold (10-50%)

### Supported Languages

**OCR**: English (extensible to other languages with appropriate Tesseract data files)

**Translation Target**: Romanian (primary), with extensibility for additional languages

**TTS Voices**: 
- Romanian: ro-RO-AlinaNeural, ro-RO-EmilNeural

## Permissions

Subzy requires the following permissions:

- **Internet**: For cloud-based translation and TTS services
- **Foreground Service**: To run continuously in the background
- **Media Projection**: For screen capture functionality
- **Record Audio**: For TTS audio output
- **Post Notifications**: To display service status

## Privacy & Data

- Screenshots are captured temporarily and processed locally before being sent to Azure
- Only extracted text is sent to Azure Translator and Azure Speech Services
- No screenshots or subtitle data are permanently stored
- All cloud communications use HTTPS encryption
- Azure processes data according to Microsoft's privacy policy

## Troubleshooting

### OCR Not Working
- Ensure Tesseract trained data files are present in the app's data directory
- Check that subtitles are clearly visible on screen
- Adjust brightness/contrast settings for better recognition

### Translation Failing
- Verify Azure Translator API key is correct
- Check internet connection
- Ensure API key has not expired or reached quota limits

### TTS Not Speaking
- Verify Azure Speech API key is correct
- Check device audio output and volume
- Test with the debug page's TTS test button

### Service Stops Unexpectedly
- Check battery optimization settings for Subzy
- Ensure foreground service permission is granted
- Review logs in the debug page

## Debug Features

Access the Debug page to:
- Test OCR, Translation, and TTS individually
- View real-time logs
- Check service initialization status
- Verify Azure API connectivity

## Known Limitations

- DRM-protected content may prevent screen capture on some devices
- OCR accuracy depends on subtitle quality and font clarity
- Translation requires internet connection
- Battery consumption increases during active use

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

## License

This project is open source and available under the MIT License.

## Support

For issues, questions, or feedback:
- Open an issue on GitHub
- Contact: support@subzy.app

## Acknowledgments

- Tesseract OCR for text recognition
- Azure Cognitive Services for translation and TTS
- .NET MAUI team for the cross-platform framework
- Community Toolkit for MAUI helpers

---

**Note**: This application is designed for accessibility purposes. Always ensure you have proper rights to capture and process content from streaming services.
