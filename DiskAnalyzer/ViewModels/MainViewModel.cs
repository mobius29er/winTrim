using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskAnalyzer.Models;
using DiskAnalyzer.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace DiskAnalyzer.ViewModels;

/// <summary>
/// Main ViewModel handling all disk analysis operations
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IFileScanner _fileScanner;
    private readonly IGameDetector _gameDetector;
    private readonly ICleanupAdvisor _cleanupAdvisor;
    private readonly ICategoryClassifier _categoryClassifier;
    private readonly ISettingsService _settingsService;
    private readonly IExportService _exportService;
    
    private CancellationTokenSource? _cancellationTokenSource;
    
    // Stores files by category for the interactive category view
    private Dictionary<string, List<FileSystemItem>> _filesByCategory = new();

    #region Observable Properties

    [ObservableProperty]
    private string _selectedPath = string.Empty;

    [ObservableProperty]
    private ScanProgress _scanProgress = new();

    [ObservableProperty]
    private ScanResult? _scanResult;

    [ObservableProperty]
    private FileSystemItem? _selectedItem;

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _rootItems = new();

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
    private ObservableCollection<FileSystemItem> _categoryFiles = new();

    [ObservableProperty]
    private string? _selectedCategoryName;

    [ObservableProperty]
    private ISeries[] _topFoldersSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _folderContents = new();

    [ObservableProperty]
    private string? _selectedFolderName;

    [ObservableProperty]
    private bool _canStart = true;

    [ObservableProperty]
    private bool _canStop;

    [ObservableProperty]
    private bool _canPause;

    [ObservableProperty]
    private bool _canResume;

    [ObservableProperty]
    private string _statusText = "Select a drive or folder to begin scanning";

    [ObservableProperty]
    private AppTheme _selectedTheme = AppTheme.Default;

    [ObservableProperty]
    private FontSizePreset _selectedFontSize = FontSizePreset.Medium;

    [ObservableProperty]
    private TreemapColorScheme _selectedTreemapColors = TreemapColorScheme.Vivid;

    [ObservableProperty]
    private TreemapColorMode _selectedTreemapColorMode = TreemapColorMode.Depth;

    [ObservableProperty]
    private int _treemapMaxDepth = 3;

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private bool _canExport;

    [ObservableProperty]
    private bool _hasCachedScan;

    [ObservableProperty]
    private string? _cachedScanInfo;

    [ObservableProperty]
    private bool _hasQuickCleanItems;

    [ObservableProperty]
    private string? _quickCleanInfo;

    [ObservableProperty]
    private CleanupSuggestion? _selectedCleanupSuggestion;

    [ObservableProperty]
    private ObservableCollection<CleanupFileItem> _cleanupFiles = new();

    // File Explorer filter properties
    [ObservableProperty]
    private string _fileExplorerSearchText = string.Empty;

    [ObservableProperty]
    private string _fileExplorerFilter = "All";

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _filteredChildren = new();

    // Tree view filtering properties
    [ObservableProperty]
    private string _treeSearchText = string.Empty;

    [ObservableProperty]
    private string _treeSortBy = "Size";

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _filteredRootItems = new();

    public string[] FileExplorerFilterOptions { get; } = new[]
    {
        "All", "Documents", "Media", "Code", "Archives", "Large Files (>100MB)", "Old Files (>90 days)"
    };

    public string[] TreeSortOptions { get; } = new[] { "Size", "Name", "Date" };

    partial void OnFileExplorerSearchTextChanged(string value) => ApplyFileExplorerFilters();
    partial void OnFileExplorerFilterChanged(string value) => ApplyFileExplorerFilters();
    partial void OnTreeSearchTextChanged(string value) => ApplyTreeFilters();
    partial void OnTreeSortByChanged(string value) => ApplyTreeFilters();

    #endregion

    public MainViewModel()
    {
        // Create services
        _settingsService = new SettingsService();
        _exportService = new ExportService();
        _categoryClassifier = new CategoryClassifier();
        _gameDetector = new GameDetector();
        _cleanupAdvisor = new CleanupAdvisor();
        _fileScanner = new FileScanner(_gameDetector, _cleanupAdvisor, _categoryClassifier);

        // Load settings
        SelectedTheme = _settingsService.Theme;
        SelectedFontSize = _settingsService.FontSize;
        SelectedTreemapColors = _settingsService.TreemapColors;
        TreemapMaxDepth = _settingsService.TreemapMaxDepth;
        
        ApplyTheme();
        ApplyFontSize();
        UpdateTreemapLegend();

        // Load available drives
        LoadDrives();

        // Check for cached scan from previous session
        CheckForCachedScan();
    }

    // Settings change handlers
    partial void OnSelectedFontSizeChanged(FontSizePreset value)
    {
        _settingsService.FontSize = value;
        ApplyFontSize();
    }

    partial void OnSelectedTreemapColorsChanged(TreemapColorScheme value)
    {
        _settingsService.TreemapColors = value;
        UpdateTreemapLegend();
    }

    partial void OnSelectedTreemapColorModeChanged(TreemapColorMode value)
    {
        UpdateTreemapLegend();
    }

    partial void OnTreemapMaxDepthChanged(int value)
    {
        _settingsService.TreemapMaxDepth = value;
    }

    private void UpdateTreemapLegend()
    {
        TreemapLegendItems.Clear();
        var layoutService = new TreemapLayoutService();
        layoutService.SetColorScheme(SelectedTreemapColors);
        layoutService.SetColorMode(SelectedTreemapColorMode);
        
        foreach (var item in layoutService.GetLegendItems())
        {
            TreemapLegendItems.Add(new TreemapLegendItem
            {
                Label = item.Key,
                Color = System.Windows.Media.Color.FromArgb(item.Value.Alpha, item.Value.Red, item.Value.Green, item.Value.Blue)
            });
        }
    }

    private void ApplyFontSize()
    {
        var baseFontSize = _settingsService.GetBaseFontSize();
        Application.Current.Resources["BaseFontSize"] = baseFontSize;
        Application.Current.Resources["SmallFontSize"] = baseFontSize - 2;
        Application.Current.Resources["LargeFontSize"] = baseFontSize + 2;
        Application.Current.Resources["HeaderFontSize"] = baseFontSize + 6;
        Application.Current.Resources["TitleFontSize"] = baseFontSize + 4;
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    public FontSizePreset[] AvailableFontSizes { get; } = Enum.GetValues<FontSizePreset>();
    public TreemapColorScheme[] AvailableTreemapColors { get; } = Enum.GetValues<TreemapColorScheme>();
    public TreemapColorMode[] AvailableTreemapColorModes { get; } = Enum.GetValues<TreemapColorMode>();

    private void CheckForCachedScan()
    {
        if (_settingsService.HasCachedScan)
        {
            var cache = _settingsService.LoadScanCache();
            if (cache != null)
            {
                HasCachedScan = true;
                var age = DateTime.Now - cache.ScanDate;
                var ageText = age.TotalDays >= 1 ? $"{(int)age.TotalDays}d ago" : 
                              age.TotalHours >= 1 ? $"{(int)age.TotalHours}h ago" : 
                              $"{(int)age.TotalMinutes}m ago";
                CachedScanInfo = $"📁 {cache.RootPath} • {cache.TotalSizeFormatted} • {ageText}";
                StatusText = "Previous scan available - Click 'Restore' or start a new scan";
            }
        }
    }

    [RelayCommand]
    private void RestoreCachedScan()
    {
        var cache = _settingsService.LoadScanCache();
        if (cache == null) return;

        // Populate UI from cache
        LargestFiles.Clear();
        foreach (var f in cache.LargestFiles)
        {
            LargestFiles.Add(new FileSystemItem
            {
                Name = f.Name,
                FullPath = f.FullPath,
                Size = f.Size,
                LastAccessed = f.LastAccessed,
                LastModified = f.LastModified,
                Category = Enum.TryParse<ItemCategory>(f.Category, out var cat) ? cat : ItemCategory.Other
            });
        }

        LargestFolders.Clear();
        foreach (var f in cache.LargestFolders)
        {
            var folder = new FileSystemItem
            {
                Name = f.Name,
                FullPath = f.FullPath,
                Size = f.Size,
                IsFolder = true
            };
            foreach (var c in f.Children)
            {
                folder.Children.Add(new FileSystemItem
                {
                    Name = c.Name,
                    FullPath = c.FullPath,
                    Size = c.Size,
                    IsFolder = c.IsFolder
                });
            }
            LargestFolders.Add(folder);
        }

        Games.Clear();
        foreach (var g in cache.Games)
        {
            Games.Add(new GameInstallation
            {
                Name = g.Name,
                Path = g.Path,
                Size = g.Size,
                Platform = Enum.TryParse<GamePlatform>(g.Platform, out var plat) ? plat : GamePlatform.Other,
                LastPlayed = g.LastPlayed
            });
        }

        CleanupSuggestions.Clear();
        foreach (var s in cache.CleanupSuggestions)
        {
            CleanupSuggestions.Add(new CleanupSuggestion
            {
                Description = s.Description,
                Path = s.Path,
                PotentialSavings = s.PotentialSavings,
                RiskLevel = Enum.TryParse<CleanupRisk>(s.RiskLevel, out var risk) ? risk : CleanupRisk.Medium
            });
        }

        // Restore developer tools from cache
        DevToolItems.Clear();
        foreach (var d in cache.DevTools)
        {
            DevToolItems.Add(new CleanupItem
            {
                Name = d.Name,
                Path = d.Path,
                SizeBytes = d.SizeBytes,
                Category = d.Category,
                Recommendation = d.Recommendation,
                Risk = Enum.TryParse<CleanupRisk>(d.Risk, out var devRisk) ? devRisk : CleanupRisk.Medium
            });
        }

        // Restore RootItem for treemap from cached tree
        if (cache.RootTree != null)
        {
            var rootItem = RestoreTreeNode(cache.RootTree);
            // Create a minimal ScanResult for treemap binding
            ScanResult = new ScanResult
            {
                RootPath = cache.RootPath,
                RootItem = rootItem,
                TotalSize = cache.TotalSize,
                TotalFiles = cache.TotalFiles,
                TotalFolders = cache.TotalFolders
            };
            
            RootItems.Clear();
            RootItems.Add(rootItem);
            RefreshTreeView();
        }

        // Update quick clean availability
        UpdateQuickCleanStatus();

        // Update charts from cached data
        UpdateChartsFromCache(cache);

        // Restore category files from cache (instead of building from limited tree)
        RestoreCategoryFilesFromCache(cache);

        // Auto-select largest category if available
        if (_filesByCategory.Count > 0)
        {
            var largestCategory = cache.CategoryBreakdown
                .OrderByDescending(c => c.TotalSize)
                .FirstOrDefault();
            if (largestCategory != null)
            {
                SelectCategory(largestCategory.Category.ToString());
            }
        }

        SelectedPath = cache.RootPath;
        HasCachedScan = false;
        CanExport = true;
        StatusText = $"Restored scan from {cache.ScanDate:g} • {cache.TotalFiles:N0} files, {cache.TotalSizeFormatted}";
    }

    /// <summary>
    /// Restore a FileSystemItem tree from cached tree node
    /// </summary>
    private FileSystemItem RestoreTreeNode(CachedTreeNode node)
    {
        var item = new FileSystemItem
        {
            Name = node.Name,
            FullPath = node.FullPath,
            Size = node.Size,
            IsFolder = node.IsFolder,
            Category = Enum.TryParse<ItemCategory>(node.Category, out var cat) ? cat : ItemCategory.Other
        };

        foreach (var child in node.Children)
        {
            var childItem = RestoreTreeNode(child);
            childItem.Parent = item;
            item.Children.Add(childItem);
        }

        return item;
    }

    private void UpdateChartsFromCache(ScanCache cache)
    {
        var colors = new SkiaSharp.SKColor[]
        {
            SkiaSharp.SKColors.RoyalBlue, SkiaSharp.SKColors.Coral, SkiaSharp.SKColors.MediumSeaGreen, 
            SkiaSharp.SKColors.Gold, SkiaSharp.SKColors.MediumPurple, SkiaSharp.SKColors.Tomato, 
            SkiaSharp.SKColors.SteelBlue, SkiaSharp.SKColors.Orange, SkiaSharp.SKColors.Teal, 
            SkiaSharp.SKColors.IndianRed, SkiaSharp.SKColors.SlateGray
        };

        // Category pie chart
        var categoryData = cache.CategoryBreakdown
            .OrderByDescending(c => c.TotalSize)
            .Take(10)
            .Select((c, i) => new LiveChartsCore.SkiaSharpView.PieSeries<double>
            {
                Values = new[] { (double)c.TotalSize },
                Name = $"{c.Category} ({c.SizeFormatted})",
                Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(colors[i % colors.Length])
            })
            .ToArray();

        CategorySeries = categoryData;

        // Folders bar chart
        var folderData = cache.LargestFolders
            .Take(10)
            .Select((f, i) => new LiveChartsCore.SkiaSharpView.RowSeries<double>
            {
                Values = new[] { (double)f.Size },
                Name = $"{f.Name} ({f.SizeFormatted})",
                Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(colors[i % colors.Length])
            })
            .ToArray();

        TopFoldersSeries = folderData;
    }

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        _settingsService.Theme = value;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        try
        {
            var app = Application.Current;
            var resources = app.Resources.MergedDictionaries;
            
            // Remove existing color dictionary
            var existingColors = resources.FirstOrDefault(d => 
                d.Source?.OriginalString.Contains("Colors.xaml") == true);
            
            if (existingColors != null)
                resources.Remove(existingColors);

            // Select theme file based on current theme
            string themeFile = SelectedTheme switch
            {
                AppTheme.Default => "Themes/DefaultColors.xaml",
                AppTheme.Tech => "Themes/TechColors.xaml",
                AppTheme.Enterprise => "Themes/EnterpriseColors.xaml",
                AppTheme.TerminalGreen => "Themes/TerminalGreenColors.xaml",
                AppTheme.TerminalRed => "Themes/TerminalRedColors.xaml",
                _ => "Themes/DefaultColors.xaml"  // Default to retrofuturistic theme
            };
            
            resources.Insert(0, new ResourceDictionary { Source = new Uri(themeFile, UriKind.Relative) });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to apply theme: {ex.Message}");
        }
    }

    private void LoadDrives()
    {
        AvailableDrives.Clear();
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            AvailableDrives.Add(drive);
        }
        
        if (AvailableDrives.Any())
        {
            SelectedDrive = AvailableDrives.First();
            SelectedPath = SelectedDrive.RootDirectory.FullName;
        }
    }

    partial void OnSelectedDriveChanged(DriveInfo? value)
    {
        if (value != null)
        {
            SelectedPath = value.RootDirectory.FullName;
        }
    }

    #region Commands

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Folder to Analyze"
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedPath = dialog.FolderName;
            SelectedDrive = null; // Deselect drive when custom folder is chosen
        }
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartScanAsync()
    {
        if (string.IsNullOrEmpty(SelectedPath) || !Directory.Exists(SelectedPath))
        {
            MessageBox.Show("Please select a valid folder to scan.", "Invalid Path", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Reset pause state before starting
        _fileScanner.Resume();
        
        UpdateCommandStates(scanning: true);
        ScanProgress.Reset();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            StatusText = "Scanning...";
            
            var progress = new Progress<ScanProgress>(p =>
            {
                // Update on UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ScanProgress = p;
                    OnPropertyChanged(nameof(ScanProgress));
                });
            });

            ScanResult = await _fileScanner.ScanAsync(SelectedPath, progress, _cancellationTokenSource.Token);

            // Update UI with results (works for both complete and partial results)
            UpdateResults();
            
            // Save scan to cache for next app launch
            if (ScanResult != null && !ScanResult.WasCancelled)
            {
                _settingsService.SaveScanCache(ScanResult);
            }
            
            if (ScanResult != null && ScanResult.WasCancelled)
            {
                StatusText = $"Scan cancelled - showing {ScanResult.TotalFiles:N0} files found so far ({ScanResult.RootItem?.SizeFormatted})";
            }
            else if (ScanResult != null)
            {
                StatusText = $"Scan completed - {ScanResult.TotalFiles:N0} files, {ScanResult.RootItem?.SizeFormatted}";
            }
        }
        catch (OperationCanceledException)
        {
            // This shouldn't happen anymore as we return partial results, but keep as fallback
            if (ScanResult != null)
            {
                UpdateResults();
                StatusText = $"Scan stopped - {ScanResult.TotalFiles:N0} files found";
            }
            else
            {
                StatusText = "Scan cancelled";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            MessageBox.Show($"An error occurred during scanning:\n{ex.Message}", "Scan Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            UpdateCommandStates(scanning: false);
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void StopScan()
    {
        _fileScanner.Resume(); // Ensure not paused so cancellation can propagate
        _cancellationTokenSource?.Cancel();
        StatusText = "Stopping scan...";
    }

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void PauseScan()
    {
        // Real pause - scanner will wait at next checkpoint
        _fileScanner.Pause();
        UpdateCommandStates(scanning: true, paused: true);
        StatusText = "Scan paused - click Resume to continue";
    }

    [RelayCommand(CanExecute = nameof(CanResume))]
    private void ResumeScan()
    {
        // Resume paused scan
        _fileScanner.Resume();
        UpdateCommandStates(scanning: true, paused: false);
        StatusText = "Resuming scan...";
    }

    [RelayCommand]
    private void OpenInExplorer(FileSystemItem? item)
    {
        if (item == null) return;

        try
        {
            var path = item.IsFolder ? item.FullPath : Path.GetDirectoryName(item.FullPath);
            if (!string.IsNullOrEmpty(path))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{item.FullPath}\"");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open explorer: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void CopyPath(FileSystemItem? item)
    {
        if (item != null)
        {
            Clipboard.SetText(item.FullPath);
        }
    }

    [RelayCommand]
    private void OpenGameFolder(GameInstallation? game)
    {
        if (game == null) return;

        try
        {
            System.Diagnostics.Process.Start("explorer.exe", $"\"{game.Path}\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open explorer: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void CopyGamePath(GameInstallation? game)
    {
        if (game != null)
        {
            Clipboard.SetText(game.Path);
        }
    }

    [RelayCommand]
    private void RefreshDrives()
    {
        LoadDrives();
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportToCsvAsync()
    {
        if (ScanResult == null) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export to CSV",
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"DiskAnalysis_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportToCsvAsync(ScanResult, dialog.FileName);
                StatusText = $"Exported to {dialog.FileName}";
                MessageBox.Show($"Successfully exported to:\n{dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportToJsonAsync()
    {
        if (ScanResult == null) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export to JSON",
            Filter = "JSON Files (*.json)|*.json",
            FileName = $"DiskAnalysis_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportToJsonAsync(ScanResult, dialog.FileName);
                StatusText = $"Exported to {dialog.FileName}";
                MessageBox.Show($"Successfully exported to:\n{dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Available theme options for the UI
    /// </summary>
    public AppTheme[] AvailableThemes { get; } = Enum.GetValues<AppTheme>();

    [RelayCommand]
    private void CycleTheme()
    {
        // Cycle through themes: Tech -> Enterprise -> TerminalGreen -> TerminalRed -> Tech
        SelectedTheme = SelectedTheme switch
        {
            AppTheme.Tech => AppTheme.Enterprise,
            AppTheme.Enterprise => AppTheme.TerminalGreen,
            AppTheme.TerminalGreen => AppTheme.TerminalRed,
            AppTheme.TerminalRed => AppTheme.Tech,
            _ => AppTheme.Tech
        };
    }

    [RelayCommand]
    private async Task QuickCleanAsync()
    {
        // Get ONLY safe cleanup suggestions - no risk to user data
        var safeItems = CleanupSuggestions.Where(s => s.RiskLevel == CleanupRisk.Safe).ToList();
        
        if (!safeItems.Any())
        {
            MessageBox.Show(
                "No safe cleanup items available.\n\nAll suggested cleanups require manual review due to higher risk levels.",
                "Quick Clean",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // Show selection dialog
        var dialog = new Views.QuickCleanDialog(safeItems)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() != true || !dialog.Confirmed)
            return;

        var selectedItems = dialog.GetSelectedItems().ToList();
        if (!selectedItems.Any())
            return;

        // Execute cleanup
        StatusText = "🧹 Cleaning up...";
        
        var cleanupService = new CleanupService();
        var cleanupResult = await cleanupService.ExecuteCleanupAsync(selectedItems, CleanupRisk.Low);

        // Show results
        var resultMessage = $"Cleanup complete!\n\n" +
            $"Items cleaned: {cleanupResult.ItemsCleaned}\n" +
            $"Space recovered: {cleanupResult.BytesRecoveredFormatted}";

        if (cleanupResult.HasErrors)
        {
            resultMessage += $"\n\nSome items could not be cleaned:\n" +
                string.Join("\n", cleanupResult.Errors.Take(5));
        }

        MessageBox.Show(resultMessage, "Quick Clean - Complete", MessageBoxButton.OK, 
            cleanupResult.HasErrors ? MessageBoxImage.Warning : MessageBoxImage.Information);

        // Remove cleaned items from the suggestions list
        foreach (var cleaned in cleanupResult.CleanedItems)
        {
            var item = CleanupSuggestions.FirstOrDefault(s => s.Description == cleaned);
            if (item != null)
            {
                CleanupSuggestions.Remove(item);
            }
        }

        // Update quick clean availability
        UpdateQuickCleanStatus();

        StatusText = $"✅ Cleanup complete - recovered {cleanupResult.BytesRecoveredFormatted}";
    }

    private static string FormatBytes(long bytes)
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

    /// <summary>
    /// Short format for pie chart labels (e.g., "1.5TB" without space)
    /// </summary>
    private static string FormatBytesShort(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        // Use shorter format for pie chart labels
        return size >= 100 ? $"{size:N0}{suffixes[suffixIndex]}" : $"{size:N1}{suffixes[suffixIndex]}";
    }

    #endregion

    #region Private Methods

    private void UpdateCommandStates(bool scanning, bool paused = false)
    {
        CanStart = !scanning;
        CanStop = scanning;
        CanPause = scanning && !paused;
        CanResume = scanning && paused;

        StartScanCommand.NotifyCanExecuteChanged();
        StopScanCommand.NotifyCanExecuteChanged();
        PauseScanCommand.NotifyCanExecuteChanged();
        ResumeScanCommand.NotifyCanExecuteChanged();
    }

    private void UpdateResults()
    {
        if (ScanResult == null) return;

        // Enable export now that we have results
        CanExport = true;
        ExportToCsvCommand.NotifyCanExecuteChanged();
        ExportToJsonCommand.NotifyCanExecuteChanged();

        // Update tree view
        RootItems.Clear();
        if (ScanResult.RootItem != null)
        {
            RootItems.Add(ScanResult.RootItem);
        }
        RefreshTreeView();

        // Update largest files
        LargestFiles.Clear();
        foreach (var file in ScanResult.LargestFiles.Take(50))
        {
            LargestFiles.Add(file);
        }

        // Update largest folders
        LargestFolders.Clear();
        foreach (var folder in ScanResult.LargestFolders.Take(20))
        {
            LargestFolders.Add(folder);
        }

        // Update games
        Games.Clear();
        foreach (var game in ScanResult.GameInstallations)
        {
            Games.Add(game);
        }

        // Update developer tools from scan results
        DevToolItems.Clear();
        foreach (var devTool in ScanResult.DevTools)
        {
            DevToolItems.Add(devTool);
        }

        // Update cleanup suggestions
        CleanupSuggestions.Clear();
        foreach (var suggestion in ScanResult.CleanupSuggestions)
        {
            CleanupSuggestions.Add(suggestion);
        }

        // Update quick clean availability
        UpdateQuickCleanStatus();

        // Build category-to-files mapping
        BuildCategoryFilesMap();

        // Update charts
        UpdateCharts();
        
        // Auto-select largest category
        if (_filesByCategory.Count > 0)
        {
            var largestCategory = ScanResult.CategoryBreakdown
                .OrderByDescending(c => c.Value.TotalSize)
                .FirstOrDefault();
            if (largestCategory.Key != default)
            {
                SelectCategory(largestCategory.Key.ToString());
            }
        }
    }

    /// <summary>
    /// Restore category files from cache data instead of from tree
    /// </summary>
    private void RestoreCategoryFilesFromCache(ScanCache cache)
    {
        _filesByCategory.Clear();
        CategoryFiles.Clear();
        SelectedCategoryName = null;

        foreach (var category in cache.CategoryBreakdown)
        {
            if (category.TopFiles != null && category.TopFiles.Count > 0)
            {
                var files = category.TopFiles.Select(f => new FileSystemItem
                {
                    Name = f.Name,
                    FullPath = f.FullPath,
                    Size = f.Size,
                    IsFolder = false,
                    Category = Enum.TryParse<ItemCategory>(category.Category, out var cat) ? cat : ItemCategory.Other,
                    LastAccessed = f.LastAccessed,
                    LastModified = f.LastModified
                }).ToList();

                _filesByCategory[category.Category] = files;
            }
        }
    }

    private void BuildCategoryFilesMap()
    {
        _filesByCategory.Clear();
        CategoryFiles.Clear();
        SelectedCategoryName = null;

        if (ScanResult?.RootItem == null)
            return;

        // Recursively collect files by category
        CollectFilesByCategory(ScanResult.RootItem);
        
        // Sort files within each category by size (largest first)
        foreach (var key in _filesByCategory.Keys.ToList())
        {
            _filesByCategory[key] = _filesByCategory[key]
                .OrderByDescending(f => f.Size)
                .Take(100) // Limit to top 100 per category for performance
                .ToList();
        }
    }

    private void CollectFilesByCategory(FileSystemItem item)
    {
        if (!item.IsFolder)
        {
            var categoryName = item.Category.ToString();
            if (!_filesByCategory.ContainsKey(categoryName))
            {
                _filesByCategory[categoryName] = new List<FileSystemItem>();
            }
            _filesByCategory[categoryName].Add(item);
        }

        foreach (var child in item.Children)
        {
            CollectFilesByCategory(child);
        }
    }

    [RelayCommand]
    private void SelectCategory(string? categoryName)
    {
        if (string.IsNullOrEmpty(categoryName))
            return;

        // Extract just the category name (remove size info if present)
        var name = categoryName;
        var parenIndex = name.IndexOf(" (");
        if (parenIndex > 0)
        {
            name = name.Substring(0, parenIndex);
        }

        SelectedCategoryName = name;
        CategoryFiles.Clear();

        if (_filesByCategory.TryGetValue(name, out var files))
        {
            foreach (var file in files)
            {
                CategoryFiles.Add(file);
            }
        }
    }

    [RelayCommand]
    private void SelectFolder(string? folderName)
    {
        if (string.IsNullOrEmpty(folderName) || ScanResult?.LargestFolders == null)
            return;

        // Extract just the folder name (remove size info if present)
        var name = folderName;
        var parenIndex = name.IndexOf(" (");
        if (parenIndex > 0)
        {
            name = name.Substring(0, parenIndex);
        }

        SelectedFolderName = name;
        FolderContents.Clear();

        // Find the folder and show its contents
        var folder = ScanResult.LargestFolders.FirstOrDefault(f => f.Name == name);
        if (folder != null)
        {
            // Show immediate children sorted by size
            var contents = folder.Children
                .OrderByDescending(c => c.Size)
                .Take(50);
            
            foreach (var item in contents)
            {
                FolderContents.Add(item);
            }
        }
    }

    private void UpdateCharts()
    {
        if (ScanResult?.CategoryBreakdown == null || !ScanResult.CategoryBreakdown.Any())
            return;

        // Category pie chart
        var colors = new SKColor[]
        {
            SKColors.RoyalBlue, SKColors.Coral, SKColors.MediumSeaGreen, SKColors.Gold,
            SKColors.MediumPurple, SKColors.Tomato, SKColors.SteelBlue, SKColors.Orange,
            SKColors.Teal, SKColors.IndianRed, SKColors.SlateGray
        };

        var categoryData = ScanResult.CategoryBreakdown
            .OrderByDescending(c => c.Value.TotalSize)
            .Take(10)
            .Select((c, i) => new PieSeries<double>
            {
                Values = new[] { (double)c.Value.TotalSize },
                Name = $"{c.Key} ({c.Value.SizeFormatted})",
                Fill = new SolidColorPaint(colors[i % colors.Length])
            })
            .ToArray();

        CategorySeries = categoryData;

        // Top folders bar chart
        if (ScanResult.LargestFolders.Any())
        {
            var folderData = ScanResult.LargestFolders
                .Take(10)
                .Select((f, i) => new RowSeries<double>
                {
                    Values = new[] { (double)f.Size },
                    Name = $"{f.Name} ({f.SizeFormatted})",
                    Fill = new SolidColorPaint(colors[i % colors.Length])
                })
                .ToArray();

            TopFoldersSeries = folderData;
        }
    }

    private void UpdateQuickCleanStatus()
    {
        // Only count Safe risk items for Quick Clean - 100% safe guarantee
        var safeItems = CleanupSuggestions.Where(s => s.RiskLevel == CleanupRisk.Safe).ToList();
        HasQuickCleanItems = safeItems.Any();
        
        if (HasQuickCleanItems)
        {
            var totalSavings = safeItems.Sum(s => s.PotentialSavings);
            QuickCleanInfo = $"🧹 {safeItems.Count} items • {FormatBytes(totalSavings)}";
        }
        else
        {
            QuickCleanInfo = null;
        }
    }

    [RelayCommand]
    private void ViewCleanupDetails(CleanupSuggestion? suggestion)
    {
        if (suggestion == null) return;

        SelectedCleanupSuggestion = suggestion;
        CleanupFiles.Clear();

        foreach (var filePath in suggestion.AffectedFiles.Take(100)) // Limit to 100 files
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists)
                {
                    CleanupFiles.Add(new CleanupFileItem
                    {
                        Name = fileInfo.Name,
                        FullPath = filePath,
                        Size = fileInfo.Length,
                        LastAccessed = fileInfo.LastAccessTime
                    });
                }
                else if (Directory.Exists(filePath))
                {
                    var dirInfo = new DirectoryInfo(filePath);
                    CleanupFiles.Add(new CleanupFileItem
                    {
                        Name = dirInfo.Name,
                        FullPath = filePath,
                        Size = 0,
                        IsFolder = true,
                        LastAccessed = dirInfo.LastAccessTime
                    });
                }
            }
            catch
            {
                // Skip files we can't access
            }
        }
    }

    [RelayCommand]
    private void OpenCleanupFile(CleanupFileItem? item)
    {
        if (item == null) return;

        try
        {
            if (item.IsFolder)
            {
                System.Diagnostics.Process.Start("explorer.exe", $"\"{item.FullPath}\"");
            }
            else
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{item.FullPath}\"");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open location: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void CopyCleanupPath(CleanupFileItem? item)
    {
        if (item != null)
        {
            Clipboard.SetText(item.FullPath);
        }
    }

    [RelayCommand]
    private void OpenFileLocation(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        try
        {
            if (System.IO.Directory.Exists(filePath))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"\"{filePath}\"");
            }
            else if (System.IO.File.Exists(filePath))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open location: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFileExplorerFilters()
    {
        FilteredChildren.Clear();

        if (SelectedItem == null)
            return;

        var children = SelectedItem.Children.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(FileExplorerSearchText))
        {
            var searchLower = FileExplorerSearchText.ToLowerInvariant();
            children = children.Where(c => 
                c.Name.ToLowerInvariant().Contains(searchLower) ||
                c.FullPath.ToLowerInvariant().Contains(searchLower));
        }

        // Apply category filter
        children = FileExplorerFilter switch
        {
            "Documents" => children.Where(c => c.Category == ItemCategory.Document || 
                                               c.Extension is ".pdf" or ".doc" or ".docx" or ".txt" or ".xls" or ".xlsx"),
            "Media" => children.Where(c => c.Category == ItemCategory.Image || 
                                          c.Category == ItemCategory.Video || 
                                          c.Category == ItemCategory.Audio ||
                                          c.Extension is ".jpg" or ".png" or ".mp4" or ".mp3" or ".gif" or ".avi"),
            "Code" => children.Where(c => c.Category == ItemCategory.Code ||
                                         c.Extension is ".cs" or ".js" or ".ts" or ".py" or ".java" or ".cpp" or ".h"),
            "Archives" => children.Where(c => c.Category == ItemCategory.Archive ||
                                             c.Extension is ".zip" or ".rar" or ".7z" or ".tar" or ".gz"),
            "Large Files (>100MB)" => children.Where(c => c.Size > 100 * 1024 * 1024),
            "Old Files (>90 days)" => children.Where(c => c.DaysSinceAccessed > 90),
            _ => children // "All"
        };

        // Sort by size descending
        foreach (var item in children.OrderByDescending(c => c.Size).Take(100))
        {
            FilteredChildren.Add(item);
        }
    }

    /// <summary>
    /// Apply filtering and sorting to the tree view
    /// </summary>
    private void ApplyTreeFilters()
    {
        FilteredRootItems.Clear();

        if (!RootItems.Any())
            return;

        var items = RootItems.AsEnumerable();

        // Apply search filter (searches recursively through children names)
        if (!string.IsNullOrWhiteSpace(TreeSearchText))
        {
            var searchLower = TreeSearchText.ToLowerInvariant();
            items = items.Where(item => ItemMatchesSearch(item, searchLower));
        }

        // Apply sorting
        items = TreeSortBy switch
        {
            "Name" => items.OrderBy(i => i.Name),
            "Date" => items.OrderByDescending(i => i.LastModified),
            _ => items.OrderByDescending(i => i.Size) // "Size" default
        };

        foreach (var item in items)
        {
            FilteredRootItems.Add(item);
        }
    }

    /// <summary>
    /// Recursively check if item or any children match search
    /// </summary>
    private bool ItemMatchesSearch(FileSystemItem item, string searchLower)
    {
        if (item.Name.ToLowerInvariant().Contains(searchLower))
            return true;

        return item.Children.Any(child => ItemMatchesSearch(child, searchLower));
    }

    /// <summary>
    /// Refresh tree filter when root items change
    /// </summary>
    public void RefreshTreeView()
    {
        ApplyTreeFilters();
    }

    public void UpdateSelectedItemChildren()
    {
        ApplyFileExplorerFilters();
    }

    [RelayCommand]
    private void OpenDevToolPath(CleanupItem? item)
    {
        if (item == null) return;

        try
        {
            if (Directory.Exists(item.Path))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"\"{item.Path}\"");
            }
            else if (File.Exists(item.Path))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{item.Path}\"");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open location: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void CopyDevToolPath(CleanupItem? item)
    {
        if (item != null)
        {
            Clipboard.SetText(item.Path);
        }
    }

    #endregion
}

/// <summary>
/// Represents a file in the cleanup details view
/// </summary>
public class CleanupFileItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public bool IsFolder { get; set; }
    public DateTime LastAccessed { get; set; }

    public string SizeFormatted
    {
        get
        {
            if (IsFolder) return "Folder";
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = Size;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:N2} {suffixes[suffixIndex]}";
        }
    }
}

/// <summary>
/// Legend item for treemap color mode display
/// </summary>
public class TreemapLegendItem
{
    public string Label { get; set; } = string.Empty;
    public System.Windows.Media.Color Color { get; set; }
}