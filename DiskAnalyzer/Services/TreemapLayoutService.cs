using System;
using System.Collections.Generic;
using System.Linq;
using DiskAnalyzer.Models;
using SkiaSharp;

namespace DiskAnalyzer.Services;

/// <summary>
/// Implements the Squarified Treemap algorithm for visualizing hierarchical data
/// Based on: "Squarified Treemaps" by Bruls, Huizing, and van Wijk
/// 
/// Key design: Uses separate LayoutElement class for calculations to avoid
/// corrupting the actual Size property on tiles (which stores file bytes).
/// </summary>
public sealed class TreemapLayoutService : ITreemapLayoutService
{
    // Color palette for different depths and categories
    private static readonly SKColor[] DepthColors = new[]
    {
        SKColor.Parse("#3B82F6"), // Blue
        SKColor.Parse("#10B981"), // Green
        SKColor.Parse("#F59E0B"), // Amber
        SKColor.Parse("#EF4444"), // Red
        SKColor.Parse("#8B5CF6"), // Purple
        SKColor.Parse("#EC4899"), // Pink
        SKColor.Parse("#06B6D4"), // Cyan
        SKColor.Parse("#84CC16"), // Lime
        SKColor.Parse("#F97316"), // Orange
        SKColor.Parse("#6366F1"), // Indigo
    };

    private static readonly Dictionary<ItemCategory, SKColor> CategoryColors = new()
    {
        { ItemCategory.Document, SKColor.Parse("#3B82F6") },
        { ItemCategory.Image, SKColor.Parse("#EC4899") },
        { ItemCategory.Video, SKColor.Parse("#EF4444") },
        { ItemCategory.Audio, SKColor.Parse("#F59E0B") },
        { ItemCategory.Archive, SKColor.Parse("#6366F1") },
        { ItemCategory.Code, SKColor.Parse("#10B981") },
        { ItemCategory.Executable, SKColor.Parse("#8B5CF6") },
        { ItemCategory.Game, SKColor.Parse("#F97316") },
        { ItemCategory.System, SKColor.Parse("#64748B") },
        { ItemCategory.Temporary, SKColor.Parse("#94A3B8") },
        { ItemCategory.Other, SKColor.Parse("#CBD5E1") },
    };

    /// <summary>
    /// Internal class for layout calculations - keeps pixel area separate from file size
    /// </summary>
    private class LayoutElement
    {
        public TreemapTile Tile { get; set; } = null!;
        public double Area { get; set; }  // Calculated pixel area (NOT file size)
    }

    public TreemapTile BuildTreemap(FileSystemItem root, float width, float height, int maxDepth = 3)
    {
        var rootTile = new TreemapTile
        {
            Name = root.Name,
            FullPath = root.FullPath,
            Size = root.Size,
            SizeFormatted = root.SizeFormatted,
            IsFolder = root.IsFolder,
            Depth = 0,
            Bounds = new SKRect(0, 0, width, height),
            Color = DepthColors[0],
            SourceItem = root
        };

        if (root.Size > 0 && root.Children.Any())
        {
            LayoutChildren(rootTile, root.Children.ToList(), rootTile.Bounds, maxDepth);
        }

        return rootTile;
    }

    private void LayoutChildren(TreemapTile parent, List<FileSystemItem> children, SKRect layoutBounds, int maxDepth)
    {
        if (parent.Depth >= maxDepth || !children.Any())
            return;

        // Filter and sort children by size (largest first)
        var validChildren = children
            .Where(c => c.Size > 0)
            .OrderByDescending(c => c.Size)
            .ToList();

        if (!validChildren.Any())
            return;

        // Create tiles for children (Size remains as file bytes - NEVER overwrite)
        var childTiles = validChildren.Select(child => new TreemapTile
        {
            Name = child.Name,
            FullPath = child.FullPath,
            Size = child.Size,
            SizeFormatted = child.SizeFormatted,
            IsFolder = child.IsFolder,
            Depth = parent.Depth + 1,
            Parent = parent,
            Color = GetColorForItem(child, parent.Depth + 1),
            SourceItem = child
        }).ToList();

        parent.Children = childTiles;

        // Calculate pixel areas using separate LayoutElement (preserves Size)
        double totalSize = validChildren.Sum(c => (double)c.Size);
        double totalArea = layoutBounds.Width * layoutBounds.Height;

        var elements = childTiles.Select(t => new LayoutElement
        {
            Tile = t,
            Area = (t.Size / totalSize) * totalArea  // Area for layout, Size stays intact
        }).ToList();

        // Apply squarified layout algorithm
        SquarifyRecursive(elements, new List<LayoutElement>(), layoutBounds, Math.Min(layoutBounds.Width, layoutBounds.Height));

        // Recursively layout grandchildren for folders
        foreach (var childTile in childTiles.Where(t => t.IsFolder && t.SourceItem != null))
        {
            var grandchildren = childTile.SourceItem!.Children.ToList();
            if (!grandchildren.Any()) continue;

            // Calculate inner bounds for children (header space + padding)
            const float headerHeight = 18f;
            const float padding = 2f;

            var innerBounds = new SKRect(
                childTile.Bounds.Left + padding,
                childTile.Bounds.Top + headerHeight,
                childTile.Bounds.Right - padding,
                childTile.Bounds.Bottom - padding);

            // Only recurse if we have enough space
            if (innerBounds.Width > 20 && innerBounds.Height > 20)
            {
                LayoutChildren(childTile, grandchildren, innerBounds, maxDepth);
            }
        }
    }

