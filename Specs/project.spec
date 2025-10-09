# Project Specification: Android Subtitle Reader & Translator

## Project Intent

The purpose of this project is to develop an Android application, primarily designed for Android TV but ideally compatible with Android phones as well, to enhance accessibility for users with low vision or print disabilities. The application will capture, translate, and speak subtitles from various streaming apps to improve accessibility.

## Core Features

- **Subtitle Reading:** Use Tesseract OCR (Optical Character Recognition) to capture and read subtitles directly from the screen image, independent of app accessibility APIs.
- **Translation:** Translate subtitles from English to Romanian (primary), with extensibility for other languages. Focus on AI-based translation models that can take context into account (e.g., Marian NMT, OpenNMT, Microsoft/Google APIs).
- **Text-to-Speech:** Speak subtitles aloud using Romanian as the primary voice. Prioritize AI-based TTS for expressive, natural speech inflexion (e.g., Coqui TTS, Azure/Google neural TTS).
- **Local AI Engines:** Prefer local (on-device) AI models for OCR and Text-to-Speech to maintain privacy and offline functionality; cloud-based options (Microsoft, Google) may be considered for improved quality/latency and will be evaluated for cost.
- **Multi-application Support:** Work across various streaming apps (Netflix, HBO, Amazon Prime, etc.) regardless of their accessibility support by reading pixels from the screen.
- **Permissions:** Accept the need for full administrator or accessibility permissions to capture the displayed image on the device.
- **Development Framework:** Use C# with .NET MAUI for cross-platform Android development.

## Target Audience

- Individuals with low vision or other print disabilities who want to access subtitles in spoken form and/or in their native language.

## Exploration Areas

- Compare available local AI models for OCR and Text-to-Speech on Android devices (including Coqui TTS for Romanian).
- Investigate Android OS capabilities for screen capture, especially on Android TV and phones, including permission requirements and limitations.
- Explore translation model options for on-device or cloud-based translation, evaluating both cost (for cloud) and performance (for local).
- Assess potential challenges in supporting streaming apps and handling DRM or protected content.

## Next Steps

This high-level specification serves as a foundation for further research and refinement. Options for implementation will be explored, focusing on quality, cost, and performance. A more detailed technical specification will be developed based on these investigations.