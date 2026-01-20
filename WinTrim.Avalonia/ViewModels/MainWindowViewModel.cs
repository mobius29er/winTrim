using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using WinTrim.Avalonia.Services;
using WinTrim.Core.Models;
using WinTrim.Core.Services;

namespace WinTrim.Avalonia.ViewModels;

/// <summary>
/// Wrapper for DriveInfo that provides better display names for macOS
/// </summary>
public class DriveDisplayInfo
{
    public DriveInfo DriveInfo { get; }
    public string DisplayName { get; }
    public string Path { get; }
    public long TotalSize => DriveInfo.IsReady ? DriveInfo.TotalSize : 0;
    public long AvailableFreeSpace => DriveInfo.IsReady ? DriveInfo.AvailableFreeSpace : 0;
    public string DriveType { get; }
    
    public DriveDisplayInfo(DriveInfo driveInfo, string? displayName = null)
    {
        DriveInfo = driveInfo;
        Path = driveInfo.Name;
        
        // Determine display name
        if (!string.IsNullOrEmpty(displayName))
        {
            DisplayName = displayName;
        }
        else if (Path == "/")
        {
            DisplayName = "Macintosh HD";
        }
        else if (Path.StartsWith("/Volumes/"))
        {
            DisplayName = System.IO.Path.GetFileName(Path);
        }
        else if (!string.IsNullOrEmpty(driveInfo.VolumeLabel))
        {
            DisplayName = driveInfo.VolumeLabel;
        }
        else
        {
            DisplayName = Path;
        }
        
        // Determine drive type description
        DriveType = driveInfo.DriveType switch
        {
            System.IO.DriveType.Fixed => "Local Disk",
            System.IO.DriveType.Network => "Network Drive",
            System.IO.DriveType.Removable => "Removable",
            _ => "Drive"
        };
    }
    
    public override string ToString() => DisplayName;
}

