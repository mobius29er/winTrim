using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
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
/// Flat row for virtualized duplicate file display
/// </summary>
public class DuplicateFileRow : System.ComponentModel.INotifyPropertyChanged
{
    private bool _isSelected;
    
    /// <summary>When true, suppresses PropertyChanged events for bulk operations</summary>
    public static bool SuppressNotifications { get; set; }
    
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    
    /// <summary>The underlying file</summary>
    public DuplicateFile File { get; init; } = null!;
    
    /// <summary>The group this file belongs to</summary>
    public DuplicateGroup Group { get; init; } = null!;
    
    /// <summary>Group number for display</summary>
    public int GroupNumber { get; init; }
    
    /// <summary>Whether this is an even-numbered group (for alternating background)</summary>
    public bool IsEvenGroup => GroupNumber % 2 == 0;
    
    /// <summary>Whether this row is the first in its group (for visual separation)</summary>
    public bool IsFirstInGroup { get; init; }
    
    /// <summary>File name</summary>
    public string Name => File.Name;
    
    /// <summary>Directory path</summary>
    public string Directory => File.Directory;
    
    /// <summary>Full path for searching</summary>
    public string FullPath => File.FullPath;
    
    /// <summary>File size</summary>
    public long Size => File.Size;
    
    /// <summary>Last modified date</summary>
    public DateTime LastModified => File.LastModified;
    
    /// <summary>Whether this is the original (keep) file</summary>
    public bool IsOriginal => File.IsOriginal;
    
    /// <summary>Match percentage (100% for exact hash match)</summary>
    public int MatchPercent => 100; // Hash-based = exact match
    
    /// <summary>Match type description</summary>
    public string MatchType => "Exact";
    
    /// <summary>Whether this file is selected for deletion</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value && !IsOriginal)
            {
                _isSelected = value;
                File.IsMarkedForDeletion = value;
                // Only fire PropertyChanged when not in bulk update mode
                if (!SuppressNotifications)
                {
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }
    }
    
    /// <summary>Set selection without triggering any events</summary>
    public void SetSelectedSilent(bool value)
    {
        if (!IsOriginal)
        {
            _isSelected = value;
            File.IsMarkedForDeletion = value;
        }
    }
    
    /// <summary>Number of files in group</summary>
    public int GroupFileCount => Group.Files.Count;
    
    /// <summary>Wasted space in group</summary>
    public long GroupWastedSpace => Group.WastedSpace;
}

/// <summary>
/// Item in the Collector (staging area for files to be deleted)
/// </summary>
public class CollectorItem
{
    /// <summary>Full path to the file or folder</summary>
    public string FullPath { get; init; } = string.Empty;
    
    /// <summary>Display name</summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>Size in bytes</summary>
    public long Size { get; init; }
    
    /// <summary>Whether this is a folder</summary>
    public bool IsFolder { get; init; }
    
    /// <summary>Icon for display</summary>
    public string Icon => IsFolder ? "📁" : GetFileIcon();
    
    /// <summary>Source description (e.g., "Duplicates", "Large Files", etc.)</summary>
    public string Source { get; init; } = string.Empty;
    
    private string GetFileIcon()
    {
        var ext = System.IO.Path.GetExtension(Name)?.ToLowerInvariant();
        return ext switch
        {
            ".app" => "📦",
            ".dmg" or ".pkg" => "💿",
            ".zip" or ".tar" or ".gz" or ".rar" or ".7z" => "🗜️",
            ".mp3" or ".wav" or ".aac" or ".flac" => "🎵",
            ".mp4" or ".mov" or ".avi" or ".mkv" => "🎬",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => "🖼️",
            ".pdf" => "📄",
            ".doc" or ".docx" => "📝",
            ".xls" or ".xlsx" => "📊",
            ".log" or ".txt" => "📋",
            ".cache" or ".tmp" => "🗑️",
            _ => "📄"
        };
    }
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
    private readonly IFileAccessManager _fileAccessManager;
    private readonly IExportService _exportService;
    private readonly IDuplicateScanner _duplicateScanner;
    private readonly ITimeMachineAnalyzer _timeMachineAnalyzer;
    private readonly IAppLogger _logger;
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

    /// <summary>
    /// Selected folder path from folder picker (takes priority over drive selection)
    /// </summary>
    [ObservableProperty]
    private string? _selectedFolderPath;

    /// <summary>
    /// Display name for the selected folder
    /// </summary>
    [ObservableProperty]
    private string _selectedFolderDisplayName = string.Empty;

    /// <summary>
    /// List of recently accessed folders (for quick selection)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<GrantedFolder> _recentFolders = new();

    /// <summary>
    /// Whether to show the folder picker mode (for sandbox compliance)
    /// </summary>
    [ObservableProperty]
    private bool _useFolderPickerMode;

    #endregion

