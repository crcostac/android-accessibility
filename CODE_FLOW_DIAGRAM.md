# Code Flow Diagram: PNG Viewer with OCR

## Component Interaction

```
┌─────────────────────────────────────────────────────────────────┐
│                         DebugPage.xaml                          │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ CollectionView                                            │ │
│  │ ItemsSource={Binding PngFiles}                           │ │
│  │ SelectedItem={Binding SelectedPngFile}                   │ │
│  └───────────────────────────────────────────────────────────┘ │
│                           │                                     │
│                           │ User Selection                      │
│                           ▼                                     │
│  ┌─────────────────────┐     ┌─────────────────────┐          │
│  │ Image               │     │ Editor              │          │
│  │ Source={Binding     │     │ Text={Binding       │          │
│  │   SelectedImage}    │     │   OcrResultText}    │          │
│  └─────────────────────┘     └─────────────────────┘          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                               │
                               │ Data Binding
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                      DebugViewModel.cs                          │
│                                                                 │
│  Properties:                                                    │
│  ┌────────────────────────────────────────────────────────┐   │
│  │ ObservableCollection<string> PngFiles                  │   │
│  │ string? SelectedPngFile  ◄──┐                         │   │
│  │ ImageSource? SelectedImage    │                         │   │
│  │ string OcrResultText          │                         │   │
│  └────────────────────────────────────────────────────────┘   │
│                                    │                            │
│  Commands & Methods:               │ Property Changed           │
│  ┌────────────────────────────────┼──────────────────────────┐ │
│  │ LoadPngFilesCommand            │                          │ │
│  │   └─> LoadPngFilesAsync()      │                          │ │
│  │                                 │                          │ │
│  │ OnSelectedPngFileChanged() ◄───┘                          │ │
│  │   └─> LoadAndProcessSelectedImageAsync()                 │ │
│  │         └─> PerformOcrOnFileAsync()                       │ │
│  └──────────────────────────────────────────────────────────┘ │
│                               │                                 │
└───────────────────────────────┼─────────────────────────────────┘
                                │
                                │ Service Calls
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Platform & Services Layer                     │
│                                                                 │
│  ┌─────────────────────┐  ┌────────────────────────────────┐  │
│  │ Android Storage API │  │ IOcrService                     │  │
│  │                     │  │  (MlKitOcrService)             │  │
│  │ - Environment.      │  │                                │  │
│  │   GetExternal       │  │ - InitializeAsync()            │  │
│  │   StoragePublic     │  │ - ExtractTextAsync(Bitmap)     │  │
│  │   Directory()       │  │                                │  │
│  │                     │  │                                │  │
│  │ - Directory.        │  │ Uses: ML Kit Text Recognition  │  │
│  │   GetFiles()        │  │                                │  │
│  │                     │  │                                │  │
│  │ - BitmapFactory.    │  │                                │  │
│  │   DecodeFileAsync() │  │                                │  │
│  └─────────────────────┘  └────────────────────────────────┘  │
│                                                                 │
│  ┌─────────────────────┐  ┌────────────────────────────────┐  │
│  │ MAUI Permissions    │  │ ILoggingService                │  │
│  │                     │  │                                │  │
│  │ - CheckStatusAsync  │  │ - Info(), Debug()              │  │
│  │ - RequestAsync      │  │ - Error()                      │  │
│  └─────────────────────┘  └────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Detailed Flow Sequence

### Initialization Flow

```
App Start
    │
    ├─> Navigate to Debug Page
    │       │
    │       └─> DebugViewModel Constructor
    │               │
    │               ├─> LoadSystemInfo()
    │               │
    │               └─> LoadPngFilesAsync()
    │                       │
    │                       ├─> Check Storage Permission
    │                       │   ├─ Granted? Continue
    │                       │   └─ Denied? Request Permission
    │                       │
    │                       ├─> Get Downloads Path
    │                       │   Android.OS.Environment.
    │                       │   GetExternalStoragePublicDirectory()
    │                       │
    │                       ├─> Scan for PNG Files
    │                       │   Directory.GetFiles("*.png")
    │                       │
    │                       └─> Populate PngFiles Collection
    │                               │
    │                               └─> UI Updates via Data Binding
    │
    └─> Debug Page Displayed
