using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// Interface for detecting developer tool caches and cleanable items
/// </summary>
public interface IDevToolDetector
{
    /// <summary>
    /// Detects all developer tool caches and returns cleanup items
    /// </summary>
    Task<List<CleanupItem>> DetectDevToolsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets Android emulator/AVD locations
    /// </summary>
    IEnumerable<CleanupItem> GetAndroidEmulators();
    
    /// <summary>
    /// Gets package manager caches (npm, NuGet, pip, cargo, etc.)
    /// </summary>
    IEnumerable<CleanupItem> GetPackageManagerCaches();
    
    /// <summary>
    /// Gets IDE caches (VS Code, JetBrains, etc.)
    /// </summary>
    IEnumerable<CleanupItem> GetIdeCaches();
    
    /// <summary>
    /// Gets Docker-related cleanable items
    /// </summary>
    IEnumerable<CleanupItem> GetDockerItems();
    
    /// <summary>
    /// Gets old node_modules folders from projects
    /// </summary>
    Task<IEnumerable<CleanupItem>> GetNodeModulesAsync(string searchPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets Visual Studio bin/obj folders
    /// </summary>
    Task<IEnumerable<CleanupItem>> GetVsBuildFoldersAsync(string searchPath, CancellationToken cancellationToken = default);
}
