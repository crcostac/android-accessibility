# Tesseract4Android Integration Guide

This document describes the integration of Tesseract4Android as the OCR engine for Android.

## Overview

The previous implementation used the Charles Weld Tesseract .NET wrapper, which threw "Unsupported operation system" errors on Android because it lacked native library support for the platform. This integration replaces it with Tesseract4AndroidOcrService, a native Android OCR service designed to use Tesseract4Android bindings.

## Architecture

The application now uses `Tesseract4AndroidOcrService` exclusively for OCR functionality:

- **OCR Service**: `Tesseract4AndroidOcrService` located in `Platforms/Android/Services/`
- **Package Dependencies**: The Tesseract NuGet package has been removed from the project
- **Service Registration**: `Tesseract4AndroidOcrService` is registered directly in `MauiProgram.cs`

## Project Configuration

The following changes have been made:

1. **Subzy.csproj**:
   - Removed the Tesseract NuGet package reference
   - No conditional compilation properties needed

2. **MauiProgram.cs**:
   - Direct service registration: `builder.Services.AddSingleton<IOcrService, Subzy.Platforms.Android.Services.Tesseract4AndroidOcrService>();`
   - No conditional compilation needed

3. **TesseractOcrService.cs**:
   - Removed from the project as it is no longer needed

4. **Tesseract4AndroidOcrService.cs**:
   - Located in `Platforms/Android/Services/`
   - Implements `IOcrService` interface
   - No conditional compilation needed

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
   - Reference the binding library in Subzy.csproj

2. **Update Tesseract4AndroidOcrService**:
   - Replace TODO comments with actual Tesseract4Android API calls
   - Test initialization and text extraction
   - Handle Android-specific exceptions

3. **Add Traineddata Assets**:
   - Include `eng.traineddata` and other language files in the `Assets/tessdata` folder
   - Implement asset copying logic in `InitializeAsync`

4. **Testing**:
   - Verify OCR functionality on Android devices
   - Test performance and accuracy

## Benefits

- **Native Performance**: Tesseract4Android provides optimized native performance on Android
- **Better Support**: Actively maintained library specifically designed for Android
- **Simplified Architecture**: Single OCR implementation without conditional compilation
- **Future-Proof**: Ready to add the AAR binding when available

## File Structure

```
Subzy/
├── Services/
│   └── Interfaces/
│       └── IOcrService.cs
├── Platforms/
│   └── Android/
│       └── Services/
│           └── Tesseract4AndroidOcrService.cs
├── MauiProgram.cs (direct registration)
└── Subzy.csproj (no Tesseract NuGet package)
```

## Error Handling

The implementation gracefully handles missing Tesseract4Android bindings:

- `TypeLoadException`: Caught when binding types are not available
- `Java.Lang.Throwable`: Caught for Android/Java-specific errors
- Returns user-friendly error messages instead of crashing
- Sets `IsInitialized = false` when bindings are unavailable

## Troubleshooting

**Q: OCR returns "[OCR not available - Tesseract4Android binding not yet added]"**  
A: The Tesseract4Android AAR binding hasn't been integrated yet. This message indicates the service is working correctly but waiting for the actual binding library to be added.

## References

- [Tesseract4Android GitHub](https://github.com/adaptech-cz/Tesseract4Android)
- [Xamarin Android Bindings](https://learn.microsoft.com/en-us/xamarin/android/platform/binding-java-library/)
- [.NET MAUI Android Platform Code](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/)