    /// <summary>
    /// Constructor with DI-injected services
    /// </summary>
    public MainWindowViewModel(IFileScanner fileScanner, IPlatformService platformService, IThemeService themeService, ISettingsService settingsService, IFileAccessManager fileAccessManager, IExportService exportService, IDuplicateScanner duplicateScanner, ITimeMachineAnalyzer timeMachineAnalyzer, IAppLogger logger)
    {
        _logger = logger;
        _logger.LogInfo("MainWindowViewModel constructor called");
        _fileScanner = fileScanner;
        _platformService = platformService;
        _themeService = themeService;
        _settingsService = settingsService;
        _fileAccessManager = fileAccessManager;
        _exportService = exportService;
        _duplicateScanner = duplicateScanner;
        _timeMachineAnalyzer = timeMachineAnalyzer;
        
        // On macOS, use folder picker mode for sandbox compliance
        // On Windows/Linux, drive dropdown works fine but folder picker is also available
        UseFolderPickerMode = OperatingSystem.IsMacOS();
        
        // Load saved theme from settings (or use default)
        var savedTheme = _settingsService.Theme;
        if (AvailableThemes.Contains(savedTheme))
        {
            _selectedTheme = savedTheme; // Set backing field directly to avoid triggering save
        }
        _themeService.ApplyTheme(_selectedTheme);
        _logger.LogInfo($"Loaded theme from settings: {_selectedTheme}");
        
        // Load settings
        ExpressScanEnabled = _settingsService.ExpressScanEnabled;
        
        // Load or create cleanup folder
        var savedCleanupPath = _settingsService.CleanupFolderPath;
        if (!string.IsNullOrEmpty(savedCleanupPath) && Directory.Exists(savedCleanupPath))
        {
            _cleanupFolderPath = savedCleanupPath;
        }
        else
        {
            // Auto-create a DeleteMe folder on the Desktop
            EnsureDeleteMeFolderExists();
        }
        
        LoadAvailableDrives();
        _ = LoadRecentFoldersAsync(); // Fire and forget - loads recent folders for quick access
        UpdateCachedScanInfo();
        _logger.LogInfo($"Constructor complete. AvailableDrives: {AvailableDrives.Count}, SelectedDrive: {SelectedDrive?.DisplayName ?? "null"}");
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
    /// Loads recent folders from the file access manager
    /// </summary>
    private async Task LoadRecentFoldersAsync()
    {
        try
        {
            var folders = await _fileAccessManager.GetGrantedFoldersAsync();
            RecentFolders.Clear();
            foreach (var folder in folders)
            {
                RecentFolders.Add(folder);
            }
            _logger.LogDebug($"Loaded {folders.Count} recent folders");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to load recent folders: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Called when SelectedTheme property changes
    /// </summary>
    partial void OnSelectedThemeChanged(string value)
    {
        _logger.LogDebug($"OnSelectedThemeChanged called with: {value}");
        _themeService.ApplyTheme(value);
        _settingsService.Theme = value; // Persist selection
    }
    
    /// <summary>
    /// Called when SelectedFontSize property changes
    /// </summary>
    partial void OnSelectedFontSizeChanged(int value)
    {
        _logger.LogDebug($"OnSelectedFontSizeChanged called with: {value}");
        _themeService.ApplyFontSize(value);
    }

    /// <summary>
    /// Called when SelectedTreemapColorMode property changes
    /// </summary>
    partial void OnSelectedTreemapColorModeChanged(string value)
    {
        _logger.LogDebug($"OnSelectedTreemapColorModeChanged called with: {value}");
        UpdateTreemapLegend(value);
    }

    partial void OnSelectedItemChanged(FileSystemItem? value)
    {
        ApplyFileExplorerFilter();
    }

    partial void OnFileExplorerSearchTextChanged(string value)
    {
        ApplyFileExplorerFilter();
    }

    partial void OnFileExplorerFilterChanged(string value)
    {
        ApplyFileExplorerFilter();
    }

    partial void OnTreeSearchTextChanged(string value)
    {
        ApplyTreeFilter();
    }

    partial void OnTreeSortByChanged(string value)
    {
        ApplyTreeFilter();
    }

    private void ApplyFileExplorerFilter()
    {
        FilteredChildren.Clear();

        if (SelectedItem == null)
            return;

        var children = SelectedItem.Children.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(FileExplorerSearchText))
        {
            var search = FileExplorerSearchText;
            children = children.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        // Apply type filter
        children = FileExplorerFilter switch
        {
            "Large Files" => children.Where(c => c.Size > 100 * 1024 * 1024), // > 100MB
            "Old Files" => children.Where(c => c.IsStale),
            _ => children
        };

        foreach (var child in children.OrderByDescending(c => c.Size))
            FilteredChildren.Add(child);
    }

    private void ApplyTreeFilter()
    {
        if (ScanResult?.RootItem == null)
            return;

        FilteredRootItems.Clear();

        if (string.IsNullOrWhiteSpace(TreeSearchText))
        {
            FilteredRootItems.Add(ScanResult.RootItem);
        }
        else
        {
            // Filter: show root but the search text highlights matching folders
            // For simplicity, still show full tree (TreeView search is visual)
            FilteredRootItems.Add(ScanResult.RootItem);
        }
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

    /// <summary>
    /// Opens folder picker dialog to select a folder to scan.
    /// Required for macOS sandbox compliance.
    /// </summary>
    [RelayCommand]
    private async Task ChooseFolder()
    {
        _logger.LogInfo("ChooseFolder command invoked");
        
        var folderPath = await _fileAccessManager.RequestFolderAccessAsync();
        if (!string.IsNullOrEmpty(folderPath))
        {
            SelectedFolderPath = folderPath;
            SelectedFolderDisplayName = Path.GetFileName(folderPath) ?? folderPath;
            
            // Refresh recent folders list
            await LoadRecentFoldersAsync();
            
            _logger.LogInfo($"Folder selected: {folderPath}");
        }
    }

    /// <summary>
    /// Select a folder from the recent folders list
    /// </summary>
    [RelayCommand]
    private async Task SelectRecentFolder(GrantedFolder folder)
    {
        if (folder == null) return;
        
        _logger.LogInfo($"Selecting recent folder: {folder.Path}");
        
        // On macOS, we need to restore access from bookmark before scanning
        if (OperatingSystem.IsMacOS() && _fileAccessManager is Services.AvaloniaFileAccessManager avaloniaManager)
        {
            var hasAccess = await avaloniaManager.RestoreAccessFromBookmarkAsync(folder.Path);
            if (!hasAccess)
            {
                _logger.LogWarning($"Could not restore access to {folder.Path}, requesting new access");
                // Bookmark may have expired - need to request access again
                StatusText = "Access expired. Please choose the folder again.";
                return;
            }
        }
        
        SelectedFolderPath = folder.Path;
        SelectedFolderDisplayName = folder.DisplayName;
    }

    /// <summary>
    /// Remove a folder from recent folders list
    /// </summary>
    [RelayCommand]
    private async Task RemoveRecentFolder(GrantedFolder folder)
    {
        if (folder == null) return;
        
        await _fileAccessManager.RevokeAccessAsync(folder.Path);
        await LoadRecentFoldersAsync();
        
        // Clear selection if we removed the selected folder
        if (SelectedFolderPath == folder.Path)
        {
            SelectedFolderPath = null;
            SelectedFolderDisplayName = string.Empty;
        }
    }

    [RelayCommand]
    private async Task StartScan()
    {
        // Determine scan path - prefer folder picker selection, fallback to drive selection
        string? scanPath = null;
        string displayName = string.Empty;
        var scanModeLabel = "Full";

        if (!string.IsNullOrEmpty(SelectedFolderPath) && Directory.Exists(SelectedFolderPath))
        {
            // Use folder picker selection (required for macOS sandbox)
            scanPath = SelectedFolderPath;
            displayName = SelectedFolderDisplayName;
            
            // On macOS, ensure we have access before scanning
            if (OperatingSystem.IsMacOS())
            {
                if (!_fileAccessManager.StartAccessingSecurityScopedResource(scanPath))
                {
                    // Try to restore from bookmark
                    if (_fileAccessManager is Services.AvaloniaFileAccessManager avaloniaManager)
                    {
                        var hasAccess = await avaloniaManager.RestoreAccessFromBookmarkAsync(scanPath);
                        if (!hasAccess)
                        {
                            StatusText = "Access denied. Please choose the folder again.";
                            _logger.LogWarning($"No access to {scanPath}");
                            return;
                        }
                    }
                }
            }
        }
        else if (SelectedDrive != null)
        {
            // Fallback to drive selection (Windows/Linux, or non-sandboxed macOS)
            scanPath = SelectedDrive.Path;
            displayName = SelectedDrive.DisplayName;
            
            if (ExpressScanEnabled && SelectedDrive.Path == "/")
            {
                // Express mode: scan only the current user's home directory
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(homeDir) && Directory.Exists(homeDir))
                {
                    scanPath = homeDir;
                    displayName = Path.GetFileName(homeDir) ?? "Home";
                    scanModeLabel = "Express";
                }
            }
        }
        else
        {
            StatusText = "Please select a folder to scan";
            return;
        }

        _logger.LogInfo($"StartScan called for: {displayName} ({scanPath}) - {scanModeLabel} mode");
        
        CanStart = false;
        CanStop = true;
        CanPause = true;
        ScanProgress.Reset();
        ScanProgress.State = ScanState.Scanning;
        StatusText = ExpressScanEnabled && scanModeLabel == "Express" 
            ? $"Express scanning {Path.GetFileName(scanPath)}..." 
            : $"Scanning {displayName}...";

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
        
        _logger.LogDebug($"PopulateResults - RootItem: {result.RootItem?.Name ?? "null"}");
        _logger.LogDebug($"PopulateResults - LargestFiles: {result.LargestFiles.Count}, Games: {result.GameInstallations.Count}, DevTools: {result.DevTools.Count}");
        
        // Populate largest files
        LargestFiles.Clear();
        foreach (var file in result.LargestFiles)
        {
            LargestFiles.Add(file);
        }
        _logger.LogDebug($"PopulateResults - LargestFiles collection now has: {LargestFiles.Count} items");

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
        
        _logger.LogInfo($"Recalling cached scan from {cache.ScanDate}");
        
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
        // Use the file access manager for sandbox-compliant folder selection
        await ChooseFolder();
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

    #region Export Commands

    [RelayCommand]
    private async Task ExportToCsv()
    {
        if (ScanResult == null)
        {
            StatusText = "No scan results to export";
            return;
        }

        var storageProvider = GetStorageProvider();
        if (storageProvider == null) return;

        var file = await storageProvider.SaveFilePickerAsync(new global::Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Export to CSV",
            SuggestedFileName = $"WinTrim_Export_{DateTime.Now:yyyy-MM-dd}",
            DefaultExtension = "csv",
            FileTypeChoices = new[]
            {
                new global::Avalonia.Platform.Storage.FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } }
            }
        });

        if (file != null)
        {
            var path = file.TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
            {
                await _exportService.ExportToCsvAsync(ScanResult, path);
                StatusText = $"Exported to {Path.GetFileName(path)}";
            }
        }
    }

    [RelayCommand]
    private async Task ExportToJson()
    {
        if (ScanResult == null)
        {
            StatusText = "No scan results to export";
            return;
        }

        var storageProvider = GetStorageProvider();
        if (storageProvider == null) return;

        var file = await storageProvider.SaveFilePickerAsync(new global::Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Export to JSON",
            SuggestedFileName = $"WinTrim_Export_{DateTime.Now:yyyy-MM-dd}",
            DefaultExtension = "json",
            FileTypeChoices = new[]
            {
                new global::Avalonia.Platform.Storage.FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } }
            }
        });

        if (file != null)
        {
            var path = file.TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
            {
                await _exportService.ExportToJsonAsync(ScanResult, path);
                StatusText = $"Exported to {Path.GetFileName(path)}";
            }
        }
    }

    private IStorageProvider? GetStorageProvider()
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.StorageProvider;
        }
        return null;
    }

    #endregion

    #region Delete Commands

    /// <summary>
    /// Whether we're showing a delete confirmation
    /// </summary>
    [ObservableProperty]
    private bool _isDeleteConfirmationOpen;

    /// <summary>
    /// Item pending deletion (for confirmation dialog)
    /// </summary>
    [ObservableProperty]
    private FileSystemItem? _itemToDelete;

    /// <summary>
    /// Request to delete the selected item (shows confirmation)
    /// </summary>
    [RelayCommand]
    private void RequestDeleteItem(FileSystemItem? item)
    {
        if (item == null) return;
        
        ItemToDelete = item;
        IsDeleteConfirmationOpen = true;
        _logger.LogInfo($"Delete requested for: {item.FullPath}");
    }

    /// <summary>
    /// Confirm and execute deletion
    /// </summary>
    [RelayCommand]
    private void ConfirmDelete()
    {
        if (ItemToDelete == null) return;

        var path = ItemToDelete.FullPath;
        var name = ItemToDelete.Name;
        var size = ItemToDelete.Size;

        try
        {
            var success = _platformService.MoveToTrash(path);
            if (success)
            {
                // Remove from UI
                RemoveItemFromTree(ItemToDelete);
                StatusText = $"Moved to Trash: {name} ({FormatSize(size)})";
                _logger.LogInfo($"Deleted: {path}");
            }
            else
            {
                StatusText = $"Failed to delete: {name}";
                _logger.LogWarning($"Failed to delete: {path}");
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error deleting: {ex.Message}";
            _logger.LogError($"Delete error: {ex.Message}", ex);
        }
        finally
        {
            IsDeleteConfirmationOpen = false;
            ItemToDelete = null;
        }
    }

    /// <summary>
    /// Cancel deletion
    /// </summary>
    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteConfirmationOpen = false;
        ItemToDelete = null;
    }

    /// <summary>
    /// Open the item's containing folder in file explorer
    /// </summary>
    [RelayCommand]
    private void RevealInFinder(object? item)
    {
        string? path = item switch
        {
            FileSystemItem fsi => fsi.FullPath,
            DuplicateFile df => df.FullPath,
            string s => s,
            _ => null
        };
        
        if (!string.IsNullOrEmpty(path))
        {
            _platformService.OpenInExplorer(path);
        }
    }

    /// <summary>
    /// Open the duplicate file row's containing folder in file explorer
    /// </summary>
    [RelayCommand]
    private void RevealDuplicateInFinder(DuplicateFileRow? row)
    {
        if (row?.File?.FullPath != null)
        {
            _platformService.OpenInExplorer(row.File.FullPath);
        }
    }

    /// <summary>
    /// Remove an item from the tree after deletion
    /// </summary>
    private void RemoveItemFromTree(FileSystemItem item)
    {
        // Remove from parent's children
        if (item.Parent != null)
        {
            item.Parent.Children.Remove(item);
            
            // Update parent sizes up the tree
            var current = item.Parent;
            while (current != null)
            {
                current.Size -= item.Size;
                current = current.Parent;
            }
        }

        // Remove from flat lists
        LargestFiles.Remove(item);
        LargestFolders.Remove(item);
        
        // Refresh treemap
        OnPropertyChanged(nameof(TreemapRootItem));
    }

    #endregion

    #region Collector (DaisyDisk-style staging area)

    /// <summary>
    /// Items collected for potential deletion
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CollectorItem> _collectorItems = new();

    /// <summary>
    /// Whether the collector panel is expanded
    /// </summary>
    [ObservableProperty]
    private bool _isCollectorOpen;

    /// <summary>
    /// Path to the cleanup folder where files are moved
    /// </summary>
    [ObservableProperty]
    private string _cleanupFolderPath = string.Empty;

    /// <summary>
    /// Whether a cleanup folder has been set
    /// </summary>
    public bool HasCleanupFolder => !string.IsNullOrEmpty(CleanupFolderPath) && Directory.Exists(CleanupFolderPath);

    /// <summary>
    /// Display name for the cleanup folder
    /// </summary>
    public string CleanupFolderName => HasCleanupFolder ? Path.GetFileName(CleanupFolderPath) : "Not Set";

    /// <summary>
    /// Total size of items in collector
    /// </summary>
    public long CollectorTotalSize => CollectorItems.Sum(i => i.Size);

    /// <summary>
    /// Formatted total size
    /// </summary>
    public string CollectorTotalSizeFormatted => FormatSize(CollectorTotalSize);

    /// <summary>
    /// Number of items in collector
    /// </summary>
    public int CollectorItemCount => CollectorItems.Count;

    /// <summary>
    /// Whether collector has items
    /// </summary>
    public bool HasCollectorItems => CollectorItems.Count > 0;

    /// <summary>
    /// Storage provider for folder picker (set from View)
    /// </summary>
    public IStorageProvider? StorageProvider { get; set; }

    /// <summary>
    /// Choose/create the cleanup folder
    /// </summary>
    [RelayCommand]
    private async Task ChooseCleanupFolderAsync()
    {
        if (StorageProvider == null) return;

        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose or Create a Cleanup Folder",
            AllowMultiple = false
        });

        if (result.Count > 0)
        {
            var folder = result[0];
            CleanupFolderPath = folder.Path.LocalPath;
            _settingsService.CleanupFolderPath = CleanupFolderPath;
            OnPropertyChanged(nameof(HasCleanupFolder));
            OnPropertyChanged(nameof(CleanupFolderName));
            StatusText = $"Cleanup folder set to: {CleanupFolderName}";
            _logger.LogInfo($"Cleanup folder set: {CleanupFolderPath}");
        }
    }

    /// <summary>
    /// Auto-create a DeleteMe folder on the Desktop
    /// </summary>
    private void EnsureDeleteMeFolderExists()
    {
        try
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var deleteMePath = Path.Combine(desktopPath, "DeleteMe");
            
            if (!Directory.Exists(deleteMePath))
            {
                Directory.CreateDirectory(deleteMePath);
                _logger.LogInfo($"Created DeleteMe folder: {deleteMePath}");
            }
            
            CleanupFolderPath = deleteMePath;
            _settingsService.CleanupFolderPath = deleteMePath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Could not create DeleteMe folder: {ex.Message}");
        }
    }

    /// <summary>
    /// Reveal cleanup folder in Finder
    /// </summary>
    [RelayCommand]
    private void RevealCleanupFolder()
    {
        if (HasCleanupFolder)
        {
            _platformService.OpenInExplorer(CleanupFolderPath);
        }
    }

    /// <summary>
    /// Add a file system item to the collector
    /// </summary>
    [RelayCommand]
    private void AddToCollector(FileSystemItem? item)
    {
        if (item == null) return;
        
        // Don't add duplicates
        if (CollectorItems.Any(c => c.FullPath == item.FullPath)) 
        {
            StatusText = $"Already in Collector: {item.Name}";
            return;
        }
        
        CollectorItems.Add(new CollectorItem
        {
            FullPath = item.FullPath,
            Name = item.Name,
            Size = item.Size,
            IsFolder = item.IsFolder,
            Source = "File Browser"
        });
        
        UpdateCollectorStats();
        IsCollectorOpen = true;
        StatusText = $"Added to Collector: {item.Name}";
        _logger.LogInfo($"Added to collector: {item.FullPath}");
    }

    /// <summary>
    /// Add a duplicate file to the collector
    /// </summary>
    [RelayCommand]
    private void AddDuplicateToCollector(DuplicateFileRow? row)
    {
        if (row == null || row.IsOriginal) return;
        
        // Don't add duplicates
        if (CollectorItems.Any(c => c.FullPath == row.FullPath)) 
        {
            StatusText = $"Already in Collector: {row.Name}";
            return;
        }
        
        CollectorItems.Add(new CollectorItem
        {
            FullPath = row.FullPath,
            Name = row.Name,
            Size = row.Size,
            IsFolder = false,
            Source = "Duplicate"
        });
        
        UpdateCollectorStats();
        IsCollectorOpen = true;
        StatusText = $"Added to Collector: {row.Name}";
    }

    /// <summary>
    /// Add selected duplicates to collector
    /// </summary>
    [RelayCommand]
    private void AddSelectedDuplicatesToCollector()
    {
        var selected = DuplicateFileRows
            .Where(r => r.IsSelected && !r.IsOriginal)
            .ToList();

        if (!selected.Any()) return;

        var addedCount = 0;
        foreach (var row in selected)
        {
            if (!CollectorItems.Any(c => c.FullPath == row.FullPath))
            {
                CollectorItems.Add(new CollectorItem
                {
                    FullPath = row.FullPath,
                    Name = row.Name,
                    Size = row.Size,
                    IsFolder = false,
                    Source = "Duplicate"
                });
                addedCount++;
            }
        }
        
        UpdateCollectorStats();
        IsCollectorOpen = true;
        StatusText = $"Added {addedCount} items to Collector";
    }

    /// <summary>
    /// Add a generic path to the collector (for drag-drop)
    /// </summary>
    public void AddPathToCollector(string path, string source = "Drop")
    {
        if (string.IsNullOrEmpty(path)) return;
        
        // Don't add duplicates
        if (CollectorItems.Any(c => c.FullPath == path)) return;
        
        try
        {
            var isFolder = Directory.Exists(path);
            var name = Path.GetFileName(path);
            long size = 0;
            
            if (isFolder)
            {
                var dirInfo = new DirectoryInfo(path);
                size = GetDirectorySize(dirInfo);
            }
            else if (File.Exists(path))
            {
                size = new FileInfo(path).Length;
            }
            
            CollectorItems.Add(new CollectorItem
            {
                FullPath = path,
                Name = name,
                Size = size,
                IsFolder = isFolder,
                Source = source
            });
            
            UpdateCollectorStats();
            IsCollectorOpen = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to add to collector: {path} - {ex.Message}");
        }
    }

    private long GetDirectorySize(DirectoryInfo dir)
    {
        try
        {
            return dir.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Remove an item from the collector
    /// </summary>
    [RelayCommand]
    private void RemoveFromCollector(CollectorItem? item)
    {
        if (item == null) return;
        
        CollectorItems.Remove(item);
        UpdateCollectorStats();
        StatusText = $"Removed from Collector: {item.Name}";
    }

    /// <summary>
    /// Clear all items from collector
    /// </summary>
    [RelayCommand]
    private void ClearCollector()
    {
        CollectorItems.Clear();
        UpdateCollectorStats();
        StatusText = "Collector cleared";
    }

    /// <summary>
    /// Toggle collector panel visibility
    /// </summary>
    [RelayCommand]
    private void ToggleCollector()
    {
        IsCollectorOpen = !IsCollectorOpen;
    }

    /// <summary>
    /// Move all collector items to the Cleanup Folder
    /// </summary>
    [RelayCommand]
    private async Task MoveToCleanupFolderAsync()
    {
        if (!CollectorItems.Any()) return;
        
        // If no cleanup folder is set, prompt user to choose one
        if (!HasCleanupFolder)
        {
            await ChooseCleanupFolderAsync();
            if (!HasCleanupFolder)
            {
                StatusText = "Please choose a cleanup folder first";
                return;
            }
        }
        
        var items = CollectorItems.ToList();
        var movedCount = 0;
        var movedSpace = 0L;
        var failedItems = new List<string>();
        
        foreach (var item in items)
        {
            try
            {
                var destPath = GetUniqueDestinationPath(item.FullPath, CleanupFolderPath);
                
                if (item.IsFolder && Directory.Exists(item.FullPath))
                {
                    Directory.Move(item.FullPath, destPath);
                    movedCount++;
                    movedSpace += item.Size;
                    CollectorItems.Remove(item);
                }
                else if (File.Exists(item.FullPath))
                {
                    File.Move(item.FullPath, destPath);
                    movedCount++;
                    movedSpace += item.Size;
                    CollectorItems.Remove(item);
                }
                else
                {
                    failedItems.Add($"{item.Name} (not found)");
                    CollectorItems.Remove(item); // Remove stale items
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to move {item.FullPath}: {ex.Message}");
                failedItems.Add(item.Name);
            }
        }
        
        UpdateCollectorStats();
        
        if (movedCount > 0)
        {
            if (failedItems.Any())
            {
                StatusText = $"Moved {movedCount} items ({FormatSize(movedSpace)}), {failedItems.Count} failed";
            }
            else
            {
                StatusText = $"Moved {movedCount} items to cleanup folder ({FormatSize(movedSpace)})";
                IsCollectorOpen = false;
            }
            
            // Rescan duplicates if needed
            if (DuplicateGroups.Any())
            {
                await ScanForDuplicatesAsync();
            }
        }
        else if (failedItems.Any())
        {
            StatusText = $"Failed to move {failedItems.Count} items. Check folder permissions.";
        }
    }

    /// <summary>
    /// Get a unique destination path, adding numbers if file already exists
    /// </summary>
    private string GetUniqueDestinationPath(string sourcePath, string destFolder)
    {
        var fileName = Path.GetFileName(sourcePath);
        var destPath = Path.Combine(destFolder, fileName);
        
        if (!File.Exists(destPath) && !Directory.Exists(destPath))
            return destPath;
        
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var counter = 1;
        
        while (File.Exists(destPath) || Directory.Exists(destPath))
        {
            destPath = Path.Combine(destFolder, $"{nameWithoutExt} ({counter}){ext}");
            counter++;
        }
        
        return destPath;
    }

    private void UpdateCollectorStats()
    {
        OnPropertyChanged(nameof(CollectorTotalSize));
        OnPropertyChanged(nameof(CollectorTotalSizeFormatted));
        OnPropertyChanged(nameof(CollectorItemCount));
        OnPropertyChanged(nameof(HasCollectorItems));
    }

    #endregion

    #region Duplicate Scanner

    /// <summary>
    /// Results from the duplicate scan (groups for logic)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DuplicateGroup> _duplicateGroups = new();

    /// <summary>
    /// Flat list of all duplicate files for virtualized display
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DuplicateFileRow> _duplicateFileRows = new();

    /// <summary>
    /// Filtered view of duplicate file rows based on search
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DuplicateFileRow> _filteredDuplicateRows = new();

    /// <summary>
    /// Search text for filtering duplicates
    /// </summary>
    private string _duplicateSearchText = string.Empty;
    public string DuplicateSearchText
    {
        get => _duplicateSearchText;
        set
        {
            if (SetProperty(ref _duplicateSearchText, value))
            {
                FilterDuplicateRows();
            }
        }
    }

    /// <summary>
    /// Filter duplicate rows based on search text
    /// </summary>
    private void FilterDuplicateRows()
    {
        if (string.IsNullOrWhiteSpace(DuplicateSearchText))
        {
            FilteredDuplicateRows = new ObservableCollection<DuplicateFileRow>(DuplicateFileRows);
        }
        else
        {
            var searchLower = DuplicateSearchText.ToLowerInvariant();
            var filtered = DuplicateFileRows
                .Where(r => r.Name.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                           r.Directory.Contains(searchLower, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            // Re-number groups for filtered results and mark first-in-group
            var reNumbered = new List<DuplicateFileRow>();
            var seenGroups = new HashSet<int>();
            foreach (var row in filtered)
            {
                // We keep the original GroupNumber for coloring but track first-in-filtered-view
                reNumbered.Add(row);
            }
            
            FilteredDuplicateRows = new ObservableCollection<DuplicateFileRow>(reNumbered);
        }
        
        OnPropertyChanged(nameof(FilteredDuplicateCount));
    }

    /// <summary>
    /// Count of filtered rows
    /// </summary>
    public int FilteredDuplicateCount => FilteredDuplicateRows.Count;

    /// <summary>
    /// Whether a duplicate scan is in progress
    /// </summary>
    [ObservableProperty]
    private bool _isDuplicateScanRunning;

    /// <summary>
    /// Progress message for duplicate scan
    /// </summary>
    [ObservableProperty]
    private string _duplicateScanStatus = string.Empty;

    /// <summary>
    /// Progress percentage for duplicate scan (0-100)
    /// </summary>
    [ObservableProperty]
    private double _duplicateScanProgress;

    /// <summary>
    /// Files processed during duplicate scan
    /// </summary>
    [ObservableProperty]
    private int _duplicateFilesProcessed;

    /// <summary>
    /// Total files to process during duplicate scan
    /// </summary>
    [ObservableProperty]
    private int _duplicateTotalFiles;

    /// <summary>
    /// Formatted progress string (e.g., "45%")
    /// </summary>
    public string DuplicateScanProgressFormatted => DuplicateTotalFiles > 0 ? $"{DuplicateScanProgress:F0}%" : "";

    /// <summary>
    /// Formatted files progress string (e.g., "1,234 / 5,678") - empty during initial scan
    /// </summary>
    public string DuplicateFilesProgressFormatted => DuplicateTotalFiles > 0 
        ? $"{DuplicateFilesProcessed:N0} / {DuplicateTotalFiles:N0}" 
        : "";

    /// <summary>
    /// Total wasted space from duplicates
    /// </summary>
    [ObservableProperty]
    private long _totalDuplicateWastedSpace;

    /// <summary>
    /// Total number of duplicate files
    /// </summary>
    [ObservableProperty]
    private int _totalDuplicateCount;

    /// <summary>
    /// Selected duplicates count and size
    /// </summary>
    [ObservableProperty]
    private int _selectedDuplicatesCount;

    [ObservableProperty]
    private long _selectedDuplicatesSize;

    /// <summary>
    /// Formatted wasted space string
    /// </summary>
    public string DuplicateWastedSpaceFormatted => FormatSize(TotalDuplicateWastedSpace);

    /// <summary>
    /// Formatted selected duplicates size
    /// </summary>
    public string SelectedDuplicatesSizeFormatted => FormatSize(SelectedDuplicatesSize);

    /// <summary>
    /// Whether we have duplicates to show
    /// </summary>
    public bool HasDuplicates => DuplicateFileRows.Count > 0;

    /// <summary>
    /// Whether we have selected duplicates
    /// </summary>
    public bool HasSelectedDuplicates => SelectedDuplicatesCount > 0;

    /// <summary>
    /// Update selected duplicates count/size
    /// </summary>
    private void UpdateSelectedDuplicatesStats()
    {
        var selected = DuplicateFileRows
            .Where(r => r.IsSelected)
            .ToList();
        
        SelectedDuplicatesCount = selected.Count;
        SelectedDuplicatesSize = selected.Sum(r => r.Size);
        OnPropertyChanged(nameof(SelectedDuplicatesSizeFormatted));
        OnPropertyChanged(nameof(HasSelectedDuplicates));
    }

    /// <summary>
    /// Handler for when a duplicate file row's selection changes
    /// </summary>
    private void OnDuplicateRowPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DuplicateFileRow.IsSelected) && !DuplicateFileRow.SuppressNotifications)
        {
            UpdateSelectedDuplicatesStats();
        }
    }

    /// <summary>
    /// Handler for when a duplicate file's selection changes (legacy)
    /// </summary>
    private void OnDuplicateFilePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DuplicateFile.IsMarkedForDeletion))
        {
            UpdateSelectedDuplicatesStats();
        }
    }

    /// <summary>
    /// Scan for duplicate files
    /// </summary>
    [RelayCommand]
    private async Task ScanForDuplicatesAsync()
    {
        if (ScanResult == null)
        {
            _logger.LogWarning("ScanForDuplicatesAsync called but ScanResult is null");
            DuplicateScanStatus = "Please scan a folder first before searching for duplicates";
            return;
        }
        
        if (IsDuplicateScanRunning)
        {
            _logger.LogWarning("ScanForDuplicatesAsync called but scan already running");
            return;
        }
        
        _logger.LogInfo($"Starting duplicate scan. ScanResult has RootItem: {ScanResult.RootItem != null}");

        IsDuplicateScanRunning = true;
        
        // Unsubscribe from existing property changes
        foreach (var row in DuplicateFileRows)
        {
            row.PropertyChanged -= OnDuplicateRowPropertyChanged;
        }
        foreach (var group in DuplicateGroups)
        {
            foreach (var file in group.Files)
            {
                file.PropertyChanged -= OnDuplicateFilePropertyChanged;
            }
        }
        DuplicateGroups.Clear();
        DuplicateFileRows.Clear();
        SelectedDuplicatesCount = 0;
        SelectedDuplicatesSize = 0;
        
        DuplicateScanStatus = "Scanning files...";
        DuplicateScanProgress = 0;
        DuplicateFilesProcessed = 0;
        DuplicateTotalFiles = 0;
        
        // Start cycling status messages during initial scan
        var statusMessages = new[] { "Scanning files...", "Please wait...", "Processing will start soon..." };
        var messageIndex = 0;
        using var statusTimer = new System.Timers.Timer(800);
        statusTimer.Elapsed += (s, e) =>
        {
            if (DuplicateTotalFiles == 0 && IsDuplicateScanRunning)
            {
                messageIndex = (messageIndex + 1) % statusMessages.Length;
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    DuplicateScanStatus = statusMessages[messageIndex];
                });
            }
        };
        statusTimer.Start();

        try
        {
            var progress = new Progress<DuplicateScanProgress>(p =>
            {
                DuplicateScanStatus = p.Message;
                DuplicateScanProgress = p.ProgressPercent;
                DuplicateFilesProcessed = p.FilesProcessed;
                DuplicateTotalFiles = p.TotalFiles;
                TotalDuplicateCount = p.DuplicateGroupsFound;
                TotalDuplicateWastedSpace = p.WastedSpaceFound;
                OnPropertyChanged(nameof(DuplicateScanProgressFormatted));
                OnPropertyChanged(nameof(DuplicateFilesProgressFormatted));
            });

            var results = await _duplicateScanner.ScanForDuplicatesAsync(
                ScanResult,
                minFileSize: 1024, // 1KB minimum
                progress,
                _cancellationTokenSource?.Token ?? CancellationToken.None);

            _logger.LogInfo($"Duplicate scan returned {results.Count} groups");

            foreach (var group in results)
            {
                DuplicateGroups.Add(group);
                // Subscribe to property changes on each file for live stats updates
                foreach (var file in group.Files)
                {
                    file.PropertyChanged += OnDuplicateFilePropertyChanged;
                }
            }

            // Build flat list for virtualized DataGrid display
            DuplicateFileRows.Clear();
            int groupNum = 1;
            foreach (var group in results)
            {
                bool isFirst = true;
                foreach (var file in group.Files)
                {
                    var row = new DuplicateFileRow
                    {
                        File = file,
                        Group = group,
                        GroupNumber = groupNum,
                        IsFirstInGroup = isFirst
                    };
                    row.PropertyChanged += OnDuplicateRowPropertyChanged;
                    DuplicateFileRows.Add(row);
                    isFirst = false;
                }
                groupNum++;
            }

            TotalDuplicateWastedSpace = results.Sum(g => g.WastedSpace);
            TotalDuplicateCount = results.Sum(g => g.DuplicateCount);

            _logger.LogInfo($"Added {DuplicateFileRows.Count} rows to DuplicateFileRows");

            // Initialize filtered view
            DuplicateSearchText = string.Empty;
            FilterDuplicateRows();
            
            _logger.LogInfo($"FilteredDuplicateRows has {FilteredDuplicateRows.Count} rows");

            DuplicateScanStatus = results.Count > 0
                ? $"Found {results.Count} duplicate groups ({FormatSize(TotalDuplicateWastedSpace)} recoverable)"
                : "No duplicates found";

            OnPropertyChanged(nameof(HasDuplicates));
            OnPropertyChanged(nameof(DuplicateWastedSpaceFormatted));
        }
        catch (OperationCanceledException)
        {
            DuplicateScanStatus = "Duplicate scan cancelled";
        }
        catch (Exception ex)
        {
            DuplicateScanStatus = $"Error: {ex.Message}";
            _logger.LogError("Duplicate scan error", ex);
        }
        finally
        {
            IsDuplicateScanRunning = false;
        }
    }

    /// <summary>
    /// Delete selected duplicate files
    /// </summary>
    [RelayCommand]
    private async Task DeleteSelectedDuplicatesAsync()
    {
        var markedRows = DuplicateFileRows
            .Where(r => r.IsSelected && !r.IsOriginal)
            .ToList();

        if (!markedRows.Any()) return;

        var deletedCount = 0;
        var freedSpace = 0L;

        foreach (var row in markedRows)
        {
            try
            {
                if (_platformService.MoveToTrash(row.File.FullPath))
                {
                    deletedCount++;
                    freedSpace += row.Size;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to delete {row.File.FullPath}: {ex.Message}");
            }
        }

        // Refresh the duplicate list
        if (deletedCount > 0)
        {
            StatusText = $"Deleted {deletedCount} files, freed {FormatSize(freedSpace)}";
            await ScanForDuplicatesAsync(); // Rescan to update list
        }
    }

    /// <summary>
    /// Toggle selection of a duplicate file for deletion
    /// </summary>
    [RelayCommand]
    private void ToggleDuplicateSelection(DuplicateFile? file)
    {
        if (file == null || file.IsOriginal) return; // Can't delete the "original"
        file.IsMarkedForDeletion = !file.IsMarkedForDeletion;
        UpdateSelectedDuplicatesStats();
    }

    /// <summary>
    /// Select all duplicates (except originals) for deletion
    /// </summary>
    [RelayCommand]
    private void SelectAllDuplicates()
    {
        // Use silent selection to avoid triggering thousands of UI updates
        DuplicateFileRow.SuppressNotifications = true;
        try
        {
            foreach (var row in DuplicateFileRows)
            {
                if (!row.IsOriginal)
                {
                    row.SetSelectedSilent(true);
                }
            }
        }
        finally
        {
            DuplicateFileRow.SuppressNotifications = false;
        }
        
        // Single update at the end
        UpdateSelectedDuplicatesStats();
        
        // Notify DataGrid that items have changed (triggers refresh)
        OnPropertyChanged(nameof(FilteredDuplicateRows));
    }

    /// <summary>
    /// Deselect all duplicates
    /// </summary>
    [RelayCommand]
    private void DeselectAllDuplicates()
    {
        // Use silent selection to avoid triggering thousands of UI updates
        DuplicateFileRow.SuppressNotifications = true;
        try
        {
            foreach (var row in DuplicateFileRows)
            {
                row.SetSelectedSilent(false);
            }
        }
        finally
        {
            DuplicateFileRow.SuppressNotifications = false;
        }
        
        // Single update at the end
        UpdateSelectedDuplicatesStats();
        
        // Notify DataGrid that items have changed
        OnPropertyChanged(nameof(FilteredDuplicateRows));
    }

    /// <summary>
    /// Toggle selection of a duplicate row (called from checkbox command)
    /// </summary>
    [RelayCommand]
    private void ToggleDuplicateRowSelection(DuplicateFileRow? row)
    {
        // The checkbox binding handles the toggle, we just update stats
        UpdateSelectedDuplicatesStats();
    }

    #endregion

    #region Time Machine Analyzer (macOS only)

    /// <summary>
    /// Whether Time Machine is available on this system
    /// </summary>
    public bool IsTimeMachineAvailable => _timeMachineAnalyzer.IsTimeMachineAvailable;

    /// <summary>
    /// Available Time Machine destinations
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<TimeMachineDestination> _timeMachineDestinations = new();

    /// <summary>
    /// Selected Time Machine destination
    /// </summary>
    [ObservableProperty]
    private TimeMachineDestination? _selectedTimeMachineDestination;

    /// <summary>
    /// Current Time Machine analysis results
    /// </summary>
    [ObservableProperty]
    private TimeMachineAnalysis? _timeMachineAnalysis;

    /// <summary>
    /// Whether a Time Machine scan is running
    /// </summary>
    [ObservableProperty]
    private bool _isTimeMachineScanRunning;

    /// <summary>
    /// Time Machine scan progress message
    /// </summary>
    [ObservableProperty]
    private string _timeMachineStatus = string.Empty;

    /// <summary>
    /// Time Machine scan progress percentage
    /// </summary>
    [ObservableProperty]
    private double _timeMachineProgress;

    /// <summary>
    /// Whether we have Time Machine analysis results
    /// </summary>
    public bool HasTimeMachineAnalysis => TimeMachineAnalysis != null && TimeMachineAnalysis.Suggestions.Any();

    /// <summary>
    /// Load available Time Machine destinations
    /// </summary>
    [RelayCommand]
    private async Task LoadTimeMachineDestinationsAsync()
    {
        if (!IsTimeMachineAvailable) return;

        TimeMachineDestinations.Clear();
        TimeMachineStatus = "Discovering backup destinations...";

        try
        {
            var destinations = await _timeMachineAnalyzer.GetBackupDestinationsAsync();
            foreach (var dest in destinations)
            {
                TimeMachineDestinations.Add(dest);
            }

            if (TimeMachineDestinations.Any())
            {
                SelectedTimeMachineDestination = TimeMachineDestinations.First();
                TimeMachineStatus = $"Found {destinations.Count} backup destination(s)";
            }
            else
            {
                TimeMachineStatus = "No Time Machine destinations found";
            }
        }
        catch (Exception ex)
        {
            TimeMachineStatus = $"Error: {ex.Message}";
            _logger.LogError("Error loading Time Machine destinations", ex);
        }
    }

    /// <summary>
    /// Analyze Time Machine backups
    /// </summary>
    [RelayCommand]
    private async Task AnalyzeTimeMachineAsync()
    {
        if (SelectedTimeMachineDestination == null || IsTimeMachineScanRunning) return;

        IsTimeMachineScanRunning = true;
        TimeMachineStatus = "Analyzing backups...";
        TimeMachineProgress = 0;

        try
        {
            var progress = new Progress<TimeMachineProgress>(p =>
            {
                TimeMachineStatus = p.Message;
                TimeMachineProgress = p.ProgressPercent;
            });

            TimeMachineAnalysis = await _timeMachineAnalyzer.AnalyzeBackupsAsync(
                SelectedTimeMachineDestination,
                progress,
                _cancellationTokenSource?.Token ?? CancellationToken.None);

            OnPropertyChanged(nameof(HasTimeMachineAnalysis));
        }
        catch (OperationCanceledException)
        {
            TimeMachineStatus = "Analysis cancelled";
        }
        catch (Exception ex)
        {
            TimeMachineStatus = $"Error: {ex.Message}";
            _logger.LogError("Time Machine analysis error", ex);
        }
        finally
        {
            IsTimeMachineScanRunning = false;
        }
    }

    /// <summary>
    /// Exclude a path from Time Machine backups
    /// </summary>
    [RelayCommand]
    private async Task ExcludeFromTimeMachineAsync(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;

        var success = await _timeMachineAnalyzer.ExcludePathAsync(path);
        if (success)
        {
            StatusText = $"Added to Time Machine exclusions: {Path.GetFileName(path)}";
        }
        else
        {
            StatusText = $"Failed to exclude path from Time Machine";
        }
    }

    #endregion

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

    // Note: QuickClean functionality is implemented through the UI event handler
    // in MainWindow.axaml.cs (QuickCleanButton_Click), which opens QuickCleanDialog

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
