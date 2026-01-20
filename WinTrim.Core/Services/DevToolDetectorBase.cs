using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// Base class for cross-platform developer tool detection.
/// Contains shared logic; platform-specific paths are provided by derived classes.
/// </summary>
public abstract class DevToolDetectorBase : IDevToolDetector
{
    /// <summary>
    /// Platform service for getting system paths
    /// </summary>
    protected readonly IPlatformService PlatformService;
    
    protected DevToolDetectorBase(IPlatformService platformService)
    {
        PlatformService = platformService;
    }
    
    /// <summary>
    /// Scans for all developer tool caches and returns cleanup items.
    /// </summary>
    public virtual async Task<List<CleanupItem>> ScanAllAsync()
    {
        var results = new List<CleanupItem>();

        var tasks = new List<Task<List<CleanupItem>>>
        {
            ScanGradleAndBuildCachesAsync(),
            ScanNodeCachesAsync(),
            ScanContainerCachesAsync(),
            ScanIdeCachesAsync(),
            ScanPlatformSpecificAsync()
        };

        await Task.WhenAll(tasks);

        foreach (var task in tasks)
        {
            results.AddRange(await task);
        }

        return results;
    }
    
    /// <summary>
    /// Returns cleanup suggestions in unified format for UI integration.
    /// </summary>
    public async Task<List<CleanupSuggestion>> GetSuggestionsAsync()
    {
        var items = await ScanAllAsync();
        return items.Select(item => item.ToCleanupSuggestion()).ToList();
    }
    
    /// <summary>
    /// Legacy interface support - redirects to ScanAllAsync
    /// </summary>
    public Task<List<CleanupItem>> DetectDevToolsAsync(System.Threading.CancellationToken cancellationToken = default)
        => ScanAllAsync();

    #region Cross-Platform Scans
    
    /// <summary>
    /// Scan Gradle, Maven, NuGet, Cargo, Go caches - these exist on all platforms
    /// </summary>
    protected virtual async Task<List<CleanupItem>> ScanGradleAndBuildCachesAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            var userPath = PlatformService.GetUserProfilePath();
            
            var devCaches = new (string Path, string Name, string Recommendation, CleanupRisk Risk)[]
            {
                (Path.Combine(userPath, ".gradle", "caches"), "Gradle Build Cache", 
                    "Safe to delete. Will rebuild on next compile.", CleanupRisk.Safe),
                (Path.Combine(userPath, ".gradle", "wrapper", "dists"), "Gradle Distributions", 
                    "Old Gradle versions. Safe to delete unused ones.", CleanupRisk.Low),
                (Path.Combine(userPath, ".nuget", "packages"), "NuGet Package Cache", 
                    "Safe to delete. NuGet will re-download on next build.", CleanupRisk.Safe),
                (Path.Combine(userPath, ".m2", "repository"), "Maven Repository Cache", 
                    "Safe to delete. Maven will re-download dependencies.", CleanupRisk.Safe),
                (Path.Combine(userPath, ".cargo", "registry"), "Rust Cargo Cache", 
                    "Safe to delete. Cargo will re-download crates.", CleanupRisk.Safe),
                (Path.Combine(userPath, ".cache", "pip"), "Python Pip Cache", 
                    "Safe to delete. Pip will re-download packages.", CleanupRisk.Safe),
                (Path.Combine(userPath, "go", "pkg", "mod", "cache"), "Go Module Cache", 
                    "Safe to delete. Go will re-download modules.", CleanupRisk.Safe),
            };

            foreach (var (path, name, recommendation, risk) in devCaches)
            {
                ScanDirectoryIfLarge(items, path, name, "Developer Tools", recommendation, risk, 500);
            }