/// <summary>
/// Main ViewModel handling all disk analysis operations for Avalonia
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IFileScanner _fileScanner;
    private readonly IPlatformService _platformService;
    private readonly IThemeService _themeService;
    private readonly ISettingsService _settingsService;
    private CancellationTokenSource? _cancellationTokenSource;

    #region Observable Properties

    [ObservableProperty]
    private string _selectedPath = string.Empty;

    [ObservableProperty]
    private ScanProgress _scanProgress = new();

    [ObservableProperty]
    private ScanResult? _scanResult;

    /// <summary>
    /// Direct binding property for TreemapControl.SourceItem
    /// Updated separately to ensure proper binding notification
    /// </summary>
    [ObservableProperty]
    private FileSystemItem? _treemapRootItem;

    [ObservableProperty]
    private FileSystemItem? _selectedItem;

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _rootItems = new();

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _filteredRootItems = new();

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _largestFiles = new();

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _largestFolders = new();

    [ObservableProperty]
    private ObservableCollection<GameInstallation> _games = new();

    [ObservableProperty]
    private ObservableCollection<CleanupItem> _devToolItems = new();

    [ObservableProperty]
    private ObservableCollection<CleanupSuggestion> _cleanupSuggestions = new();

    [ObservableProperty]
    private ObservableCollection<TreemapLegendItem> _treemapLegendItems = new();

    [ObservableProperty]
    private ObservableCollection<DriveDisplayInfo> _availableDrives = new();

    [ObservableProperty]
    private DriveDisplayInfo? _selectedDrive;

    [ObservableProperty]
    private ISeries[] _categorySeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ObservableCollection<CategoryLegendItem> _categoryLegendItems = new();

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _categoryFiles = new();

    [ObservableProperty]
    private string? _selectedCategoryName;

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _folderContents = new();

    [ObservableProperty]
    private string? _selectedFolderName;

    [ObservableProperty]
    private ObservableCollection<CleanupFileInfo> _cleanupFiles = new();

    [ObservableProperty]
    private string? _selectedCleanupDescription;

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _filteredChildren = new();

    [ObservableProperty]
    private bool _canStart = true;

    [ObservableProperty]
    private bool _canStop;

    [ObservableProperty]
    private bool _canPause;

    [ObservableProperty]
    private bool _canResume;

    [ObservableProperty]
    private string _statusText = "Ready to scan";

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private bool _hasQuickCleanItems;

    [ObservableProperty]
    private bool _expressScanEnabled = true; // Default to express scan for faster results

    [ObservableProperty]
    private string _quickCleanInfo = string.Empty;

    [ObservableProperty]
    private string _treeSearchText = string.Empty;

    [ObservableProperty]
    private string _treeSortBy = "Size";

    [ObservableProperty]
    private ObservableCollection<string> _treeSortOptions = new() { "Name", "Size", "Date" };

    [ObservableProperty]
    private string _fileExplorerSearchText = string.Empty;

    [ObservableProperty]
    private string _fileExplorerFilter = "All Files";

    [ObservableProperty]
    private ObservableCollection<string> _fileExplorerFilterOptions = new() { "All Files", "Large Files", "Old Files" };

    [ObservableProperty]
    private ObservableCollection<string> _availableThemes = new() { "Retrofuturistic", "Tech", "Enterprise", "TerminalGreen", "TerminalRed" };

    [ObservableProperty]
    private string _selectedTheme = "Retrofuturistic";

    [ObservableProperty]
    private ObservableCollection<int> _availableFontSizes = new() { 12, 14, 16, 18, 20 };

    [ObservableProperty]
    private int _selectedFontSize = 14;

    [ObservableProperty]
    private ObservableCollection<string> _availableTreemapColorModes = new() { "Depth", "Category", "Age", "FileType" };

    [ObservableProperty]
    private string _selectedTreemapColorMode = "Depth";

    [ObservableProperty]
    private int _treemapMaxDepth = 3;

    [ObservableProperty]
    private bool _hasCachedScan;

    [ObservableProperty]
    private string _cachedScanInfo = string.Empty;

    #endregion

    /// <summary>
    /// Constructor with DI-injected services
    /// </summary>
    public MainWindowViewModel(IFileScanner fileScanner, IPlatformService platformService, IThemeService themeService, ISettingsService settingsService)
    {
        Console.WriteLine("[ViewModel] Constructor called");
        _fileScanner = fileScanner;
        _platformService = platformService;
        _themeService = themeService;
        _settingsService = settingsService;
        
        // Load saved theme from settings (or use default)
        var savedTheme = _settingsService.Theme;
        if (AvailableThemes.Contains(savedTheme))
        {
            _selectedTheme = savedTheme; // Set backing field directly to avoid triggering save
        }
        _themeService.ApplyTheme(_selectedTheme);
        Console.WriteLine($"[ViewModel] Loaded theme from settings: {_selectedTheme}");
        
        // Load settings
        ExpressScanEnabled = _settingsService.ExpressScanEnabled;
        
        LoadAvailableDrives();
        UpdateCachedScanInfo();
        Console.WriteLine($"[ViewModel] Constructor complete. AvailableDrives: {AvailableDrives.Count}, SelectedDrive: {SelectedDrive?.DisplayName ?? "null"}");
    }
    
    /// <summary>
    /// Updates the cached scan info for UI display
    /// </summary>
    private void UpdateCachedScanInfo()
    {
        HasCachedScan = _settingsService.HasCachedScan;
        if (HasCachedScan)
        {
            var info = _settingsService.GetCacheInfo();
            if (info.HasValue)
            {
                var (scanDate, rootPath, wasExpressMode) = info.Value;
                var timeAgo = GetTimeAgo(scanDate);
                var mode = wasExpressMode ? "Express" : "Full";
                CachedScanInfo = $"Last scan: {timeAgo} ({mode})";
            }
        }
        else
        {
            CachedScanInfo = string.Empty;
        }
    }
    
    /// <summary>
    /// Gets a human-readable "time ago" string
    /// </summary>
    private static string GetTimeAgo(DateTime date)
    {
        var span = DateTime.Now - date;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return date.ToString("MMM d");
    }
    
    /// <summary>
    /// Called when SelectedTheme property changes
    /// </summary>
    partial void OnSelectedThemeChanged(string value)
    {
        Console.WriteLine($"[ViewModel] OnSelectedThemeChanged called with: {value}");
        _themeService.ApplyTheme(value);
        _settingsService.Theme = value; // Persist selection
    }
    
    /// <summary>
    /// Called when SelectedFontSize property changes
    /// </summary>
    partial void OnSelectedFontSizeChanged(int value)
    {
        Console.WriteLine($"[ViewModel] OnSelectedFontSizeChanged called with: {value}");
        _themeService.ApplyFontSize(value);
    }

    /// <summary>
    /// Called when SelectedTreemapColorMode property changes
    /// </summary>
    partial void OnSelectedTreemapColorModeChanged(string value)
    {
        Console.WriteLine($"[ViewModel] OnSelectedTreemapColorModeChanged called with: {value}");
        UpdateTreemapLegend(value);
    }

    /// <summary>
    /// Updates the treemap legend based on the selected color mode
    /// </summary>
    private void UpdateTreemapLegend(string colorMode)
    {
        TreemapLegendItems.Clear();
        
        switch (colorMode)
        {
            case "Category":
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Document", Color = "#2563EB" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Image", Color = "#DB2777" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Video", Color = "#DC2626" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Audio", Color = "#D97706" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Archive", Color = "#7C3AED" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Code", Color = "#059669" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Executable", Color = "#6366F1" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Game", Color = "#EA580C" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "System", Color = "#475569" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Temporary", Color = "#64748B" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Other", Color = "#78716C" });
                break;
                
            case "Age":
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "< 7 days", Color = "#EF4444" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "< 30 days", Color = "#F97316" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "1-3 months", Color = "#EAB308" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "3-6 months", Color = "#22C55E" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "6-12 months", Color = "#06B6D4" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "1-2 years", Color = "#3B82F6" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "2+ years", Color = "#6366F1" });
                break;
                
            case "FileType":
                TreemapLegendItems.Add(new TreemapLegendItem { Label = ".exe/.dll", Color = "#6366F1" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = ".mp4/.mkv", Color = "#DC2626" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = ".mp3/.wav", Color = "#F59E0B" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = ".jpg/.png", Color = "#EC4899" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = ".pdf/.doc", Color = "#2563EB" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = ".zip/.rar", Color = "#8B5CF6" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Code", Color = "#10B981" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Other", Color = "#6B7280" });
                break;
                
            case "Depth":
            default:
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Level 1", Color = "#2563EB" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Level 2", Color = "#EF4444" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Level 3", Color = "#22C55E" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Level 4", Color = "#F59E0B" });
                TreemapLegendItems.Add(new TreemapLegendItem { Label = "Level 5+", Color = "#8B5CF6" });
                break;
        }
    }

    private void LoadAvailableDrives()
    {
        AvailableDrives.Clear();
        
        if (OperatingSystem.IsMacOS())
        {
            // On macOS, use filtered drive list to hide system volumes
            foreach (var drive in GetMacOSDrives())
            {
                AvailableDrives.Add(drive);
            }
        }
        else
        {
            // Windows/Linux - show ready fixed/removable drives
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && (drive.DriveType == DriveType.Fixed || 
                                       drive.DriveType == DriveType.Removable ||
                                       drive.DriveType == DriveType.Network))
                {
                    AvailableDrives.Add(new DriveDisplayInfo(drive));
                }
            }
        }
        
        if (AvailableDrives.Count > 0)
        {
            SelectedDrive = AvailableDrives[0];
        }
    }
    
    /// <summary>
    /// Gets filtered list of drives for macOS, hiding system volumes
    /// </summary>
    private static List<DriveDisplayInfo> GetMacOSDrives()
    {
        var drives = new List<DriveDisplayInfo>();
        var addedPaths = new HashSet<string>();
        
        // Always include root volume (main Mac disk) - shown as "Macintosh HD"
        var rootDrive = new DriveInfo("/");
        if (rootDrive.IsReady)
        {
            addedPaths.Add("/");
            drives.Add(new DriveDisplayInfo(rootDrive, "Macintosh HD"));
        }
        
        // Scan /Volumes for external drives (including NAS mounts)
        var volumesPath = "/Volumes";
        if (Directory.Exists(volumesPath))
        {
            foreach (var volumePath in Directory.GetDirectories(volumesPath))
            {
                var volumeName = Path.GetFileName(volumePath);
                
                // Skip system and hidden volumes
                if (ShouldSkipMacVolume(volumeName, volumePath))
                    continue;
                
                if (addedPaths.Contains(volumePath))
                    continue;
                
                try
                {
                    var drive = new DriveInfo(volumePath);
                    if (drive.IsReady)
                    {
                        addedPaths.Add(volumePath);
                        // Use the volume folder name as the display name
                        drives.Add(new DriveDisplayInfo(drive, volumeName));
                    }
                }
                catch
                {
                    // Skip volumes we can't access
                }
            }
        }
        
        return drives;
    }
    
    /// <summary>
    /// Determines if a macOS volume should be hidden from users
    /// </summary>
    private static bool ShouldSkipMacVolume(string volumeName, string volumePath)
    {
        // Skip main disk symlinks in /Volumes
        if (volumeName == "Macintosh HD" || volumeName == "Macintosh HD - Data")
            return true;
        
        // Skip APFS system container volumes
        var lowerName = volumeName.ToLowerInvariant();
        var systemNames = new[] { "preboot", "recovery", "vm", "update", "xarts", 
                                   "iscpreboot", "hardware", "data", "home" };
        if (systemNames.Contains(lowerName))
            return true;
        
        // Skip system volume paths
        if (volumePath.StartsWith("/System/Volumes", StringComparison.OrdinalIgnoreCase))
            return true;
        
        // Skip Xcode simulator volumes
        if (volumePath.Contains("/CoreSimulator/", StringComparison.OrdinalIgnoreCase))
            return true;
        
        // Skip Apple-internal volumes
        if (lowerName.StartsWith("com.apple."))
            return true;
        
        return false;
    }

    #region Commands

    [RelayCommand]
    private async Task StartScan()
    {
        if (SelectedDrive == null) return;

        // Determine scan path - Express mode scans only user's home directory for speed
        var scanPath = SelectedDrive.Path;
        var scanModeLabel = "Full";
        
        if (ExpressScanEnabled && SelectedDrive.Path == "/")
        {
            // Express mode: scan only the current user's home directory
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(homeDir) && Directory.Exists(homeDir))
            {
                scanPath = homeDir;
                scanModeLabel = "Express";
            }
        }

        Console.WriteLine($"[ViewModel] StartScan called for drive: {SelectedDrive.DisplayName} ({scanPath}) - {scanModeLabel} mode");
        
        CanStart = false;
        CanStop = true;
        CanPause = true;
        ScanProgress.Reset();
        ScanProgress.State = ScanState.Scanning;
        StatusText = ExpressScanEnabled && scanModeLabel == "Express" 
            ? $"Express scanning {Path.GetFileName(scanPath)}..." 
            : $"Scanning {SelectedDrive.DisplayName}...";

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var progress = new Progress<ScanProgress>(p =>
            {
                // Update UI with progress - use properties to trigger notifications
                ScanProgress.State = p.State;
                ScanProgress.CurrentFolder = p.CurrentFolder;
                ScanProgress.FilesScanned = p._filesScanned;
                ScanProgress.FoldersScanned = p._foldersScanned;
                ScanProgress.BytesScanned = p._bytesScanned;
                ScanProgress.ErrorCount = p._errorCount;
                ScanProgress.ProgressPercentage = p.ProgressPercentage;
                ScanProgress.StatusMessage = p.StatusMessage;
            });

            // Run actual scan using injected FileScanner
            var result = await _fileScanner.ScanAsync(
                scanPath, 
                progress, 
                _cancellationTokenSource.Token);

            // Update UI with results (includes partial results if cancelled)
            ScanResult = result;
            
            // Populate collections for UI binding
            PopulateResultsFromScan(result);
            
            // Save to cache for recall (only if not cancelled and has real results)
            if (!result.WasCancelled && result.TotalFiles > 0)
            {
                var wasExpressMode = scanModeLabel == "Express";
                _settingsService.SaveScanCache(result, wasExpressMode);
                _settingsService.ExpressScanEnabled = ExpressScanEnabled;
                UpdateCachedScanInfo();
            }
            
            StatusText = result.WasCancelled 
                ? $"Scan stopped - showing partial results: {result.TotalFiles:N0} files, {result.TotalFolders:N0} folders" 
                : $"Scan complete: {result.TotalFiles:N0} files, {result.TotalFolders:N0} folders";
        }
        catch (OperationCanceledException)
        {
            // FileScanner handles cancellation internally and returns partial results
            // This catch is a fallback in case something else throws
            StatusText = "Scan stopped";
        }
        catch (Exception ex)
        {
            StatusText = $"Scan failed: {ex.Message}";
        }
        finally
        {
            CanStart = true;
            CanStop = false;
            CanPause = false;
            CanResume = false;
        }
    }

    private void PopulateResultsFromScan(ScanResult result)
    {
        // Set treemap root item for direct binding
        TreemapRootItem = result.RootItem;
        
        Console.WriteLine($"[PopulateResults] RootItem: {result.RootItem?.Name ?? "null"}");
        Console.WriteLine($"[PopulateResults] LargestFiles count: {result.LargestFiles.Count}");
        Console.WriteLine($"[PopulateResults] Games count: {result.GameInstallations.Count}");
        Console.WriteLine($"[PopulateResults] DevTools count: {result.DevTools.Count}");
        
        // Populate largest files
        LargestFiles.Clear();
        foreach (var file in result.LargestFiles)
        {
            LargestFiles.Add(file);
        }
        Console.WriteLine($"[PopulateResults] LargestFiles collection now has: {LargestFiles.Count} items");

        // Populate largest folders
        LargestFolders.Clear();
        foreach (var folder in result.LargestFolders.Take(20))
        {
            LargestFolders.Add(folder);
        }

        // Populate games
        Games.Clear();
        foreach (var game in result.GameInstallations)
        {
            Games.Add(game);
        }

        // Populate dev tools
        DevToolItems.Clear();
        foreach (var item in result.DevTools)
        {
            DevToolItems.Add(item);
        }

        // Populate cleanup suggestions
        CleanupSuggestions.Clear();
        foreach (var suggestion in result.CleanupSuggestions)
        {
            CleanupSuggestions.Add(suggestion);
        }

        // Populate root items for tree view
        RootItems.Clear();
        FilteredRootItems.Clear();
        if (result.RootItem != null)
        {
            RootItems.Add(result.RootItem);
            FilteredRootItems.Add(result.RootItem);
        }

        // Build category pie chart
        BuildCategorySeries(result);
        
        // Update quick clean info
        UpdateQuickCleanInfo(result);
        
        // Initialize treemap legend
        UpdateTreemapLegend(SelectedTreemapColorMode);
    }

    private void BuildCategorySeries(ScanResult result)
    {
        var categoryColors = new Dictionary<ItemCategory, SKColor>
        {
            { ItemCategory.Document, SKColor.Parse("#4CAF50") },
            { ItemCategory.Image, SKColor.Parse("#2196F3") },
            { ItemCategory.Video, SKColor.Parse("#9C27B0") },
            { ItemCategory.Audio, SKColor.Parse("#FF9800") },
            { ItemCategory.Archive, SKColor.Parse("#795548") },
            { ItemCategory.Code, SKColor.Parse("#00BCD4") },
            { ItemCategory.Executable, SKColor.Parse("#F44336") },
            { ItemCategory.Game, SKColor.Parse("#673AB7") },
            { ItemCategory.System, SKColor.Parse("#607D8B") },
            { ItemCategory.Temporary, SKColor.Parse("#FF5722") },
            { ItemCategory.Other, SKColor.Parse("#9E9E9E") }
        };

        var series = new List<ISeries>();
        CategoryLegendItems.Clear();

        foreach (var kvp in result.CategoryBreakdown.OrderByDescending(c => c.Value.TotalSize))
        {
            var category = kvp.Key;
            var stats = kvp.Value;
            
            if (stats.TotalSize <= 0) continue;
            
            var color = categoryColors.GetValueOrDefault(category, SKColor.Parse("#9E9E9E"));
            
            series.Add(new PieSeries<double>
            {
                Values = new[] { (double)stats.TotalSize },
                Name = category.ToString(),
                Fill = new SolidColorPaint(color),
                Pushout = 0
            });

            CategoryLegendItems.Add(new CategoryLegendItem
            {
                Name = category.ToString(),
                CategoryKey = category.ToString(),
                Color = color.ToString(),
                FileCount = stats.FileCount,
                SizeFormatted = stats.SizeFormatted
            });
        }

        CategorySeries = series.ToArray();
    }

    private void UpdateQuickCleanInfo(ScanResult result)
    {
        var safeItems = result.CleanupSuggestions
            .Where(s => s.RiskLevel <= CleanupRisk.Low)
            .ToList();
        
        var totalSavings = safeItems.Sum(s => s.PotentialSavings);
        HasQuickCleanItems = safeItems.Count > 0;
        QuickCleanInfo = HasQuickCleanItems 
            ? $"({safeItems.Count} items, ~{FormatSize(totalSavings)})" 
            : string.Empty;
    }

    private static string FormatSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:N2} {suffixes[suffixIndex]}";
    }

    [RelayCommand]
    private void StopScan()
    {
        _cancellationTokenSource?.Cancel();
        CanStop = false;
        CanPause = false;
        CanResume = false;
        StatusText = "Scan stopped";
    }

    [RelayCommand]
    private void RecallLastScan()
    {
        if (!HasCachedScan) return;
        
        var cache = _settingsService.LoadScanCache();
        if (cache == null)
        {
            StatusText = "No cached scan available";
            return;
        }
        
        Console.WriteLine($"[ViewModel] Recalling cached scan from {cache.ScanDate}");
        
        // Reconstruct a ScanResult from the cache
        var result = ReconstructScanResultFromCache(cache);
        
        // Populate UI
        ScanResult = result;
        PopulateResultsFromScan(result);
        
        var timeAgo = GetTimeAgo(cache.ScanDate);
        var mode = cache.WasExpressMode ? "Express" : "Full";
        StatusText = $"Restored {mode} scan from {timeAgo}: {cache.TotalFiles:N0} files, {cache.TotalFolders:N0} folders";
    }
    
    /// <summary>
    /// Reconstructs a ScanResult from the cached data
    /// </summary>
    private ScanResult ReconstructScanResultFromCache(ScanCache cache)
    {
        // Reconstruct root tree from cache
        FileSystemItem? rootItem = null;
        if (cache.RootTree != null)
        {
            rootItem = ReconstructTreeNode(cache.RootTree);
        }
        
        // Reconstruct largest files
        var largestFiles = cache.LargestFiles.Select(f => new FileSystemItem
        {
            Name = f.Name,
            FullPath = f.FullPath,
            Size = f.Size,
            IsFolder = false,
            Category = Enum.TryParse<ItemCategory>(f.Category, out var cat) ? cat : ItemCategory.Other,
            LastAccessed = f.LastAccessed,
            LastModified = f.LastModified
        }).ToList();
        
        // Reconstruct largest folders
        var largestFolders = cache.LargestFolders.Select(f => 
        {
            var folder = new FileSystemItem
            {
                Name = f.Name,
                FullPath = f.FullPath,
                Size = f.Size,
                IsFolder = true,
                Category = ItemCategory.Other
            };
            foreach (var child in f.Children)
            {
                folder.Children.Add(new FileSystemItem
                {
                    Name = child.Name,
                    FullPath = child.FullPath,
                    Size = child.Size,
                    IsFolder = child.IsFolder,
                    Category = ItemCategory.Other
                });
            }
            return folder;
        }).ToList();
        
        // Reconstruct games
        var games = cache.Games.Select(g => new GameInstallation
        {
            Name = g.Name,
            Path = g.Path,
            Size = g.Size,
            Platform = Enum.TryParse<GamePlatform>(g.Platform, out var plat) ? plat : GamePlatform.Other,
            LastPlayed = g.LastPlayed
        }).ToList();
        
        // Reconstruct dev tools
        var devTools = cache.DevTools.Select(d => new CleanupItem
        {
            Name = d.Name,
            Path = d.Path,
            SizeBytes = d.SizeBytes,
            Category = d.Category,
            Recommendation = d.Recommendation,
            Risk = Enum.TryParse<CleanupRisk>(d.Risk, out var risk) ? risk : CleanupRisk.Low
        }).ToList();
        
        // Reconstruct cleanup suggestions
        var cleanupSuggestions = cache.CleanupSuggestions.Select(s => new CleanupSuggestion
        {
            Description = s.Description,
            Path = s.Path,
            PotentialSavings = s.PotentialSavings,
            RiskLevel = Enum.TryParse<CleanupRisk>(s.RiskLevel, out var risk) ? risk : CleanupRisk.Low
        }).ToList();
        
        // Reconstruct category breakdown
        var categoryBreakdown = cache.CategoryBreakdown.ToDictionary(
            c => Enum.TryParse<ItemCategory>(c.Category, out var cat) ? cat : ItemCategory.Other,
            c => new CategoryStats
            {
                TotalSize = c.TotalSize,
                FileCount = c.FileCount
            });
        
        return new ScanResult
        {
            RootPath = cache.RootPath,
            RootItem = rootItem,
            TotalSize = cache.TotalSize,
            TotalFiles = cache.TotalFiles,
            TotalFolders = cache.TotalFolders,
            ScanStarted = cache.ScanDate - cache.Duration,
            ScanCompleted = cache.ScanDate,
            LargestFiles = largestFiles,
            LargestFolders = largestFolders,
            GameInstallations = games,
            DevTools = devTools,
            CleanupSuggestions = cleanupSuggestions,
            CategoryBreakdown = categoryBreakdown,
            WasCancelled = false
        };
    }
    
    /// <summary>
    /// Recursively reconstructs a FileSystemItem from a CachedTreeNode
    /// </summary>
    private FileSystemItem ReconstructTreeNode(CachedTreeNode node)
    {
        var item = new FileSystemItem
        {
            Name = node.Name,
            FullPath = node.FullPath,
            Size = node.Size,
            IsFolder = node.IsFolder,
            Category = Enum.TryParse<ItemCategory>(node.Category, out var cat) ? cat : ItemCategory.Other
        };
        
        foreach (var childNode in node.Children)
        {
            item.Children.Add(ReconstructTreeNode(childNode));
        }
        
        return item;
    }

    [RelayCommand]
    private void PauseScan()
    {
        _fileScanner.Pause();
        CanPause = false;
        CanResume = true;
        ScanProgress.State = ScanState.Paused;
        StatusText = "Scan paused";
    }

    [RelayCommand]
    private void ResumeScan()
    {
        _fileScanner.Resume();
        CanPause = true;
        CanResume = false;
        ScanProgress.State = ScanState.Scanning;
        StatusText = "Scanning...";
    }

    [RelayCommand]
    private async Task BrowseFolder()
    {
        // TODO: Implement folder browser dialog
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void RefreshDrives()
    {
        LoadAvailableDrives();
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    [RelayCommand]
    private void SelectCategory(string? categoryName)
    {
        SelectedCategoryName = categoryName;
        CategoryFiles.Clear();
        
        if (string.IsNullOrEmpty(categoryName) || !RootItems.Any()) return;
        
        // Parse the category name to enum
        if (!Enum.TryParse<ItemCategory>(categoryName, out var category)) return;
        
        // Recursively find files matching this category from the root item
        var matchingFiles = new List<FileSystemItem>();
        CollectFilesByCategory(RootItems.First(), category, matchingFiles, 100);
        
        foreach (var file in matchingFiles.OrderByDescending(f => f.Size))
        {
            CategoryFiles.Add(file);
        }
    }

    private void CollectFilesByCategory(FileSystemItem item, ItemCategory category, List<FileSystemItem> results, int maxFiles)
    {
        if (results.Count >= maxFiles) return;
        
        // Check files (not folders)
        if (!item.IsFolder && item.Category == category)
        {
            results.Add(item);
        }
        
        // Recurse into children
        foreach (var child in item.Children)
        {
            if (results.Count >= maxFiles) break;
            CollectFilesByCategory(child, category, results, maxFiles);
        }
    }

    [RelayCommand]
    private void SelectCleanup(CleanupSuggestion? suggestion)
    {
        if (suggestion == null) return;
        
        SelectedCleanupDescription = suggestion.Description;
        
        CleanupFiles.Clear();
        foreach (var file in suggestion.AffectedFiles)
        {
            var fileInfo = CleanupFileInfo.FromPath(file, suggestion.RiskLevel);
            CleanupFiles.Add(fileInfo);
        }
    }

    [RelayCommand]
    private void SelectFolder(FileSystemItem? folder)
    {
        if (folder == null) return;
        
        SelectedFolderName = folder.Name;
        
        // Clear and populate FolderContents with the folder's children
        FolderContents.Clear();
        
        // If the folder has children already loaded, use them
        if (folder.Children.Any())
        {
            foreach (var child in folder.Children.OrderByDescending(c => c.Size).Take(50))
            {
                FolderContents.Add(child);
            }
        }
        else if (folder.IsFolder && System.IO.Directory.Exists(folder.FullPath))
        {
            // Otherwise, enumerate the folder contents directly
            try
            {
                var dirInfo = new System.IO.DirectoryInfo(folder.FullPath);
                var items = new List<FileSystemItem>();
                
                // Get top files and folders
                foreach (var subDir in dirInfo.EnumerateDirectories().Take(25))
                {
                    try
                    {
                        items.Add(new FileSystemItem
                        {
                            Name = subDir.Name,
                            FullPath = subDir.FullName,
                            IsFolder = true,
                            LastModified = subDir.LastWriteTime,
                            Size = 0 // Would need to calculate
                        });
                    }
                    catch { /* Skip inaccessible directories */ }
                }
                
                foreach (var file in dirInfo.EnumerateFiles().Take(25))
                {
                    try
                    {
                        items.Add(new FileSystemItem
                        {
                            Name = file.Name,
                            FullPath = file.FullName,
                            IsFolder = false,
                            Size = file.Length,
                            LastModified = file.LastWriteTime,
                            Extension = file.Extension
                        });
                    }
                    catch { /* Skip inaccessible files */ }
                }
                
                foreach (var item in items.OrderByDescending(i => i.Size).Take(50))
                {
                    FolderContents.Add(item);
                }
            }
            catch
            {
                // Handle access denied or other errors silently
            }
        }
    }

    [RelayCommand]
    private void OpenInExplorer(FileSystemItem? item)
    {
        if (item == null) return;
        _platformService.OpenInExplorer(item.FullPath);
    }

    [RelayCommand]
    private async Task CopyPath(FileSystemItem? item)
    {
        if (item == null) return;
        await CopyTextToClipboard(item.FullPath);
    }

    [RelayCommand]
    private void OpenGameFolder(GameInstallation? game)
    {
        if (game == null) return;
        _platformService.OpenInExplorer(game.Path);
    }

    [RelayCommand]
    private async Task CopyGamePath(GameInstallation? game)
    {
        if (game == null) return;
        await CopyTextToClipboard(game.Path);
    }

    [RelayCommand]
    private void OpenDevToolPath(CleanupItem? item)
    {
        if (item == null) return;
        _platformService.OpenInExplorer(item.Path);
    }

    [RelayCommand]
    private async Task CopyDevToolPath(CleanupItem? item)
    {
        if (item == null) return;
        await CopyTextToClipboard(item.Path);
    }

    [RelayCommand]
    private void OpenCleanupFilePath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        // Open the folder containing the file
        var folder = System.IO.Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder))
        {
            _platformService.OpenInExplorer(folder);
        }
    }

    [RelayCommand]
    private async Task CopyCleanupFilePath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        await CopyTextToClipboard(filePath);
    }

    private async Task CopyTextToClipboard(string text)
    {
        // Use Avalonia clipboard
        if (global::Avalonia.Application.Current?.ApplicationLifetime 
            is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
            }
        }
    }

    [RelayCommand]
    private async Task QuickClean()
    {
        // TODO: Implement quick clean functionality
        await Task.CompletedTask;
    }

    #endregion
}

/// <summary>
/// Represents a legend item for the treemap visualization
/// </summary>
public class TreemapLegendItem
{
    public string Label { get; set; } = string.Empty;
    public string Color { get; set; } = "#808080";
}

/// <summary>
/// Represents a legend item for the category pie chart
/// </summary>
public class CategoryLegendItem
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#808080";
    public int FileCount { get; set; }
    public string CategoryKey { get; set; } = string.Empty;
    public string SizeFormatted { get; set; } = string.Empty;
    public string DisplayText => $"{Name} ({SizeFormatted})";
    public string FileCountText => $"({FileCount} files)";
}
