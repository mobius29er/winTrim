using System;
using System.Threading;
using System.Threading.Tasks;
using DiskAnalyzer.Models;

namespace DiskAnalyzer.Services;

/// <summary>
/// Interface for file system scanning operations
/// </summary>
public interface IFileScanner
{
    /// <summary>
    /// Scans a directory and returns the complete file tree
    /// </summary>
    Task<ScanResult> ScanAsync(string path, IProgress<ScanProgress> progress, CancellationToken cancellationToken);
    
    /// <summary>
    /// Gets the total size of a folder recursively
    /// </summary>
    Task<long> GetFolderSizeAsync(string path, CancellationToken cancellationToken);
}
