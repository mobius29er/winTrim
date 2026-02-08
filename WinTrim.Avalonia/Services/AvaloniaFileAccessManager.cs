using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using WinTrim.Core.Services;

namespace WinTrim.Avalonia.Services;

/// <summary>
/// Unified file access manager using Avalonia's StorageProvider.
/// Handles security-scoped bookmarks on macOS, simple paths on Windows/Linux.
/// 
/// Key behaviors:
/// - macOS: Uses StorageProvider bookmarks for sandbox compliance
/// - Windows/Linux: Bookmarks are just paths (no sandbox restrictions)
/// </summary>
public class AvaloniaFileAccessManager : IFileAccessManager
{
    private readonly ISettingsService _settingsService;
    private readonly IAppLogger _logger;
    
    // Track actively accessed folders (for security-scoped resource management)
    private readonly Dictionary<string, IStorageFolder> _activeAccessFolders = new();
    
    private static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public AvaloniaFileAccessManager(ISettingsService settingsService, IAppLogger logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Request folder access via system folder picker.
    /// On macOS: Creates security-scoped bookmark.
    /// On Windows/Linux: Simply returns path.
    /// </summary>
    public async Task<string?> RequestFolderAccessAsync()
    {
        var storageProvider = GetStorageProvider();
        if (storageProvider == null)
        {
            _logger.LogWarning("StorageProvider not available");
            return null;
        }

        try
        {
            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Folder to Scan",
                AllowMultiple = false
            });

            var folder = folders.FirstOrDefault();
            if (folder == null)
            {
                _logger.LogDebug("User cancelled folder picker");
                return null;
            }

            var path = folder.TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                _logger.LogWarning("Could not get local path from selected folder");
                return null;
            }

            // Save bookmark for persistent access (critical for macOS sandbox)
            await SaveBookmarkAsync(folder, path);
            
            // Add to recent paths
            _settingsService.AddRecentScanPath(path);
            
            _logger.LogInfo($"Folder access granted: {path}");
            return path;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to request folder access: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// Save a security-scoped bookmark for the folder
    /// </summary>
    private async Task SaveBookmarkAsync(IStorageFolder folder, string path)
    {
        try
        {
            var bookmarkId = await folder.SaveBookmarkAsync();
            if (!string.IsNullOrEmpty(bookmarkId))
            {
                _settingsService.SaveFolderBookmark(path, bookmarkId);
                _logger.LogDebug($"Saved bookmark for: {path}");
            }
            else
            {
                _logger.LogDebug($"Bookmark not available for: {path} (normal on Windows/Linux)");
            }
        }
        catch (Exception ex)
        {
            // Bookmark saving may not be supported on all platforms
            _logger.LogDebug($"Could not save bookmark: {ex.Message}");
        }
    }

