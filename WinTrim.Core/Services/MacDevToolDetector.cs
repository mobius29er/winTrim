using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// macOS-specific developer tool detector.
/// Handles XCode, Homebrew, macOS-specific Docker, and Apple development caches.
/// </summary>
public class MacDevToolDetector : DevToolDetectorBase
{
    private readonly string _userHome;
    private readonly string _libraryPath;
    private readonly string _libraryCaches;
    
    public MacDevToolDetector(IPlatformService platformService) : base(platformService)
    {
        _userHome = platformService.GetUserProfilePath();
        _libraryPath = Path.Combine(_userHome, "Library");
        _libraryCaches = Path.Combine(_libraryPath, "Caches");
    }
    
    protected override IEnumerable<(string Path, string Name)> GetPlatformNodeCachePaths()
    {
        return new[]
        {
            (Path.Combine(_libraryCaches, "npm"), "NPM Cache"),
            (Path.Combine(_libraryCaches, "Yarn"), "Yarn Cache"),
            (Path.Combine(_libraryCaches, "pnpm"), "pnpm Cache"),
        };
    }
    
    protected override IEnumerable<(string Path, string Name, string Recommendation)> GetDockerPaths()
    {
        return new[]
        {
            // Docker Desktop on Mac uses a qcow2 virtual disk
            (Path.Combine(_libraryPath, "Containers", "com.docker.docker", "Data", "vms", "0", "data", "Docker.raw"),
                "Docker Desktop VM", "High disk usage. Use 'docker system prune' to clean."),
            (Path.Combine(_libraryPath, "Containers", "com.docker.docker", "Data", "vms", "0", "data", "Docker.qcow2"),
                "Docker Desktop VM (Legacy)", "High disk usage. Use 'docker system prune' to clean."),
            (Path.Combine(_userHome, ".docker"),
                "Docker Config & Caches", "Docker configuration and cache files."),
        };
    }
    
    protected override IEnumerable<(string Path, string Name, string Recommendation, CleanupRisk Risk, int MinSizeMb)> GetIdeCachePaths()
    {
        return new[]
        {
            (Path.Combine(_libraryCaches, "JetBrains"), "JetBrains IDE Cache",
                "IDE caches and indexes. Safe to delete but IDEs will re-index.", CleanupRisk.Low, 500),
            (Path.Combine(_libraryCaches, "com.microsoft.VSCode"), "VS Code Cache",
                "VS Code cache files. Safe to delete.", CleanupRisk.Safe, 200),
            (Path.Combine(_libraryCaches, "com.microsoft.VSCode.ShipIt"), "VS Code Update Cache",
                "VS Code update downloads. Safe to delete.", CleanupRisk.Safe, 100),
            (Path.Combine(_libraryPath, "Application Support", "Code", "CachedExtensionVSIXs"), "VS Code Extension Cache",
                "Cached extension downloads. Safe to delete.", CleanupRisk.Safe, 100),
        };
    }
    
    protected override async Task<List<CleanupItem>> ScanPlatformSpecificAsync()
    {
        var items = new List<CleanupItem>();
        
        var tasks = new List<Task<List<CleanupItem>>>
        {
            ScanXcodeDerivedDataAsync(),
            ScanXcodeArchivesAsync(),
            ScanXcodeSimulatorsAsync(),
            ScanHomebrewAsync(),
            ScanCocoaPodsAsync(),
            ScanMacOsSystemCachesAsync()
        };
        
        await Task.WhenAll(tasks);
        
        foreach (var task in tasks)
        {
            items.AddRange(await task);
        }
        
        return items;
    }
    
    /// <summary>
    /// Scan XCode DerivedData - often 5-50GB of build artifacts
    /// </summary>
    private async Task<List<CleanupItem>> ScanXcodeDerivedDataAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            var derivedDataPath = Path.Combine(_libraryPath, "Developer", "Xcode", "DerivedData");

            if (!Directory.Exists(derivedDataPath)) return items;

