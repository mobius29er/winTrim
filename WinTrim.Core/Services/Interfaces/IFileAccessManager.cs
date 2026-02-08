using System.Collections.Generic;
using System.Threading.Tasks;

namespace WinTrim.Core.Services;

/// <summary>
/// Cross-platform file access management for handling sandbox restrictions.
/// - macOS: Uses security-scoped bookmarks for persistent folder access
/// - Windows/Linux: Simple path storage (no sandbox restrictions)
/// </summary>
public interface IFileAccessManager
{
    /// <summary>
    /// Request access to a folder via system folder picker.
    /// On macOS: Creates and saves a security-scoped bookmark.
    /// On Windows/Linux: Simply returns the selected path.
    /// </summary>
    /// <returns>The selected folder path, or null if cancelled</returns>
    Task<string?> RequestFolderAccessAsync();

    /// <summary>
    /// Get list of folders the user has granted access to.
    /// On macOS: Returns folders with valid security-scoped bookmarks.
    /// On Windows/Linux: Returns recently accessed folders.
    /// </summary>
    Task<List<GrantedFolder>> GetGrantedFoldersAsync();

    /// <summary>
    /// Check if the app has access to a specific path.
    /// On macOS: Checks if path is within a bookmarked folder.
    /// On Windows/Linux: Always returns true.
    /// </summary>
    bool HasAccessToPath(string path);

    /// <summary>
    /// Revoke access to a previously granted folder.
    /// On macOS: Removes the security-scoped bookmark.
    /// On Windows/Linux: Removes from recent folders list.
    /// </summary>
    Task RevokeAccessAsync(string folderPath);

    /// <summary>
    /// Start accessing a bookmarked folder (macOS only).
    /// Must be called before accessing files in a bookmarked folder.
    /// On Windows/Linux: No-op.
    /// </summary>
    /// <returns>True if access started successfully</returns>
    bool StartAccessingSecurityScopedResource(string folderPath);

    /// <summary>
    /// Stop accessing a bookmarked folder (macOS only).
    /// Should be called when done accessing files.
    /// On Windows/Linux: No-op.
    /// </summary>
    void StopAccessingSecurityScopedResource(string folderPath);
}

/// <summary>
/// Represents a folder the user has granted access to
/// </summary>
public class GrantedFolder
{
    public required string Path { get; init; }
    public required string DisplayName { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTime GrantedDate { get; init; }
    public required DateTime LastAccessedDate { get; init; }
}
