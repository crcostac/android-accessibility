# Copilot Instructions for android-accessibility (Subzy)

## Project Overview

**Subzy** is an Android accessibility application designed to enhance the viewing experience for users with low vision and print disabilities. It captures, translates, and speaks aloud on-screen subtitles in real-time from any streaming application.

**Key Features:**
- Real-time subtitle capture using screen recording
- OCR processing with Google ML Kit Text Recognition v2
- Translation via Azure Cognitive Services (Translator API)
- Text-to-speech using Azure Neural TTS
- Background service for continuous operation
- Configurable per-app color profiles for optimal subtitle detection

## Technology Stack

- **Framework**: .NET 9.0 MAUI targeting Android (API 21+)
- **Language**: C# 12
- **Architecture**: MVVM pattern
- **OCR**: Google ML Kit Text Recognition v2 (on-device)
- **Translation**: Azure Cognitive Services Translator API
- **TTS**: Azure Cognitive Services Speech API (Neural voices)
- **Storage**: MAUI Preferences API and JSON serialization
- **UI**: XAML with data binding

## Project Structure

```
Subzy/
├── Models/                  # Data models and domain entities
├── Services/                # Business logic and external integrations
│   └── Interfaces/          # Service abstractions for testability
├── ViewModels/              # MVVM presentation logic
├── Views/                   # XAML UI with data binding
├── Helpers/                 # Utility classes and converters
├── Platforms/Android/       # Android-specific implementations
│   └── Services/            # Platform services (screen capture, etc.)
└── Resources/               # Assets, strings, and styles
```

## High-Level Specification

**IMPORTANT**: Always refer to `Specs/project.spec` for a comprehensive high-level description of this project, including:
- Overall architecture and design principles
- 5-stage optimized OCR workflow
- Per-app color profile system
- Performance optimization strategies
- Implementation requirements (15 major components)
- Accessibility and usability guidelines

## Coding Standards

### C# Conventions
- Follow standard C# coding conventions and naming guidelines
- Use meaningful, descriptive names for variables, methods, and classes
- Add XML documentation comments (`///`) for all public APIs
- Keep methods focused and concise (single responsibility principle)
- Use `async`/`await` for asynchronous operations
- Implement proper exception handling with logging

### MVVM Pattern
- Use `CommunityToolkit.Mvvm` for MVVM implementation
- ViewModels should inherit from `ObservableObject`
- Use `[ObservableProperty]` for bindable properties
- Use `[RelayCommand]` for command methods
- Keep ViewModels testable (no direct UI dependencies)
- Views should only contain XAML and minimal code-behind

### Architecture Guidelines

#### Models
- Plain C# classes representing data structures
- No business logic (data transfer objects)
- Implement `INotifyPropertyChanged` only when needed for binding
- Use properties with validation where appropriate

#### Services
- Implement interfaces for all services (found in `Services/Interfaces/`)
- Abstract external dependencies (OCR, translation, TTS) behind interfaces
- Centralize error handling and logging
- Support dependency injection
- Keep services stateless where possible

#### ViewModels
- Handle presentation logic and user interactions
- Coordinate between services and views
- Implement command patterns for user actions
- Use the `ILoggingService` for logging
- Access settings via `SettingsService`

#### Views
- Use XAML for UI definitions
- Leverage data binding to ViewModels
- Minimize code-behind (only platform-specific UI code)
- Ensure accessibility (large touch targets, screen reader support)

#### Helpers
- Utility classes and extension methods
- Value converters for XAML binding
- Constants (see `Helpers/Constants.cs`)
- Permission management utilities

### Dependency Injection
- Register all services in `MauiProgram.cs`
- Use constructor injection
- Prefer interfaces over concrete types in constructors

### Error Handling & Logging
- Use the centralized `ILoggingService` for all logging
- Log levels: Debug, Info, Warning, Error
- Always log exceptions with context
- Handle exceptions gracefully with user-friendly messages
- Avoid silent failures

### Resource Management
- Dispose of resources properly (implement `IDisposable` where needed)
- Be mindful of battery consumption
- Implement adaptive scheduling based on device state
- Use efficient image processing algorithms

## Testing Guidelines

### Test Coverage
- Test on Android API levels 21+ (Android 5.0 and higher)
- Test on both phones and Android TV devices
- Verify functionality with multiple streaming applications
- Test with different subtitle styles and colors
- Check battery impact and performance metrics

