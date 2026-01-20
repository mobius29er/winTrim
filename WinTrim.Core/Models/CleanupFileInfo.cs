using System;
using System.IO;

namespace WinTrim.Core.Models;

/// <summary>
/// Represents a cleanup file with rich metadata for UI display.
/// Lightweight model specifically for cleanup file details.
/// </summary>
public class CleanupFileInfo
{
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTime LastAccessed { get; init; } = DateTime.MinValue;
    public DateTime LastModified { get; init; } = DateTime.MinValue;
    public CleanupRisk Risk { get; init; } = CleanupRisk.Medium;

    /// <summary>
    /// Creates a CleanupFileInfo from a file path with optional risk level
    /// </summary>
    public static CleanupFileInfo FromPath(string path, CleanupRisk risk = CleanupRisk.Medium)
    {
        var info = new CleanupFileInfo
        {
            FilePath = path,
            FileName = Path.GetFileName(path),
            Risk = risk
        };

        try
        {
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                return new CleanupFileInfo
                {
                    FilePath = path,
                    FileName = fileInfo.Name,
                    SizeBytes = fileInfo.Length,
                    LastAccessed = fileInfo.LastAccessTime,
                    LastModified = fileInfo.LastWriteTime,
                    Risk = risk
                };
            }
        }
        catch
        {
            // File may be inaccessible - return with path info only
        }

        return info;
    }

    /// <summary>
    /// Human-readable size string
    /// </summary>
    public string SizeFormatted => FormatSize(SizeBytes);

    /// <summary>
    /// Human-readable last accessed time
    /// </summary>
    public string LastAccessedFormatted
    {
        get
        {
            if (LastAccessed == DateTime.MinValue) return "Unknown";
            var days = (DateTime.Now - LastAccessed).Days;
            if (days == 0) return "Today";
            if (days == 1) return "Yesterday";
            if (days < 7) return $"{days} days ago";
            if (days < 30) return $"{days / 7} weeks ago";
            if (days < 365) return $"{days / 30} months ago";
            return $"{days / 365}+ years ago";
        }
    }

    /// <summary>
    /// Human-readable last modified time
    /// </summary>
    public string LastModifiedFormatted
    {
        get
        {
            if (LastModified == DateTime.MinValue) return "Unknown";
            return LastModified.ToString("yyyy-MM-dd HH:mm");
        }
    }

    /// <summary>
    /// Days since last accessed
    /// </summary>
    public int DaysSinceAccessed => LastAccessed == DateTime.MinValue 
        ? -1 
        : (DateTime.Now - LastAccessed).Days;

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "0 B";
        
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
