using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// High-performance file scanner with async enumeration and pause/resume support.
/// Cross-platform implementation for WinTrim.Core.
/// </summary>
public sealed class FileScanner : IFileScanner
{
    private readonly IGameDetector _gameDetector;
    private readonly ICleanupAdvisor _cleanupAdvisor;
    private readonly ICategoryClassifier _categoryClassifier;
    private readonly IDevToolDetector _devToolDetector;
    
    // Thread-safe collections for parallel processing results
    private readonly ConcurrentBag<FileSystemItem> _largestFiles = new();
    private readonly ConcurrentDictionary<ItemCategory, CategoryStats> _categoryStats = new();
    
    // Pause mechanism using ManualResetEventSlim (more efficient than ManualResetEvent)
    private readonly ManualResetEventSlim _pauseEvent = new(true); // Initially signaled (not paused)
    private volatile bool _isPaused;
    
    // Performance tuning
    private const int LargestFilesLimit = 100;
    private const int MaxParallelism = 4; // Limit parallel directory scans to prevent disk thrashing
    private static readonly EnumerationOptions FastEnumerationOptions = new()
    {
        IgnoreInaccessible = true,
        RecurseSubdirectories = false, // We handle recursion manually for better control
        AttributesToSkip = FileAttributes.ReparsePoint // Skip symlinks/junctions
    };

    public bool IsPaused => _isPaused;

    public FileScanner(
        IGameDetector gameDetector, 
        ICleanupAdvisor cleanupAdvisor, 
        ICategoryClassifier categoryClassifier,
        IDevToolDetector devToolDetector)
    {
        _gameDetector = gameDetector;
        _cleanupAdvisor = cleanupAdvisor;
        _categoryClassifier = categoryClassifier;
        _devToolDetector = devToolDetector;
    }
    
    /// <summary>
    /// Pauses the current scan. Scanner will stop at next checkpoint and wait.
    /// </summary>
    public void Pause()
    {
        _isPaused = true;
        _pauseEvent.Reset(); // Block threads waiting on this event
    }
    
    /// <summary>
    /// Resumes a paused scan.
    /// </summary>
    public void Resume()
    {
        _isPaused = false;
        _pauseEvent.Set(); // Allow threads to continue
    }

    public async Task<ScanResult> ScanAsync(string path, IProgress<ScanProgress> progress, CancellationToken cancellationToken)
    {
        // Clear previous state
        _largestFiles.Clear();
        _categoryStats.Clear();
        _isPaused = false;
        _pauseEvent.Set();
        
        var result = new ScanResult
        {
            RootPath = path,
            ScanStarted = DateTime.Now
        };

        var scanProgress = new ScanProgress
        {
            State = ScanState.Scanning,
            StatusMessage = "Starting scan..."
        };

        try
        {
            // Create root item
            var rootInfo = new DirectoryInfo(path);
            if (!rootInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Directory not found: {path}");
            }

            var rootItem = new FileSystemItem
            {
                Name = rootInfo.Name,
                FullPath = rootInfo.FullName,
                IsFolder = true,
                Created = rootInfo.CreationTime,
                LastModified = rootInfo.LastWriteTime,
                LastAccessed = rootInfo.LastAccessTime
            };

            result.RootItem = rootItem;

            // Scan recursively
            await ScanDirectoryAsync(rootItem, scanProgress, progress, cancellationToken);
            Console.WriteLine($"[FileScanner] Recursive scan complete. Total files in bag: {_largestFiles.Count}");

            // Calculate percentages and finalize
            CalculatePercentages(rootItem);

            // Get largest files (sorted)
            result.LargestFiles = _largestFiles
                .OrderByDescending(f => f.Size)
                .Take(LargestFilesLimit)
                .ToList();
            Console.WriteLine($"[FileScanner] LargestFiles sorted and limited: {result.LargestFiles.Count}");
            if (result.LargestFiles.Count > 0)
            {
                Console.WriteLine($"[FileScanner] Top file: {result.LargestFiles[0].Name} ({result.LargestFiles[0].Size} bytes)");
            }

            // Get largest folders
            result.LargestFolders = GetLargestFolders(rootItem, 50);
            Console.WriteLine($"[FileScanner] LargestFolders: {result.LargestFolders.Count}");

            // Get oldest accessed files
            result.OldestAccessedFiles = _largestFiles
                .Where(f => f.DaysSinceAccessed > 30)
                .OrderByDescending(f => f.DaysSinceAccessed)
                .Take(50)
                .ToList();

            // Detect games
            Console.WriteLine($"[FileScanner] Starting game detection...");
            result.GameInstallations = await _gameDetector.DetectGamesAsync(path, cancellationToken);
            Console.WriteLine($"[FileScanner] Games found: {result.GameInstallations.Count}");

            // Scan for developer tools (parallel with cleanup suggestions)
            Console.WriteLine($"[FileScanner] Starting dev tools detection...");
            var devToolTask = _devToolDetector.ScanAllAsync();

            // Get cleanup suggestions
            result.CleanupSuggestions = await _cleanupAdvisor.GetSuggestionsAsync(rootItem, cancellationToken);

            // Wait for dev tools scan
            result.DevTools = (await devToolTask).OrderByDescending(d => d.SizeBytes).ToList();
            Console.WriteLine($"[FileScanner] DevTools found: {result.DevTools.Count}");

            // Category breakdown
            result.CategoryBreakdown = new Dictionary<ItemCategory, CategoryStats>(_categoryStats);

            result.TotalSize = rootItem.Size;
            result.TotalFiles = scanProgress.FilesScanned;
            result.TotalFolders = scanProgress.FoldersScanned;
            result.ErrorCount = scanProgress.ErrorCount;
            result.ScanCompleted = DateTime.Now;

            Console.WriteLine($"[FileScanner] Scan completed. Files: {result.TotalFiles}, Folders: {result.TotalFolders}");

            scanProgress.State = ScanState.Completed;
            scanProgress.StatusMessage = "Scan completed";
            scanProgress.ProgressPercentage = 100;
            progress.Report(scanProgress);
        }
        catch (OperationCanceledException)
        {
            // Return partial results on cancellation - don't discard the work done!
            if (result.RootItem != null)
            {
                CalculatePercentages(result.RootItem);
                
                result.LargestFiles = _largestFiles
                    .OrderByDescending(f => f.Size)
                    .Take(LargestFilesLimit)
                    .ToList();
                
                result.LargestFolders = GetLargestFolders(result.RootItem, 50);
                result.CategoryBreakdown = new Dictionary<ItemCategory, CategoryStats>(_categoryStats);
                result.TotalSize = result.RootItem.Size;
                result.TotalFiles = scanProgress.FilesScanned;
                result.TotalFolders = scanProgress.FoldersScanned;
                result.ErrorCount = scanProgress.ErrorCount;
            }
            
            scanProgress.State = ScanState.Cancelled;
            scanProgress.StatusMessage = "Scan cancelled - showing partial results";
            progress.Report(scanProgress);
            
            result.ScanCompleted = DateTime.Now;
            result.WasCancelled = true;
        }
        catch (Exception ex)
        {
            scanProgress.State = ScanState.Error;
            scanProgress.StatusMessage = $"Error: {ex.Message}";
            progress.Report(scanProgress);
            throw;
        }

        return result;
    }

