namespace WinTrim.Core.Services;

/// <summary>
/// Service for analyzing Time Machine backups on macOS.
/// Helps users understand backup storage and find large files consuming backup space.
/// </summary>
public interface ITimeMachineAnalyzer
{
    /// <summary>
    /// Check if Time Machine backups are available on this system
    /// </summary>
    bool IsTimeMachineAvailable { get; }

    /// <summary>
    /// Get all Time Machine backup destinations
    /// </summary>
    Task<IReadOnlyList<TimeMachineDestination>> GetBackupDestinationsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get all backup snapshots for a destination
    /// </summary>
    Task<IReadOnlyList<TimeMachineSnapshot>> GetSnapshotsAsync(
        TimeMachineDestination destination,
        CancellationToken ct = default);

    /// <summary>
    /// Analyze largest files in Time Machine backups
    /// </summary>
    Task<TimeMachineAnalysis> AnalyzeBackupsAsync(
        TimeMachineDestination destination,
        IProgress<TimeMachineProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get files that are excluded from backup (and could be re-included)
    /// </summary>
    Task<IReadOnlyList<string>> GetExcludedPathsAsync(CancellationToken ct = default);

    /// <summary>
    /// Add a path to Time Machine exclusions
    /// </summary>
    Task<bool> ExcludePathAsync(string path, CancellationToken ct = default);
}

/// <summary>
/// A Time Machine backup destination (external drive, network drive, etc.)
/// </summary>
public class TimeMachineDestination
{
    /// <summary>
    /// Display name of the backup destination
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Mount path (e.g., /Volumes/TimeMachine)
    /// </summary>
    public string MountPath { get; init; } = string.Empty;

    /// <summary>
    /// Path to the Backups.backupdb folder
    /// </summary>
    public string BackupDbPath { get; init; } = string.Empty;

    /// <summary>
    /// Total size of the backup destination
    /// </summary>
    public long TotalSize { get; init; }

    /// <summary>
    /// Used space
    /// </summary>
    public long UsedSpace { get; init; }

    /// <summary>
    /// Free space
    /// </summary>
    public long FreeSpace => TotalSize - UsedSpace;

    /// <summary>
    /// Whether this is a local or network destination
    /// </summary>
    public bool IsNetworkDestination { get; init; }

    /// <summary>
    /// Whether the destination is currently available/mounted
    /// </summary>
    public bool IsAvailable { get; init; }
}

/// <summary>
/// A single Time Machine backup snapshot
/// </summary>
public class TimeMachineSnapshot
{
    /// <summary>
    /// Date and time of the backup
    /// </summary>
    public DateTime BackupDate { get; init; }

    /// <summary>
    /// Path to the snapshot
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Size of this snapshot (incremental)
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// Whether this is the latest backup
    /// </summary>
    public bool IsLatest { get; init; }

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string DisplayName => BackupDate.ToString("yyyy-MM-dd HH:mm");
}

/// <summary>
/// Results of analyzing Time Machine backups
/// </summary>
public class TimeMachineAnalysis
{
    /// <summary>
    /// Total backup size across all snapshots
    /// </summary>
    public long TotalBackupSize { get; set; }

    /// <summary>
    /// Number of backup snapshots
    /// </summary>
    public int SnapshotCount { get; set; }

    /// <summary>
    /// Oldest backup date
    /// </summary>
    public DateTime? OldestBackup { get; set; }

    /// <summary>
    /// Newest backup date
    /// </summary>
    public DateTime? NewestBackup { get; set; }

    /// <summary>
    /// Files consuming the most backup space
    /// </summary>
    public List<TimeMachineLargeFile> LargestFiles { get; set; } = new();

    /// <summary>
    /// Folders consuming the most backup space
    /// </summary>
    public List<TimeMachineLargeFolder> LargestFolders { get; set; } = new();

