using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Hashing;
using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// Finds duplicate files using size-first, hash-second algorithm with xxHash64.
/// </summary>
public class DuplicateScanner : IDuplicateScanner
{
    private readonly IAppLogger _logger;
    
    // Optimal buffer size for file I/O
    private const int BufferSize = 81920; // 80KB
    
    // First few KB for quick hash comparison before full file hash
    private const int QuickHashSize = 4096; // 4KB
    
    // Maximum number of files to process to avoid scanning forever
    private const int MaxFilesToProcess = 50000;
    
    // Default minimum file size (10KB) - skip tiny files
    private const long DefaultMinFileSize = 10240;

    public DuplicateScanner(IAppLogger logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<DuplicateGroup>> ScanForDuplicatesAsync(
        string rootPath,
        long minFileSize = 1024,
        IProgress<DuplicateScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInfo($"Starting duplicate scan from: {rootPath}");

        // Collect all files
        var allFiles = new List<FileInfo>();
        try
        {
            var dirInfo = new DirectoryInfo(rootPath);
            CollectFiles(dirInfo, allFiles, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error collecting files: {ex.Message}", ex);
            return Array.Empty<DuplicateGroup>();
        }

        return await FindDuplicatesInFilesAsync(allFiles, minFileSize, progress, cancellationToken);
    }

    public async Task<IReadOnlyList<DuplicateGroup>> ScanForDuplicatesAsync(
        ScanResult scanResult,
        long minFileSize = 1024,
        IProgress<DuplicateScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInfo($"Starting duplicate scan from scan result");
        
        if (scanResult.RootItem == null)
        {
            _logger.LogWarning("Duplicate scan: RootItem is null, cannot scan");
            progress?.Report(new DuplicateScanProgress
            {
                Phase = DuplicateScanPhase.Complete,
                Message = "No scan data available. Please scan a folder first.",
                FilesProcessed = 0,
                TotalFiles = 0
            });
            return Array.Empty<DuplicateGroup>();
        }

        // Report initial progress immediately
        progress?.Report(new DuplicateScanProgress
        {
            Phase = DuplicateScanPhase.GroupingBySize,
            Message = "Scanning files, please wait...",
            FilesProcessed = 0,
            TotalFiles = 0 // Will show indeterminate state
        });

        // Run file collection on background thread to avoid blocking UI
        var allFiles = await Task.Run(() =>
        {
            var files = new List<FileInfo>();
            CollectFilesFromScanResult(scanResult.RootItem, files);
            _logger.LogInfo($"Duplicate scan: Collected {files.Count} files from scan result");
            return files;
        }, cancellationToken);

        progress?.Report(new DuplicateScanProgress
        {
            Phase = DuplicateScanPhase.GroupingBySize,
            Message = $"Analyzing {allFiles.Count:N0} files for duplicates...",
            FilesProcessed = 0,
            TotalFiles = allFiles.Count
        });

        return await FindDuplicatesInFilesAsync(allFiles, minFileSize, progress, cancellationToken);
    }

    private void CollectFiles(DirectoryInfo dir, List<FileInfo> files, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        try
        {
            // Add files from current directory
            foreach (var file in dir.EnumerateFiles())
            {
                if (ct.IsCancellationRequested) return;
                files.Add(file);
            }

            // Recurse into subdirectories
            foreach (var subDir in dir.EnumerateDirectories())
            {
                if (ct.IsCancellationRequested) return;
                
                // Skip symbolic links to avoid infinite loops
                if ((subDir.Attributes & FileAttributes.ReparsePoint) != 0)
                    continue;

                CollectFiles(subDir, files, ct);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore access denied
        }
        catch (IOException)
        {
            // Ignore I/O errors
        }
    }

    private void CollectFilesFromScanResult(FileSystemItem? item, List<FileInfo> files)
    {
        if (item == null) return;

        if (!item.IsFolder && !string.IsNullOrEmpty(item.FullPath))
        {
            try
            {
                var fi = new FileInfo(item.FullPath);
                if (fi.Exists)
                    files.Add(fi);
            }
            catch { /* Ignore errors */ }
        }

        // Check both Children collection and _children field for ObservableCollection
        var children = item.Children;
        if (children != null && children.Count > 0)
        {
            foreach (var child in children.ToList()) // ToList to avoid collection modified during enumeration
            {
                CollectFilesFromScanResult(child, files);
            }
        }
    }

    private async Task<IReadOnlyList<DuplicateGroup>> FindDuplicatesInFilesAsync(
        List<FileInfo> allFiles,
        long minFileSize,
        IProgress<DuplicateScanProgress>? progress,
        CancellationToken ct)
    {
        // Use larger minimum to filter out tiny files
        var effectiveMinSize = Math.Max(minFileSize, DefaultMinFileSize);
        
        // Phase 1: Group by size (duplicates must have same size)
        progress?.Report(new DuplicateScanProgress
        {
            Phase = DuplicateScanPhase.GroupingBySize,
            Message = $"Analyzing {allFiles.Count:N0} files...",
            FilesProcessed = 0,
            TotalFiles = allFiles.Count
        });

        // Run grouping on background thread - prioritize larger files for more savings
        var sizeGroups = await Task.Run(() =>
        {
            return allFiles
                .Where(f => f.Length >= effectiveMinSize)
                .GroupBy(f => f.Length)
                .Where(g => g.Count() > 1 && g.Count() <= 100) // Skip groups with too many files (likely system files)
                .OrderByDescending(g => g.Key * (g.Count() - 1)) // Prioritize by potential savings
                .Take(500) // Limit to top 500 size groups for performance
                .ToList();
        }, ct);

        var potentialDuplicates = sizeGroups.SelectMany(g => g).ToList();
        
        // Cap total files to process
        if (potentialDuplicates.Count > MaxFilesToProcess)
        {
            _logger.LogInfo($"Limiting from {potentialDuplicates.Count} to {MaxFilesToProcess} files");
            sizeGroups = sizeGroups
                .TakeWhile((_, count) => sizeGroups.Take(count + 1).SelectMany(g => g).Count() <= MaxFilesToProcess)
                .ToList();
            potentialDuplicates = sizeGroups.SelectMany(g => g).ToList();
        }

        _logger.LogInfo($"Processing {potentialDuplicates.Count} files in {sizeGroups.Count} size groups (min size: {effectiveMinSize:N0} bytes)");

        progress?.Report(new DuplicateScanProgress
        {
            Phase = DuplicateScanPhase.ComputingHashes,
            Message = $"Found {potentialDuplicates.Count:N0} candidates in {sizeGroups.Count:N0} groups",
            FilesProcessed = 0,
            TotalFiles = potentialDuplicates.Count
        });

        if (potentialDuplicates.Count == 0)
        {
            progress?.Report(new DuplicateScanProgress
            {
                Phase = DuplicateScanPhase.Complete,
                Message = "No potential duplicates found (files ≥10KB)",
                FilesProcessed = 1,
                TotalFiles = 1
            });
            return Array.Empty<DuplicateGroup>();
        }

        // Phase 2: Two-stage hashing - quick hash first, full hash only for matches
        var processedCount = 0;
        var duplicateGroups = new ConcurrentDictionary<string, ConcurrentBag<FileInfo>>();
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2); // More parallelism

        foreach (var sizeGroup in sizeGroups)
        {
            if (ct.IsCancellationRequested) break;

            var filesInGroup = sizeGroup.ToList();
            var size = sizeGroup.Key;
            
            // For small groups (2-3 files), just hash directly
            if (filesInGroup.Count <= 3)
            {
                var tasks = filesInGroup.Select(async file =>
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        var hash = await ComputeFileHashAsync(file, ct);
                        if (hash != null)
                        {
                            var key = $"{size}:{hash}";
                            duplicateGroups.GetOrAdd(key, _ => new ConcurrentBag<FileInfo>()).Add(file);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                        ReportProgress(ref processedCount, potentialDuplicates.Count, progress);
                    }
                });
                await Task.WhenAll(tasks);
            }
            else
            {
                // For larger groups, use quick hash first
                var quickHashGroups = new ConcurrentDictionary<string, ConcurrentBag<FileInfo>>();
                
                var quickTasks = filesInGroup.Select(async file =>
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        var quickHash = await ComputeQuickHashAsync(file, ct);
                        if (quickHash != null)
                        {
                            quickHashGroups.GetOrAdd(quickHash, _ => new ConcurrentBag<FileInfo>()).Add(file);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                await Task.WhenAll(quickTasks);

                // Only full-hash files with matching quick hashes
                foreach (var quickGroup in quickHashGroups.Values.Where(g => g.Count > 1))
                {
                    var fullTasks = quickGroup.Select(async file =>
                    {
                        await semaphore.WaitAsync(ct);
                        try
                        {
                            var hash = await ComputeFileHashAsync(file, ct);
                            if (hash != null)
                            {
                                var key = $"{size}:{hash}";
                                duplicateGroups.GetOrAdd(key, _ => new ConcurrentBag<FileInfo>()).Add(file);
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                            ReportProgress(ref processedCount, potentialDuplicates.Count, progress);
                        }
                    });
                    await Task.WhenAll(fullTasks);
                }
                
                // Count skipped files as processed
                var skipped = quickHashGroups.Values.Where(g => g.Count == 1).Sum(g => g.Count);
                Interlocked.Add(ref processedCount, skipped);
                ReportProgress(ref processedCount, potentialDuplicates.Count, progress);
            }
        }

        // Phase 3: Build final duplicate groups
        progress?.Report(new DuplicateScanProgress
        {
            Phase = DuplicateScanPhase.Finalizing,
            Message = "Building results...",
            FilesProcessed = potentialDuplicates.Count,
            TotalFiles = potentialDuplicates.Count
        });

        var result = await Task.Run(() => duplicateGroups
            .Where(kvp => kvp.Value.Count > 1)
            .Select(kvp =>
            {
                var parts = kvp.Key.Split(':');
                var size = long.Parse(parts[0]);
                var hash = parts[1];
                var files = kvp.Value
                    .OrderBy(f => f.LastWriteTime)
                    .Select((f, i) => new DuplicateFile
                    {
                        FullPath = f.FullName,
                        Size = f.Length,
                        LastModified = f.LastWriteTime,
                        IsOriginal = i == 0
                    })
                    .ToList();

                return new DuplicateGroup
                {
                    Hash = hash,
                    FileSize = size,
                    Files = files
                };
            })
            .OrderByDescending(g => g.WastedSpace)
            .ToList(), ct);

        var totalWasted = result.Sum(g => g.WastedSpace);
        _logger.LogInfo($"Found {result.Count} duplicate groups, {result.Sum(g => g.DuplicateCount)} duplicates, {totalWasted:N0} bytes wasted");

        progress?.Report(new DuplicateScanProgress
        {
            Phase = DuplicateScanPhase.Complete,
            Message = $"Found {result.Count} duplicate groups",
            FilesProcessed = potentialDuplicates.Count,
            TotalFiles = potentialDuplicates.Count,
            DuplicateGroupsFound = result.Count,
            WastedSpaceFound = totalWasted
        });

        return result;
    }

    private void ReportProgress(ref int processedCount, int total, IProgress<DuplicateScanProgress>? progress)
    {
        var count = Interlocked.Increment(ref processedCount);
        if (count % 25 == 0 || count == total)
        {
            progress?.Report(new DuplicateScanProgress
            {
                Phase = DuplicateScanPhase.ComputingHashes,
                Message = $"Comparing files... {count:N0} / {total:N0}",
                FilesProcessed = count,
                TotalFiles = total
            });
        }
    }

    private async Task<string?> ComputeQuickHashAsync(FileInfo file, CancellationToken ct)
    {
        try
        {
            var buffer = new byte[QuickHashSize];
            await using var stream = new FileStream(
                file.FullName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                QuickHashSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, QuickHashSize), ct);
            if (bytesRead == 0) return null;

            var hasher = new XxHash64();
            hasher.Append(buffer.AsSpan(0, bytesRead));
            return Convert.ToHexString(hasher.GetCurrentHash());
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> ComputeFileHashAsync(FileInfo file, CancellationToken ct)
    {
        try
        {
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                var hasher = new XxHash64();
                
                await using var stream = new FileStream(
                    file.FullName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    BufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);

                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, BufferSize), ct)) > 0)
                {
                    hasher.Append(buffer.AsSpan(0, bytesRead));
                }

                return Convert.ToHexString(hasher.GetCurrentHash());
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // File access issues - skip this file
            return null;
        }
    }
}
