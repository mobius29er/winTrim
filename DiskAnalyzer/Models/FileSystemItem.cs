using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DiskAnalyzer.Models;

/// <summary>
/// Represents a file system item (file or folder) with metadata
/// </summary>
public partial class FileSystemItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _fullPath = string.Empty;

    [ObservableProperty]
    private long _size;

    [ObservableProperty]
    private DateTime _lastAccessed;

    [ObservableProperty]
    private DateTime _lastModified;

    [ObservableProperty]
    private DateTime _created;

    [ObservableProperty]
    private bool _isFolder;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private FileSystemItem? _parent;

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _children = new();

    [ObservableProperty]
    private string _extension = string.Empty;

    [ObservableProperty]
    private ItemCategory _category = ItemCategory.Other;

    [ObservableProperty]
    private double _percentageOfParent;

    /// <summary>
    /// Human-readable size string
    /// </summary>
    public string SizeFormatted => FormatSize(Size);

    /// <summary>
    /// Days since last accessed
    /// </summary>
    public int DaysSinceAccessed => (DateTime.Now - LastAccessed).Days;

    /// <summary>
    /// Indicates if the file hasn't been accessed in 90+ days
    /// </summary>
    public bool IsStale => DaysSinceAccessed > 90;

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
}

/// <summary>
/// Categories for file types
/// </summary>
public enum ItemCategory
{
    Document,
    Image,
    Video,
    Audio,
    Archive,
    Code,
    Executable,
    Game,
    System,
    Temporary,
    Other
}
