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
/// Main ViewModel handling all disk analysis operations for Avalonia
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IFileScanner _fileScanner;
    private readonly IPlatformService _platformService;
    private readonly IThemeService _themeService;
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
    private ObservableCollection<DriveInfo> _availableDrives = new();

    [ObservableProperty]
    private DriveInfo? _selectedDrive;

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
    private ObservableCollection<string> _cleanupFiles = new();

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
    private ObservableCollection<string> _availableThemes = new() { "Default", "Tech", "Enterprise", "TerminalGreen", "TerminalRed" };

    [ObservableProperty]
    private string _selectedTheme = "Default";

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

    #endregion

    /// <summary>
    /// Constructor with DI-injected services
    /// </summary>
    public MainWindowViewModel(IFileScanner fileScanner, IPlatformService platformService, IThemeService themeService)
    {
        Console.WriteLine("[ViewModel] Constructor called");
        _fileScanner = fileScanner;
        _platformService = platformService;
        _themeService = themeService;
        
        // Apply default theme on startup
        _themeService.ApplyTheme("Default");
        
        LoadAvailableDrives();
        Console.WriteLine($"[ViewModel] Constructor complete. AvailableDrives: {AvailableDrives.Count}, SelectedDrive: {SelectedDrive?.Name ?? "null"}");
    }
    
    /// <summary>
    /// Called when SelectedTheme property changes
    /// </summary>
    partial void OnSelectedThemeChanged(string value)
    {
        Console.WriteLine($"[ViewModel] OnSelectedThemeChanged called with: {value}");
        _themeService.ApplyTheme(value);
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
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady)
            {
                AvailableDrives.Add(drive);
            }
        }
        if (AvailableDrives.Count > 0)
        {
            SelectedDrive = AvailableDrives[0];
        }
    }

    #region Commands

    [RelayCommand]
    private async Task StartScan()
    {
        if (SelectedDrive == null) return;

        Console.WriteLine($"[ViewModel] StartScan called for drive: {SelectedDrive.Name}");
        
        CanStart = false;
        CanStop = true;
        CanPause = true;
        ScanProgress.Reset();
        ScanProgress.State = ScanState.Scanning;
        StatusText = $"Scanning {SelectedDrive.Name}...";

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
                SelectedDrive.RootDirectory.FullName, 
                progress, 
                _cancellationTokenSource.Token);

            // Update UI with results (includes partial results if cancelled)
            ScanResult = result;
            
            // Populate collections for UI binding
            PopulateResultsFromScan(result);
            
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
            CleanupFiles.Add(file);
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
