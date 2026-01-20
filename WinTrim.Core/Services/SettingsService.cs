using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// Manages user settings and scan cache with persistence to JSON files
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
            "WinTrim");
        
        Directory.CreateDirectory(_appDataPath);
        _settingsPath = Path.Combine(_appDataPath, "settings.json");
        _scanCachePath = Path.Combine(_appDataPath, "scan-cache.json");
        _settings = Load();
    }

    /// <summary>
    /// Current application theme name
    /// </summary>
    public string Theme
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
    /// Font size
    /// </summary>
    public int FontSize
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
    /// Treemap color mode
    /// </summary>
    public string TreemapColorMode
    {
        get => _settings.TreemapColorMode;
        set
        {
            _settings.TreemapColorMode = value;
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

    /// <summary>
    /// Express scan mode enabled
    /// </summary>
    public bool ExpressScanEnabled
    {
        get => _settings.ExpressScanEnabled;
        set
        {
            _settings.ExpressScanEnabled = value;
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

    /// <summary>
    /// Current EULA version - increment when terms change significantly
    /// </summary>
    public string CurrentEulaVersion => "1.0";

    /// <summary>
    /// Whether user has accepted the current EULA version
    /// </summary>
    public bool EulaAccepted
    {
        get => _settings.EulaAccepted && _settings.EulaVersion == CurrentEulaVersion;
        set
        {
            _settings.EulaAccepted = value;
            Save();
        }
    }

    /// <summary>
    /// Record EULA acceptance with timestamp and version
    /// </summary>
    public void AcceptEula()
    {
        _settings.EulaAccepted = true;
        _settings.EulaAcceptedDate = DateTime.UtcNow;
        _settings.EulaVersion = CurrentEulaVersion;
        Save();
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
    public void SaveScanCache(ScanResult result, bool wasExpressMode = false)
    {
        try
        {
            var cache = new ScanCache
            {
                ScanDate = DateTime.Now,
                RootPath = result.RootPath,
                Duration = result.Duration,
                WasExpressMode = wasExpressMode,
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
                DevTools = result.DevTools.Take(100).Select(d => new CachedDevToolItem
                {
                    Name = d.Name,
                    Path = d.Path,
                    SizeBytes = d.SizeBytes,
                    Category = d.Category,
                    Recommendation = d.Recommendation,
                    Risk = d.Risk.ToString()
                }).ToList(),
                RootTree = result.RootItem != null ? CacheTreeNode(result.RootItem, 0, 4) : null
            };

            var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_scanCachePath, json);
            Console.WriteLine($"[SettingsService] Saved scan cache: {result.TotalFiles:N0} files, {result.TotalFolders:N0} folders");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SettingsService] Failed to save scan cache: {ex.Message}");
        }
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
        var filesByCategory = new Dictionary<string, List<FileSystemItem>>();
        if (result.RootItem != null)
        {
            CollectFilesByCategory(result.RootItem, filesByCategory);
        }

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
                var cache = JsonSerializer.Deserialize<ScanCache>(json);
                if (cache != null)
                {
                    Console.WriteLine($"[SettingsService] Loaded scan cache from {cache.ScanDate}: {cache.TotalFiles:N0} files");
                }
                return cache;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SettingsService] Failed to load scan cache: {ex.Message}");
        }
        
        return null;
    }

    /// <summary>
    /// Check if a cached scan exists
    /// </summary>
    public bool HasCachedScan => File.Exists(_scanCachePath);

    /// <summary>
    /// Get cache info without loading full cache
    /// </summary>
    public (DateTime scanDate, string rootPath, bool wasExpressMode)? GetCacheInfo()
    {
        try
        {
            if (File.Exists(_scanCachePath))
            {
                var json = File.ReadAllText(_scanCachePath);
                var cache = JsonSerializer.Deserialize<ScanCache>(json);
                if (cache != null)
                {
                    return (cache.ScanDate, cache.RootPath, cache.WasExpressMode);
                }
            }
        }
        catch { }
        return null;
    }

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
    string Theme { get; set; }
    int FontSize { get; set; }
    string TreemapColorMode { get; set; }
    int TreemapMaxDepth { get; set; }
    bool ExpressScanEnabled { get; set; }
    string? LastScanPath { get; set; }
    bool EulaAccepted { get; set; }
    string CurrentEulaVersion { get; }
    void AcceptEula();
    void SaveScanCache(ScanResult result, bool wasExpressMode = false);
    ScanCache? LoadScanCache();
    bool HasCachedScan { get; }
    (DateTime scanDate, string rootPath, bool wasExpressMode)? GetCacheInfo();
    void ClearScanCache();
    event EventHandler? SettingsChanged;
}

public class UserSettings
{
    /// <summary>
    /// Current application theme
    /// </summary>
    public string Theme { get; set; } = "Retrofuturistic";
    
    /// <summary>
    /// Font size
    /// </summary>
    public int FontSize { get; set; } = 14;
    
    /// <summary>
    /// Treemap color mode
    /// </summary>
    public string TreemapColorMode { get; set; } = "Depth";
    
    /// <summary>
    /// Treemap depth (1-5)
    /// </summary>
    public int TreemapMaxDepth { get; set; } = 3;
    
    /// <summary>
    /// Express scan mode enabled
    /// </summary>
    public bool ExpressScanEnabled { get; set; } = true;
    
    /// <summary>
    /// Last scanned path for quick restore
    /// </summary>
    public string? LastScanPath { get; set; }
    
    /// <summary>
    /// Whether user has accepted the EULA/Terms of Service
    /// </summary>
    public bool EulaAccepted { get; set; } = false;
    
    /// <summary>
    /// Date when EULA was accepted
    /// </summary>
    public DateTime? EulaAcceptedDate { get; set; }
    
    /// <summary>
    /// Version of EULA that was accepted
    /// </summary>
    public string? EulaVersion { get; set; }
}
