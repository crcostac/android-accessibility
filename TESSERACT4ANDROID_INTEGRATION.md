# Tesseract4Android Integration Guide

This document describes the scaffolding for integrating Tesseract4Android as an alternative OCR engine for Android.

## Overview

The current implementation uses the Charles Weld Tesseract .NET wrapper, which throws "Unsupported operation system" errors on Android because it lacks native library support for the platform. This integration scaffolds support for Tesseract4Android, a native Android OCR library, while maintaining backward compatibility with the existing implementation.

## Architecture

The integration uses conditional compilation to switch between OCR implementations:

- **Default (USE_TESSERACT4ANDROID = false)**: Uses the existing `TesseractOcrService` with the Tesseract NuGet package
- **Android Native (USE_TESSERACT4ANDROID = true)**: Uses the new `Tesseract4AndroidOcrService` with Tesseract4Android bindings

## Configuration

### Enabling Tesseract4Android

To enable Tesseract4Android support, set the MSBuild property in your build:

```bash
dotnet build -p:UseTesseract4Android=true
```

Or edit `Subzy.csproj` and change:

```xml
<UseTesseract4Android>false</UseTesseract4Android>
```

to:

```xml
<UseTesseract4Android>true</UseTesseract4Android>
```

### Project Configuration

The following changes have been made to support conditional compilation:

1. **Subzy.csproj**:
   - Added `UseTesseract4Android` property (default: `false`)
   - Conditional `DefineConstants` for `USE_TESSERACT4ANDROID` symbol
   - Conditional package reference for the Tesseract package (excluded when using Tesseract4Android)

2. **MauiProgram.cs**:
   - Conditional service registration based on `USE_TESSERACT4ANDROID` symbol
   - Registers `Tesseract4AndroidOcrService` when enabled, otherwise `TesseractOcrService`

3. **TesseractOcrService.cs**:
   - Wrapped with `#if !USE_TESSERACT4ANDROID` to exclude when using Android native implementation

4. **Tesseract4AndroidOcrService.cs** (NEW):
   - Located in `Platforms/Android/Services/`
   - Implements `IOcrService` interface
   - Conditional compilation with `#if ANDROID && USE_TESSERACT4ANDROID`

## Implementation Status

### ✅ Completed

- [x] Build system configuration with MSBuild property
- [x] Conditional compilation symbols
- [x] Service scaffolding with IOcrService interface
- [x] Tessdata directory management
- [x] Error handling for missing binding types
- [x] Graceful fallback messaging

### 🚧 Pending (Requires Tesseract4Android AAR Binding)

The following features are scaffolded with TODO comments and require the actual Tesseract4Android binding to be added:

1. **Engine Initialization**:
   ```csharp
   // TODO: Initialize Tesseract4Android engine when binding is available
   // _tessBaseAPI = new TessBaseAPI();
   // var initResult = _tessBaseAPI.Init(tessDataPath, "eng");
   ```

2. **Text Extraction**:
   ```csharp
   // TODO: Implement actual OCR extraction
   // using var bitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
   // _tessBaseAPI?.SetImage(bitmap);
   // var text = _tessBaseAPI?.GetUTF8Text();
   ```

3. **Resource Cleanup**:
   ```csharp
   // TODO: Dispose Tesseract4Android resources
   // _tessBaseAPI?.End();
   // _tessBaseAPI?.Recycle();
   ```

4. **Traineddata File Copying**:
   ```csharp
   // TODO: Copy traineddata from assets to tessdata directory
   // using var assetStream = Android.App.Application.Context.Assets?.Open("tessdata/eng.traineddata");
   // await assetStream.CopyToAsync(fileStream);
   ```

## Next Steps

To complete the Tesseract4Android integration:

1. **Add Tesseract4Android AAR Binding**:
   - Create an Android Bindings Library project
   - Add the Tesseract4Android AAR file
   - Generate C# bindings
   - Reference the binding library in Subzy.csproj when `UseTesseract4Android` is true

2. **Update Tesseract4AndroidOcrService**:
   - Replace TODO comments with actual Tesseract4Android API calls
   - Test initialization and text extraction
   - Handle Android-specific exceptions

3. **Add Traineddata Assets**:
   - Include `eng.traineddata` and other language files in the `Assets/tessdata` folder
   - Implement asset copying logic in `InitializeAsync`

4. **Testing**:
   - Test with both `UseTesseract4Android=false` (default) and `UseTesseract4Android=true`
   - Verify OCR functionality on Android devices
   - Compare performance and accuracy between implementations

## Benefits

- **Native Performance**: Tesseract4Android provides optimized native performance on Android
- **Better Support**: Actively maintained library specifically designed for Android
- **Backward Compatibility**: Existing implementation remains functional as fallback
- **No Breaking Changes**: Default behavior unchanged; opt-in via build flag
- **Future-Proof**: Easy to add the AAR binding when ready without disrupting current builds

## File Structure

```
Subzy/
├── Services/
│   ├── Interfaces/
│   │   └── IOcrService.cs
│   └── TesseractOcrService.cs (#if !USE_TESSERACT4ANDROID)
├── Platforms/
│   └── Android/
│       └── Services/
│           └── Tesseract4AndroidOcrService.cs (#if ANDROID && USE_TESSERACT4ANDROID)
├── MauiProgram.cs (conditional registration)
└── Subzy.csproj (UseTesseract4Android property)
```

## Error Handling

The implementation gracefully handles missing Tesseract4Android bindings:

- `TypeLoadException`: Caught when binding types are not available
- `Java.Lang.Throwable`: Caught for Android/Java-specific errors
- Returns user-friendly error messages instead of crashing
- Sets `IsInitialized = false` when bindings are unavailable

## Troubleshooting

**Q: Build fails when `UseTesseract4Android=true`**  
A: This is expected if the Tesseract4Android AAR binding hasn't been added yet. The code is scaffolded with TODOs but won't compile fully without the actual binding library.

**Q: OCR returns "[OCR not available - Tesseract4Android binding not yet added]"**  
A: The binding hasn't been integrated yet. This message indicates the scaffolding is working correctly.

**Q: How do I revert to the old OCR implementation?**  
A: Simply build with `UseTesseract4Android=false` (the default) or ensure the property is not set.

## References

- [Tesseract4Android GitHub](https://github.com/adaptech-cz/Tesseract4Android)
- [Xamarin Android Bindings](https://learn.microsoft.com/en-us/xamarin/android/platform/binding-java-library/)
- [.NET MAUI Android Platform Code](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/)
