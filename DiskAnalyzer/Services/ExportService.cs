using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DiskAnalyzer.Models;

namespace DiskAnalyzer.Services;

/// <summary>
/// Export scan results to CSV and JSON formats
/// </summary>
public sealed class ExportService : IExportService
{
    public async Task ExportToCsvAsync(ScanResult result, string filePath)
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine("Name,Path,Size (Bytes),Size (Formatted),Type,Category,Last Accessed,Last Modified,Days Since Accessed");
        
        // Largest files
        foreach (var file in result.LargestFiles)
        {
            sb.AppendLine($"\"{EscapeCsv(file.Name)}\",\"{EscapeCsv(file.FullPath)}\",{file.Size},\"{file.SizeFormatted}\",\"File\",\"{file.Category}\",\"{file.LastAccessed:yyyy-MM-dd HH:mm}\",\"{file.LastModified:yyyy-MM-dd HH:mm}\",{file.DaysSinceAccessed}");
        }
        
        await File.WriteAllTextAsync(filePath, sb.ToString());
    }

    public async Task ExportLargestFilesToCsvAsync(IEnumerable<FileSystemItem> files, string filePath)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("Rank,Name,Path,Size (Bytes),Size (Formatted),Category,Last Accessed,Days Since Accessed");
        
        int rank = 1;
        foreach (var file in files)
        {
            sb.AppendLine($"{rank},\"{EscapeCsv(file.Name)}\",\"{EscapeCsv(file.FullPath)}\",{file.Size},\"{file.SizeFormatted}\",\"{file.Category}\",\"{file.LastAccessed:yyyy-MM-dd HH:mm}\",{file.DaysSinceAccessed}");
            rank++;
        }
        
        await File.WriteAllTextAsync(filePath, sb.ToString());
    }

    public async Task ExportToJsonAsync(ScanResult result, string filePath)
    {
        var exportData = new ExportModel
        {
            ExportDate = DateTime.Now,
            ScanPath = result.RootPath,
            ScanDuration = result.Duration.ToString(),
            TotalSize = result.TotalSize,
            TotalSizeFormatted = result.RootItem?.SizeFormatted ?? "0 B",
            TotalFiles = result.TotalFiles,
            TotalFolders = result.TotalFolders,
            LargestFiles = result.LargestFiles.Select(f => new FileExportModel
            {
                Name = f.Name,
                Path = f.FullPath,
                Size = f.Size,
                SizeFormatted = f.SizeFormatted,
                Category = f.Category.ToString(),
                LastAccessed = f.LastAccessed,
                LastModified = f.LastModified,
                DaysSinceAccessed = f.DaysSinceAccessed
            }).ToList(),
            LargestFolders = result.LargestFolders.Select(f => new FolderExportModel
            {
                Name = f.Name,
                Path = f.FullPath,
                Size = f.Size,
                SizeFormatted = f.SizeFormatted
            }).ToList(),
            GameInstallations = result.GameInstallations.Select(g => new GameExportModel
            {
                Name = g.Name,
                Path = g.Path,
                Size = g.Size,
                SizeFormatted = g.SizeFormatted,
                Platform = g.Platform.ToString(),
                LastPlayed = g.LastPlayed
            }).ToList(),
            CleanupSuggestions = result.CleanupSuggestions.Select(c => new CleanupExportModel
            {
                Description = c.Description,
                Path = c.Path,
                PotentialSavings = c.PotentialSavings,
                SavingsFormatted = c.SavingsFormatted,
                RiskLevel = c.RiskLevel.ToString()
            }).ToList(),
            CategoryBreakdown = result.CategoryBreakdown.Select(c => new CategoryExportModel
            {
                Category = c.Key.ToString(),
                TotalSize = c.Value.TotalSize,
                SizeFormatted = c.Value.SizeFormatted,
                FileCount = c.Value.FileCount,
                Percentage = c.Value.Percentage
            }).ToList()
        };

        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(exportData, options);
        await File.WriteAllTextAsync(filePath, json);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("\"", "\"\"");
    }
}

public interface IExportService
{
    Task ExportToCsvAsync(ScanResult result, string filePath);
    Task ExportLargestFilesToCsvAsync(IEnumerable<FileSystemItem> files, string filePath);
    Task ExportToJsonAsync(ScanResult result, string filePath);
}

#region Export Models

public class ExportModel
{
    public DateTime ExportDate { get; set; }
    public string ScanPath { get; set; } = string.Empty;
    public string ScanDuration { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public string TotalSizeFormatted { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int TotalFolders { get; set; }
    public List<FileExportModel> LargestFiles { get; set; } = new();
    public List<FolderExportModel> LargestFolders { get; set; } = new();
    public List<GameExportModel> GameInstallations { get; set; } = new();
    public List<CleanupExportModel> CleanupSuggestions { get; set; } = new();
    public List<CategoryExportModel> CategoryBreakdown { get; set; } = new();
}

public class FileExportModel
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime LastAccessed { get; set; }
    public DateTime LastModified { get; set; }
    public int DaysSinceAccessed { get; set; }
}

public class FolderExportModel
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
}

public class GameExportModel
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime? LastPlayed { get; set; }
}

public class CleanupExportModel
{
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long PotentialSavings { get; set; }
    public string SavingsFormatted { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
}

public class CategoryExportModel
{
    public string Category { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public double Percentage { get; set; }
}

#endregion
