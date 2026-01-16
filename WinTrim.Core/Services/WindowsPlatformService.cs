using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace WinTrim.Core.Services;

/// <summary>
/// Windows-specific implementation of IPlatformService
/// </summary>
public class WindowsPlatformService : IPlatformService
{
    public OperatingSystemType CurrentOS => OperatingSystemType.Windows;

    public string GetUserFolder() => 
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public string GetAppDataFolder() => 
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    public string GetLocalAppDataFolder() => 
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public string GetTempFolder() => Path.GetTempPath();

    public IEnumerable<DriveInfoModel> GetDrives()
    {
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady) continue;
            
            yield return new DriveInfoModel
            {
                Name = drive.Name,
                RootPath = drive.RootDirectory.FullName,
                Label = drive.VolumeLabel,
                DriveFormat = drive.DriveFormat,
                DriveType = (DriveTypeEnum)(int)drive.DriveType,
                TotalSize = drive.TotalSize,
                AvailableFreeSpace = drive.AvailableFreeSpace,
                TotalFreeSpace = drive.TotalFreeSpace,
                IsReady = drive.IsReady
            };
        }
    }

    public IEnumerable<string> GetBrowserCachePaths()
    {
        var localAppData = GetLocalAppDataFolder();
        
        return new[]
        {
            // Chrome
            Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Cache"),
            Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Code Cache"),
            // Edge
            Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Cache"),
            Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Code Cache"),
            // Firefox
            Path.Combine(localAppData, "Mozilla", "Firefox", "Profiles"),
            // Brave
            Path.Combine(localAppData, "BraveSoftware", "Brave-Browser", "User Data", "Default", "Cache"),
            // Opera
            Path.Combine(GetAppDataFolder(), "Opera Software", "Opera Stable", "Cache"),
        };
    }

    public IEnumerable<string> GetSystemLogPaths()
    {
        return new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Logs"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Panther"),
            Path.Combine(GetLocalAppDataFolder(), "CrashDumps"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "WER"),
        };
    }

    public void OpenInExplorer(string path)
    {
        if (File.Exists(path))
        {
            // Select the file in Explorer
            Process.Start("explorer.exe", $"/select,\"{path}\"");
        }
        else if (Directory.Exists(path))
        {
            Process.Start("explorer.exe", $"\"{path}\"");
        }
    }

    public bool MoveToTrash(string path)
    {
        try
        {
            // Use Shell32 FileOperation for proper recycle bin support
            // This is a simplified version - full implementation would use COM interop
            if (File.Exists(path))
            {
                FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                return true;
            }
            else if (Directory.Exists(path))
            {
                FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                return true;
            }
        }
        catch
        {
            // Fallback: Try to move to recycle bin via shell
            try
            {
                return NativeMethods.MoveToRecycleBin(path);
            }
            catch { }
        }
        return false;
    }

    /// <summary>
    /// Native methods for Windows-specific operations
    /// </summary>
    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        private const uint FO_DELETE = 0x0003;
        private const ushort FOF_ALLOWUNDO = 0x0040;
        private const ushort FOF_NOCONFIRMATION = 0x0010;
        private const ushort FOF_NOERRORUI = 0x0400;
        private const ushort FOF_SILENT = 0x0004;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        public static bool MoveToRecycleBin(string path)
        {
            var fileOp = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = path + '\0' + '\0', // Double null terminated
                fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_SILENT
            };
            return SHFileOperation(ref fileOp) == 0;
        }
    }
}

// Placeholder classes for VB FileSystem - will use P/Invoke instead
internal static class FileSystem
{
    public static void DeleteFile(string path, UIOption option, RecycleOption recycle)
    {
        // Use native method
        throw new NotImplementedException("Use NativeMethods.MoveToRecycleBin instead");
    }
    
    public static void DeleteDirectory(string path, UIOption option, RecycleOption recycle)
    {
        throw new NotImplementedException("Use NativeMethods.MoveToRecycleBin instead");
    }
}

internal enum UIOption { OnlyErrorDialogs }
internal enum RecycleOption { SendToRecycleBin }
