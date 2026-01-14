using CommunityToolkit.Mvvm.ComponentModel;

namespace DiskAnalyzer.Models;

/// <summary>
/// Tracks the current state of a scan operation
/// </summary>
public partial class ScanProgress : ObservableObject
{
    [ObservableProperty]
    private ScanState _state = ScanState.Idle;

    [ObservableProperty]
    private string _currentFolder = string.Empty;

    [ObservableProperty]
    private int _filesScanned;

    [ObservableProperty]
    private int _foldersScanned;

    [ObservableProperty]
    private long _bytesScanned;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _statusMessage = "Ready to scan";

    public string BytesFormatted => FormatSize(BytesScanned);

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
        FilesScanned = 0;
        FoldersScanned = 0;
        BytesScanned = 0;
        ErrorCount = 0;
        ProgressPercentage = 0;
        StatusMessage = "Ready to scan";
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