            return items;
        });
    }
    
    /// <summary>
    /// Scan NPM, Yarn, pnpm caches - cross-platform
    /// </summary>
    protected virtual async Task<List<CleanupItem>> ScanNodeCachesAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            var userPath = PlatformService.GetUserProfilePath();
            
            var nodeCaches = new (string Path, string Name)[]
            {
                (Path.Combine(userPath, ".npm"), "NPM Global Cache"),
                (Path.Combine(userPath, ".yarn", "cache"), "Yarn Berry Cache"),
                (Path.Combine(userPath, ".pnpm-store"), "pnpm Store"),
            };

            foreach (var (path, name) in nodeCaches)
            {
                ScanDirectoryIfLarge(items, path, name, "Developer Tools",
                    "Safe to delete. Package manager will re-download.", CleanupRisk.Safe, 200);
            }
            
            // Platform-specific node cache locations
            foreach (var path in GetPlatformNodeCachePaths())
            {
                ScanDirectoryIfLarge(items, path.Path, path.Name, "Developer Tools",
                    "Safe to delete. Package manager will re-download.", CleanupRisk.Safe, 200);
            }

            return items;
        });
    }
    
    /// <summary>
    /// Scan Docker and container caches
    /// </summary>
    protected virtual async Task<List<CleanupItem>> ScanContainerCachesAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            
            foreach (var dockerPath in GetDockerPaths())
            {
                try
                {
                    if (File.Exists(dockerPath.Path))
                    {
                        var fileInfo = new FileInfo(dockerPath.Path);
                        if (fileInfo.Length > 1L * 1024 * 1024 * 1024) // > 1GB
                        {
                            items.Add(new CleanupItem
                            {
                                Name = dockerPath.Name,
                                Path = dockerPath.Path,
                                SizeBytes = fileInfo.Length,
                                Category = "Developer Tools",
                                Recommendation = dockerPath.Recommendation,
                                Risk = CleanupRisk.High
                            });
                        }
                    }
                    else if (Directory.Exists(dockerPath.Path))
                    {
                        ScanDirectoryIfLarge(items, dockerPath.Path, dockerPath.Name, "Developer Tools",
                            dockerPath.Recommendation, CleanupRisk.High, 1024);
                    }
                }
                catch { /* Skip inaccessible */ }
            }

            return items;
        });
    }
    
    /// <summary>
    /// Scan IDE caches (VS Code, JetBrains, etc.)
    /// </summary>
    protected virtual async Task<List<CleanupItem>> ScanIdeCachesAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            
            foreach (var cache in GetIdeCachePaths())
            {
                ScanDirectoryIfLarge(items, cache.Path, cache.Name, "Developer Tools",
                    cache.Recommendation, cache.Risk, cache.MinSizeMb);
            }

            return items;
        });
    }
    
    #endregion
    
    #region Platform-Specific Abstract Methods
    
    /// <summary>
    /// Override to provide platform-specific scanning (e.g., XCode on Mac, WSL on Windows)
    /// </summary>
    protected abstract Task<List<CleanupItem>> ScanPlatformSpecificAsync();
    
    /// <summary>
    /// Override to provide platform-specific Node.js cache paths
    /// </summary>
    protected abstract IEnumerable<(string Path, string Name)> GetPlatformNodeCachePaths();
    
    /// <summary>
    /// Override to provide platform-specific Docker paths
    /// </summary>
    protected abstract IEnumerable<(string Path, string Name, string Recommendation)> GetDockerPaths();
    
    /// <summary>
    /// Override to provide platform-specific IDE cache paths
    /// </summary>
    protected abstract IEnumerable<(string Path, string Name, string Recommendation, CleanupRisk Risk, int MinSizeMb)> GetIdeCachePaths();
    
    #endregion
    
    #region Helpers
    
    protected void ScanDirectoryIfLarge(List<CleanupItem> items, string path, string name, 
        string category, string recommendation, CleanupRisk risk, int minSizeMb)
    {
        try
        {
            if (Directory.Exists(path))
            {
                var dirInfo = new DirectoryInfo(path);
                long size = GetDirectorySize(dirInfo);
                
                if (size > minSizeMb * 1024L * 1024)
                {
                    items.Add(new CleanupItem
                    {
                        Name = name,
                        Path = path,
                        SizeBytes = size,
                        Category = category,
                        Recommendation = recommendation,
                        Risk = risk,
                        LastAccessed = dirInfo.LastAccessTime
                    });
                }
            }
        }
        catch { /* Skip inaccessible */ }
    }
    
    protected static long GetDirectorySize(DirectoryInfo directory, int maxDepth = 20)
    {
        // Prevent infinite recursion
        if (maxDepth <= 0) return 0;
        
        long size = 0;
        try
        {
            // Skip symlinks to prevent following into other filesystems
            if ((directory.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                return 0;
            
            // Skip common problematic macOS paths that can cause hangs
            var name = directory.Name.ToLowerInvariant();
            if (name == "coresimulator" || name == "simulators" || name == ".timemachine" || 
                name == "time machine backups" || name.Contains(".mobilebackups"))
                return 0;
            
            foreach (var file in directory.GetFiles())
            {
                try { size += file.Length; } catch { }
            }

            foreach (var subDir in directory.GetDirectories())
            {
                // Skip symlinks
                if ((subDir.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                {
                    size += GetDirectorySize(subDir, maxDepth - 1);
                }
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (DirectoryNotFoundException) { }
        catch { /* Catch all for any other errors */ }
        
        return size;
    }
    
    #endregion
}
