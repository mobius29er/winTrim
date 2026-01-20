using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// Interface for detecting developer tool caches and cleanable items.
/// Platform-specific implementations provide actual detection logic.
/// </summary>
public interface IDevToolDetector
{
    /// <summary>
    /// Scans for all developer tool caches and returns cleanup items.
    /// </summary>
    Task<List<CleanupItem>> ScanAllAsync();
    
    /// <summary>
    /// Returns cleanup suggestions in unified format for UI integration.
    /// </summary>
    Task<List<CleanupSuggestion>> GetSuggestionsAsync();
    
    /// <summary>
    /// Legacy method - redirects to ScanAllAsync
    /// </summary>
    Task<List<CleanupItem>> DetectDevToolsAsync(CancellationToken cancellationToken = default);
}
