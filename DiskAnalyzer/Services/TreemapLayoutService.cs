using System;
using System.Collections.Generic;
using System.Linq;
using DiskAnalyzer.Models;
using SkiaSharp;

namespace DiskAnalyzer.Services;

/// <summary>
/// Implements the Squarified Treemap algorithm for visualizing hierarchical data
/// Based on: "Squarified Treemaps" by Bruls, Huizing, and van Wijk
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
            LayoutChildren(rootTile, root.Children.ToList(), maxDepth);
        }

        return rootTile;
    }

    private void LayoutChildren(TreemapTile parent, List<FileSystemItem> children, int maxDepth)
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

        // Create tiles for children
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

        // Apply squarified layout
        Squarify(childTiles, parent.Bounds, parent.Size);

        parent.Children = childTiles;

        // Recursively layout grandchildren for folders
        foreach (var childTile in childTiles.Where(t => t.IsFolder && t.SourceItem != null))
        {
            var grandchildren = childTile.SourceItem!.Children.ToList();
            if (grandchildren.Any())
            {
                // Add small padding for nested rectangles
                var paddedBounds = new SKRect(
                    childTile.Bounds.Left + 2,
                    childTile.Bounds.Top + 16, // Leave room for label
                    childTile.Bounds.Right - 2,
                    childTile.Bounds.Bottom - 2);

                if (paddedBounds.Width > 20 && paddedBounds.Height > 20)
                {
                    childTile.Bounds = new SKRect(
                        childTile.Bounds.Left, childTile.Bounds.Top,
                        childTile.Bounds.Right, childTile.Bounds.Bottom);
                    
                    LayoutChildren(childTile, grandchildren, maxDepth);
                }
            }
        }
    }

    private void Squarify(List<TreemapTile> tiles, SKRect bounds, long totalSize)
    {
        if (!tiles.Any() || totalSize == 0)
            return;

        // Normalize sizes to fit in bounds
        var totalArea = bounds.Width * bounds.Height;
        foreach (var tile in tiles)
        {
            tile.Size = (long)((double)tile.Size / totalSize * totalArea);
        }

        SquarifyRecursive(tiles, new List<TreemapTile>(), bounds, GetShortestSide(bounds));
    }

    private void SquarifyRecursive(List<TreemapTile> remaining, List<TreemapTile> row, SKRect bounds, float shortestSide)
    {
        if (!remaining.Any())
        {
            LayoutRow(row, bounds, shortestSide);
            return;
        }

        var next = remaining.First();
        var newRow = row.ToList();
        newRow.Add(next);

        if (row.Count == 0 || WorstRatio(row, shortestSide) >= WorstRatio(newRow, shortestSide))
        {
            // Add to current row
            remaining.RemoveAt(0);
            SquarifyRecursive(remaining, newRow, bounds, shortestSide);
        }
        else
        {
            // Start new row
            var newBounds = LayoutRow(row, bounds, shortestSide);
            SquarifyRecursive(remaining, new List<TreemapTile>(), newBounds, GetShortestSide(newBounds));
        }
    }

    private SKRect LayoutRow(List<TreemapTile> row, SKRect bounds, float shortestSide)
    {
        if (!row.Any())
            return bounds;

        var totalSize = row.Sum(t => t.Size);
        var isHorizontal = bounds.Width >= bounds.Height;
        var rowSize = (float)totalSize / shortestSide;

        float offset = 0;
        foreach (var tile in row)
        {
            var tileSize = (float)tile.Size / rowSize;

            if (isHorizontal)
            {
                tile.Bounds = new SKRect(
                    bounds.Left,
                    bounds.Top + offset,
                    bounds.Left + rowSize,
                    bounds.Top + offset + tileSize);
                offset += tileSize;
            }
            else
            {
                tile.Bounds = new SKRect(
                    bounds.Left + offset,
                    bounds.Top,
                    bounds.Left + offset + tileSize,
                    bounds.Top + rowSize);
                offset += tileSize;
            }
        }

        // Return remaining bounds
        if (isHorizontal)
        {
            return new SKRect(bounds.Left + rowSize, bounds.Top, bounds.Right, bounds.Bottom);
        }
        else
        {
            return new SKRect(bounds.Left, bounds.Top + rowSize, bounds.Right, bounds.Bottom);
        }
    }

    private float WorstRatio(List<TreemapTile> row, float shortestSide)
    {
        if (!row.Any())
            return float.MaxValue;

        var sum = row.Sum(t => (float)t.Size);
        var max = row.Max(t => (float)t.Size);
        var min = row.Min(t => (float)t.Size);

        var s2 = shortestSide * shortestSide;
        var sumSq = sum * sum;

        return Math.Max(s2 * max / sumSq, sumSq / (s2 * min));
    }

    private float GetShortestSide(SKRect bounds)
    {
        return Math.Min(bounds.Width, bounds.Height);
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
