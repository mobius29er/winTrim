using System;
using System.Collections.Generic;
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
    /// <summary>Retrofuturistic - Teal/Cyan with orange accents on deep black (Territory Studio inspired)</summary>
    Default,
    /// <summary>Blade Runner 2049 neon - Cyan/Pink on void black</summary>
    Tech,
    /// <summary>Professional Windows-style - Clean blues and grays</summary>
    Enterprise,
    /// <summary>Classic terminal - Green on black</summary>
    TerminalGreen,
    /// <summary>Alert terminal - Red on black</summary>
    TerminalRed
}

/// <summary>
/// Font size presets
/// </summary>
public enum FontSizePreset
{
    Small,      // 11px base
    Medium,     // 13px base (default)
    Large,      // 15px base
    ExtraLarge  // 17px base
}

/// <summary>
/// Treemap color scheme options
/// </summary>
public enum TreemapColorScheme
{
    /// <summary>High contrast blues, greens, oranges - easy to read</summary>
    Vivid,
    /// <summary>Softer pastel tones</summary>
    Pastel,
    /// <summary>Ocean-inspired blues and teals</summary>
    Ocean,
    /// <summary>Warm sunset colors - oranges, reds, yellows</summary>
    Warm,
    /// <summary>Cool mint/purple tones</summary>
    Cool
}

/// <summary>
/// Treemap coloring mode - what determines tile colors
/// </summary>
public enum TreemapColorMode
{
    /// <summary>Colors based on folder depth</summary>
    Depth,
    /// <summary>Colors based on file category (Document, Video, etc.)</summary>
    Category,
    /// <summary>Colors based on file age (red=new, blue=old)</summary>
    Age,
    /// <summary>Colors based on file extension</summary>
    FileType
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

