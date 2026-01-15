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
