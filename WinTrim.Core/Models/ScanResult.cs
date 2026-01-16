using System;
using System.Collections.Generic;

namespace WinTrim.Core.Models;

/// <summary>
/// Holds the complete scan results and analytics
/// </summary>
public class ScanResult
{
    public string RootPath { get; set; } = string.Empty;
    public DateTime ScanStarted { get; set; }
    public DateTime? ScanCompleted { get; set; }
    public TimeSpan Duration => (ScanCompleted ?? DateTime.Now) - ScanStarted;
    
    /// <summary>
    /// Indicates if the scan was cancelled (partial results)
    /// </summary>
    public bool WasCancelled { get; set; }
    
    public long TotalSize { get; set; }
    public int TotalFiles { get; set; }
    public int TotalFolders { get; set; }
    public int ErrorCount { get; set; }
    
    public List<FileSystemItem> LargestFiles { get; set; } = new();
    public List<FileSystemItem> LargestFolders { get; set; } = new();
    public List<FileSystemItem> OldestAccessedFiles { get; set; } = new();
    public List<GameInstallation> GameInstallations { get; set; } = new();
    public Dictionary<ItemCategory, CategoryStats> CategoryBreakdown { get; set; } = new();
    public List<CleanupSuggestion> CleanupSuggestions { get; set; } = new();
    
    /// <summary>
    /// Developer tool caches and cleanable items (npm, NuGet, pip, etc.)
    /// </summary>
    public List<CleanupItem> DevTools { get; set; } = new();
    
    public FileSystemItem? RootItem { get; set; }
}

/// <summary>
/// Statistics for a file category
/// Thread-safe for concurrent updates
/// </summary>
public class CategoryStats
{
    public ItemCategory Category { get; set; }
    
    // Public fields for thread-safe Interlocked operations
    public long TotalSize;
    public int FileCount;
    
    public double Percentage { get; set; }
    
    public string SizeFormatted => FormatSize(TotalSize);

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
/// Represents a detected game installation
/// </summary>
public class GameInstallation
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public GamePlatform Platform { get; set; }
    public DateTime? LastPlayed { get; set; }
    
    public string SizeFormatted => FormatSize(Size);

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

public enum GamePlatform
{
    Steam,
    EpicGames,
    GOG,
    Origin,
    Ubisoft,
    Xbox,
    Lutris,      // Linux
    PlayOnMac,   // Mac
    Other
}

/// <summary>
/// Cleanup suggestion with safety level
/// </summary>
public class CleanupSuggestion
{
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long PotentialSavings { get; set; }
    public CleanupRisk RiskLevel { get; set; }
    public CleanupType Type { get; set; }
    public List<string> AffectedFiles { get; set; } = new();
    
    public string SavingsFormatted => FormatSize(PotentialSavings);

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

public enum CleanupRisk
{
    Safe,      // Temp files, caches - can be deleted freely
    Low,       // Old logs, downloads - likely safe
    Medium,    // Duplicate files - need verification
    High       // System/program files - requires care
}

public enum CleanupType
{
    TempFiles,
    BrowserCache,
    SystemCache,       // Cross-platform
    WindowsUpdate,
    RecycleBin,
    OldDownloads,
    DuplicateFiles,
    OldLogFiles,
    UnusedGames,
    LargeFiles,
    DeveloperTools,
    MediaCache,
    XcodeDerivedData,  // Mac
    HomebrewCache,     // Mac
    DockerImages       // Cross-platform
}