    private async Task ScanDirectoryAsync(
        FileSystemItem folder, 
        ScanProgress scanProgress, 
        IProgress<ScanProgress> progress,
        CancellationToken cancellationToken)
    {
        // Check for cancellation first
        cancellationToken.ThrowIfCancellationRequested();
        
        // Pause checkpoint - wait here if paused
        if (_isPaused)
        {
            scanProgress.State = ScanState.Paused;
            scanProgress.StatusMessage = $"Paused at: {folder.FullPath}";
            progress.Report(scanProgress);
            
            // Wait until resumed (or cancelled)
            _pauseEvent.Wait(cancellationToken);
            
            scanProgress.State = ScanState.Scanning;
            scanProgress.StatusMessage = "Resuming scan...";
            progress.Report(scanProgress);
        }

        scanProgress.CurrentFolder = folder.FullPath;
        Interlocked.Increment(ref scanProgress._foldersScanned);

        try
        {
            var dirInfo = new DirectoryInfo(folder.FullPath);

            // Process files using optimized enumeration
            ProcessFilesOptimized(folder, dirInfo, scanProgress, cancellationToken);

            // Get subdirectories using fast enumeration options
            var subDirs = dirInfo.EnumerateDirectories("*", FastEnumerationOptions)
                .Where(d => !ShouldSkipDirectory(d))
                .ToList();

            // Process subdirectories - use parallelism for large folders
            if (subDirs.Count > 4)
            {
                // Parallel scan for folders with many subdirectories
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = MaxParallelism,
                    CancellationToken = cancellationToken
                };

                await Parallel.ForEachAsync(subDirs, parallelOptions, async (subDir, ct) =>
                {
                    // Check for pause
                    if (_isPaused)
                    {
                        _pauseEvent.Wait(ct);
                    }

                    try
                    {
                        var subFolder = new FileSystemItem
                        {
                            Name = subDir.Name,
                            FullPath = subDir.FullName,
                            IsFolder = true,
                            Parent = folder,
                            Created = subDir.CreationTime,
                            LastModified = subDir.LastWriteTime,
                            LastAccessed = subDir.LastAccessTime
                        };

                        // Thread-safe add to children
                        lock (folder.Children)
                        {
                            folder.Children.Add(subFolder);
                        }

                        // Recursive scan
                        await ScanDirectoryAsync(subFolder, scanProgress, progress, ct);

                        // Thread-safe size update
                        Interlocked.Add(ref folder._size, subFolder.Size);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Interlocked.Increment(ref scanProgress._errorCount);
                    }
                    catch (PathTooLongException)
                    {
                        Interlocked.Increment(ref scanProgress._errorCount);
                    }
                });
            }
            else
            {
                // Sequential scan for small folders (avoid parallelism overhead)
                foreach (var subDir in subDirs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (_isPaused)
                    {
                        _pauseEvent.Wait(cancellationToken);
                    }

                    try
                    {
                        var subFolder = new FileSystemItem
                        {
                            Name = subDir.Name,
                            FullPath = subDir.FullName,
                            IsFolder = true,
                            Parent = folder,
                            Created = subDir.CreationTime,
                            LastModified = subDir.LastWriteTime,
                            LastAccessed = subDir.LastAccessTime
                        };

                        folder.Children.Add(subFolder);
                        await ScanDirectoryAsync(subFolder, scanProgress, progress, cancellationToken);
                        folder.Size += subFolder.Size;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        scanProgress.ErrorCount++;
                    }
                    catch (PathTooLongException)
                    {
                        scanProgress.ErrorCount++;
                    }
                }
            }

            // Report progress periodically
            if (scanProgress.FoldersScanned % 20 == 0)
            {
                progress.Report(scanProgress);
            }
        }
        catch (UnauthorizedAccessException)
        {
            Interlocked.Increment(ref scanProgress._errorCount);
        }
        catch (DirectoryNotFoundException)
        {
            Interlocked.Increment(ref scanProgress._errorCount);
        }
    }

    /// <summary>
    /// Optimized file processing - no async overhead, uses fast enumeration
    /// </summary>
    private void ProcessFilesOptimized(
        FileSystemItem folder,
        DirectoryInfo dirInfo,
        ScanProgress scanProgress,
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var file in dirInfo.EnumerateFiles("*", FastEnumerationOptions))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var fileItem = new FileSystemItem
                    {
                        Name = file.Name,
                        FullPath = file.FullName,
                        Size = file.Length,
                        Extension = file.Extension.ToLowerInvariant(),
                        IsFolder = false,
                        Parent = folder,
                        Created = file.CreationTime,
                        LastModified = file.LastWriteTime,
                        LastAccessed = file.LastAccessTime,
                        Category = _categoryClassifier.Classify(file.Extension)
                    };

                    // Thread-safe operations
                    lock (folder.Children)
                    {
                        folder.Children.Add(fileItem);
                    }
                    Interlocked.Add(ref folder._size, fileItem.Size);

                    // Track largest files (ConcurrentBag is already thread-safe)
                    _largestFiles.Add(fileItem);

                    // Update category stats (ConcurrentDictionary is thread-safe)
                    UpdateCategoryStats(fileItem);

                    Interlocked.Increment(ref scanProgress._filesScanned);
                    Interlocked.Add(ref scanProgress._bytesScanned, fileItem.Size);
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref scanProgress._errorCount);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            Interlocked.Increment(ref scanProgress._errorCount);
        }
    }

    private void UpdateCategoryStats(FileSystemItem file)
    {
        _categoryStats.AddOrUpdate(
            file.Category,
            _ => new CategoryStats
            {
                Category = file.Category,
                TotalSize = file.Size,
                FileCount = 1
            },
            (_, stats) =>
            {
                Interlocked.Add(ref stats.TotalSize, file.Size);
                Interlocked.Increment(ref stats.FileCount);
                return stats;
            });
    }

    private static bool ShouldSkipDirectory(DirectoryInfo dir)
    {
        // Skip system directories that typically cause issues
        var name = dir.Name.ToLowerInvariant();
        
        // Windows-specific
        if (name == "$recycle.bin" ||
            name == "system volume information" ||
            name == "$windows.~bt" ||
            name == "$windows.~ws")
            return true;
        
        // macOS-specific
        if (name == ".trash" ||
            name == ".fseventsd" ||
            name == ".spotlight-v100")
            return true;
        
        // Skip symlinks/junctions on all platforms
        return (dir.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
    }

    private void CalculatePercentages(FileSystemItem item)
    {
        if (item.Parent != null && item.Parent.Size > 0)
        {
            item.PercentageOfParent = (double)item.Size / item.Parent.Size * 100;
        }
        else
        {
            item.PercentageOfParent = 100;
        }

        foreach (var child in item.Children)
        {
            CalculatePercentages(child);
        }

        // Calculate category percentages
        var totalSize = _categoryStats.Values.Sum(c => c.TotalSize);
        if (totalSize > 0)
        {
            foreach (var category in _categoryStats.Values)
            {
                category.Percentage = (double)category.TotalSize / totalSize * 100;
            }
        }
    }

    private List<FileSystemItem> GetLargestFolders(FileSystemItem root, int limit)
    {
        var folders = new List<FileSystemItem>();
        CollectFolders(root, folders);
        return folders
            .Where(f => f.IsFolder && f.Size > 0)
            .OrderByDescending(f => f.Size)
            .Take(limit)
            .ToList();
    }

    private void CollectFolders(FileSystemItem item, List<FileSystemItem> folders)
    {
        if (item.IsFolder)
        {
            folders.Add(item);
            foreach (var child in item.Children)
            {
                CollectFolders(child, folders);
            }
        }
    }

    public async Task<long> GetFolderSizeAsync(string path, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            long size = 0;
            try
            {
                var dir = new DirectoryInfo(path);
                foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        size += file.Length;
                    }
                    catch { }
                }
            }
            catch { }
            return size;
        }, cancellationToken);
    }
}
