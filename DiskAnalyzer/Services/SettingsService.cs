using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DiskAnalyzer.Models;

namespace DiskAnalyzer.Services;

/// <summary>
/// Available application themes
/// </summary>
public enum AppTheme
{
    /// <summary>Blade Runner 2049 inspired - Cyan/Pink neon on void black</summary>
    Tech,
    /// <summary>Professional Windows-style - Clean blues and grays</summary>
    Enterprise,
    /// <summary>Classic terminal - Green on black</summary>
    TerminalGreen,
    /// <summary>Alert terminal - Red on black</summary>
    TerminalRed
}

/// <summary>
/// Manages user settings with persistence to JSON file
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly string _scanCachePath;
    private readonly string _appDataPath;
    private UserSettings _settings;

    public SettingsService()
    {
        _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinTrim", "DiskAnalyzer");
        
        Directory.CreateDirectory(_appDataPath);
        _settingsPath = Path.Combine(_appDataPath, "settings.json");
        _scanCachePath = Path.Combine(_appDataPath, "scan-cache.json");
        _settings = Load();
    }

    /// <summary>
    /// Current application theme
    /// </summary>
    public AppTheme Theme
    {
        get => _settings.Theme;
        set
        {
            _settings.Theme = value;
            Save();
        }
    }

    public string? LastScanPath
    {
        get => _settings.LastScanPath;
        set
        {
            _settings.LastScanPath = value;
            Save();
        }
    }

    private UserSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
        }
        catch { }
        
        return new UserSettings();
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch { }
    }

    /// <summary>
    /// Save scan results to cache for quick restore on next app launch
    /// </summary>
    public void SaveScanCache(ScanResult result)
    {
        try
        {
            var cache = new ScanCache
            {
                ScanDate = DateTime.Now,
                RootPath = result.RootPath,
                Duration = result.Duration,
                TotalSize = result.TotalSize,
                TotalSizeFormatted = result.RootItem?.SizeFormatted ?? "0 B",
                TotalFiles = result.TotalFiles,
                TotalFolders = result.TotalFolders,
                LargestFiles = result.LargestFiles.Take(100).Select(f => new CachedFileItem
                {
                    Name = f.Name,
                    FullPath = f.FullPath,
                    Size = f.Size,
                    SizeFormatted = f.SizeFormatted,
                    Category = f.Category.ToString(),
                    LastAccessed = f.LastAccessed,
                    LastModified = f.LastModified,
                    DaysSinceAccessed = f.DaysSinceAccessed
                }).ToList(),
                LargestFolders = result.LargestFolders.Take(50).Select(f => new CachedFolderItem
                {
                    Name = f.Name,
                    FullPath = f.FullPath,
                    Size = f.Size,
                    SizeFormatted = f.SizeFormatted,
                    Children = f.Children.Take(20).Select(c => new CachedFolderChild
                    {
                        Name = c.Name,
                        FullPath = c.FullPath,
                        Size = c.Size,
                        SizeFormatted = c.SizeFormatted,
                        IsFolder = c.IsFolder
                    }).ToList()
                }).ToList(),
                Games = result.GameInstallations.Take(50).Select(g => new CachedGameItem
                {
                    Name = g.Name,
                    Path = g.Path,
                    Size = g.Size,
                    SizeFormatted = g.SizeFormatted,
                    Platform = g.Platform.ToString(),
                    LastPlayed = g.LastPlayed
                }).ToList(),
                CategoryBreakdown = result.CategoryBreakdown.Select(c => new CachedCategoryItem
                {
                    Category = c.Key.ToString(),
                    TotalSize = c.Value.TotalSize,
                    SizeFormatted = c.Value.SizeFormatted,
                    FileCount = c.Value.FileCount
                }).ToList(),
                CleanupSuggestions = result.CleanupSuggestions.Take(50).Select(s => new CachedCleanupItem
                {
                    Description = s.Description,
                    Path = s.Path,
                    PotentialSavings = s.PotentialSavings,
                    SavingsFormatted = s.SavingsFormatted,
                    RiskLevel = s.RiskLevel.ToString()
                }).ToList()
            };

            var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_scanCachePath, json);
        }
        catch { }
    }

    /// <summary>
    /// Load cached scan results from previous session
    /// </summary>
    public ScanCache? LoadScanCache()
    {
        try
        {
            if (File.Exists(_scanCachePath))
            {
                var json = File.ReadAllText(_scanCachePath);
                return JsonSerializer.Deserialize<ScanCache>(json);
            }
        }
        catch { }
        
        return null;
    }

    /// <summary>
    /// Check if a cached scan exists
    /// </summary>
    public bool HasCachedScan => File.Exists(_scanCachePath);

    /// <summary>
    /// Delete the scan cache
    /// </summary>
    public void ClearScanCache()
    {
        try
        {
            if (File.Exists(_scanCachePath))
                File.Delete(_scanCachePath);
        }
        catch { }
    }
}

public interface ISettingsService
{
    AppTheme Theme { get; set; }
    string? LastScanPath { get; set; }
    void SaveScanCache(ScanResult result);
    ScanCache? LoadScanCache();
    bool HasCachedScan { get; }
    void ClearScanCache();
}

public class UserSettings
{
    /// <summary>
    /// Current application theme (default: Tech/Blade Runner style)
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AppTheme Theme { get; set; } = AppTheme.Tech;
    
    /// <summary>
    /// Last scanned path for quick restore
    /// </summary>
    public string? LastScanPath { get; set; }
}