```

### Image Selection Flow

```
User Taps PNG File in List
    │
    └─> SelectedPngFile Property Changed
            │
            └─> OnSelectedPngFileChanged() Triggered
                    │
                    ├─> LoadAndProcessSelectedImageAsync()
                    │       │
                    │       ├─> Load Image for Display
                    │       │   ImageSource.FromFile(filePath)
                    │       │       │
                    │       │       └─> SelectedImage Updated
                    │       │               │
                    │       │               └─> UI Shows Image
                    │       │
                    │       └─> PerformOcrOnFileAsync()
                    │               │
                    │               ├─> Check OCR Service Status
                    │               │   If Not Initialized:
                    │               │   └─> InitializeAsync()
                    │               │
                    │               ├─> Load Image as Bitmap
                    │               │   BitmapFactory.DecodeFileAsync()
                    │               │
                    │               ├─> Perform OCR
                    │               │   _ocrService.ExtractTextAsync(bitmap)
                    │               │       │
                    │               │       └─> ML Kit Processing
                    │               │           (On-Device)
                    │               │
                    │               └─> Update OcrResultText
                    │                       │
                    │                       └─> UI Shows Extracted Text
                    │
                    └─> Log Progress to Debug Output
```

### Permission Request Flow

```
LoadPngFilesAsync() Called
    │
    └─> Permissions.CheckStatusAsync<StorageRead>()
            │
            ├─ Already Granted
            │       │
            │       └─> Continue to File Loading
            │
            └─ Not Granted
                    │
                    └─> Permissions.RequestAsync<StorageRead>()
                            │
                            ├─ User Grants
                            │       │
                            │       └─> Continue to File Loading
                            │
                            └─ User Denies
                                    │
                                    └─> Show Error Message
                                        Return Early
```

### Error Handling Flow

```
Any Operation
    │
    └─> try {
            │
            └─> Perform Operation
                    │
                    ├─ Success
                    │       │
                    │       └─> Update UI
                    │           Log Success
                    │
                    └─ Exception
                            │
                            └─> catch (Exception ex) {
                                    │
                                    ├─> Log Error (ILoggingService)
                                    │
                                    ├─> Update UI with Error Message
                                    │   (OcrResultText or Debug Output)
                                    │
                                    └─> User Sees Friendly Message
                                }
```

## Key Integration Points

### 1. Data Binding (XAML ↔ ViewModel)
- **PngFiles** → CollectionView.ItemsSource
- **SelectedPngFile** → CollectionView.SelectedItem
- **SelectedImage** → Image.Source
- **OcrResultText** → Editor.Text
- **LoadPngFilesCommand** → Button.Command

### 2. MVVM Pattern
- ObservableProperty attributes generate INotifyPropertyChanged
- RelayCommand attributes generate ICommand implementations
- Partial methods for property change notifications

### 3. Async Operations
- All I/O operations are async (file loading, OCR)
- Proper async/await patterns throughout
- UI remains responsive during processing

### 4. Platform-Specific Code
```csharp
#if ANDROID
    // Android-specific implementation
    var path = Android.OS.Environment.GetExternalStoragePublicDirectory(...);
    var bitmap = await BitmapFactory.DecodeFileAsync(filePath);
    var text = await _ocrService.ExtractTextAsync(bitmap);
#else
    // Fallback for other platforms
    var path = Path.Combine(Environment.GetFolderPath(...), "Downloads");
    OcrResultText = "[OCR only available on Android]";
#endif
```

### 5. Service Dependency Injection
```csharp
public DebugViewModel(
    ILoggingService logger,
    IOcrService ocrService,
    ...)
{
    _logger = logger;
    _ocrService = ocrService;
    // Services injected via DI container
}
```

## Testing Points

1. **Permission Handling**: Test grant/deny scenarios
2. **Empty Folder**: Test with no PNG files
3. **Large Files**: Test with high-resolution images
4. **Text Extraction**: Test with various text types and languages
5. **Selection**: Test rapid file selection changes
6. **Error Scenarios**: Test with corrupted images, missing files
7. **UI Responsiveness**: Verify async operations don't freeze UI
8. **Memory**: Verify no memory leaks with multiple selections