            try
            {
                // Scan the entire DerivedData folder
                long totalSize = GetDirectorySize(new DirectoryInfo(derivedDataPath));
                
                if (totalSize > 1L * 1024 * 1024 * 1024) // > 1GB
                {
                    items.Add(new CleanupItem
                    {
                        Name = "XCode DerivedData",
                        Path = derivedDataPath,
                        SizeBytes = totalSize,
                        Category = "Developer Tools",
                        Recommendation = "Build cache for all Xcode projects. Safe to delete - will rebuild on next compile.",
                        Risk = CleanupRisk.Safe
                    });
                }
                
                // Also list individual project caches > 500MB
                foreach (var projectDir in Directory.GetDirectories(derivedDataPath))
                {
                    var dirInfo = new DirectoryInfo(projectDir);
                    long size = GetDirectorySize(dirInfo);
                    
                    if (size > 500L * 1024 * 1024)
                    {
                        // Extract project name from folder (format: ProjectName-hashcode)
                        var projectName = dirInfo.Name.Split('-').FirstOrDefault() ?? dirInfo.Name;
                        
                        items.Add(new CleanupItem
                        {
                            Name = $"XCode: {projectName}",
                            Path = projectDir,
                            SizeBytes = size,
                            Category = "Developer Tools",
                            Recommendation = $"Build cache for '{projectName}'. Safe to delete.",
                            Risk = CleanupRisk.Safe
                        });
                    }
                }
            }
            catch { }

