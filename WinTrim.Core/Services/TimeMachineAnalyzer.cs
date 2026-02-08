using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WinTrim.Core.Services;

/// <summary>
/// Analyzes Time Machine backups on macOS using tmutil and file system operations.
/// </summary>
public class TimeMachineAnalyzer : ITimeMachineAnalyzer
{
    private readonly IAppLogger _logger;
    
    // Common paths that are good candidates for exclusion
    private static readonly Dictionary<string, (TimeMachineSuggestionCategory Category, SuggestionRisk Risk, string Reason)> ExclusionCandidates = new()
    {
        // Developer artifacts (safe to exclude)
        { "Library/Developer/Xcode/DerivedData", (TimeMachineSuggestionCategory.DeveloperArtifacts, SuggestionRisk.Low, "Xcode build artifacts - easily regenerated") },
        { "Library/Developer/Xcode/iOS DeviceSupport", (TimeMachineSuggestionCategory.DeveloperArtifacts, SuggestionRisk.Low, "iOS device symbols - re-downloaded when needed") },
        { "Library/Caches", (TimeMachineSuggestionCategory.ApplicationCaches, SuggestionRisk.Low, "Application caches - auto-regenerated") },
        { ".npm", (TimeMachineSuggestionCategory.DeveloperArtifacts, SuggestionRisk.Low, "NPM cache - easily re-downloaded") },
        { ".nuget", (TimeMachineSuggestionCategory.DeveloperArtifacts, SuggestionRisk.Low, "NuGet cache - easily re-downloaded") },
        { "go/pkg", (TimeMachineSuggestionCategory.DeveloperArtifacts, SuggestionRisk.Low, "Go package cache - easily re-downloaded") },
        { ".cargo", (TimeMachineSuggestionCategory.DeveloperArtifacts, SuggestionRisk.Low, "Rust/Cargo cache - easily re-downloaded") },
        { ".gradle", (TimeMachineSuggestionCategory.DeveloperArtifacts, SuggestionRisk.Low, "Gradle cache - easily re-downloaded") },
        { ".m2/repository", (TimeMachineSuggestionCategory.DeveloperArtifacts, SuggestionRisk.Low, "Maven cache - easily re-downloaded") },
        
        // Virtual machines
        { "Virtual Machines.localized", (TimeMachineSuggestionCategory.VirtualMachines, SuggestionRisk.Medium, "Virtual machine images - typically large and change frequently") },
        { ".docker", (TimeMachineSuggestionCategory.VirtualMachines, SuggestionRisk.Low, "Docker data - images can be re-pulled") },
        { "Parallels", (TimeMachineSuggestionCategory.VirtualMachines, SuggestionRisk.Medium, "Parallels VMs - consider backing up separately") },
        
        // Large media
        { "Movies", (TimeMachineSuggestionCategory.LargeMedia, SuggestionRisk.High, "Movies folder - consider iCloud or separate backup") },
        { "Music/Music/Media.localized", (TimeMachineSuggestionCategory.LargeMedia, SuggestionRisk.Medium, "Music library - backed up by iCloud if using Apple Music") },
    };

    public TimeMachineAnalyzer(IAppLogger logger)
    {
        _logger = logger;
    }

    public bool IsTimeMachineAvailable => OperatingSystem.IsMacOS();