    /// <summary>
    /// Suggestions for reducing backup size
    /// </summary>
    public List<TimeMachineSuggestion> Suggestions { get; set; } = new();
}

/// <summary>
/// A large file in Time Machine backups
/// </summary>
public class TimeMachineLargeFile
{
    /// <summary>
    /// Relative path from user home
    /// </summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>
    /// Full current path (if it still exists)
    /// </summary>
    public string? CurrentPath { get; init; }

    /// <summary>
    /// Size of the file
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// Number of backup versions of this file
    /// </summary>
    public int VersionCount { get; init; }

    /// <summary>
    /// Total space used by all versions
    /// </summary>
    public long TotalSpaceUsed { get; init; }

    /// <summary>
    /// File extension
    /// </summary>
    public string Extension => Path.GetExtension(RelativePath).ToLowerInvariant();

    /// <summary>
    /// Whether the file still exists in the current system
    /// </summary>
    public bool StillExists => CurrentPath != null && File.Exists(CurrentPath);
}

/// <summary>
/// A large folder in Time Machine backups
/// </summary>
public class TimeMachineLargeFolder
{
    /// <summary>
    /// Relative path from user home
    /// </summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>
    /// Full current path
    /// </summary>
    public string? CurrentPath { get; init; }

    /// <summary>
    /// Total size of the folder in backups
    /// </summary>
    public long TotalSize { get; init; }

    /// <summary>
    /// Number of files in this folder
    /// </summary>
    public int FileCount { get; init; }

    /// <summary>
    /// Suggested action (exclude, archive, etc.)
    /// </summary>
    public string? SuggestedAction { get; init; }

    /// <summary>
    /// Folder name
    /// </summary>
    public string Name => Path.GetFileName(RelativePath.TrimEnd('/'));
}

/// <summary>
/// A suggestion for reducing Time Machine backup size
/// </summary>
public class TimeMachineSuggestion
{
    /// <summary>
    /// Title of the suggestion
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Detailed description
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Estimated space savings
    /// </summary>
    public long EstimatedSavings { get; init; }

    /// <summary>
    /// Path to exclude (if applicable)
    /// </summary>
    public string? PathToExclude { get; init; }

    /// <summary>
    /// Category of suggestion
    /// </summary>
    public TimeMachineSuggestionCategory Category { get; init; }

    /// <summary>
    /// Risk level of following this suggestion
    /// </summary>
    public SuggestionRisk Risk { get; init; }
}

/// <summary>
/// Categories of Time Machine suggestions
/// </summary>
public enum TimeMachineSuggestionCategory
{
    /// <summary>
    /// Large media files (videos, etc.)
    /// </summary>
    LargeMedia,

    /// <summary>
    /// Developer caches and build artifacts
    /// </summary>
    DeveloperArtifacts,

    /// <summary>
    /// Application caches
    /// </summary>
    ApplicationCaches,

    /// <summary>
    /// Virtual machine images
    /// </summary>
    VirtualMachines,

    /// <summary>
    /// System or library files
    /// </summary>
    SystemFiles,

    /// <summary>
    /// Other suggestions
    /// </summary>
    Other
}

/// <summary>
/// Risk level for suggestions
/// </summary>
public enum SuggestionRisk
{
    /// <summary>
    /// Safe to exclude - easily regenerated
    /// </summary>
    Low,

    /// <summary>
    /// Some risk - data can be recovered but may take time
    /// </summary>
    Medium,

    /// <summary>
    /// High risk - data may not be recoverable
    /// </summary>
    High
}

/// <summary>
/// Progress information for Time Machine analysis
/// </summary>
public class TimeMachineProgress
{
    /// <summary>
    /// Current phase
    /// </summary>
    public string Phase { get; init; } = string.Empty;

    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public double ProgressPercent { get; init; }

    /// <summary>
    /// Files analyzed so far
    /// </summary>
    public int FilesAnalyzed { get; init; }
}
