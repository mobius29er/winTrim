using WinTrim.Core.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinTrim.Core.Services;

/// <summary>
/// Service for finding duplicate files in a directory tree.
/// Uses size-first, hash-second algorithm for optimal performance.
/// </summary>
public interface IDuplicateScanner
{
    /// <summary>
    /// Scan for duplicate files starting from the given root path.
    /// </summary>
    /// <param name="rootPath">The root directory to scan</param>
    /// <param name="minFileSize">Minimum file size in bytes to consider (default 1KB)</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Groups of duplicate files</returns>
    Task<IReadOnlyList<DuplicateGroup>> ScanForDuplicatesAsync(
        string rootPath,
        long minFileSize = 1024,
        IProgress<DuplicateScanProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scan for duplicate files within an existing scan result.
    /// </summary>
    /// <param name="scanResult">Previously scanned result</param>
    /// <param name="minFileSize">Minimum file size in bytes to consider</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Groups of duplicate files</returns>
    Task<IReadOnlyList<DuplicateGroup>> ScanForDuplicatesAsync(
        ScanResult scanResult,
        long minFileSize = 1024,
        IProgress<DuplicateScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A group of duplicate files (same content, different locations)
/// </summary>
public class DuplicateGroup
{
    /// <summary>
    /// The hash of the file content (xxHash64)
    /// </summary>
    public string Hash { get; init; } = string.Empty;

    /// <summary>
    /// Size of each file in bytes
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    /// All files in this duplicate group
    /// </summary>
    public List<DuplicateFile> Files { get; init; } = new();

    /// <summary>
    /// Total wasted space (size × (count - 1))
    /// </summary>
    public long WastedSpace => FileSize * (Files.Count - 1);

    /// <summary>
    /// Number of duplicates (excluding the original)
    /// </summary>
    public int DuplicateCount => Files.Count - 1;

    /// <summary>
    /// The original file (the one to keep)
    /// </summary>
    public DuplicateFile? OriginalFile => Files.FirstOrDefault(f => f.IsOriginal);

    /// <summary>
    /// Whether all duplicates (non-originals) are selected for deletion
    /// </summary>
    public bool AllDuplicatesSelected
    {
        get => Files.Where(f => !f.IsOriginal).All(f => f.IsMarkedForDeletion);
        set
        {
            foreach (var file in Files.Where(f => !f.IsOriginal))
            {
                file.IsMarkedForDeletion = value;
            }
        }
    }
}

/// <summary>
/// A single file within a duplicate group
/// </summary>
public class DuplicateFile : INotifyPropertyChanged
{
    private bool _isMarkedForDeletion;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Full path to the file
    /// </summary>
    public string FullPath { get; init; } = string.Empty;

    /// <summary>
    /// File name only
    /// </summary>
    public string Name => Path.GetFileName(FullPath);

    /// <summary>
    /// Parent directory path
    /// </summary>
    public string Directory => Path.GetDirectoryName(FullPath) ?? string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime LastModified { get; init; }

    /// <summary>
    /// Whether this file is marked for deletion
    /// </summary>
    public bool IsMarkedForDeletion
    {
        get => _isMarkedForDeletion;
        set
        {
            if (_isMarkedForDeletion != value)
            {
                _isMarkedForDeletion = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Whether this is the "keep" file (typically the oldest or in a preferred location)
    /// </summary>
    public bool IsOriginal { get; set; }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Progress information for duplicate scanning
/// </summary>
public class DuplicateScanProgress
{
    /// <summary>
    /// Current phase of the scan
    /// </summary>
    public DuplicateScanPhase Phase { get; init; }

    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Files processed so far
    /// </summary>
    public int FilesProcessed { get; init; }

    /// <summary>
    /// Total files to process
    /// </summary>
    public int TotalFiles { get; init; }

    /// <summary>
    /// Progress percentage (0-100) - calculated from FilesProcessed/TotalFiles
    /// </summary>
    public double ProgressPercent => TotalFiles > 0 ? (double)FilesProcessed / TotalFiles * 100 : 0;

    /// <summary>
    /// Number of duplicate groups found so far
    /// </summary>
    public int DuplicateGroupsFound { get; init; }

    /// <summary>
    /// Total wasted space found so far
    /// </summary>
    public long WastedSpaceFound { get; init; }
}

/// <summary>
/// Phases of the duplicate scanning process
/// </summary>
public enum DuplicateScanPhase
{
    /// <summary>
    /// Grouping files by size
    /// </summary>
    GroupingBySize,

    /// <summary>
    /// Computing hashes for potential duplicates
    /// </summary>
    ComputingHashes,

    /// <summary>
    /// Finalizing results
    /// </summary>
    Finalizing,

    /// <summary>
    /// Scan complete
    /// </summary>
    Complete
}