    private void SquarifyRecursive(List<LayoutElement> remaining, List<LayoutElement> row, SKRect bounds, double shortestSide)
    {
        if (!remaining.Any())
        {
            LayoutRow(row, bounds, shortestSide);
            return;
        }

        var next = remaining[0];
        var rowWithNext = new List<LayoutElement>(row) { next };

        if (row.Count == 0 || WorstRatio(row, shortestSide) >= WorstRatio(rowWithNext, shortestSide))
        {
            // Add to current row - improves or maintains aspect ratio
            remaining.RemoveAt(0);
            SquarifyRecursive(remaining, rowWithNext, bounds, shortestSide);
        }
        else
        {
            // Row is complete - layout and start new row with remaining bounds
            var newBounds = LayoutRow(row, bounds, shortestSide);
            SquarifyRecursive(remaining, new List<LayoutElement>(), newBounds, Math.Min(newBounds.Width, newBounds.Height));
        }
    }

    private SKRect LayoutRow(List<LayoutElement> row, SKRect bounds, double shortestSide)
    {
        if (!row.Any())
            return bounds;

        double totalArea = row.Sum(e => e.Area);
        double rowThickness = totalArea / shortestSide;  // Width of the strip we're laying out

        bool layoutVertically = bounds.Width >= bounds.Height;  // Stack items in the shorter dimension
        double offset = 0;

        foreach (var element in row)
        {
            double itemLength = element.Area / rowThickness;

            if (layoutVertically)
            {
                // Vertical strip on the left side, items stack top-to-bottom
                element.Tile.Bounds = new SKRect(
                    (float)bounds.Left,
                    (float)(bounds.Top + offset),
                    (float)(bounds.Left + rowThickness),
                    (float)(bounds.Top + offset + itemLength));
                offset += itemLength;
            }
            else
            {
                // Horizontal strip on the top, items stack left-to-right
                element.Tile.Bounds = new SKRect(
                    (float)(bounds.Left + offset),
                    (float)bounds.Top,
                    (float)(bounds.Left + offset + itemLength),
                    (float)(bounds.Top + rowThickness));
                offset += itemLength;
            }
        }

        // Return remaining bounds after this row
        if (layoutVertically)
        {
            return new SKRect((float)(bounds.Left + rowThickness), bounds.Top, bounds.Right, bounds.Bottom);
        }
        else
        {
            return new SKRect(bounds.Left, (float)(bounds.Top + rowThickness), bounds.Right, bounds.Bottom);
        }
    }

    private double WorstRatio(List<LayoutElement> row, double shortestSide)
    {
        if (!row.Any())
            return double.MaxValue;

        double sum = row.Sum(e => e.Area);
        double max = row.Max(e => e.Area);
        double min = row.Min(e => e.Area);

        if (sum == 0 || min == 0)
            return double.MaxValue;

        double s2 = shortestSide * shortestSide;
        double sumSq = sum * sum;

        // Aspect ratio formula from the Squarified Treemap paper
        return Math.Max((s2 * max) / sumSq, sumSq / (s2 * min));
    }

    private SKColor GetColorForItem(FileSystemItem item, int depth)
    {
        if (!item.IsFolder)
        {
            // Use category color for files
            return CategoryColors.GetValueOrDefault(item.Category, CategoryColors[ItemCategory.Other]);
        }

        // Use depth-based color for folders
        return DepthColors[depth % DepthColors.Length];
    }
}

public interface ITreemapLayoutService
{
    TreemapTile BuildTreemap(FileSystemItem root, float width, float height, int maxDepth = 3);
}
