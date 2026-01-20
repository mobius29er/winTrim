using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace WinTrim.Core.Services;

/// <summary>
/// macOS-specific implementation of IPlatformService
/// </summary>
public class MacPlatformService : IPlatformService
{
    private readonly string _userHome;
    private readonly string _libraryPath;

    public MacPlatformService()
    {
        _userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _libraryPath = Path.Combine(_userHome, "Library");
    }

    public OperatingSystemType CurrentOS => OperatingSystemType.MacOS;

    public string GetUserFolder() => _userHome;

    public string GetAppDataFolder() => 
        Path.Combine(_libraryPath, "Application Support");

    public string GetLocalAppDataFolder() => 
        Path.Combine(_libraryPath, "Application Support");

    public string GetTempFolder() => 
        Path.Combine(Path.GetTempPath());

    public IEnumerable<DriveInfoModel> GetDrives()
    {
        var addedPaths = new HashSet<string>();
        
        // Always include the user's home folder root (main disk)
        var mainDrive = CreateDriveInfoSafe("/", "Macintosh HD");
        if (mainDrive != null)
        {
            addedPaths.Add("/");
            yield return mainDrive;
        }

        // Scan /Volumes for external drives, NAS, and other mounted volumes
        var volumesPath = "/Volumes";
        if (Directory.Exists(volumesPath))
        {
            foreach (var volume in Directory.GetDirectories(volumesPath))
            {
                var volumeName = Path.GetFileName(volume);
                
                // Skip system volumes and internal macOS partitions
                if (ShouldSkipVolume(volumeName, volume))
                    continue;
                
                // Skip if we already added this (like main disk symlink)
                if (addedPaths.Contains(volume))
                    continue;
                
                var driveInfo = CreateDriveInfoSafe(volume, volumeName);
                if (driveInfo != null)
                {
                    addedPaths.Add(volume);
                    yield return driveInfo;
                }
            }
        }
    }
    
    /// <summary>
    /// Determines if a volume should be hidden from the user
    /// </summary>
    private static bool ShouldSkipVolume(string volumeName, string volumePath)
    {
        // Skip the main disk symlink in /Volumes (we already show root /)
        if (volumeName == "Macintosh HD" || volumeName == "Macintosh HD - Data")
            return true;
        
        // Skip macOS system volumes (APFS container volumes)
        var lowerName = volumeName.ToLowerInvariant();
        if (lowerName == "preboot" || 
            lowerName == "recovery" || 
            lowerName == "vm" || 
            lowerName == "update" ||
            lowerName.StartsWith("com.apple."))
            return true;
            
        // Check if path indicates a system volume
        if (volumePath.StartsWith("/System/Volumes", StringComparison.OrdinalIgnoreCase))
            return true;
            
        // Skip iOS/watchOS/tvOS simulator volumes
        if (volumePath.Contains("/CoreSimulator/", StringComparison.OrdinalIgnoreCase))
            return true;
            
        // Skip hidden/system volumes (xarts, iSCPreboot, Hardware, etc.)
        var systemVolumes = new[] { "xarts", "iscpreboot", "hardware", "data", "home" };
        if (systemVolumes.Contains(lowerName))
            return true;
            
        return false;
    }

    private static DriveInfoModel? CreateDriveInfoSafe(string path, string label)
    {
        try
        {
            var driveInfo = new DriveInfo(path);
            if (!driveInfo.IsReady) return null;

            return new DriveInfoModel
            {
                Name = path,
                RootPath = path,
                Label = label,
                DriveFormat = driveInfo.DriveFormat,
                DriveType = path == "/" ? DriveTypeEnum.Fixed : DriveTypeEnum.Removable,
                TotalSize = driveInfo.TotalSize,
                AvailableFreeSpace = driveInfo.AvailableFreeSpace,
                TotalFreeSpace = driveInfo.TotalFreeSpace,
                IsReady = true
            };
        }
        catch
        {
            return null;
        }
    }

    public IEnumerable<string> GetBrowserCachePaths()
    {
        var caches = Path.Combine(_libraryPath, "Caches");
        var appSupport = Path.Combine(_libraryPath, "Application Support");
        
        return new[]
        {
            // Chrome
            Path.Combine(appSupport, "Google", "Chrome", "Default", "Cache"),
            Path.Combine(caches, "Google", "Chrome", "Default", "Cache"),
            // Safari
            Path.Combine(caches, "com.apple.Safari"),
            Path.Combine(_libraryPath, "Safari"),
            // Firefox
            Path.Combine(appSupport, "Firefox", "Profiles"),
            Path.Combine(caches, "Firefox", "Profiles"),
            // Edge
            Path.Combine(appSupport, "Microsoft Edge", "Default", "Cache"),
            Path.Combine(caches, "Microsoft Edge"),
            // Brave
            Path.Combine(appSupport, "BraveSoftware", "Brave-Browser", "Default", "Cache"),
            // Arc
            Path.Combine(appSupport, "Arc", "User Data", "Default", "Cache"),
        };
    }

    public IEnumerable<string> GetSystemLogPaths()
    {
        return new[]
        {
            Path.Combine(_libraryPath, "Logs"),
            "/var/log",
            Path.Combine(_userHome, ".local", "share", "logs"),
        };
    }

    public void OpenInExplorer(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                // Reveal file in Finder
                Process.Start("open", $"-R \"{path}\"");
            }
            else if (Directory.Exists(path))
            {
                // Open folder in Finder
                Process.Start("open", $"\"{path}\"");
            }
        }
        catch
        {
            // Fallback: just try to open
            try
            {
                Process.Start("open", path);
            }
            catch { }
        }
    }

    public bool MoveToTrash(string path)
    {
        try
        {
            // Use AppleScript to move to Trash (proper macOS way)
            var script = $"tell application \"Finder\" to delete POSIX file \"{path}\"";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = $"-e '{script}'",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            // Fallback: move to ~/.Trash manually
            try
            {
                var trashPath = Path.Combine(_userHome, ".Trash");
                var fileName = Path.GetFileName(path);
                var destPath = Path.Combine(trashPath, fileName);
                
                // Handle name collisions
                var counter = 1;
                while (File.Exists(destPath) || Directory.Exists(destPath))
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var ext = Path.GetExtension(fileName);
                    destPath = Path.Combine(trashPath, $"{nameWithoutExt} {counter}{ext}");
                    counter++;
                }
                
                if (File.Exists(path))
                {
                    File.Move(path, destPath);
                    return true;
                }
                else if (Directory.Exists(path))
                {
                    Directory.Move(path, destPath);
                    return true;
                }
            }
            catch { }
        }
        return false;
    }
}
