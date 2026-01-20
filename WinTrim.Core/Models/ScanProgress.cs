using CommunityToolkit.Mvvm.ComponentModel;

namespace WinTrim.Core.Models;

/// <summary>
/// Tracks the current state of a scan operation
/// Thread-safe for concurrent updates using Interlocked operations
/// </summary>
public partial class ScanProgress : ObservableObject
{
    [ObservableProperty]
    private ScanState _state = ScanState.Idle;

    [ObservableProperty]
    private string _currentFolder = string.Empty;

    // Public fields for thread-safe Interlocked operations
    public int _filesScanned;
    public int _foldersScanned;
    public long _bytesScanned;
    public int _errorCount;

    // Properties that read from the fields
    public int FilesScanned
    {
        get => _filesScanned;
        set => SetProperty(ref _filesScanned, value);
    }

    public int FoldersScanned
    {
        get => _foldersScanned;
        set => SetProperty(ref _foldersScanned, value);
    }

    public long BytesScanned
    {
        get => _bytesScanned;
        set => SetProperty(ref _bytesScanned, value);
    }

    public int ErrorCount
    {
        get => _errorCount;
        set => SetProperty(ref _errorCount, value);
    }

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _statusMessage = "Ready to scan";

    [ObservableProperty]
    private long _totalDiskSize;

    [ObservableProperty]
    private long _usedDiskSpace;

    public string BytesFormatted => FormatSize(BytesScanned);
    public string TotalDiskSizeFormatted => FormatSize(TotalDiskSize);
    public string UsedDiskSpaceFormatted => FormatSize(UsedDiskSpace);

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

    public void Reset()
    {
        State = ScanState.Idle;
        CurrentFolder = string.Empty;
        _filesScanned = 0;
        _foldersScanned = 0;
        _bytesScanned = 0;
        _errorCount = 0;
        ProgressPercentage = 0;
        StatusMessage = "Ready to scan";
        TotalDiskSize = 0;
        UsedDiskSpace = 0;
        
        // Notify property changes
        OnPropertyChanged(nameof(FilesScanned));
        OnPropertyChanged(nameof(FoldersScanned));
        OnPropertyChanged(nameof(BytesScanned));
        OnPropertyChanged(nameof(ErrorCount));
    }
}

public enum ScanState
{
    Idle,
    Scanning,
    Paused,
    Completed,
    Cancelled,
    Error
}
