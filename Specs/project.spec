# Project Specification

## 1. Introduction
... (existing content)

## 5. Optimized OCR Workflow
The new 5-stage OCR workflow includes:
- **Stage 1**: Image Acquisition
- **Stage 2**: Preprocessing with per-app color profiles
- **Stage 3**: Text Recognition with foreground app detection
- **Stage 4**: Post-Processing using perceptual hashing for change detection
- **Stage 5**: Output Generation

This optimized pipeline enhances efficiency and accuracy.

## 6. Performance Optimization
The performance of the OCR system has been optimized through several strategies:
- **Adaptive Processing**: Tailors processing based on the foreground app.
- **Caching Mechanisms**: Reduces repeated calculations for the same images.

## 7. Per-App Color Profiles
The per-app color profile system allows for customized processing based on the application's color scheme, improving text recognition accuracy.

## 8. Interactive Color Picker
An interactive color picker will be integrated to allow users to select and adjust color profiles for their specific needs.

## 9. Performance Strategy
We will implement performance monitoring to continually assess and optimize the OCR processing times.

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

5. **Optimized 5-Stage OCR Workflow**
   - **Stage 1: Detect Foreground App (~1ms)** - Uses UsageStatsManager to detect active app and load associated color profile
   - **Stage 2: Color Filter + Noise Removal (~20-25ms)** - Single-pass algorithm that filters subtitle colors and removes noise based on neighbor analysis
   - **Stage 3: Perceptual Hashing (~10ms)** - Uses dHash algorithm to detect changes; skips OCR if Hamming distance < threshold
   - **Stage 4: Run OCR (~200-500ms)** - Only executed if hash indicates content changed; runs Tesseract on filtered image
   - **Stage 5: Translation & TTS** - Existing logic for Azure Translator and Neural TTS
   - **Performance:** Expected ~90% reduction in OCR operations (36ms vs 236-536ms per frame)

6. **Per-App Color Profile System**
   - **Color Profile Manager** - Manages dictionary of app package → color profile mappings
   - **Foreground App Detection** - Uses UsageStatsManager API to detect currently active streaming app
   - **MRU Color List** - Each app maintains up to 5 subtitle colors in Most Recently Used order
   - **Interactive Color Picker** - Semi-transparent overlay with tap-to-pick functionality using histogram analysis
   - **Automatic Profile Switching** - System automatically switches profiles when user changes apps
   - **Profile Persistence** - Color profiles saved to JSON storage via SettingsService

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
