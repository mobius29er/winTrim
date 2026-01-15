using System;
using System.Collections.Generic;

namespace DiskAnalyzer.Models;

/// <summary>
/// Lightweight cache of scan results for quick restore on app startup.
/// Stores summary data only (not full tree) to keep file size manageable.
/// </summary>
public class ScanCache
{
    public string Version { get; set; } = "1.1";
    public DateTime ScanDate { get; set; }
    public string RootPath { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    
    // Summary stats
    public long TotalSize { get; set; }
    public string TotalSizeFormatted { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int TotalFolders { get; set; }
    
    // Top items (limited to keep cache small)
    public List<CachedFileItem> LargestFiles { get; set; } = new();
    public List<CachedFolderItem> LargestFolders { get; set; } = new();
    public List<CachedGameItem> Games { get; set; } = new();
    public List<CachedCategoryItem> CategoryBreakdown { get; set; } = new();
    public List<CachedCleanupItem> CleanupSuggestions { get; set; } = new();
    
    // Developer tools (cached for restore)
    public List<CachedDevToolItem> DevTools { get; set; } = new();
    
    // Root folder tree for treemap (limited depth for size)
    public CachedTreeNode? RootTree { get; set; }
}

public class CachedFileItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime LastAccessed { get; set; }
    public DateTime LastModified { get; set; }
    public int DaysSinceAccessed { get; set; }
}

public class CachedFolderItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public List<CachedFolderChild> Children { get; set; } = new();
}

public class CachedFolderChild
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public bool IsFolder { get; set; }
}

public class CachedGameItem
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime? LastPlayed { get; set; }
}

public class CachedCategoryItem
{
    public string Category { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public int FileCount { get; set; }
    /// <summary>Top files in this category (up to 50 largest)</summary>
    public List<CachedCategoryFileItem> TopFiles { get; set; } = new();
}

public class CachedCategoryFileItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public DateTime LastAccessed { get; set; }
    public DateTime LastModified { get; set; }
}

public class CachedCleanupItem
{
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long PotentialSavings { get; set; }
    public string SavingsFormatted { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
}

/// <summary>
/// Cached developer tool item
/// </summary>
public class CachedDevToolItem
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string Risk { get; set; } = string.Empty;
}

/// <summary>
/// Cached tree node for treemap visualization (limited depth)
/// </summary>
public class CachedTreeNode
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public bool IsFolder { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<CachedTreeNode> Children { get; set; } = new();
}
