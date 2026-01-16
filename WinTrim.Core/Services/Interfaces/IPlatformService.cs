using System.Collections.Generic;

namespace WinTrim.Core.Services;

/// <summary>
/// Platform-specific service interface for abstracting OS differences
/// </summary>
public interface IPlatformService
{
    /// <summary>
    /// Gets the current operating system
    /// </summary>
    OperatingSystemType CurrentOS { get; }
    
    /// <summary>
    /// Gets the user's home/profile folder (alias: GetUserProfilePath)
    /// </summary>
    string GetUserFolder();
    
    /// <summary>
    /// Gets the user's home/profile folder
    /// </summary>
    string GetUserProfilePath() => GetUserFolder();
    
    /// <summary>
    /// Gets the application data folder (roaming)
    /// </summary>
    string GetAppDataFolder();
    
    /// <summary>
    /// Gets the roaming app data folder (alias)
    /// </summary>
    string GetAppDataRoamingPath() => GetAppDataFolder();
    
    /// <summary>
    /// Gets the local application data folder
    /// </summary>
    string GetLocalAppDataFolder();
    
    /// <summary>
    /// Gets the local application data folder (alias)
    /// </summary>
    string GetAppDataLocalPath() => GetLocalAppDataFolder();
    
    /// <summary>
    /// Gets the system temp folder
    /// </summary>
    string GetTempFolder();
    
    /// <summary>
    /// Gets the system temp folder (alias)
    /// </summary>
    string GetTempPath() => GetTempFolder();
    
    /// <summary>
    /// Gets available drives/volumes
    /// </summary>
    IEnumerable<DriveInfoModel> GetDrives();
    
    /// <summary>
    /// Gets browser cache locations for the platform
    /// </summary>
    IEnumerable<string> GetBrowserCachePaths();
    
    /// <summary>
    /// Gets system log locations
    /// </summary>
    IEnumerable<string> GetSystemLogPaths();
    
    /// <summary>
    /// Opens a folder in the native file explorer
    /// </summary>
    void OpenInExplorer(string path);
    
    /// <summary>
    /// Moves an item to the recycle bin/trash
    /// </summary>
    bool MoveToTrash(string path);
}

/// <summary>
/// Cross-platform drive/volume information
/// </summary>
public class DriveInfoModel
{
    public string Name { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string DriveFormat { get; set; } = string.Empty;
    public DriveTypeEnum DriveType { get; set; }
    public long TotalSize { get; set; }
    public long AvailableFreeSpace { get; set; }
    public long TotalFreeSpace { get; set; }
    public bool IsReady { get; set; }
    
    public string TotalSizeFormatted => FormatSize(TotalSize);
    public string FreeSpaceFormatted => FormatSize(AvailableFreeSpace);
    public double UsedPercentage => TotalSize > 0 ? (double)(TotalSize - AvailableFreeSpace) / TotalSize * 100 : 0;
    
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

public enum DriveTypeEnum
{
    Unknown,
    NoRootDirectory,
    Removable,
    Fixed,
    Network,
    CDRom,
    Ram
}

public enum OperatingSystemType
{
    Windows,
    MacOS,
    Linux,
    Unknown
}