    public event EventHandler? SettingsChanged;

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
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Font size preset
    /// </summary>
    public FontSizePreset FontSize
    {
        get => _settings.FontSize;
        set
        {
            _settings.FontSize = value;
            Save();
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Treemap color scheme
    /// </summary>
    public TreemapColorScheme TreemapColors
    {
        get => _settings.TreemapColors;
        set
        {
            _settings.TreemapColors = value;
            Save();
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Show file extensions in treemap labels
    /// </summary>
    public bool ShowExtensionsInTreemap
    {
        get => _settings.ShowExtensionsInTreemap;
        set
        {
            _settings.ShowExtensionsInTreemap = value;
            Save();
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Treemap max depth (1-5)
    /// </summary>
    public int TreemapMaxDepth
    {
        get => _settings.TreemapMaxDepth;
        set
        {
            _settings.TreemapMaxDepth = Math.Clamp(value, 1, 5);
            Save();
            SettingsChanged?.Invoke(this, EventArgs.Empty);
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

    /// <summary>
    /// Get the base font size in pixels for the current preset
    /// </summary>
    public double GetBaseFontSize() => FontSize switch
    {
        FontSizePreset.Small => 11,
        FontSizePreset.Medium => 13,
        FontSizePreset.Large => 15,
        FontSizePreset.ExtraLarge => 17,
        _ => 13
    };

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
                CategoryBreakdown = BuildCategoryBreakdownWithFiles(result),
                CleanupSuggestions = result.CleanupSuggestions.Take(50).Select(s => new CachedCleanupItem
                {
                    Description = s.Description,
                    Path = s.Path,
                    PotentialSavings = s.PotentialSavings,
                    SavingsFormatted = s.SavingsFormatted,
                    RiskLevel = s.RiskLevel.ToString()
                }).ToList(),
                // Cache developer tools from scan results
                DevTools = result.DevTools.Take(100).Select(d => new CachedDevToolItem
                {
                    Name = d.Name,
                    Path = d.Path,
                    SizeBytes = d.SizeBytes,
                    Category = d.Category,
                    Recommendation = d.Recommendation,
                    Risk = d.Risk.ToString()
                }).ToList(),
                // Cache tree for treemap (limited depth)
                RootTree = result.RootItem != null ? CacheTreeNode(result.RootItem, 0, 4) : null
            };

            var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_scanCachePath, json);
        }
        catch { }
    }

    /// <summary>
    /// Recursively cache tree nodes up to maxDepth
    /// </summary>
    private CachedTreeNode? CacheTreeNode(FileSystemItem item, int depth, int maxDepth)
    {
        if (depth > maxDepth) return null;
        
        var node = new CachedTreeNode
        {
            Name = item.Name,
            FullPath = item.FullPath,
            Size = item.Size,
            IsFolder = item.IsFolder,
            Category = item.Category.ToString()
        };

        if (item.IsFolder && depth < maxDepth)
        {
            // Only cache children sorted by size (top 50 per level to limit size)
            var topChildren = item.Children
                .OrderByDescending(c => c.Size)
                .Take(50)
                .Select(c => CacheTreeNode(c, depth + 1, maxDepth))
                .Where(c => c != null)
                .Cast<CachedTreeNode>()
                .ToList();
            
            node.Children = topChildren;
        }

        return node;
    }

    /// <summary>
    /// Build category breakdown with top files per category
    /// </summary>
    private List<CachedCategoryItem> BuildCategoryBreakdownWithFiles(ScanResult result)
    {
        // Collect files by category from the tree
        var filesByCategory = new Dictionary<string, List<FileSystemItem>>();
        if (result.RootItem != null)
        {
            CollectFilesByCategory(result.RootItem, filesByCategory);
        }

        // Create cached category items with top files
        return result.CategoryBreakdown.Select(c =>
        {
            var categoryName = c.Key.ToString();
            var topFiles = new List<CachedCategoryFileItem>();
            
            if (filesByCategory.TryGetValue(categoryName, out var files))
            {
                topFiles = files
                    .OrderByDescending(f => f.Size)
                    .Take(50)
                    .Select(f => new CachedCategoryFileItem
                    {
                        Name = f.Name,
                        FullPath = f.FullPath,
                        Size = f.Size,
                        SizeFormatted = f.SizeFormatted,
                        LastAccessed = f.LastAccessed,
                        LastModified = f.LastModified
                    })
                    .ToList();
            }
            
            return new CachedCategoryItem
            {
                Category = categoryName,
                TotalSize = c.Value.TotalSize,
                SizeFormatted = c.Value.SizeFormatted,
                FileCount = c.Value.FileCount,
                TopFiles = topFiles
            };
        }).ToList();
    }

    /// <summary>
    /// Recursively collect files by category from the tree
    /// </summary>
    private void CollectFilesByCategory(FileSystemItem item, Dictionary<string, List<FileSystemItem>> filesByCategory)
    {
        if (!item.IsFolder)
        {
            var categoryName = item.Category.ToString();
            if (!filesByCategory.ContainsKey(categoryName))
            {
                filesByCategory[categoryName] = new List<FileSystemItem>();
            }
            filesByCategory[categoryName].Add(item);
        }

        foreach (var child in item.Children)
        {
            CollectFilesByCategory(child, filesByCategory);
        }
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
    FontSizePreset FontSize { get; set; }
    TreemapColorScheme TreemapColors { get; set; }
    bool ShowExtensionsInTreemap { get; set; }
    int TreemapMaxDepth { get; set; }
    string? LastScanPath { get; set; }
    double GetBaseFontSize();
    void SaveScanCache(ScanResult result);
    ScanCache? LoadScanCache();
    bool HasCachedScan { get; }
    void ClearScanCache();
    event EventHandler? SettingsChanged;
}

public class UserSettings
{
    /// <summary>
    /// Current application theme (default: Tech/Blade Runner style)
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AppTheme Theme { get; set; } = AppTheme.Default;
    
    /// <summary>
    /// Font size preset
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FontSizePreset FontSize { get; set; } = FontSizePreset.Medium;
    
    /// <summary>
    /// Treemap color scheme
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TreemapColorScheme TreemapColors { get; set; } = TreemapColorScheme.Vivid;
    
    /// <summary>
    /// Show file extensions in treemap
    /// </summary>
    public bool ShowExtensionsInTreemap { get; set; } = true;
    
    /// <summary>
    /// Treemap depth (1-5)
    /// </summary>
    public int TreemapMaxDepth { get; set; } = 3;
    
    /// <summary>
    /// Last scanned path for quick restore
    /// </summary>
    public string? LastScanPath { get; set; }
}
