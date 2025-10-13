# Quick Reference: Debug Page PNG Viewer with OCR

## For Developers

### Quick Start
1. Open Debug page in the app
2. PNG files from Downloads folder load automatically
3. Tap any PNG file to view and perform OCR
4. Extracted text appears next to the image

### Key Files
- **ViewModel**: `Subzy/ViewModels/DebugViewModel.cs`
- **View**: `Subzy/Views/DebugPage.xaml`
- **Manifest**: `Subzy/Platforms/Android/AndroidManifest.xml`

### Important Properties (DebugViewModel)
```csharp
ObservableCollection<string> PngFiles        // List of PNG file paths
string? SelectedPngFile                      // Currently selected file
ImageSource? SelectedImage                   // Image to display
string OcrResultText                         // Extracted OCR text
```

### Key Commands
```csharp
LoadPngFilesCommand         // Scans Downloads folder
OnSelectedPngFileChanged()  // Handles file selection (auto-triggered)
```

### How It Works
```
1. LoadPngFilesAsync()
   - Requests storage permissions
   - Scans Downloads folder
   - Populates PngFiles collection

2. User selects file → OnSelectedPngFileChanged()
   - Loads image: ImageSource.FromFile()
   - Calls PerformOcrOnFileAsync()

3. PerformOcrOnFileAsync()
   - Loads as Android Bitmap
   - Calls IOcrService.ExtractTextAsync()
   - Updates OcrResultText
```

### Permissions Required
```xml
READ_EXTERNAL_STORAGE (Android ≤12)
READ_MEDIA_IMAGES (Android ≥13)
```

### Error Handling
- Permission denied → Clear error message
- No files found → "No PNG files found" message
- OCR fails → Error shown in result box
- Image decode fails → Logged and displayed

### Common Issues & Solutions

**Issue**: No files appear in list
- **Solution**: Check storage permissions are granted
- **Solution**: Verify PNG files exist in Downloads folder
- **Solution**: Check Debug Output for error messages

**Issue**: OCR returns empty text
- **Solution**: Ensure image contains readable text
- **Solution**: Check image quality and contrast
- **Solution**: Verify OCR service is initialized (check Debug Output)

**Issue**: Permission request not showing
- **Solution**: Check AndroidManifest.xml has required permissions
- **Solution**: Verify app hasn't permanently denied permissions in Settings

**Issue**: Image not displaying
- **Solution**: Check file path is valid
- **Solution**: Verify file is readable PNG format
- **Solution**: Check Debug Output for loading errors

### Testing Scenarios

**Basic Flow**:
1. Open Debug page
2. Verify PNG files listed
3. Select a file
4. Verify image displays
5. Verify OCR text appears

**Permission Testing**:
1. First run → Should request permission
2. Deny → Should show error message
3. Grant later → Use Refresh button

**Error Testing**:
1. Empty Downloads → Should show "No files found"
2. Corrupted image → Should show decode error
3. Image with no text → Should show "[No text detected]"

### Extending the Feature

**Add New Image Format**:
```csharp
// In LoadPngFilesAsync()
var imageFiles = Directory.GetFiles(downloadsPath, "*.jpg", SearchOption.TopDirectoryOnly);
// Merge with PNG files
```

**Add OCR Language Selection**:
```csharp
// In PerformOcrOnFileAsync()
await _ocrService.ExtractTextAsync(bitmap, selectedLanguage);
```

**Add Image Preprocessing**:
```csharp
// Before OCR
var preprocessed = await _imageProcessor.EnhanceImageAsync(imageBytes);
var bitmap = await BitmapFactory.DecodeByteArrayAsync(preprocessed, 0, preprocessed.Length);
```

### Performance Notes
- Images loaded lazily (only when selected)
- OCR runs on-device (ML Kit) - no network required
- File scanning is async - UI remains responsive
- Bitmaps disposed after OCR to free memory

### Debug Output Location
All operations log to:
- Debug Output section (bottom of page)
- Log file (via ILoggingService)

### Useful Commands for Testing
```csharp
// In DebugViewModel, you can add:
[RelayCommand]
private async Task TestWithSampleImage()
{
    // Create test image with text
    // Select it programmatically
    // Verify OCR works
}
```

### Related Services
- **IOcrService**: Handles text extraction
- **ILoggingService**: Logs all operations
- **IImageProcessor**: Could be used for preprocessing

### UI Customization
Edit `DebugPage.xaml` to change:
- CollectionView height: `HeightRequest="120"`
- Image preview size: `HeightRequest="200"`
- Text editor size: `HeightRequest="200"`
- Colors, fonts, spacing

### Code Quality Checklist
- ✓ Async/await properly used
- ✓ Error handling comprehensive
- ✓ Logging implemented
- ✓ Memory management (Bitmap disposal)
- ✓ UI responsive (no blocking operations)
- ✓ MVVM pattern followed
- ✓ Comments on complex logic

### Integration Points
```
DebugViewModel
    ├─> IOcrService (ML Kit)
    ├─> ILoggingService
    ├─> Android.OS.Environment (Storage)
    ├─> Android.Graphics.BitmapFactory
    └─> MAUI Permissions API
```

### Build & Deploy
```bash
# Build
dotnet build Subzy/Subzy.csproj -c Debug

# Deploy to device
# (Use Visual Studio or VS Code with Android emulator/device)
```

### Troubleshooting Commands
```bash
# Check app permissions
adb shell dumpsys package com.accessibility.subzy | grep permission

# List Downloads folder contents
adb shell ls -la /storage/emulated/0/Download/*.png

# View app logs
adb logcat | grep Subzy
```

## For Testers

### What to Test
1. **File Listing**: Files appear after opening page
2. **Refresh**: Refresh button updates list
3. **Selection**: Tapping file shows image and extracts text
4. **Permissions**: First-time permission request works
5. **Errors**: Graceful handling of missing files, no permissions, etc.

### Test Images
Create test PNGs with:
- Plain text on white background
- Multi-line text
- Mixed languages
- Rotated text
- Low contrast text
- Very large images
- Very small images
- Images with no text

### Expected Behavior
- Immediate file listing on page load
- Smooth selection (no UI freeze)
- Clear error messages
- Accurate text extraction for clean images
- Debug output shows progress

### Bug Reporting
Include in bug reports:
1. Steps to reproduce
2. Expected vs actual behavior
3. Screenshot of UI
4. Debug Output text
5. Test image used (if relevant)

## Quick Command Reference
```csharp
// Refresh file list
LoadPngFilesCommand.Execute(null);

// Get current selection
var selected = SelectedPngFile;

// Check OCR result
var text = OcrResultText;

// Access file collection
var count = PngFiles.Count;
```

## Contact & Support
- See project README.md for contribution guidelines
- Check IMPLEMENTATION_SUMMARY_DEBUG_PAGE.md for detailed docs
- Review CODE_FLOW_DIAGRAM.md for architecture details
