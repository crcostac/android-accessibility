# Project Specification: Android Subtitle Reader & Translator

## Project Intent

The purpose of this project is to develop an Android application, primarily designed for Android TV but ideally compatible with Android phones as well, to enhance accessibility for users with low vision and other print disabilities by capturing, translating, and speaking aloud on-screen subtitles.

## Core Features

- **Name:** Subzy will be the name of the project and of the installed app.
- **Subtitle Reading:** Use Tesseract OCR (Optical Character Recognition) to capture and read subtitles directly from the screen image, independent of app accessibility APIs.
- **Translation:** Translate subtitles from English to Romanian (primary), with extensibility for other languages. Use cloud-based translation via Microsoft Azure Translator for high-quality, low-latency translations.
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

This high-level specification serves as a foundation for further research and refinement. Options for implementation will be explored, focusing on quality, cost, and performance. A more detailed technical specification will follow.

---

## Implementation

The following are the major code components and logic required for the implementation:

1. **Project Skeleton**
   - C# solution using .NET MAUI targeting Android (with extensibility for other platforms if needed).
   - Entry point and basic navigation.

2. **Background Service for Screen Capture**
   - A service running in the background that periodically captures screenshots of the current screen.
   - Handles permissions for capturing the screen on both Android TV and phones.
   - Efficient scheduling of screenshot intervals.

3. **Configuration UI**
   - User interface for setting preferences:
     - Enable/disable background service
     - Snapshot frequency
     - Image pre-processing options (e.g., brightness, contrast for OCR optimization)
     - Enable/disable translation and target language selection
     - Enable/disable TTS and select voice

4. **Persistence of User Preferences**
   - Mechanism to persist user settings and preferences across app sessions and device restarts.
   - Use platform-appropriate storage APIs (e.g., Xamarin.Essentials Preferences, local SQLite, or .NET MAUI Preferences API).
   - Load preferences at startup and apply them to UI and services.

5. **Workflow Classes**
   - Logic for the main workflow:
     - Take screenshot
     - Apply image post-processing to enhance subtitle region
     - Detect changes (compare current image with previous to avoid redundant OCR)
     - If image is different, apply OCR to extract text
     - Translate extracted text if needed
     - Apply TTS to the final output

6. **Subtitle Region Detection**
   - Logic to specify or automatically detect the region of interest (ROI) for subtitles within the captured image.
   - Optionally allow user configuration for region selection in the UI.

7. **Wrappers for External Services**
   - **Tesseract OCR Wrapper:** Encapsulate interaction with Tesseract library for local OCR processing. Expose simple API for image-to-text conversion.
   - **Azure Translator Wrapper:** Encapsulate calls to Azure Translator API, handle authentication, language selection, error handling, and API limits.
   - **Azure TTS Wrapper:** Encapsulate calls to Azure Text-to-Speech API, manage voice selection, audio playback, and error conditions.
   - Abstract interface to allow easy swapping or extension to other providers.

8. **Error Handling & Logging**
   - Centralized error handling for all critical operations (screen capture, OCR, translation, TTS).
   - Logging to local files (and optionally cloud, respecting privacy) for troubleshooting and diagnostics.

9. **Permission Management & Onboarding**
   - User onboarding flow to guide initial setup, permissions requests, and accessibility service activation.
   - UI elements to request and show permission status.

10. **Accessibility and Usability**
    - Ensure all UI elements are accessible (large text, high contrast, screen reader support).
    - Provide in-app help and tooltips for visually impaired users.

11. **Resource Management**
    - Optimize resource usage to minimize battery drain and CPU load.
    - Adaptive scheduling of background operations based on usage and device state.

12. **Extensibility**
    - Design interfaces for translation, TTS, and OCR to support additional languages or providers in the future.

13. **Data Privacy**
    - Make users aware of what data is processed locally and what is sent to cloud services.
    - Ensure all transmissions to cloud APIs are secure.

14. **Feedback & Reporting**
    - Mechanism for users to report bugs, request features, and send diagnostic logs (optional).

15. **Testing & Debugging**
    - Debug UI for developers/testers to simulate and test the OCR, translation, and TTS pipeline.

---

This implementation section provides a detailed outline to guide technical design and development. Further refinement and breakdown into specific classes, services, and UI components will take place during the detailed design phase.
