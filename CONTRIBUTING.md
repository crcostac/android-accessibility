# Contributing to Subzy

Thank you for your interest in contributing to Subzy! This document provides guidelines and instructions for contributing to the project.

## Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment for all contributors.

## How to Contribute

### Reporting Bugs

If you find a bug, please create an issue on GitHub with:
- A clear, descriptive title
- Steps to reproduce the issue
- Expected behavior
- Actual behavior
- Device information (Android version, device model)
- Relevant logs from the debug console

### Suggesting Enhancements

Feature requests are welcome! Please create an issue with:
- A clear description of the feature
- Use cases and benefits
- Any implementation ideas you have

### Pull Requests

1. **Fork the repository** and create your branch from `main`
2. **Make your changes** following the coding standards
3. **Test your changes** thoroughly on Android devices
4. **Update documentation** if needed
5. **Commit your changes** with clear commit messages
6. **Push to your fork** and submit a pull request

### Development Setup

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/android-accessibility.git
cd android-accessibility/Subzy

# Install .NET MAUI workload
dotnet workload install maui-android

# Restore dependencies
dotnet restore

# Build the project
dotnet build -c Debug -f net9.0-android

# Run on connected device
dotnet build -c Debug -f net9.0-android -t:Run
```

### Coding Standards

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and concise
- Use MVVM pattern for UI code
- Implement interfaces for testability

### Architecture Guidelines

- **Models**: Data structures and domain models
- **Services**: Business logic and external integrations
- **ViewModels**: Presentation logic using MVVM pattern
- **Views**: XAML UI with data binding
- **Helpers**: Utility classes and converters

### Testing

- Test on multiple Android versions (API 21+)
- Test on both phones and Android TV
- Verify screen capture works properly
- Test with different streaming applications
- Check battery impact and performance
- Verify accessibility features

### Areas for Contribution

We welcome contributions in these areas:

1. **OCR Improvements**
   - Better image preprocessing
   - Support for additional languages
   - Improved accuracy

2. **Translation**
   - Additional language support
   - Offline translation options
   - Translation quality improvements

3. **UI/UX**
   - Better accessibility features
   - Dark mode support
   - Improved onboarding flow
   - Better error messages

4. **Performance**
   - Battery optimization
   - Reduced CPU usage
   - Faster processing pipeline
   - Memory management

5. **Documentation**
   - User guides
   - Developer documentation
   - Video tutorials
   - Translations

6. **Testing**
   - Unit tests
   - Integration tests
   - UI tests
   - Performance benchmarks

### Commit Message Guidelines

Use clear and descriptive commit messages:

```
Add Romanian TTS voice selection

- Implement voice picker in settings
- Add Romanian neural voices
- Update settings service to persist voice selection
```

### License

By contributing to Subzy, you agree that your contributions will be licensed under the MIT License.

## Questions?

Feel free to create an issue if you have questions about contributing!
