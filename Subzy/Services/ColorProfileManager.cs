using SkiaSharp;
using Subzy.Models;
using Subzy.Services.Interfaces;
using System.Text.Json;

namespace Subzy.Services;

/// <summary>
/// Manages per-app color profiles for subtitle detection.
/// </summary>
public class ColorProfileManager
{
    private readonly ILoggingService _logger;
    private readonly SettingsService _settingsService;
    private Dictionary<string, SubtitleColorProfile> _profiles = new();
    private const string ProfilesKey = "ColorProfiles";
    private const int MaxColorsPerProfile = 5;
    private const int SimilarColorTolerance = 10;

    public ColorProfileManager(ILoggingService logger, SettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
        LoadProfiles();
    }

    /// <summary>
    /// Gets the active color profile for an app. Creates default if none exists.
    /// </summary>
    /// <param name="appPackageName">Android package name</param>
    /// <param name="appDisplayName">Optional display name</param>
    /// <returns>Color profile for the app</returns>
    public SubtitleColorProfile GetActiveProfile(string appPackageName, string? appDisplayName = null)
    {
        if (_profiles.TryGetValue(appPackageName, out var profile))
        {
            profile.LastUsed = DateTime.Now;
            _logger.Debug($"Loaded profile for {appPackageName} with {profile.SubtitleColors.Count} colors");
            return profile;
        }

        // Create default profile with white color
        var newProfile = new SubtitleColorProfile
        {
            AppPackageName = appPackageName,
            AppDisplayName = appDisplayName ?? appPackageName,
            SubtitleColors = new List<SKColor> { SKColors.White },
            ColorTolerance = 30,
            CreatedAt = DateTime.Now,
            LastUsed = DateTime.Now
        };

        _profiles[appPackageName] = newProfile;
        _logger.Info($"Created default profile for {appPackageName}");
        SaveProfiles();

        return newProfile;
    }

    /// <summary>
    /// Adds a color to an app's profile with MRU behavior.
    /// </summary>
    /// <param name="appPackageName">Android package name</param>
    /// <param name="appDisplayName">Display name</param>
    /// <param name="color">Color to add</param>
    public void AddColorToApp(string appPackageName, string appDisplayName, SKColor color)
    {
        var profile = GetActiveProfile(appPackageName, appDisplayName);

        // Check for similar colors to prevent duplicates
        var hasSimilarColor = profile.SubtitleColors.Any(c => 
            ColorDistance(c, color) <= SimilarColorTolerance);

        if (hasSimilarColor)
        {
            _logger.Debug($"Similar color already exists in profile for {appPackageName}");
            return;
        }

        // Insert at position 0 (MRU)
        profile.SubtitleColors.Insert(0, color);

        // Trim to max 5 colors
        if (profile.SubtitleColors.Count > MaxColorsPerProfile)
        {
            profile.SubtitleColors = profile.SubtitleColors.Take(MaxColorsPerProfile).ToList();
        }

        profile.LastUsed = DateTime.Now;
        _logger.Info($"Added color #{color.Red:X2}{color.Green:X2}{color.Blue:X2} to {appDisplayName} (total: {profile.SubtitleColors.Count})");
        
        SaveProfiles();
    }

    /// <summary>
    /// Gets all known colors across all profiles.
    /// </summary>
    /// <returns>List of all colors</returns>
    public List<SKColor> GetAllKnownColors()
    {
        var allColors = new List<SKColor>();
        foreach (var profile in _profiles.Values)
        {
            allColors.AddRange(profile.SubtitleColors);
        }
        return allColors.Distinct().ToList();
    }

    /// <summary>
    /// Removes a specific color from an app's profile.
    /// </summary>
    /// <param name="appPackageName">Android package name</param>
    /// <param name="color">Color to remove</param>
    public void RemoveColorFromApp(string appPackageName, SKColor color)
    {
        if (_profiles.TryGetValue(appPackageName, out var profile))
        {
            profile.SubtitleColors.RemoveAll(c => 
                c.Red == color.Red && c.Green == color.Green && c.Blue == color.Blue);
            
            // Ensure at least white color remains
            if (profile.SubtitleColors.Count == 0)
            {
                profile.SubtitleColors.Add(SKColors.White);
            }

            SaveProfiles();
            _logger.Info($"Removed color from {appPackageName}");
        }
    }

    /// <summary>
    /// Clears all colors for an app and resets to default.
    /// </summary>
    /// <param name="appPackageName">Android package name</param>
    public void ClearProfileForApp(string appPackageName)
    {
        if (_profiles.ContainsKey(appPackageName))
        {
            _profiles.Remove(appPackageName);
            SaveProfiles();
            _logger.Info($"Cleared profile for {appPackageName}");
        }
    }

    /// <summary>
    /// Gets all profiles sorted by last used.
    /// </summary>
    /// <returns>List of profiles</returns>
    public List<SubtitleColorProfile> GetAllProfiles()
    {
        return _profiles.Values
            .OrderByDescending(p => p.LastUsed)
            .ToList();
    }

    /// <summary>
    /// Saves profiles to persistent storage.
    /// </summary>
    public void SaveProfiles()
    {
        try
        {
            var json = JsonSerializer.Serialize(_profiles, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            Preferences.Set(ProfilesKey, json);
            _logger.Debug($"Saved {_profiles.Count} color profiles");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to save color profiles", ex);
        }
    }

    /// <summary>
    /// Loads profiles from persistent storage.
    /// </summary>
    public void LoadProfiles()
    {
        try
        {
            var json = Preferences.Get(ProfilesKey, string.Empty);
            
            if (!string.IsNullOrEmpty(json))
            {
                _profiles = JsonSerializer.Deserialize<Dictionary<string, SubtitleColorProfile>>(json) 
                    ?? new Dictionary<string, SubtitleColorProfile>();
                
                _logger.Info($"Loaded {_profiles.Count} color profiles");
            }
            else
            {
                _profiles = new Dictionary<string, SubtitleColorProfile>();
                _logger.Debug("No color profiles found, starting fresh");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load color profiles", ex);
            _profiles = new Dictionary<string, SubtitleColorProfile>();
        }
    }

    /// <summary>
    /// Calculates RGB color distance between two colors.
    /// </summary>
    private static int ColorDistance(SKColor c1, SKColor c2)
    {
        var dr = Math.Abs(c1.Red - c2.Red);
        var dg = Math.Abs(c1.Green - c2.Green);
        var db = Math.Abs(c1.Blue - c2.Blue);
        return dr + dg + db;
    }
}
