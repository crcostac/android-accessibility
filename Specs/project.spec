# Project Specification: Android Subtitle Reader & Translator

## Project Intent

The purpose of this project is to develop an Android application, primarily designed for Android TV but ideally compatible with Android phones as well, to enhance accessibility for users with low vision and other print disabilities by capturing, translating, and speaking aloud on-screen subtitles.

## Core Features

- **Multi-application Screen Capture Support:** Work across various streaming apps (Netflix, HBO, Amazon Prime, etc.) regardless of their accessibility support by reading pixels from the screen.
- **Subtitle Reading:** Use Xamarin.Tesseract OCR (Optical Character Recognition) to capture and read subtitles directly from the screen image, independent of app accessibility APIs.
- **Translation:** Translate subtitles from English to Romanian (primary), with extensibility for other languages. Use cloud-based translation via Microsoft Azure Translator for high-quality, low-latency translations.
- **Text-to-Speech:** Speak subtitles aloud using Romanian as the primary voice. Use cloud-based neural TTS via Microsoft Azure for expressive, natural speech inflexion.
- **Permissions:** Accept the need for full administrator or accessibility permissions to capture the displayed image on the device.
- **Development Framework:** Use C# with .NET MAUI for cross-platform Android development.
- **Extensibility:** Design interfaces for translation, TTS, and OCR to support additional languages or providers in the future. Abstract interface to allow easy swapping or extension to other providers for OCR, TTS and Translation.
- **Performance:** Image capture and preprocessing must be highly optimized. Ensure no unnecessary bitmap conversions, avoid GetPixel/SetPixel calls for each pixel. Use efficient algorithms for color filtering and noise removal.

## Per-component Specification

### Screen Capture
- Configurable snapshot frequency (e.g., every 0.5-5 seconds).
- Efficient background service to minimize battery and CPU usage.
- Handle permissions for screen capture with minimal user intervention (ideally ask once during installation).
- Configurable: log captured images for debugging purposes.

### Image Preprocessing
- Interactive color picker to allow users to select subtitle colors from the screen.
- Per-app color profiles to automatically switch subtitle colors based on the foreground app (eg Netflix app package name).
- Blank out pixels not matching subtitle colors to improve OCR accuracy.
- Optionally also blank out pixels that don't have sufficient neighbours of the same color (to remove noise).
- Use perceptual hashing (dHash) to detect if the pre-processed screen content has changed significantly since the last capture, skipping OCR if unchanged.

### OCR
- Must be done on-device using Xamarin.Tesseract or similar library.
- Must support Romanian characters and diacritics.
- Do not continue to translate if OCR returns same text as last time.

### Translation
- Use Microsoft Azure Translator for high-quality translations.
- Configurable target language (default Romanian).
- Detect if the current language is already the target language, and skip translation if so.
- Evaluate: can we include previous context (eg last N lines within X seconds) to improve translation quality, but without including old text in the output?

### Text-to-Speech
- Use Microsoft Azure Neural TTS for natural-sounding speech.
- Configurable voice (default Romanian).
- Queue text to avoid overlapping speech.
- Optionally include short pauses between sentences for better clarity.
- Evaluate: can we include previous context (eg last N lines within X seconds) to improve speech naturalness, but without including old text in the output?
- Evaluate: can we detect if OCR has returned garbled text (eg random characters) and skip TTS in that case?
- Evaluate: can we just send the text to a LLM (eg GPT-4) to perform translation, normalization, inclusion of previous context, and TTS in one step?

### Testing & Debugging
- Debug UI for developers/testers to simulate and test the OCR, translation, and TTS pipeline.
- Logging to local files (and optionally cloud, respecting privacy) for troubleshooting and diagnostics.
- UI for OCR testing will show: list of captured images, UI element to show selected image, UI element to show the results of the OCR on the selected image. 
- UI for Translation testing will show: text box to enter text, button to translate, text box to show translated text.
- UI for TTS testing will show: text box to enter text, button to speak, status indicator for speaking.
- UI for logging will show log entries and button to clear logs.