    /// <summary>
    /// Get list of folders the user has granted access to
    /// </summary>
    public async Task<List<GrantedFolder>> GetGrantedFoldersAsync()
    {
        var result = new List<GrantedFolder>();
        var storageProvider = GetStorageProvider();

        foreach (var path in _settingsService.RecentScanPaths)
        {
            if (!Directory.Exists(path)) continue;

            // Try to restore from bookmark first (for macOS)
            var hasBookmarkAccess = false;
            var bookmarkId = _settingsService.GetFolderBookmark(path);
            
            if (!string.IsNullOrEmpty(bookmarkId) && storageProvider != null)
            {
                try
                {
                    var folder = await storageProvider.OpenFolderBookmarkAsync(bookmarkId);
                    hasBookmarkAccess = folder != null;
                    
                    if (folder != null && IsMacOS)
                    {
                        // Store for later use
                        _activeAccessFolders[path] = folder;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"Could not restore bookmark for {path}: {ex.Message}");
                }
            }

            // On Windows/Linux, we always have access if the folder exists
            var hasAccess = hasBookmarkAccess || !IsMacOS;

            if (hasAccess)
            {
                result.Add(new GrantedFolder
                {
                    Path = path,
                    DisplayName = GetDisplayName(path),
                    SizeBytes = GetDriveUsedSpace(path),
                    GrantedDate = DateTime.Now, // Not tracked precisely
                    LastAccessedDate = Directory.GetLastWriteTime(path)
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Check if the app has access to a specific path
    /// </summary>
    public bool HasAccessToPath(string path)
    {
        // On Windows/Linux, always have access if path exists
        if (!IsMacOS)
        {
            return Directory.Exists(path) || File.Exists(path);
        }

        // On macOS, check if path is within an actively accessed folder
        foreach (var activePath in _activeAccessFolders.Keys)
        {
            if (path.StartsWith(activePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Check if we have a bookmark for any parent folder
        foreach (var bookmarkedPath in _settingsService.FolderBookmarks.Keys)
        {
            if (path.StartsWith(bookmarkedPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Revoke access to a folder
    /// </summary>
    public async Task RevokeAccessAsync(string folderPath)
    {
        // Release any active access
        if (_activeAccessFolders.TryGetValue(folderPath, out var folder))
        {
            try
            {
                if (folder is IStorageBookmarkFolder bookmarkFolder)
                {
                    await bookmarkFolder.ReleaseBookmarkAsync();
                }
                folder.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Error releasing folder access: {ex.Message}");
            }
            _activeAccessFolders.Remove(folderPath);
        }

        // Remove from settings
        _settingsService.RemoveFolderBookmark(folderPath);
        _settingsService.RemoveRecentScanPath(folderPath);
        
        _logger.LogInfo($"Revoked access to: {folderPath}");
    }

    /// <summary>
    /// Start accessing a bookmarked folder (macOS only).
    /// Must call before accessing files in a bookmarked folder.
    /// </summary>
    public bool StartAccessingSecurityScopedResource(string folderPath)
    {
        // Non-macOS: always succeeds
        if (!IsMacOS) return true;

        // Check if already accessing
        if (_activeAccessFolders.ContainsKey(folderPath))
        {
            return true;
        }

        // Try to restore from bookmark
        var bookmarkId = _settingsService.GetFolderBookmark(folderPath);
        if (string.IsNullOrEmpty(bookmarkId))
        {
            _logger.LogWarning($"No bookmark found for: {folderPath}");
            return false;
        }

        // Bookmark restoration is async - caller should use GetGrantedFoldersAsync first
        // to ensure bookmarks are restored
        return _activeAccessFolders.ContainsKey(folderPath);
    }

    /// <summary>
    /// Stop accessing a bookmarked folder (macOS only)
    /// </summary>
    public void StopAccessingSecurityScopedResource(string folderPath)
    {
        if (!IsMacOS) return;

        if (_activeAccessFolders.TryGetValue(folderPath, out var folder))
        {
            folder.Dispose();
            _activeAccessFolders.Remove(folderPath);
            _logger.LogDebug($"Stopped accessing: {folderPath}");
        }
    }

    /// <summary>
    /// Restore access to a folder from a saved bookmark.
    /// Call this before scanning a previously accessed folder.
    /// </summary>
    public async Task<bool> RestoreAccessFromBookmarkAsync(string folderPath)
    {
        if (!IsMacOS)
        {
            // Non-macOS: just check if folder exists
            return Directory.Exists(folderPath);
        }

        var bookmarkId = _settingsService.GetFolderBookmark(folderPath);
        if (string.IsNullOrEmpty(bookmarkId))
        {
            _logger.LogDebug($"No bookmark for: {folderPath}");
            return false;
        }

        var storageProvider = GetStorageProvider();
        if (storageProvider == null) return false;

        try
        {
            var folder = await storageProvider.OpenFolderBookmarkAsync(bookmarkId);
            if (folder != null)
            {
                _activeAccessFolders[folderPath] = folder;
                _logger.LogInfo($"Restored access from bookmark: {folderPath}");
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to restore bookmark: {ex.Message}", ex);
            // Bookmark may have become invalid - remove it
            _settingsService.RemoveFolderBookmark(folderPath);
        }

        return false;
    }

    #region Helper Methods

    private IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.StorageProvider;
        }
        return null;
    }

    private string GetDisplayName(string path)
    {
        if (path == "/" || path == "\\")
            return IsMacOS ? "Macintosh HD" : "Root";
        
        if (path.StartsWith("/Volumes/"))
            return Path.GetFileName(path);
        
        // Windows drive letter
        if (path.Length >= 2 && path[1] == ':')
        {
            try
            {
                var driveInfo = new DriveInfo(path.Substring(0, 2));
                return !string.IsNullOrEmpty(driveInfo.VolumeLabel) 
                    ? driveInfo.VolumeLabel 
                    : driveInfo.Name;
            }
            catch { }
        }

        return Path.GetFileName(path) ?? path;
    }

    private long GetDriveUsedSpace(string path)
    {
        try
        {
            var root = Path.GetPathRoot(path);
            if (!string.IsNullOrEmpty(root))
            {
                var driveInfo = new DriveInfo(root);
                return driveInfo.TotalSize - driveInfo.AvailableFreeSpace;
            }
        }
        catch { }
        return 0;
    }

    #endregion
}