    public async Task<IReadOnlyList<TimeMachineDestination>> GetBackupDestinationsAsync(CancellationToken ct = default)
    {
        if (!IsTimeMachineAvailable)
            return Array.Empty<TimeMachineDestination>();

        var destinations = new List<TimeMachineDestination>();

        try
        {
            // Use tmutil to get destination info
            var output = await RunTmutilAsync("destinationinfo", ct);
            if (string.IsNullOrEmpty(output))
            {
                _logger.LogInfo("No Time Machine destinations configured");
                return destinations;
            }

            // Parse tmutil output
            // Format for local:
            // ====================================================
            // Name          : MyBackupDrive
            // Kind          : Local
            // Mount Point   : /Volumes/MyBackupDrive
            // ID            : 12345678-...
            //
            // Format for network:
            // ====================================================
            // Name          : Backup_J
            // Kind          : Network
            // URL           : smb://user@server/share
            // ID            : 12345678-...
            
            var lines = output.Split('\n');
            string? currentName = null;
            string? currentMountPoint = null;
            string? currentUrl = null;
            string? currentId = null;
            bool isNetwork = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("Name"))
                {
                    currentName = line.Split(':').LastOrDefault()?.Trim();
                }
                else if (line.StartsWith("Kind"))
                {
                    isNetwork = line.Contains("Network", StringComparison.OrdinalIgnoreCase);
                }
                else if (line.StartsWith("Mount Point"))
                {
                    currentMountPoint = line.Split(':', 2).LastOrDefault()?.Trim();
                }
                else if (line.StartsWith("URL"))
                {
                    currentUrl = line.Split(':', 2).LastOrDefault()?.Trim();
                }
                else if (line.StartsWith("ID"))
                {
                    currentId = line.Split(':').LastOrDefault()?.Trim();
                    
                    // ID is the last field, so process the destination now
                    if (!string.IsNullOrEmpty(currentName))
                    {
                        // For network destinations, find the mount point
                        if (isNetwork && string.IsNullOrEmpty(currentMountPoint))
                        {
                            currentMountPoint = await FindNetworkMountPointAsync(currentName, currentId, ct);
                        }
                        
                        if (!string.IsNullOrEmpty(currentMountPoint))
                        {
                            var backupDbPath = FindBackupDbPath(currentMountPoint);
                            var dest = CreateDestination(currentName, currentMountPoint, backupDbPath, isNetwork);
                            if (dest != null)
                            {
                                destinations.Add(dest);
                            }
                        }
                        else
                        {
                            _logger.LogInfo($"Could not find mount point for Time Machine destination: {currentName}");
                        }
                        
                        // Reset for next destination
                        currentName = null;
                        currentMountPoint = null;
                        currentUrl = null;
                        currentId = null;
                        isNetwork = false;
                    }
                }
            }

