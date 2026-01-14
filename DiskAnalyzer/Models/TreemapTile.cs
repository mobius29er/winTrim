using System.Collections.Generic;
using System.Windows;
using SkiaSharp;

namespace DiskAnalyzer.Models;

/// <summary>
/// Represents a single tile in the treemap visualization
/// </summary>
public class TreemapTile
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public int Depth { get; set; }
    public SKColor Color { get; set; }
    public SKRect Bounds { get; set; }
    public bool IsFolder { get; set; }
    public FileSystemItem? SourceItem { get; set; }
    public TreemapTile? Parent { get; set; }
    public List<TreemapTile> Children { get; set; } = new();

    /// <summary>
    /// Check if a point is within this tile's bounds
    /// </summary>
    public bool ContainsPoint(float x, float y)
    {
        return x >= Bounds.Left && x <= Bounds.Right &&
               y >= Bounds.Top && y <= Bounds.Bottom;
    }
}
