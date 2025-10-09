# Project Specification: Android Subtitle Reader & Translator

## Project Intent

The purpose of this project is to develop an Android application, primarily designed for Android TV but ideally compatible with Android phones as well, to enhance accessibility for users with low vision. The app will read subtitles displayed on the screen out loud, translating them in real time, with a particular focus on English-to-Romanian translation. Support for other language pairs is desirable.

## Core Features

- **Subtitle Reading:** Use OCR (Optical Character Recognition) to capture and read subtitles directly from the screen image, independent of app accessibility APIs.
- **Translation:** Translate subtitles from English to Romanian, with extensibility for other languages as needed.
- **Text-to-Speech:** Speak subtitles aloud using Romanian as the primary voice; support other languages where possible.
- **Local AI Engines:** Prefer local (on-device) AI models for OCR and Text-to-Speech to maintain privacy and offline functionality; cloud-based options may be considered if necessary.
- **Multi-application Support:** Work across various streaming apps (Netflix, HBO, Amazon Prime, etc.) regardless of their accessibility support by reading pixels from the screen.
- **Permissions:** Accept the need for full administrator or accessibility permissions to capture the displayed image on the device.

## Target Audience

- Individuals with low vision or other print disabilities who want to access subtitles in spoken form and/or in their native language.

## Exploration Areas

- Evaluate available local AI models for OCR and Text-to-Speech on Android devices.
- Investigate Android OS capabilities for screen capture, especially on Android TV and phones, including permission requirements and limitations.
- Explore translation model options for on-device or cloud-based translation.
- Assess potential challenges in supporting streaming apps and handling DRM or protected content.

## Next Steps

This high-level specification serves as a foundation for further research and refinement. Options for implementation will be explored, and a more detailed technical specification will be developed based on feasibility, available tools, and user needs.