            return items;
        });
    }
    
    /// <summary>
    /// Scan XCode Archives - old app builds
    /// </summary>
    private async Task<List<CleanupItem>> ScanXcodeArchivesAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            var archivesPath = Path.Combine(_libraryPath, "Developer", "Xcode", "Archives");

            if (!Directory.Exists(archivesPath)) return items;

            try
            {
                long totalSize = GetDirectorySize(new DirectoryInfo(archivesPath));
                
                if (totalSize > 500L * 1024 * 1024)
                {
                    items.Add(new CleanupItem
                    {
                        Name = "XCode Archives",
                        Path = archivesPath,
                        SizeBytes = totalSize,
                        Category = "Developer Tools",
                        Recommendation = "Old app builds for App Store submission. Keep recent ones, delete old.",
                        Risk = CleanupRisk.Medium
                    });
                }
            }
            catch { }

            return items;
        });
    }
    
    /// <summary>
    /// Scan iOS/watchOS/tvOS Simulator data
    /// </summary>
    private async Task<List<CleanupItem>> ScanXcodeSimulatorsAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            var simulatorsPath = Path.Combine(_libraryCaches, "com.apple.CoreSimulator");
            var devicesPath = Path.Combine(_libraryPath, "Developer", "CoreSimulator", "Devices");

            // Simulator caches
            if (Directory.Exists(simulatorsPath))
            {
                ScanDirectoryIfLarge(items, simulatorsPath, "iOS Simulator Cache",
                    "Developer Tools", "Simulator cache data. Safe to delete.", CleanupRisk.Safe, 500);
            }
            
            // Simulator device data
            if (Directory.Exists(devicesPath))
            {
                try
                {
                    long totalSize = GetDirectorySize(new DirectoryInfo(devicesPath));
                    
                    if (totalSize > 2L * 1024 * 1024 * 1024) // > 2GB
                    {
                        items.Add(new CleanupItem
                        {
                            Name = "iOS Simulator Devices",
                            Path = devicesPath,
                            SizeBytes = totalSize,
                            Category = "Developer Tools",
                            Recommendation = "Simulator device data. Delete unused simulators via Xcode > Window > Devices.",
                            Risk = CleanupRisk.Medium
                        });
                    }
                }
                catch { }
            }

            return items;
        });
    }
    
    /// <summary>
    /// Scan Homebrew cache and old package versions
    /// </summary>
    private async Task<List<CleanupItem>> ScanHomebrewAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            
            // Homebrew cache locations
            var homebrewCaches = new[]
            {
                Path.Combine(_libraryCaches, "Homebrew"),
                Path.Combine(_userHome, ".cache", "Homebrew"),
                "/usr/local/Homebrew/Library/Taps" // Old tap data
            };
            
            foreach (var cachePath in homebrewCaches)
            {
                ScanDirectoryIfLarge(items, cachePath, "Homebrew Cache",
                    "Developer Tools", "Downloaded packages. Run 'brew cleanup' or delete manually.", CleanupRisk.Safe, 200);
            }
            
            // Check for old Homebrew Cellar versions
            var cellarPath = "/usr/local/Cellar";
            if (!Directory.Exists(cellarPath))
            {
                cellarPath = "/opt/homebrew/Cellar"; // Apple Silicon path
            }
            
            if (Directory.Exists(cellarPath))
            {
                try
                {
                    long oldVersionsSize = 0;
                    
                    foreach (var packageDir in Directory.GetDirectories(cellarPath))
                    {
                        var versions = Directory.GetDirectories(packageDir);
                        if (versions.Length > 1)
                        {
                            // Count size of all but the newest version
                            var sortedVersions = versions.OrderByDescending(v => new DirectoryInfo(v).LastWriteTime).ToList();
                            foreach (var oldVersion in sortedVersions.Skip(1))
                            {
                                oldVersionsSize += GetDirectorySize(new DirectoryInfo(oldVersion));
                            }
                        }
                    }
                    
                    if (oldVersionsSize > 500L * 1024 * 1024)
                    {
                        items.Add(new CleanupItem
                        {
                            Name = "Homebrew Old Versions",
                            Path = cellarPath,
                            SizeBytes = oldVersionsSize,
                            Category = "Developer Tools",
                            Recommendation = "Old package versions. Run 'brew cleanup' to remove automatically.",
                            Risk = CleanupRisk.Safe
                        });
                    }
                }
                catch { }
            }

            return items;
        });
    }
    
    /// <summary>
    /// Scan CocoaPods cache
    /// </summary>
    private async Task<List<CleanupItem>> ScanCocoaPodsAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            var podsCache = Path.Combine(_libraryCaches, "CocoaPods");
            
            ScanDirectoryIfLarge(items, podsCache, "CocoaPods Cache",
                "Developer Tools", "iOS dependency cache. Safe to delete - pods will re-download.", CleanupRisk.Safe, 200);
            
            return items;
        });
    }
    
    /// <summary>
    /// Scan common macOS system caches that are safe to clean
    /// </summary>
    private async Task<List<CleanupItem>> ScanMacOsSystemCachesAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            
            // User Library Caches - generally safe to clean
            var userCaches = new (string SubPath, string Name, string Recommendation, CleanupRisk Risk)[]
            {
                ("com.apple.Safari", "Safari Cache", "Browser cache. Safe to delete.", CleanupRisk.Safe),
                ("com.spotify.client", "Spotify Cache", "Music cache. Safe to delete.", CleanupRisk.Safe),
                ("com.apple.dt.Xcode", "Xcode Cache", "IDE cache. Safe to delete.", CleanupRisk.Safe),
                ("Google", "Google Apps Cache", "Chrome and other Google app caches.", CleanupRisk.Safe),
                ("com.apple.bird", "iCloud Cache", "iCloud sync cache. Safe to delete.", CleanupRisk.Low),
            };
            
            foreach (var (subPath, name, recommendation, risk) in userCaches)
            {
                var fullPath = Path.Combine(_libraryCaches, subPath);
                ScanDirectoryIfLarge(items, fullPath, name, "System Cache", recommendation, risk, 200);
            }
            
            // Logs folder
            var logsPath = Path.Combine(_libraryPath, "Logs");
            ScanDirectoryIfLarge(items, logsPath, "Application Logs",
                "System Cache", "Log files. Safe to delete but useful for troubleshooting.", CleanupRisk.Low, 100);
            
            // Trash
            var trashPath = Path.Combine(_userHome, ".Trash");
            ScanDirectoryIfLarge(items, trashPath, "Trash",
                "System Cache", "Items in Trash. Empty to reclaim space.", CleanupRisk.Medium, 100);

            return items;
        });
    }
}
