using System;
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
    
    private CancellationTokenSource? _cancellationTokenSource;

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
    private ISeries[] _topFoldersSeries = Array.Empty<ISeries>();

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

    #endregion

    public MainViewModel()
    {
        // Create services
        _categoryClassifier = new CategoryClassifier();
        _gameDetector = new GameDetector();
        _cleanupAdvisor = new CleanupAdvisor();
        _fileScanner = new FileScanner(_gameDetector, _cleanupAdvisor, _categoryClassifier);

        // Load available drives
        LoadDrives();
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

            // Update UI with results
            UpdateResults();
            StatusText = $"Scan completed - {ScanResult.TotalFiles:N0} files, {ScanResult.RootItem?.SizeFormatted}";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Scan cancelled";
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
        _cancellationTokenSource?.Cancel();
        StatusText = "Stopping scan...";
    }

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void PauseScan()
    {
        // Pause functionality - cancel current scan but keep results
        _cancellationTokenSource?.Cancel();
        UpdateCommandStates(scanning: false);
        ScanProgress.State = ScanState.Paused;
        StatusText = "Scan paused - results preserved. Click Start to scan again.";
    }

    [RelayCommand(CanExecute = nameof(CanResume))]
    private void ResumeScan()
    {
        // Resume is essentially starting a new scan
        StartScanCommand.Execute(null);
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

        // Update charts
        UpdateCharts();
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
