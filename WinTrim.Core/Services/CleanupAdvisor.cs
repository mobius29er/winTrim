using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// Provides safe cleanup recommendations based on file analysis.
/// Cross-platform implementation for WinTrim.Core.
/// </summary>
public sealed class CleanupAdvisor : ICleanupAdvisor
{
    private readonly IPlatformService _platformService;
    
    // Known safe-to-delete patterns
    private static readonly string[] TempPatterns = { "*.tmp", "*.temp", "~*", "*.bak" };
    private static readonly string[] LogPatterns = { "*.log", "*.log.*" };

    public CleanupAdvisor(IPlatformService platformService)
    {
        _platformService = platformService;
    }

    public async Task<List<CleanupSuggestion>> GetSuggestionsAsync(FileSystemItem rootItem, CancellationToken cancellationToken)
    {
        var suggestions = new List<CleanupSuggestion>();

        await Task.Run(() =>
        {
            // Check temp folders
            suggestions.AddRange(AnalyzeTempFolders(cancellationToken));

            // Check browser caches
            suggestions.AddRange(AnalyzeBrowserCaches(cancellationToken));

            // Platform-specific cleanup
            if (OperatingSystem.IsWindows())
            {
                suggestions.AddRange(AnalyzeWindowsSpecific(cancellationToken));
            }
            else if (OperatingSystem.IsMacOS())
            {
                suggestions.AddRange(AnalyzeMacOsSpecific(cancellationToken));
            }

            // Check Downloads folder for old files
            suggestions.AddRange(AnalyzeDownloads(cancellationToken));

            // Find large files not accessed in 6+ months
            suggestions.AddRange(FindStaleFiles(rootItem, cancellationToken));

            // Find old log files
            suggestions.AddRange(FindOldLogs(rootItem, cancellationToken));

        }, cancellationToken);

        return suggestions
            .Where(s => s.PotentialSavings > 1024 * 1024) // Only show if > 1MB savings
            .OrderByDescending(s => s.PotentialSavings)
            .ToList();
    }

    private List<CleanupSuggestion> AnalyzeTempFolders(CancellationToken cancellationToken)
    {
        var suggestions = new List<CleanupSuggestion>();
        
        var tempPaths = new List<string> { _platformService.GetTempFolder() };
        
        // Add platform-specific temp locations
        if (OperatingSystem.IsWindows())
        {
            tempPaths.Add(Path.Combine(_platformService.GetLocalAppDataFolder(), "Temp"));
            var winDir = Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows";
            tempPaths.Add(Path.Combine(winDir, "Temp"));
            tempPaths.Add(Path.Combine(winDir, "Prefetch"));
        }
        else if (OperatingSystem.IsMacOS())
        {
            var userHome = _platformService.GetUserFolder();
            tempPaths.Add(Path.Combine(userHome, "Library", "Caches"));
            tempPaths.Add("/private/var/folders"); // macOS per-user temp
        }

        foreach (var tempPath in tempPaths.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(tempPath))
                continue;

            try
            {
                var size = CalculateFolderSize(tempPath, cancellationToken);
                if (size > 0)
                {
                    suggestions.Add(new CleanupSuggestion
                    {
                        Description = $"Temporary files in {Path.GetFileName(tempPath)}",
                        Path = tempPath,
                        PotentialSavings = size,
                        RiskLevel = CleanupRisk.Safe,
                        Type = CleanupType.TempFiles
                    });
                }
            }
            catch { }
        }

