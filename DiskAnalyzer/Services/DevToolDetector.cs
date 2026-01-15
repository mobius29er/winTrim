using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiskAnalyzer.Models;

namespace DiskAnalyzer.Services;

/// <summary>
/// Detects developer tools and caches that can be safely cleaned up.
/// Results integrate with CleanupAdvisor for unified cleanup suggestions.
/// </summary>
public sealed class DevToolDetector
{
    // Common paths cached for performance
    private static readonly string UserPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string AppDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static readonly string AppDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static readonly string TempPath = Path.GetTempPath();

    /// <summary>
    /// Scans for all developer tool caches and returns cleanup items.
    /// </summary>
    public async Task<List<CleanupItem>> ScanAllAsync()
    {
        var results = new List<CleanupItem>();

        // Run all scans in parallel for speed
        var tasks = new List<Task<List<CleanupItem>>>
        {
            ScanAndroidAvdsAsync(),
            ScanGradleCacheAsync(),
            ScanDockerWslAsync(),
            ScanAdobeCacheAsync(),
            ScanNodeModulesCacheAsync()
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

    // ---------------------------------------------------------
    // 1. Android Virtual Devices (AVDs)
    // ---------------------------------------------------------
    private static async Task<List<CleanupItem>> ScanAndroidAvdsAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            string avdPath = Path.Combine(UserPath, ".android", "avd");

            if (!Directory.Exists(avdPath)) return items;

            try
            {
                // Look for .avd folders
                foreach (var folder in Directory.GetDirectories(avdPath, "*.avd"))
                {
                    var dirInfo = new DirectoryInfo(folder);
                    long size = GetDirectorySize(dirInfo);

                    // Report if > 500MB
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
            catch (UnauthorizedAccessException) { /* Skip protected folders */ }
            catch (DirectoryNotFoundException) { /* Skip missing folders */ }

            return items;
        });
    }

    // ---------------------------------------------------------
    // 2. Gradle Build Caches (Android/Java)
    // ---------------------------------------------------------
    private static async Task<List<CleanupItem>> ScanGradleCacheAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            
            // Multiple dev tool cache locations
            var devCaches = new (string Path, string Name, string Category, string Recommendation, CleanupRisk Risk)[]
            {
                (Path.Combine(UserPath, ".gradle", "caches"), "Gradle Build Cache", "Developer Tools", 
                    "Safe to delete. Will rebuild on next compile.", CleanupRisk.Safe),
                (Path.Combine(UserPath, ".gradle", "wrapper", "dists"), "Gradle Distributions", "Developer Tools", 
                    "Old Gradle versions. Safe to delete unused ones.", CleanupRisk.Low),
                (Path.Combine(UserPath, ".nuget", "packages"), "NuGet Package Cache", "Developer Tools", 
                    "Safe to delete. NuGet will re-download on next build.", CleanupRisk.Safe),
                (Path.Combine(UserPath, ".m2", "repository"), "Maven Repository Cache", "Developer Tools", 
                    "Safe to delete. Maven will re-download dependencies.", CleanupRisk.Safe),
                (Path.Combine(UserPath, ".cargo", "registry"), "Rust Cargo Cache", "Developer Tools", 
                    "Safe to delete. Cargo will re-download crates.", CleanupRisk.Safe),
                (Path.Combine(UserPath, ".cache", "pip"), "Python Pip Cache", "Developer Tools", 
                    "Safe to delete. Pip will re-download packages.", CleanupRisk.Safe),
                (Path.Combine(AppDataLocal, "pip", "cache"), "Python Pip Cache (Local)", "Developer Tools", 
                    "Safe to delete. Pip will re-download packages.", CleanupRisk.Safe),
                (Path.Combine(UserPath, "go", "pkg", "mod", "cache"), "Go Module Cache", "Developer Tools", 
                    "Safe to delete. Go will re-download modules.", CleanupRisk.Safe),
                (Path.Combine(AppDataLocal, "JetBrains"), "JetBrains IDE Cache", "Developer Tools", 
                    "IDE caches and indexes. Safe to delete but IDEs will re-index.", CleanupRisk.Low),
                (Path.Combine(AppDataRoaming, "Code", "CachedExtensionVSIXs"), "VS Code Extension Cache", "Developer Tools", 
                    "Cached extension downloads. Safe to delete.", CleanupRisk.Safe),
                (Path.Combine(AppDataRoaming, "Code", "Cache"), "VS Code Cache", "Developer Tools", 
                    "VS Code cache files. Safe to delete.", CleanupRisk.Safe),
                (Path.Combine(UserPath, ".vscode-server"), "VS Code Server (WSL/Remote)", "Developer Tools", 
                    "Remote dev server. Safe to delete if not using remote development.", CleanupRisk.Medium),
            };

            foreach (var (path, name, category, recommendation, risk) in devCaches)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        long size = GetDirectorySize(new DirectoryInfo(path));
                        
                        // Report if > 500MB
                        if (size > 500L * 1024 * 1024)
                        {
                            items.Add(new CleanupItem
                            {
                                Name = name,
                                Path = path,
                                SizeBytes = size,
                                Category = category,
                                Recommendation = recommendation,
                                Risk = risk
                            });
                        }
                    }
                }
                catch { /* Skip inaccessible */ }
            }

            return items;
        });
    }

    // ---------------------------------------------------------
    // 3. Docker / WSL Virtual Disks
    // ---------------------------------------------------------
    private static async Task<List<CleanupItem>> ScanDockerWslAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            
            // Multiple possible Docker data locations
            var dockerPaths = new[]
            {
                Path.Combine(AppDataLocal, "Docker", "wsl", "data", "ext4.vhdx"),
                Path.Combine(AppDataLocal, "Docker", "wsl", "distro", "ext4.vhdx"),
                Path.Combine(AppDataLocal, "Docker", "wsl", "data"),
            };

            foreach (var dockerPath in dockerPaths)
            {
                try
                {
                    if (File.Exists(dockerPath))
                    {
                        var fileInfo = new FileInfo(dockerPath);
                        if (fileInfo.Length > 1L * 1024 * 1024 * 1024) // > 1GB
                        {
                            items.Add(new CleanupItem
                            {
                                Name = "Docker Desktop Data (WSL)",
                                Path = dockerPath,
                                SizeBytes = fileInfo.Length,
                                Category = "Developer Tools",
                                Recommendation = "High disk usage. Use 'docker system prune' to clean.",
                                Risk = CleanupRisk.High
                            });
                        }
                    }
                    else if (Directory.Exists(dockerPath))
                    {
                        // Scan for any .vhdx files in the directory
                        foreach (var vhdx in Directory.GetFiles(dockerPath, "*.vhdx"))
                        {
                            var fileInfo = new FileInfo(vhdx);
                            if (fileInfo.Length > 1L * 1024 * 1024 * 1024)
                            {
                                items.Add(new CleanupItem
                                {
                                    Name = $"Docker Data: {fileInfo.Name}",
                                    Path = vhdx,
                                    SizeBytes = fileInfo.Length,
                                    Category = "Developer Tools",
                                    Recommendation = "High disk usage. Use 'docker system prune' to clean.",
                                    Risk = CleanupRisk.High
                                });
                            }
                        }
                    }
                }
                catch { /* Skip inaccessible paths */ }
            }

            // Also check WSL distros folder for any large .vhdx files
            var wslDistrosPath = Path.Combine(AppDataLocal, "Packages");
            if (Directory.Exists(wslDistrosPath))
            {
                try
                {
                    foreach (var distroDir in Directory.GetDirectories(wslDistrosPath, "*Linux*"))
                    {
                        var localStatePath = Path.Combine(distroDir, "LocalState");
                        if (Directory.Exists(localStatePath))
                        {
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
                }
                catch { /* Skip if no access */ }
            }

            return items;
        });
    }

    // ---------------------------------------------------------
    // 4. Adobe / Creative Software Caches
    // ---------------------------------------------------------
    private static async Task<List<CleanupItem>> ScanAdobeCacheAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            
            // Adobe cache locations - comprehensive list
            var adobePaths = new (string Path, string Name, string Recommendation)[]
            {
                // Common Adobe locations
                (Path.Combine(AppDataRoaming, "Adobe", "Common", "Media Cache Files"), 
                    "Adobe Media Cache Files", "Safe to delete. Adobe apps will regenerate."),
                (Path.Combine(AppDataRoaming, "Adobe", "Common", "Media Cache"), 
                    "Adobe Media Cache Database", "Safe to delete. Adobe apps will regenerate."),
                (Path.Combine(AppDataLocal, "Adobe", "Common", "Media Cache Files"), 
                    "Adobe Media Cache Files (Local)", "Safe to delete. Adobe apps will regenerate."),
                (Path.Combine(AppDataLocal, "Adobe", "Common", "Media Cache"), 
                    "Adobe Media Cache Database (Local)", "Safe to delete. Adobe apps will regenerate."),
                    
                // After Effects
                (Path.Combine(TempPath, "Adobe", "After Effects Disk Cache"), 
                    "After Effects Disk Cache", "Safe to delete. AE will regenerate."),
                (Path.Combine(AppDataLocal, "Temp", "Adobe", "After Effects"), 
                    "After Effects Temp", "Safe to delete."),
                    
                // Premiere Pro
                (Path.Combine(AppDataRoaming, "Adobe", "Common", "Peak Files"), 
                    "Premiere Pro Peak Files", "Audio waveform cache. Safe to delete."),
                (Path.Combine(AppDataRoaming, "Adobe", "Common", "PTX"), 
                    "Premiere Pro PTX Cache", "Safe to delete."),
                    
                // Photoshop
                (Path.Combine(AppDataLocal, "Temp", "Photoshop Temp"), 
                    "Photoshop Temp Files", "Safe to delete when PS is closed."),
                    
                // Lightroom
                (Path.Combine(AppDataLocal, "Adobe", "Lightroom", "Cache"), 
                    "Lightroom Cache", "Preview cache. Safe to delete."),
                (Path.Combine(AppDataLocal, "Adobe", "Lightroom CC", "Cache"), 
                    "Lightroom CC Cache", "Preview cache. Safe to delete."),
                    
                // Illustrator
                (Path.Combine(AppDataLocal, "Adobe", "Illustrator"), 
                    "Illustrator Cache", "Safe to delete."),
                    
                // Creative Cloud
                (Path.Combine(AppDataLocal, "Adobe", "Creative Cloud", "ACC"), 
                    "Creative Cloud Cache", "Download cache. Safe to delete."),
            };

            foreach (var (path, name, recommendation) in adobePaths)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        long size = GetDirectorySize(new DirectoryInfo(path));
                        
                        // Report if > 50MB (Adobe caches can be valuable to clean)
                        if (size > 50L * 1024 * 1024)
                        {
                            items.Add(new CleanupItem
                            {
                                Name = name,
                                Path = path,
                                SizeBytes = size,
                                Category = "Media Cache",
                                Recommendation = recommendation,
                                Risk = CleanupRisk.Safe
                            });
                        }
                    }
                }
                catch { /* Skip inaccessible */ }
            }
            
            // Also scan for any large folders in Adobe AppData
            ScanAdobeAppDataFolders(items);

            return items;
        });
    }
    
    /// <summary>
    /// Scans Adobe AppData folders for any large cache directories
    /// </summary>
    private static void ScanAdobeAppDataFolders(List<CleanupItem> items)
    {
        var adobeRoots = new[]
        {
            Path.Combine(AppDataRoaming, "Adobe"),
            Path.Combine(AppDataLocal, "Adobe")
        };
        
        foreach (var root in adobeRoots)
        {
            if (!Directory.Exists(root)) continue;
            
            try
            {
                foreach (var dir in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
                {
                    var dirName = Path.GetFileName(dir).ToLowerInvariant();
                    
                    // Skip templates - these are NOT safe to delete!
                    if (dirName.Contains("template") || dirName.Contains("preset") || 
                        dirName.Contains("settings") || dirName.Contains("workspace"))
                    {
                        continue;
                    }
                    
                    // Look for cache-like folder names (safe to delete)
                    bool isSafeCache = dirName.Contains("cache") || dirName.Contains("temp") || 
                                       dirName == "crash reports";
                    
                    // Logs are lower risk but still deletable
                    bool isLogFolder = dirName.Contains("logs") || dirName.Contains("log");
                    
                    if (isSafeCache || isLogFolder)
                    {
                        // Skip if we already added this path
                        if (items.Any(i => i.Path.Equals(dir, StringComparison.OrdinalIgnoreCase)))
                            continue;
                            
                        long size = GetDirectorySize(new DirectoryInfo(dir));
                        if (size > 50L * 1024 * 1024)
                        {
                            var appName = GetAdobeAppName(dir);
                            items.Add(new CleanupItem
                            {
                                Name = $"{appName} {Path.GetFileName(dir)}",
                                Path = dir,
                                SizeBytes = size,
                                Category = "Media Cache",
                                Recommendation = isSafeCache 
                                    ? "Adobe cache folder. Safe to delete."
                                    : "Log files. Safe to delete but useful for troubleshooting.",
                                Risk = isSafeCache ? CleanupRisk.Safe : CleanupRisk.Low
                            });
                        }
                    }
                }
            }
            catch { /* Skip if access denied */ }
        }
    }
    
    private static string GetAdobeAppName(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar);
        foreach (var part in parts)
        {
            if (part.StartsWith("Adobe ") || part.StartsWith("Premiere") || 
                part.StartsWith("After Effects") || part.StartsWith("Photoshop") ||
                part.StartsWith("Lightroom") || part.StartsWith("Illustrator") ||
                part.StartsWith("InDesign") || part.StartsWith("Audition"))
            {
                return part;
            }
        }
        return "Adobe";
    }

    // ---------------------------------------------------------
    // 5. NPM / Yarn / pnpm Cache
    // ---------------------------------------------------------
    private static async Task<List<CleanupItem>> ScanNodeModulesCacheAsync()
    {
        return await Task.Run(() =>
        {
            var items = new List<CleanupItem>();
            
            var nodeCaches = new (string Path, string Name)[]
            {
                (Path.Combine(AppDataRoaming, "npm-cache"), "NPM Cache"),
                (Path.Combine(AppDataLocal, "npm-cache"), "NPM Cache (Local)"),
                (Path.Combine(AppDataLocal, "Yarn", "Cache"), "Yarn Cache"),
                (Path.Combine(AppDataLocal, "pnpm", "store"), "pnpm Store"),
                (Path.Combine(UserPath, ".npm"), "NPM Global Cache"),
                (Path.Combine(UserPath, ".yarn", "cache"), "Yarn Berry Cache"),
            };

            foreach (var (path, name) in nodeCaches)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        long size = GetDirectorySize(new DirectoryInfo(path));
                        // Report if > 200MB
                        if (size > 200L * 1024 * 1024)
                        {
                            items.Add(new CleanupItem
                            {
                                Name = name,
                                Path = path,
                                SizeBytes = size,
                                Category = "Developer Tools",
                                Recommendation = "Safe to delete. Package manager will re-download.",
                                Risk = CleanupRisk.Safe
                            });
                        }
                    }
                }
                catch { /* Skip inaccessible */ }
            }
            
            return items;
        });
    }

    // ---------------------------------------------------------
    // Helper: Safe Recursive Directory Size
    // ---------------------------------------------------------
    private static long GetDirectorySize(DirectoryInfo directory)
    {
        long size = 0;
        try
        {
            foreach (var file in directory.GetFiles())
            {
                size += file.Length;
            }

            foreach (var subDir in directory.GetDirectories())
            {
                size += GetDirectorySize(subDir);
            }
        }
        catch (UnauthorizedAccessException) { /* Skip protected folders */ }
        catch (DirectoryNotFoundException) { /* Skip missing folders */ }
        
        return size;
    }
}