# Project Specification: Android Subtitle Reader & Translator

## Project Intent

The purpose of this project is to develop an Android application, primarily designed for Android TV but ideally compatible with Android phones as well, to enhance accessibility for users with low vision or print disabilities. The application will capture, translate, and speak subtitles from various streaming apps to improve accessibility.

## Core Features

- **Subtitle Reading:** Use Tesseract OCR (Optical Character Recognition) to capture and read subtitles directly from the screen image, independent of app accessibility APIs.
- **Translation:** Translate subtitles from English to Romanian (primary), with extensibility for other languages. Use cloud-based translation via Microsoft Azure Translator for high-quality, low-latency, context-aware results.
- **Text-to-Speech:** Speak subtitles aloud using Romanian as the primary voice. Use cloud-based neural TTS via Microsoft Azure for expressive, natural speech inflexion.
- **Cloud AI Engines:** Use cloud-based AI models for translation and text-to-speech to maximize quality and reduce device resource requirements.
- **Multi-application Support:** Work across various streaming apps (Netflix, HBO, Amazon Prime, etc.) regardless of their accessibility support by reading pixels from the screen.
- **Permissions:** Accept the need for full administrator or accessibility permissions to capture the displayed image on the device.
- **Development Framework:** Use C# with .NET MAUI for cross-platform Android development.

## Target Audience

- Individuals with low vision or other print disabilities who want to access subtitles in spoken form and/or in their native language.

## Exploration Areas

- Compare available local AI models for OCR on Android devices.
- Investigate Android OS capabilities for screen capture, especially on Android TV and phones, including permission requirements and limitations.
- Explore cloud-based translation and text-to-speech options, evaluating cost and integration strategies.
- Assess potential challenges in supporting streaming apps and handling DRM or protected content.

## Next Steps

This high-level specification serves as a foundation for further research and refinement. Options for implementation will be explored, focusing on quality, cost, and performance. A more detailed technical specification will be developed based on these investigations.