# Debug Page Enhancements - PNG File Viewer with OCR

## Overview
Enhanced the DebugViewModel and DebugPage to add a PNG file viewer with integrated OCR capabilities, allowing users to browse PNG files from the Downloads folder and extract text from them using ML Kit OCR.

## Features Implemented

### 1. PNG File Listing
- **Location**: Scans the Android Downloads folder (`Environment.ExternalStorageDirectory/Download`)
- **UI Component**: CollectionView displaying all PNG files in the Downloads folder
- **Refresh Button**: Allows users to manually refresh the file list
- **Auto-load**: Files are automatically loaded when the Debug page is initialized

### 2. Image Selection and Display
- **Interactive Selection**: Users can tap on any PNG file in the list to select it
- **Image Preview**: Selected image is displayed in a preview pane with proper aspect ratio
- **Visual Feedback**: Selected file is highlighted in the list

### 3. OCR Text Extraction
- **Automatic Processing**: When a PNG file is selected, OCR is automatically performed
- **ML Kit Integration**: Uses Google ML Kit Text Recognition v2 for on-device OCR
- **Results Display**: Extracted text is shown in a scrollable text editor next to the image
- **Debug Output**: Detailed processing steps are logged in the debug console

### 4. Android Permissions
- **Storage Permissions**: Automatically requests READ_EXTERNAL_STORAGE and READ_MEDIA_IMAGES permissions
- **Permission Handling**: Graceful handling of permission denials with user feedback
- **Android 13+ Support**: Uses modern storage access APIs compatible with Android 13+

## Technical Implementation

### ViewModel Changes (DebugViewModel.cs)
```csharp
// New Properties
- ObservableCollection<string> PngFiles: List of PNG file paths
- string? SelectedPngFile: Currently selected file path
- ImageSource? SelectedImage: Image source for display
- string OcrResultText: Extracted OCR text

// New Commands
- LoadPngFilesCommand: Scans Downloads folder for PNG files
- OnSelectedPngFileChanged: Handles file selection and triggers OCR

// New Methods
- LoadPngFilesAsync(): Loads PNG files with permission checking
- LoadAndProcessSelectedImageAsync(): Loads and displays selected image
- PerformOcrOnFileAsync(): Performs OCR on the selected image file
```

### UI Changes (DebugPage.xaml)
```xml
- Added Row 1: PNG Files from Downloads section with CollectionView and Refresh button
- Added Row 2: Split view with Selected Image (left) and OCR Result (right)
- Adjusted existing rows to accommodate new features
```

### Android Manifest Updates
```xml
- Added: READ_EXTERNAL_STORAGE permission (for Android 12 and below)
- Added: READ_MEDIA_IMAGES permission (for Android 13+)
```

## Usage Instructions

1. **Navigate to Debug Page**: Open the Debug page from the app navigation
2. **Grant Permissions**: If prompted, grant storage access permissions
3. **View PNG Files**: The Downloads folder is automatically scanned for PNG files
4. **Select a File**: Tap on any PNG file in the list
5. **View Results**: 
   - The selected image appears in the "Selected Image" section
   - OCR is automatically performed
   - Extracted text appears in the "OCR Result" section
6. **Refresh**: Use the "Refresh" button to rescan the Downloads folder

## Debug Console Output
The debug console at the bottom provides detailed feedback:
- Permission request status
- Downloads folder path
- Number of PNG files found
- Selected file processing steps
- OCR initialization and results
- Any errors encountered

## Error Handling
- Permission denials are handled gracefully with informative messages
- Missing Downloads folder is reported to the user
- OCR failures are caught and displayed in both the result box and debug console
- Image decoding failures are logged and reported

## Platform Support
- **Android**: Full support with ML Kit OCR
- **Other Platforms**: Limited support (file listing works, OCR shows "not available" message)

## Dependencies
- Google ML Kit Text Recognition v2 (via Xamarin.GooglePlayServices.MLKit.Text.Recognition)
- Android BitmapFactory for image decoding
- MAUI Permissions API for runtime permission handling

## Future Enhancements
Possible improvements for future iterations:
- Support for other image formats (JPG, WEBP)
- Batch OCR processing for multiple images
- Copy/export OCR results functionality
- OCR language selection
- Image preprocessing options (contrast, brightness)
- Save OCR results to file
- Integration with translation and TTS services
