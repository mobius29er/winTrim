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
    private ObservableCollection<CleanupSuggestion> _cleanupSuggestions = new();

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
    private bool _isDarkMode;

    [ObservableProperty]
    private bool _isRedAccent;

    [ObservableProperty]
    private bool _canExport;

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
        IsDarkMode = _settingsService.IsDarkMode;
        IsRedAccent = _settingsService.Accent == ThemeAccent.Red;

        // Load available drives
        LoadDrives();
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        _settingsService.IsDarkMode = value;
        ApplyTheme();
    }

    partial void OnIsRedAccentChanged(bool value)
    {
        _settingsService.Accent = value ? ThemeAccent.Red : ThemeAccent.Green;
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

            // Determine theme file based on dark mode and accent
            string themeFile;
            if (!IsDarkMode)
            {
                themeFile = "Themes/Colors.xaml";
            }
            else
            {
                themeFile = IsRedAccent ? "Themes/TerminalRedColors.xaml" : "Themes/DarkColors.xaml";
            }
            
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
            
            if (ScanResult.WasCancelled)
            {
                StatusText = $"Scan cancelled - showing {ScanResult.TotalFiles:N0} files found so far ({ScanResult.RootItem?.SizeFormatted})";
            }
            else
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

    [RelayCommand]
    private void ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
    }

    [RelayCommand]
    private void ToggleRedAccent()
    {
        IsRedAccent = !IsRedAccent;
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

        // Update cleanup suggestions
        CleanupSuggestions.Clear();
        foreach (var suggestion in ScanResult.CleanupSuggestions)
        {
            CleanupSuggestions.Add(suggestion);
        }

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

    #endregion
}
