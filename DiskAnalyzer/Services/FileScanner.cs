using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiskAnalyzer.Models;

namespace DiskAnalyzer.Services;

/// <summary>
/// High-performance file scanner with async enumeration and pause/resume support
/// </summary>
public sealed class FileScanner : IFileScanner
{
    private readonly IGameDetector _gameDetector;
    private readonly ICleanupAdvisor _cleanupAdvisor;
    private readonly ICategoryClassifier _categoryClassifier;
    
    // Thread-safe collections for parallel processing results
    private readonly ConcurrentBag<FileSystemItem> _largestFiles = new();
    private readonly ConcurrentDictionary<ItemCategory, CategoryStats> _categoryStats = new();
    
    private const int LargestFilesLimit = 100;
    private const int BatchSize = 1000; // Process files in batches for memory efficiency

    public FileScanner(IGameDetector gameDetector, ICleanupAdvisor cleanupAdvisor, ICategoryClassifier categoryClassifier)
    {
        _gameDetector = gameDetector;
        _cleanupAdvisor = cleanupAdvisor;
        _categoryClassifier = categoryClassifier;
    }

    public async Task<ScanResult> ScanAsync(string path, IProgress<ScanProgress> progress, CancellationToken cancellationToken)
    {
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

            // Calculate percentages and finalize
            CalculatePercentages(rootItem);

            // Get largest files (sorted)
            result.LargestFiles = _largestFiles
                .OrderByDescending(f => f.Size)
                .Take(LargestFilesLimit)
                .ToList();

            // Get largest folders
            result.LargestFolders = GetLargestFolders(rootItem, 50);

            // Get oldest accessed files
            result.OldestAccessedFiles = _largestFiles
                .Where(f => f.DaysSinceAccessed > 30)
                .OrderByDescending(f => f.DaysSinceAccessed)
                .Take(50)
                .ToList();

            // Detect games
            result.GameInstallations = await _gameDetector.DetectGamesAsync(path, cancellationToken);

            // Get cleanup suggestions
            result.CleanupSuggestions = await _cleanupAdvisor.GetSuggestionsAsync(rootItem, cancellationToken);

            // Category breakdown
            result.CategoryBreakdown = new Dictionary<ItemCategory, CategoryStats>(_categoryStats);

            result.TotalSize = rootItem.Size;
            result.TotalFiles = scanProgress.FilesScanned;
            result.TotalFolders = scanProgress.FoldersScanned;
            result.ErrorCount = scanProgress.ErrorCount;
            result.ScanCompleted = DateTime.Now;

            scanProgress.State = ScanState.Completed;
            scanProgress.StatusMessage = "Scan completed";
            scanProgress.ProgressPercentage = 100;
            progress.Report(scanProgress);
        }
        catch (OperationCanceledException)
        {
            scanProgress.State = ScanState.Cancelled;
            scanProgress.StatusMessage = "Scan cancelled";
            progress.Report(scanProgress);
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
        cancellationToken.ThrowIfCancellationRequested();

        scanProgress.CurrentFolder = folder.FullPath;
        scanProgress.FoldersScanned++;

        try
        {
            var dirInfo = new DirectoryInfo(folder.FullPath);

            // Process files first (faster than directories)
            await ProcessFilesAsync(folder, dirInfo, scanProgress, progress, cancellationToken);

            // Then process subdirectories
            foreach (var subDir in dirInfo.EnumerateDirectories())
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // Skip system/hidden folders that typically cause access issues
                    if (ShouldSkipDirectory(subDir))
                        continue;

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

                    // Recursive scan
                    await ScanDirectoryAsync(subFolder, scanProgress, progress, cancellationToken);

                    // Update folder size after scanning children
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

            // Report progress periodically
            if (scanProgress.FoldersScanned % 10 == 0)
            {
                progress.Report(scanProgress);
            }
        }
        catch (UnauthorizedAccessException)
        {
            scanProgress.ErrorCount++;
        }
        catch (DirectoryNotFoundException)
        {
            scanProgress.ErrorCount++;
        }
    }

    private Task ProcessFilesAsync(
        FileSystemItem folder,
        DirectoryInfo dirInfo,
        ScanProgress scanProgress,
        IProgress<ScanProgress> progress,
        CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            try
            {
                foreach (var file in dirInfo.EnumerateFiles())
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

                        folder.Children.Add(fileItem);
                        folder.Size += fileItem.Size;

                        // Track largest files
                        _largestFiles.Add(fileItem);

                        // Update category stats
                        UpdateCategoryStats(fileItem);

                        scanProgress.FilesScanned++;
                        scanProgress.BytesScanned += fileItem.Size;
                    }
                    catch (Exception)
                    {
                        scanProgress.ErrorCount++;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                scanProgress.ErrorCount++;
            }
        }, cancellationToken);
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
                stats.TotalSize += file.Size;
                stats.FileCount++;
                return stats;
            });
    }

    private static bool ShouldSkipDirectory(DirectoryInfo dir)
    {
        // Skip system directories that typically cause issues
        var name = dir.Name.ToLowerInvariant();
        return name == "$recycle.bin" ||
               name == "system volume information" ||
               name == "$windows.~bt" ||
               name == "$windows.~ws" ||
               (dir.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint; // Skip symlinks/junctions
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
