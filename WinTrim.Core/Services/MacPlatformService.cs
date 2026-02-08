using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace WinTrim.Core.Services;

/// <summary>
/// macOS-specific implementation of IPlatformService
/// </summary>
public class MacPlatformService : IPlatformService
{
    private readonly string _userHome;
    private readonly string _libraryPath;

    // Objective-C Runtime P/Invoke for NSFileManager.trashItem
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr objc_getClass(string className);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr sel_registerName(string selectorName);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern bool objc_msgSend_bool(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation")]
    private static extern IntPtr NSStringFromClass(IntPtr cls);

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
            // Method 1: Use native NSFileManager.trashItem via Objective-C runtime
            // This is the ONLY method that works properly in App Sandbox
            if (TryTrashWithNSFileManager(path))
                return true;
            
            // Method 2: Fallback to AppleScript (works outside sandbox only)
            var script = $"tell application \"Finder\" to delete POSIX file \"{path.Replace("\"", "\\\"")}\"";
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
        catch (Exception ex)
        {
            Console.WriteLine($"MoveToTrash failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Use NSFileManager.trashItem:atURL:resultingItemURL:error: via Objective-C runtime.
    /// This is the only method that works in sandboxed macOS apps.
    /// </summary>
    private bool TryTrashWithNSFileManager(string path)
    {
        try
        {
            // Get NSFileManager class
            var nsFileManagerClass = objc_getClass("NSFileManager");
            if (nsFileManagerClass == IntPtr.Zero) return false;
            
            // Get default file manager: [NSFileManager defaultManager]
            var defaultManagerSel = sel_registerName("defaultManager");
            var fileManager = objc_msgSend(nsFileManagerClass, defaultManagerSel);
            if (fileManager == IntPtr.Zero) return false;
            
            // Create NSURL from path: [NSURL fileURLWithPath:]
            var nsUrlClass = objc_getClass("NSURL");
            if (nsUrlClass == IntPtr.Zero) return false;
            
            // Create NSString from path
            var nsStringClass = objc_getClass("NSString");
            if (nsStringClass == IntPtr.Zero) return false;
            
            var stringWithUTF8Sel = sel_registerName("stringWithUTF8String:");
            var pathPtr = Marshal.StringToHGlobalAnsi(path);
            try
            {
                var nsPath = objc_msgSend(nsStringClass, stringWithUTF8Sel, pathPtr);
                if (nsPath == IntPtr.Zero) return false;
                
                // Create NSURL: [NSURL fileURLWithPath:nsPath]
                var fileURLWithPathSel = sel_registerName("fileURLWithPath:");
                var nsUrl = objc_msgSend(nsUrlClass, fileURLWithPathSel, nsPath);
                if (nsUrl == IntPtr.Zero) return false;
                
                // Call trashItemAtURL:resultingItemURL:error:
                var trashItemSel = sel_registerName("trashItemAtURL:resultingItemURL:error:");
                var result = objc_msgSend_bool(fileManager, trashItemSel, nsUrl, IntPtr.Zero, IntPtr.Zero);
                
                return result;
            }
            finally
            {
                Marshal.FreeHGlobal(pathPtr);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NSFileManager.trashItem failed: {ex.Message}");
            return false;
        }
    }
}