        return suggestions;
    }

    private List<CleanupSuggestion> AnalyzeBrowserCaches(CancellationToken cancellationToken)
    {
        var suggestions = new List<CleanupSuggestion>();

        foreach (var cachePath in _platformService.GetBrowserCachePaths())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(cachePath))
                continue;

            try
            {
                var size = CalculateFolderSize(cachePath, cancellationToken);
                if (size > 50 * 1024 * 1024) // > 50MB
                {
                    // Extract browser name from path
                    var browserName = ExtractBrowserName(cachePath);
                    
                    suggestions.Add(new CleanupSuggestion
                    {
                        Description = $"{browserName} browser cache",
                        Path = cachePath,
                        PotentialSavings = size,
                        RiskLevel = CleanupRisk.Safe,
                        Type = CleanupType.BrowserCache
                    });
                }
            }
            catch { }
        }

        return suggestions;
    }

    private static string ExtractBrowserName(string path)
    {
        var pathLower = path.ToLowerInvariant();
        if (pathLower.Contains("chrome")) return "Chrome";
        if (pathLower.Contains("edge")) return "Edge";
        if (pathLower.Contains("firefox")) return "Firefox";
        if (pathLower.Contains("safari")) return "Safari";
        if (pathLower.Contains("brave")) return "Brave";
        if (pathLower.Contains("opera")) return "Opera";
        if (pathLower.Contains("arc")) return "Arc";
        return "Browser";
    }

    private List<CleanupSuggestion> AnalyzeWindowsSpecific(CancellationToken cancellationToken)
    {
        var suggestions = new List<CleanupSuggestion>();

        // Windows Update cache
        var windowsUpdatePaths = new[]
        {
            @"C:\Windows\SoftwareDistribution\Download",
            @"C:\Windows\Installer\$PatchCache$"
        };

        foreach (var path in windowsUpdatePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(path))
                continue;

            try
            {
                var size = CalculateFolderSize(path, cancellationToken);
                if (size > 100 * 1024 * 1024) // > 100MB
                {
                    suggestions.Add(new CleanupSuggestion
                    {
                        Description = "Windows Update cache files",
                        Path = path,
                        PotentialSavings = size,
                        RiskLevel = CleanupRisk.Low,
                        Type = CleanupType.WindowsUpdate
                    });
                }
            }
            catch { }
        }

        // Recycle Bin
        try
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var recycleBinPath = Path.Combine(drive.RootDirectory.FullName, "$Recycle.Bin");
                if (!Directory.Exists(recycleBinPath))
                    continue;

                try
                {
                    var size = CalculateFolderSize(recycleBinPath, cancellationToken);
                    if (size > 50 * 1024 * 1024) // > 50MB
                    {
                        suggestions.Add(new CleanupSuggestion
                        {
                            Description = $"Recycle Bin on {drive.Name}",
                            Path = recycleBinPath,
                            PotentialSavings = size,
                            RiskLevel = CleanupRisk.Low,
                            Type = CleanupType.RecycleBin
                        });
                    }
                }
                catch { }
            }
        }
        catch { }

        return suggestions;
    }

    private List<CleanupSuggestion> AnalyzeMacOsSpecific(CancellationToken cancellationToken)
    {
        var suggestions = new List<CleanupSuggestion>();
        var userHome = _platformService.GetUserFolder();

        // Trash
        var trashPath = Path.Combine(userHome, ".Trash");
        if (Directory.Exists(trashPath))
        {
            try
            {
                var size = CalculateFolderSize(trashPath, cancellationToken);
                if (size > 50 * 1024 * 1024)
                {
                    suggestions.Add(new CleanupSuggestion
                    {
                        Description = "Trash",
                        Path = trashPath,
                        PotentialSavings = size,
                        RiskLevel = CleanupRisk.Low,
                        Type = CleanupType.RecycleBin
                    });
                }
            }
            catch { }
        }

        // Spotlight index
        var spotlightPath = "/.Spotlight-V100";
        if (Directory.Exists(spotlightPath))
        {
            try
            {
                var size = CalculateFolderSize(spotlightPath, cancellationToken);
                if (size > 500 * 1024 * 1024) // > 500MB
                {
                    suggestions.Add(new CleanupSuggestion
                    {
                        Description = "Spotlight Index",
                        Path = spotlightPath,
                        PotentialSavings = size,
                        RiskLevel = CleanupRisk.Medium,
                        Type = CleanupType.SystemCache
                    });
                }
            }
            catch { }
        }

        // Application logs
        var logsPath = Path.Combine(userHome, "Library", "Logs");
        if (Directory.Exists(logsPath))
        {
            try
            {
                var size = CalculateFolderSize(logsPath, cancellationToken);
                if (size > 100 * 1024 * 1024)
                {
                    suggestions.Add(new CleanupSuggestion
                    {
                        Description = "Application Logs",
                        Path = logsPath,
                        PotentialSavings = size,
                        RiskLevel = CleanupRisk.Low,
                        Type = CleanupType.OldLogFiles
                    });
                }
            }
            catch { }
        }

        return suggestions;
    }

    private List<CleanupSuggestion> AnalyzeDownloads(CancellationToken cancellationToken)
    {
        var suggestions = new List<CleanupSuggestion>();
        var downloadsPath = Path.Combine(_platformService.GetUserFolder(), "Downloads");

        if (!Directory.Exists(downloadsPath))
            return suggestions;

        try
        {
            var oldFiles = new DirectoryInfo(downloadsPath)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(f => (DateTime.Now - f.LastAccessTime).Days > 90)
                .ToList();

            cancellationToken.ThrowIfCancellationRequested();

            var totalSize = oldFiles.Sum(f => f.Length);
            if (totalSize > 100 * 1024 * 1024) // > 100MB
            {
                suggestions.Add(new CleanupSuggestion
                {
                    Description = "Old files in Downloads folder (90+ days)",
                    Path = downloadsPath,
                    PotentialSavings = totalSize,
                    RiskLevel = CleanupRisk.Medium,
                    Type = CleanupType.OldDownloads,
                    AffectedFiles = oldFiles.Take(20).Select(f => f.FullName).ToList()
                });
            }
        }
        catch { }

        return suggestions;
    }

    private List<CleanupSuggestion> FindStaleFiles(FileSystemItem rootItem, CancellationToken cancellationToken)
    {
        var suggestions = new List<CleanupSuggestion>();
        var staleFiles = new List<FileSystemItem>();
        
        CollectStaleFiles(rootItem, staleFiles, 180, cancellationToken); // 6 months

        var totalSize = staleFiles.Sum(f => f.Size);
        if (totalSize > 500 * 1024 * 1024) // > 500MB
        {
            suggestions.Add(new CleanupSuggestion
            {
                Description = "Large files not accessed in 6+ months",
                Path = rootItem.FullPath,
                PotentialSavings = totalSize,
                RiskLevel = CleanupRisk.Medium,
                Type = CleanupType.LargeFiles,
                AffectedFiles = staleFiles
                    .OrderByDescending(f => f.Size)
                    .Take(50)
                    .Select(f => f.FullPath)
                    .ToList()
            });
        }

        return suggestions;
    }

    private void CollectStaleFiles(FileSystemItem item, List<FileSystemItem> staleFiles, int daysThreshold, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!item.IsFolder && item.DaysSinceAccessed > daysThreshold && item.Size > 10 * 1024 * 1024) // > 10MB
        {
            staleFiles.Add(item);
        }

        foreach (var child in item.Children)
        {
            CollectStaleFiles(child, staleFiles, daysThreshold, cancellationToken);
        }
    }

    private List<CleanupSuggestion> FindOldLogs(FileSystemItem rootItem, CancellationToken cancellationToken)
    {
        var suggestions = new List<CleanupSuggestion>();
        var logFiles = new List<FileSystemItem>();

        CollectLogFiles(rootItem, logFiles, cancellationToken);

        var oldLogs = logFiles
            .Where(f => f.DaysSinceAccessed > 30 || f.Size > 50 * 1024 * 1024) // 30 days or > 50MB
            .ToList();

        var totalSize = oldLogs.Sum(f => f.Size);
        if (totalSize > 100 * 1024 * 1024) // > 100MB
        {
            suggestions.Add(new CleanupSuggestion
            {
                Description = "Old or large log files",
                Path = rootItem.FullPath,
                PotentialSavings = totalSize,
                RiskLevel = CleanupRisk.Low,
                Type = CleanupType.OldLogFiles,
                AffectedFiles = oldLogs
                    .OrderByDescending(f => f.Size)
                    .Take(30)
                    .Select(f => f.FullPath)
                    .ToList()
            });
        }

        return suggestions;
    }

    private void CollectLogFiles(FileSystemItem item, List<FileSystemItem> logFiles, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!item.IsFolder && item.Extension == ".log")
        {
            logFiles.Add(item);
        }

        foreach (var child in item.Children)
        {
            CollectLogFiles(child, logFiles, cancellationToken);
        }
    }

    private static long CalculateFolderSize(string path, CancellationToken cancellationToken)
    {
        long size = 0;
        try
        {
            var dir = new DirectoryInfo(path);
            foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try { size += file.Length; } catch { }
            }
        }
        catch { }
        return size;
    }
}
