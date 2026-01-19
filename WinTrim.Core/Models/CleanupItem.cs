using System;
using System.Collections.Generic;

namespace WinTrim.Core.Models;

/// <summary>
/// Represents a developer tool cleanup item discovered by DevToolDetector.
/// Use ToCleanupSuggestion() to convert for UI display.
/// </summary>
public class CleanupItem
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public long SizeBytes { get; init; }
    public required string Category { get; init; }
    public required string Recommendation { get; init; }
    public CleanupRisk Risk { get; init; } = CleanupRisk.Medium;
    public DateTime LastAccessed { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Formatted size for UI display
    /// </summary>
    public string SizeFormatted
    {
        get
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = SizeBytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:N2} {suffixes[suffixIndex]}";
        }
    }

    /// <summary>
    /// Last accessed formatted for UI display
    /// </summary>
    public string LastAccessedFormatted
    {
        get
        {
            if (LastAccessed == DateTime.MinValue) return "Unknown";
            var days = (DateTime.Now - LastAccessed).Days;
            if (days == 0) return "Today";
            if (days == 1) return "Yesterday";
            if (days < 30) return $"{days} days ago";
            if (days < 365) return $"{days / 30} months ago";
            return $"{days / 365} years ago";
        }
    }

    /// <summary>
    /// Converts to CleanupSuggestion for unified UI display
    /// </summary>
    public CleanupSuggestion ToCleanupSuggestion() => new()
    {
        Description = Name,
        Path = Path,
        PotentialSavings = SizeBytes,
        RiskLevel = Risk,
        Type = CleanupType.DeveloperTools,
        AffectedFiles = new List<string> { Path }
    };
}