### Manual Testing
- Verify screen capture works correctly
- Test OCR accuracy with various subtitle fonts
- Confirm translation and TTS work end-to-end
- Check accessibility features (screen reader, large text)
- Test permission flows and onboarding

### Debug Features
- Use the Debug page (`Views/DebugPage.xaml`) for testing
- Access logs via `ILoggingService.GetLogFilePath()`
- Test individual components (OCR, translation, TTS) independently

## Important Considerations

### Performance
- The 5-stage OCR workflow includes perceptual hashing to avoid redundant OCR operations
- Per-app color profiles optimize subtitle detection
- Adaptive processing adjusts based on foreground app
- Target: ~90% reduction in OCR operations through smart caching

### Accessibility
- All UI elements must be accessible (large text, high contrast)
- Support screen readers
- Provide in-app help and tooltips
- Follow Android accessibility guidelines

### Privacy & Security
- Screenshots are processed locally and not permanently stored
- Only extracted text is sent to Azure APIs
- All cloud communications use HTTPS
- Inform users about data processing in privacy policy
- Azure API keys must be stored securely

### Permissions
Required Android permissions:
- `INTERNET` - For cloud services
- `FOREGROUND_SERVICE` - For background operation
- `RECORD_AUDIO` - For TTS output
- `POST_NOTIFICATIONS` - For service status
- Media Projection - For screen capture (requested at runtime)
- Usage Stats - For foreground app detection (per-app profiles)

### Extensibility
- Design interfaces to support additional OCR providers
- Support multiple translation services and languages
- Allow for different TTS providers and voices
- Use strategy pattern for swappable components

## Common Tasks

### Adding a New Service
1. Create interface in `Services/Interfaces/`
2. Implement service class in `Services/`
3. Add XML documentation
4. Register in `MauiProgram.cs` DI container
5. Inject into ViewModels as needed
6. Add error handling and logging

### Adding a New Settings Property
1. Add property to `Models/AppSettings.cs`
2. Update `SettingsService.LoadSettings()` and `SaveSettings()`
3. Add UI controls in `Views/SettingsPage.xaml`
4. Bind to `SettingsViewModel` properties
5. Add constants to `Helpers/Constants.cs` if needed
6. Update documentation

### Adding a New Page
1. Create XAML view in `Views/`
2. Create ViewModel in `ViewModels/`
3. Register route in `AppShell.xaml`
4. Register ViewModel in `MauiProgram.cs` DI
5. Add navigation commands to calling ViewModel
6. Implement accessibility features

## Documentation

### Keep Updated
- `README.md` - User-facing documentation
- `CONTRIBUTING.md` - Contributor guidelines
- `IMPLEMENTATION_SUMMARY.md` - Technical implementation details
- `CHANGELOG.md` - Version history and changes
- `Specs/project.spec` - High-level specification

### XML Documentation
Add XML comments to all public APIs:
```csharp
/// <summary>
/// Brief description of what the method does.
/// </summary>
/// <param name="paramName">Description of parameter</param>
/// <returns>Description of return value</returns>
```

## Build & Development

### Prerequisites
- .NET 9.0 SDK
- .NET MAUI workload: `dotnet workload install maui-android`
- Android SDK

### Build Commands
```bash
# Restore dependencies
dotnet restore

# Build for debugging
dotnet build -c Debug -f net9.0-android

# Build for release
dotnet build -c Release -f net9.0-android

# Deploy to connected device
dotnet build -c Debug -f net9.0-android -t:Run
```

### Troubleshooting
- Refer to `BUILD_NOTES.md` for build issues
- Check `ANDROIDX_FIX.md` for AndroidX compatibility
- Review logs in the Debug page within the app
- Check Azure API connectivity from Debug page

## References

- **Project Specification**: `Specs/project.spec` - **Read this first for comprehensive project understanding**
- **Contributing Guide**: `CONTRIBUTING.md`
- **Implementation Details**: `IMPLEMENTATION_SUMMARY.md`
- **Azure Setup**: `AZURE_SETUP.md`
- **.NET MAUI Docs**: https://learn.microsoft.com/en-us/dotnet/maui/
- **ML Kit Text Recognition**: https://developers.google.com/ml-kit/vision/text-recognition/v2
- **Azure Translator**: https://learn.microsoft.com/en-us/azure/cognitive-services/translator/
- **Azure Speech**: https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/

## Questions or Issues?

- Create a GitHub issue for bugs or feature requests
- Include device information, logs, and steps to reproduce
- Reference relevant specification sections when applicable
- Tag issues appropriately (bug, enhancement, documentation, etc.)
