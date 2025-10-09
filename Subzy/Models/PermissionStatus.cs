namespace Subzy.Models;

/// <summary>
/// Tracks the status of required permissions.
/// </summary>
public class PermissionStatus
{
    public bool HasScreenCapturePermission { get; set; }
    public bool HasAccessibilityPermission { get; set; }
    public bool HasInternetPermission { get; set; }
    public bool HasForegroundServicePermission { get; set; }
    public bool HasRecordAudioPermission { get; set; }
    
    public bool AllPermissionsGranted =>
        HasScreenCapturePermission &&
        HasInternetPermission &&
        HasForegroundServicePermission;
}
