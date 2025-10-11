# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-01-09

### Added
- Initial release of Subzy Android Accessibility Application
- Complete .NET MAUI application structure targeting Android
- Background screen capture service using MediaProjection API
- Tesseract OCR integration for subtitle text extraction
- Azure Translator API integration for translation (English to Romanian)
- Azure Speech Services integration for text-to-speech
- Comprehensive settings management with persistence
- Workflow orchestrator for complete subtitle processing pipeline
- Change detection to avoid redundant processing
- Image preprocessing capabilities (brightness, contrast adjustment)
- User-friendly onboarding flow
- Debug console for testing and diagnostics
- Centralized logging with file output and rotation
- Permission management helpers
- MVVM architecture with CommunityToolkit.Mvvm
- Accessibility-focused UI design
- Romanian localization support
- Comprehensive documentation (README, CONTRIBUTING)

### Technical Implementation
- Service interfaces for extensibility (IOcrService, ITranslationService, ITtsService)
- Dependency injection with .NET MAUI
- Settings persistence using MAUI Preferences API
- Android foreground service for continuous operation
- Value converters for XAML data binding
- Resource management and battery optimization hooks

### Documentation
- Complete README with setup instructions
- Azure API key configuration guide
- Building and deployment instructions
- Troubleshooting section
- Privacy and data handling documentation
- Contributing guidelines
- MIT License

### Known Limitations
- Tesseract trained data files must be added separately
- DRM-protected content may prevent screen capture on some devices
- Requires internet connection for translation and TTS
- OCR accuracy depends on subtitle quality

### Requirements
- Android 5.0 (API 21) or higher
- Internet connection
- Azure Cognitive Services API keys

## [Unreleased]

### Added
- Implemented service toggle functionality to start/stop the ScreenCaptureService from the main UI
- Added ScreenCapturePermissionActivity to handle MediaProjection permission flow
- Added Android-specific partial implementation of MainViewModel for platform service control

### Planned Features
- Automatic subtitle region detection
- Support for additional languages
- Offline OCR and translation options
- Battery usage statistics
- Custom subtitle styles support
- Widget for quick service toggle
- Export subtitle history
- Cloud sync for settings

---

For more details, see the [README](README.md) and [project specification](Specs/project.spec).
