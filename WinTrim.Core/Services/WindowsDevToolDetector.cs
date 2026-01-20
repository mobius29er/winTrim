using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// Windows-specific developer tool detector.
/// Handles WSL, Windows-specific Docker paths, Adobe caches, Android AVDs, etc.
/// </summary>
public class WindowsDevToolDetector : DevToolDetectorBase
{
    private readonly string _appDataLocal;
    private readonly string _appDataRoaming;
    private readonly string _tempPath;
    
    public WindowsDevToolDetector(IPlatformService platformService) : base(platformService)
    {
        _appDataLocal = platformService.GetAppDataLocalPath();
        _appDataRoaming = platformService.GetAppDataRoamingPath();
        _tempPath = platformService.GetTempPath();
    }
    
    protected override IEnumerable<(string Path, string Name)> GetPlatformNodeCachePaths()
    {
        return new[]
        {
            (Path.Combine(_appDataRoaming, "npm-cache"), "NPM Cache"),
            (Path.Combine(_appDataLocal, "npm-cache"), "NPM Cache (Local)"),
            (Path.Combine(_appDataLocal, "Yarn", "Cache"), "Yarn Cache"),
            (Path.Combine(_appDataLocal, "pnpm", "store"), "pnpm Store"),
        };
    }
    
    protected override IEnumerable<(string Path, string Name, string Recommendation)> GetDockerPaths()
    {
        return new[]
        {
            (Path.Combine(_appDataLocal, "Docker", "wsl", "data", "ext4.vhdx"), 
                "Docker Desktop Data (WSL)", "High disk usage. Use 'docker system prune' to clean."),
            (Path.Combine(_appDataLocal, "Docker", "wsl", "distro", "ext4.vhdx"), 
                "Docker Desktop Distro", "High disk usage. Use 'docker system prune' to clean."),
            (Path.Combine(_appDataLocal, "Docker", "wsl", "data"), 
                "Docker Data Directory", "High disk usage. Use 'docker system prune' to clean."),
        };
    }
    
    protected override IEnumerable<(string Path, string Name, string Recommendation, CleanupRisk Risk, int MinSizeMb)> GetIdeCachePaths()
    {
        return new[]
        {
            (Path.Combine(_appDataLocal, "JetBrains"), "JetBrains IDE Cache", 
                "IDE caches and indexes. Safe to delete but IDEs will re-index.", CleanupRisk.Low, 500),
            (Path.Combine(_appDataRoaming, "Code", "CachedExtensionVSIXs"), "VS Code Extension Cache", 
                "Cached extension downloads. Safe to delete.", CleanupRisk.Safe, 100),
            (Path.Combine(_appDataRoaming, "Code", "Cache"), "VS Code Cache", 
                "VS Code cache files. Safe to delete.", CleanupRisk.Safe, 200),
            (Path.Combine(_appDataLocal, "pip", "cache"), "Python Pip Cache (Local)", 
                "Safe to delete. Pip will re-download packages.", CleanupRisk.Safe, 200),
            (Path.Combine(PlatformService.GetUserProfilePath(), ".vscode-server"), "VS Code Server (WSL/Remote)", 
                "Remote dev server. Safe to delete if not using remote development.", CleanupRisk.Medium, 500),
        };
    }
    
    protected override async Task<List<CleanupItem>> ScanPlatformSpecificAsync()
    {
        var items = new List<CleanupItem>();
        
        var tasks = new List<Task<List<CleanupItem>>>
        {
            ScanAndroidAvdsAsync(),
            ScanWslDistrosAsync(),
            ScanAdobeCachesAsync()
        };
        
        await Task.WhenAll(tasks);
        
        foreach (var task in tasks)
        {
            items.AddRange(await task);
        }
        
        return items;
    }
    
    /// <summary>
    /// Scan Android Virtual Devices
    /// </summary>
    private async Task<List<CleanupItem>> ScanAndroidAvdsAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            var avdPath = Path.Combine(PlatformService.GetUserProfilePath(), ".android", "avd");

