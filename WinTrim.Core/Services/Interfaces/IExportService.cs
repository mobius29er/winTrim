using System.Collections.Generic;
using System.Threading.Tasks;
using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// Service for exporting scan results to various formats
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Export full scan results to CSV
    /// </summary>
    Task ExportToCsvAsync(ScanResult result, string filePath);
    
    /// <summary>
    /// Export specific file list to CSV
    /// </summary>
    Task ExportFilesToCsvAsync(IEnumerable<FileSystemItem> files, string filePath);
    
    /// <summary>
    /// Export full scan results to JSON
    /// </summary>
    Task ExportToJsonAsync(ScanResult result, string filePath);
}