            _logger.LogInfo($"Found {destinations.Count} Time Machine destinations");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting Time Machine destinations", ex);
        }

        return destinations;
    }

    public async Task<IReadOnlyList<TimeMachineSnapshot>> GetSnapshotsAsync(
        TimeMachineDestination destination,
        CancellationToken ct = default)
    {
        var snapshots = new List<TimeMachineSnapshot>();

        if (!destination.IsAvailable || string.IsNullOrEmpty(destination.BackupDbPath))
            return snapshots;

        try
        {
            // List snapshots using tmutil
            var output = await RunTmutilAsync($"listbackups -d \"{destination.MountPath}\"", ct);
            if (string.IsNullOrEmpty(output))
                return snapshots;

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                var path = lines[i].Trim();
                if (string.IsNullOrEmpty(path)) continue;

                // Parse date from path (format: 2024-01-15-123456)
                var dirName = Path.GetFileName(path);
                if (DateTime.TryParseExact(dirName, "yyyy-MM-dd-HHmmss", null, 
                    System.Globalization.DateTimeStyles.None, out var backupDate))
                {
                    snapshots.Add(new TimeMachineSnapshot
                    {
                        Path = path,
                        BackupDate = backupDate,
                        IsLatest = i == lines.Length - 1
                    });
                }
            }

            _logger.LogInfo($"Found {snapshots.Count} Time Machine snapshots");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error listing Time Machine snapshots", ex);
        }

        return snapshots.OrderByDescending(s => s.BackupDate).ToList();
    }

    public async Task<TimeMachineAnalysis> AnalyzeBackupsAsync(
        TimeMachineDestination destination,
        IProgress<TimeMachineProgress>? progress = null,
        CancellationToken ct = default)
    {
        var analysis = new TimeMachineAnalysis();
        
        if (!destination.IsAvailable)
        {
            _logger.LogWarning("Time Machine destination not available");
            return analysis;
        }

        try
        {
            progress?.Report(new TimeMachineProgress
            {
                Phase = "Analyzing",
                Message = "Getting backup snapshots...",
                ProgressPercent = 10
            });

            var snapshots = await GetSnapshotsAsync(destination, ct);
            analysis.SnapshotCount = snapshots.Count;
            analysis.OldestBackup = snapshots.LastOrDefault()?.BackupDate;
            analysis.NewestBackup = snapshots.FirstOrDefault()?.BackupDate;

            if (!snapshots.Any())
            {
                return analysis;
            }

            progress?.Report(new TimeMachineProgress
            {
                Phase = "Analyzing",
                Message = "Scanning latest backup for large files...",
                ProgressPercent = 30
            });

            // Analyze the latest backup for large files
            var latestSnapshot = snapshots.First();
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var homeInBackup = Path.Combine(latestSnapshot.Path, "Data", homePath.TrimStart('/'));

            var largeFiles = new List<TimeMachineLargeFile>();
            var folderSizes = new Dictionary<string, (long Size, int FileCount)>();

            if (Directory.Exists(homeInBackup))
            {
                await ScanBackupDirectoryAsync(homeInBackup, homePath, largeFiles, folderSizes, progress, ct);
            }

            progress?.Report(new TimeMachineProgress
            {
                Phase = "Finalizing",
                Message = "Generating suggestions...",
                ProgressPercent = 80
            });

            // Generate suggestions based on found files
            var suggestions = GenerateSuggestions(largeFiles, folderSizes, homePath);

            analysis.LargestFiles = largeFiles.OrderByDescending(f => f.Size).Take(50).ToList();
            analysis.LargestFolders = folderSizes
                .OrderByDescending(kv => kv.Value.Size)
                .Take(20)
                .Select(kv => new TimeMachineLargeFolder
                {
                    RelativePath = kv.Key,
                    CurrentPath = Path.Combine(homePath, kv.Key),
                    TotalSize = kv.Value.Size,
                    FileCount = kv.Value.FileCount
                })
                .ToList();
            analysis.Suggestions = suggestions;
            analysis.TotalBackupSize = destination.UsedSpace;

            progress?.Report(new TimeMachineProgress
            {
                Phase = "Complete",
                Message = $"Analysis complete. Found {analysis.Suggestions.Count} optimization suggestions.",
                ProgressPercent = 100
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Error analyzing Time Machine backups", ex);
        }

        return analysis;
    }

    public async Task<IReadOnlyList<string>> GetExcludedPathsAsync(CancellationToken ct = default)
    {
        if (!IsTimeMachineAvailable)
            return Array.Empty<string>();

        try
        {
            // Get exclusions via defaults command
            var output = await RunCommandAsync("defaults", "read /Library/Preferences/com.apple.TimeMachine SkipPaths", ct);
            if (string.IsNullOrEmpty(output))
                return Array.Empty<string>();

            // Parse the plist array format
            var paths = new List<string>();
            var matches = Regex.Matches(output, @"""([^""]+)""");
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    paths.Add(match.Groups[1].Value);
                }
            }

            return paths;
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Error getting excluded paths: {ex.Message}");
            return Array.Empty<string>();
        }
    }

    public async Task<bool> ExcludePathAsync(string path, CancellationToken ct = default)
    {
        if (!IsTimeMachineAvailable || string.IsNullOrEmpty(path))
            return false;

        try
        {
            // Use tmutil to add exclusion
            var output = await RunTmutilAsync($"addexclusion \"{path}\"", ct);
            _logger.LogInfo($"Added Time Machine exclusion: {path}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to add exclusion for {path}", ex);
            return false;
        }
    }

    #region Private Helpers

    private Task<string?> FindNetworkMountPointAsync(string name, string? id, CancellationToken ct)
    {
        try
        {
            // Network Time Machine destinations are mounted under /Volumes/.timemachine/
            // e.g., /Volumes/.timemachine/odinson._smb._tcp.local/620A2B5A-4C34-40D1-A840-0CE843A7FD57/Backup_J
            var timeMachineVolumesPath = "/Volumes/.timemachine";
            
            if (Directory.Exists(timeMachineVolumesPath))
            {
                foreach (var serverDir in Directory.GetDirectories(timeMachineVolumesPath))
                {
                    foreach (var idDir in Directory.GetDirectories(serverDir))
                    {
                        // Look for the destination by name
                        var destPath = Path.Combine(idDir, name);
                        if (Directory.Exists(destPath))
                        {
                            _logger.LogDebug($"Found network Time Machine mount: {destPath}");
                            return Task.FromResult<string?>(destPath);
                        }
                        
                        // Also check if Backups.backupdb exists directly
                        var backupDbPath = Path.Combine(idDir, "Backups.backupdb");
                        if (Directory.Exists(backupDbPath))
                        {
                            _logger.LogDebug($"Found network Time Machine mount at: {idDir}");
                            return Task.FromResult<string?>(idDir);
                        }
                    }
                }
            }
            
            // Also check for local APFS snapshots at /Volumes/Backups of *
            var volumesPath = "/Volumes";
            if (Directory.Exists(volumesPath))
            {
                foreach (var volDir in Directory.GetDirectories(volumesPath))
                {
                    var volName = Path.GetFileName(volDir);
                    if (volName.StartsWith("Backups of ", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug($"Found local Time Machine volume: {volDir}");
                        return Task.FromResult<string?>(volDir);
                    }
                }
            }
            
            _logger.LogDebug($"Could not find network mount for Time Machine destination: {name}");
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Error finding network mount point: {ex.Message}");
        }
        
        return Task.FromResult<string?>(null);
    }

    private string? FindBackupDbPath(string mountPoint)
    {
        var backupDbPath = Path.Combine(mountPoint, "Backups.backupdb");
        if (!Directory.Exists(backupDbPath))
            return null;

        // Find computer folder within Backups.backupdb
        try
        {
            var computerFolders = Directory.GetDirectories(backupDbPath);
            if (computerFolders.Length > 0)
            {
                // Return the first (usually only) computer folder
                return computerFolders[0];
            }
        }
        catch { }

        return backupDbPath;
    }

    private TimeMachineDestination? CreateDestination(string name, string mountPoint, string? backupDbPath, bool isNetwork)
    {
        try
        {
            var driveInfo = new DriveInfo(mountPoint);
            return new TimeMachineDestination
            {
                Name = name,
                MountPath = mountPoint,
                BackupDbPath = backupDbPath ?? string.Empty,
                TotalSize = driveInfo.IsReady ? driveInfo.TotalSize : 0,
                UsedSpace = driveInfo.IsReady ? driveInfo.TotalSize - driveInfo.AvailableFreeSpace : 0,
                IsNetworkDestination = isNetwork,
                IsAvailable = driveInfo.IsReady && !string.IsNullOrEmpty(backupDbPath)
            };
        }
        catch
        {
            return null;
        }
    }

    private Task ScanBackupDirectoryAsync(
        string backupPath,
        string currentHomePath,
        List<TimeMachineLargeFile> largeFiles,
        Dictionary<string, (long Size, int FileCount)> folderSizes,
        IProgress<TimeMachineProgress>? progress,
        CancellationToken ct)
    {
        return Task.Run(() =>
        {
            const long MinFileSize = 10 * 1024 * 1024; // 10 MB minimum
            var filesScanned = 0;

            try
            {
                var queue = new Queue<string>();
                queue.Enqueue(backupPath);

            while (queue.Count > 0 && !ct.IsCancellationRequested)
            {
                var currentDir = queue.Dequeue();
                
                try
                {
                    // Process files
                    foreach (var file in Directory.EnumerateFiles(currentDir))
                    {
                        ct.ThrowIfCancellationRequested();
                        
                        try
                        {
                            var fi = new FileInfo(file);
                            if (fi.Length >= MinFileSize)
                            {
                                var relativePath = file.Substring(backupPath.Length).TrimStart('/');
                                var currentPath = Path.Combine(currentHomePath, relativePath);

                                largeFiles.Add(new TimeMachineLargeFile
                                {
                                    RelativePath = relativePath,
                                    CurrentPath = File.Exists(currentPath) ? currentPath : null,
                                    Size = fi.Length,
                                    VersionCount = 1,
                                    TotalSpaceUsed = fi.Length
                                });
                            }

                            // Track folder sizes
                            var folderRelPath = Path.GetDirectoryName(file.Substring(backupPath.Length).TrimStart('/')) ?? "";
                            if (!string.IsNullOrEmpty(folderRelPath))
                            {
                                // Aggregate to top 2 levels
                                var parts = folderRelPath.Split('/');
                                var topLevel = parts.Length > 1 
                                    ? string.Join("/", parts.Take(2)) 
                                    : parts[0];
                                
                                if (!folderSizes.ContainsKey(topLevel))
                                    folderSizes[topLevel] = (0, 0);
                                
                                var current = folderSizes[topLevel];
                                folderSizes[topLevel] = (current.Size + fi.Length, current.FileCount + 1);
                            }

                            filesScanned++;
                            if (filesScanned % 500 == 0)
                            {
                                progress?.Report(new TimeMachineProgress
                                {
                                    Phase = "Scanning",
                                    Message = $"Scanned {filesScanned:N0} files...",
                                    ProgressPercent = 30 + (filesScanned % 5000) / 100.0,
                                    FilesAnalyzed = filesScanned
                                });
                            }
                        }
                        catch { /* Skip files we can't access */ }
                    }

                    // Queue subdirectories
                    foreach (var dir in Directory.EnumerateDirectories(currentDir))
                    {
                        queue.Enqueue(dir);
                    }
                }
                catch { /* Skip directories we can't access */ }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Error scanning backup directory: {ex.Message}");
        }
        }, ct);
    }

    private List<TimeMachineSuggestion> GenerateSuggestions(
        List<TimeMachineLargeFile> largeFiles,
        Dictionary<string, (long Size, int FileCount)> folderSizes,
        string homePath)
    {
        var suggestions = new List<TimeMachineSuggestion>();

        // Check for known exclusion candidates
        foreach (var (relativePath, (category, risk, reason)) in ExclusionCandidates)
        {
            if (folderSizes.TryGetValue(relativePath, out var stats) && stats.Size > 100 * 1024 * 1024) // 100 MB
            {
                suggestions.Add(new TimeMachineSuggestion
                {
                    Title = $"Exclude {Path.GetFileName(relativePath.TrimEnd('/'))}",
                    Description = reason,
                    EstimatedSavings = stats.Size,
                    PathToExclude = Path.Combine(homePath, relativePath),
                    Category = category,
                    Risk = risk
                });
            }
        }

        // Check for large VM files
        var vmFiles = largeFiles.Where(f => 
            f.Extension is ".vmdk" or ".qcow2" or ".vdi" or ".vmwarevm" or ".pvs" or ".pvm")
            .ToList();
        if (vmFiles.Any())
        {
            var totalSize = vmFiles.Sum(f => f.Size);
            suggestions.Add(new TimeMachineSuggestion
            {
                Title = "Exclude Virtual Machine Images",
                Description = $"Found {vmFiles.Count} VM images. These change frequently and are better backed up separately.",
                EstimatedSavings = totalSize,
                Category = TimeMachineSuggestionCategory.VirtualMachines,
                Risk = SuggestionRisk.Medium
            });
        }

        // Check for node_modules (very common)
        var nodeModules = folderSizes
            .Where(kv => kv.Key.Contains("node_modules"))
            .Sum(kv => kv.Value.Size);
        if (nodeModules > 500 * 1024 * 1024) // 500 MB
        {
            suggestions.Add(new TimeMachineSuggestion
            {
                Title = "Exclude node_modules Folders",
                Description = "Node.js dependencies can be reinstalled with 'npm install'.",
                EstimatedSavings = nodeModules,
                Category = TimeMachineSuggestionCategory.DeveloperArtifacts,
                Risk = SuggestionRisk.Low
            });
        }

        return suggestions.OrderByDescending(s => s.EstimatedSavings).ToList();
    }

    private async Task<string> RunTmutilAsync(string args, CancellationToken ct)
    {
        return await RunCommandAsync("tmutil", args, ct);
    }

    private async Task<string> RunCommandAsync(string command, string args, CancellationToken ct)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);
            
            return output;
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Error running {command}: {ex.Message}");
            return string.Empty;
        }
    }

    #endregion
}
