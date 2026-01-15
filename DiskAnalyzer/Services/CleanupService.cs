using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiskAnalyzer.Models;

namespace DiskAnalyzer.Services;

/// <summary>
/// Service to execute cleanup operations safely
/// </summary>
public interface ICleanupService
{
    Task<CleanupResult> ExecuteCleanupAsync(IEnumerable<CleanupSuggestion> suggestions, CleanupRisk maxRisk);
}

public class CleanupService : ICleanupService
{
    /// <summary>
    /// Execute cleanup for suggestions up to the specified risk level
    /// </summary>
    public async Task<CleanupResult> ExecuteCleanupAsync(IEnumerable<CleanupSuggestion> suggestions, CleanupRisk maxRisk)
    {
        var result = new CleanupResult();
        var eligibleSuggestions = suggestions.Where(s => s.RiskLevel <= maxRisk).ToList();

        foreach (var suggestion in eligibleSuggestions)
        {
            try
            {
                var cleaned = await CleanupSuggestionAsync(suggestion);
                if (cleaned > 0)
                {
                    result.ItemsCleaned++;
                    result.BytesRecovered += cleaned;
                    result.CleanedItems.Add(suggestion.Description);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{suggestion.Description}: {ex.Message}");
            }
        }

        return result;
    }

    private async Task<long> CleanupSuggestionAsync(CleanupSuggestion suggestion)
    {
        return await Task.Run(() =>
        {
            long bytesRecovered = 0;

            switch (suggestion.Type)
            {
                case CleanupType.TempFiles:
                case CleanupType.BrowserCache:
                case CleanupType.OldLogFiles:
                    bytesRecovered = CleanupDirectory(suggestion.Path);
                    break;

                case CleanupType.RecycleBin:
                    bytesRecovered = EmptyRecycleBin();
                    break;

                default:
                    // For other types, clean affected files if specified
                    if (suggestion.AffectedFiles.Any())
                    {
                        foreach (var file in suggestion.AffectedFiles)
                        {
                            bytesRecovered += DeleteFileSafely(file);
                        }
                    }
                    else if (Directory.Exists(suggestion.Path))
                    {
                        bytesRecovered = CleanupDirectory(suggestion.Path);
                    }
                    break;
            }

            return bytesRecovered;
        });
    }

    private long CleanupDirectory(string path)
    {
        long bytesRecovered = 0;

        if (!Directory.Exists(path))
            return 0;

        try
        {
            var dirInfo = new DirectoryInfo(path);

            // Delete files
            foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                try
                {
                    var size = file.Length;
                    file.Delete();
                    bytesRecovered += size;
                }
                catch
                {
                    // Skip files that can't be deleted (in use, permissions, etc.)
                }
            }

            // Delete empty subdirectories
            foreach (var dir in dirInfo.GetDirectories("*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.FullName.Length))
            {
                try
                {
                    if (!dir.GetFiles().Any() && !dir.GetDirectories().Any())
                    {
                        dir.Delete();
                    }
                }
                catch
                {
                    // Skip directories that can't be deleted
                }
            }
        }
        catch
        {
            // Directory access error
        }

        return bytesRecovered;
    }

    private long DeleteFileSafely(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var size = new FileInfo(filePath).Length;
                File.Delete(filePath);
                return size;
            }
        }
        catch
        {
            // File couldn't be deleted
        }
        return 0;
    }

    private long EmptyRecycleBin()
    {
        try
        {
            // Use Shell32 to empty recycle bin
            SHEmptyRecycleBin(IntPtr.Zero, null, 0x00000001 | 0x00000002); // SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI
            return 0; // Can't easily get the exact size recovered
        }
        catch
        {
            return 0;
        }
    }

    [System.Runtime.InteropServices.DllImport("Shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);
}

/// <summary>
/// Result of a cleanup operation
/// </summary>
public class CleanupResult
{
    public int ItemsCleaned { get; set; }
    public long BytesRecovered { get; set; }
    public List<string> CleanedItems { get; set; } = new();
    public List<string> Errors { get; set; } = new();

    public string BytesRecoveredFormatted
    {
        get
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = BytesRecovered;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:N2} {suffixes[suffixIndex]}";
        }
    }

    public bool HasErrors => Errors.Any();
}