            if (!Directory.Exists(avdPath)) return items;

            try
            {
                foreach (var folder in Directory.GetDirectories(avdPath, "*.avd"))
                {
                    var dirInfo = new DirectoryInfo(folder);
                    long size = GetDirectorySize(dirInfo);

                    if (size > 500L * 1024 * 1024)
                    {
                        items.Add(new CleanupItem
                        {
                            Name = $"Android Emulator: {dirInfo.Name.Replace(".avd", "").Replace("_", " ")}",
                            Path = folder,
                            SizeBytes = size,
                            Category = "Developer Tools",
                            Recommendation = "Wipes the virtual phone. Safe if you can recreate it.",
                            Risk = CleanupRisk.Medium
                        });
                    }
                }
            }
            catch { }

            return items;
        });
    }
    
    /// <summary>
    /// Scan WSL2 distribution virtual disks
    /// </summary>
    private async Task<List<CleanupItem>> ScanWslDistrosAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            var wslDistrosPath = Path.Combine(_appDataLocal, "Packages");
            
            if (!Directory.Exists(wslDistrosPath)) return items;

            try
            {
                foreach (var distroDir in Directory.GetDirectories(wslDistrosPath, "*Linux*"))
                {
                    var localStatePath = Path.Combine(distroDir, "LocalState");
                    if (!Directory.Exists(localStatePath)) continue;
                    
                    foreach (var vhdx in Directory.GetFiles(localStatePath, "*.vhdx"))
                    {
                        var fileInfo = new FileInfo(vhdx);
                        if (fileInfo.Length > 1L * 1024 * 1024 * 1024)
                        {
                            var distroName = Path.GetFileName(distroDir);
                            items.Add(new CleanupItem
                            {
                                Name = $"WSL Distro: {distroName}",
                                Path = vhdx,
                                SizeBytes = fileInfo.Length,
                                Category = "Developer Tools",
                                Recommendation = "WSL2 virtual disk. Use 'wsl --manage <distro> --optimize' to compact.",
                                Risk = CleanupRisk.High
                            });
                        }
                    }
                }
            }
            catch { }

            return items;
        });
    }
    
    /// <summary>
    /// Scan Adobe Creative Suite caches
    /// </summary>
    private async Task<List<CleanupItem>> ScanAdobeCachesAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            
            var adobePaths = new (string Path, string Name, string Recommendation)[]
            {
                (Path.Combine(_appDataRoaming, "Adobe", "Common", "Media Cache Files"), 
                    "Adobe Media Cache Files", "Safe to delete. Adobe apps will regenerate."),
                (Path.Combine(_appDataRoaming, "Adobe", "Common", "Media Cache"), 
                    "Adobe Media Cache Database", "Safe to delete. Adobe apps will regenerate."),
                (Path.Combine(_appDataLocal, "Adobe", "Common", "Media Cache Files"), 
                    "Adobe Media Cache Files (Local)", "Safe to delete. Adobe apps will regenerate."),
                (Path.Combine(_tempPath, "Adobe", "After Effects Disk Cache"), 
                    "After Effects Disk Cache", "Safe to delete. AE will regenerate."),
                (Path.Combine(_appDataRoaming, "Adobe", "Common", "Peak Files"), 
                    "Premiere Pro Peak Files", "Audio waveform cache. Safe to delete."),
                (Path.Combine(_appDataLocal, "Adobe", "Lightroom", "Cache"), 
                    "Lightroom Cache", "Preview cache. Safe to delete."),
                (Path.Combine(_appDataLocal, "Adobe", "Creative Cloud", "ACC"), 
                    "Creative Cloud Cache", "Download cache. Safe to delete."),
            };

            foreach (var (path, name, recommendation) in adobePaths)
            {
                ScanDirectoryIfLarge(items, path, name, "Media Cache", recommendation, CleanupRisk.Safe, 50);
            }

            return items;
        });
    }
}
