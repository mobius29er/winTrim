using System;
using System.Collections.Generic;
using System.Linq;
using WinTrim.Core.Models;
using SkiaSharp;

namespace WinTrim.Core.Services;

/// <summary>
/// Implements the Squarified Treemap algorithm for visualizing hierarchical data
/// Based on: "Squarified Treemaps" by Bruls, Huizing, and van Wijk
/// 
/// Key design: Uses separate LayoutElement class for calculations to avoid
/// corrupting the actual Size property on tiles (which stores file bytes).
/// </summary>
public sealed class TreemapLayoutService : ITreemapLayoutService
{
    private TreemapColorScheme _colorScheme = TreemapColorScheme.Vivid;
    private TreemapColorMode _colorMode = TreemapColorMode.Depth;

    public TreemapColorMode CurrentColorMode => _colorMode;

    // Color palettes for different schemes - designed for high contrast and readability
    private static readonly Dictionary<TreemapColorScheme, SKColor[]> DepthColorSchemes = new()
    {
        [TreemapColorScheme.Vivid] = new[]
        {
            SKColor.Parse("#2563EB"), // Strong Blue
            SKColor.Parse("#059669"), // Emerald Green
            SKColor.Parse("#D97706"), // Amber
            SKColor.Parse("#7C3AED"), // Violet
            SKColor.Parse("#0891B2"), // Cyan
            SKColor.Parse("#4F46E5"), // Indigo
            SKColor.Parse("#0D9488"), // Teal
            SKColor.Parse("#9333EA"), // Purple
            SKColor.Parse("#0284C7"), // Light Blue
            SKColor.Parse("#65A30D"), // Lime
        },
        [TreemapColorScheme.Pastel] = new[]
        {
            SKColor.Parse("#93C5FD"), // Light Blue
            SKColor.Parse("#86EFAC"), // Light Green
            SKColor.Parse("#FCD34D"), // Light Yellow
            SKColor.Parse("#C4B5FD"), // Light Purple
            SKColor.Parse("#A5F3FC"), // Light Cyan
            SKColor.Parse("#FBCFE8"), // Light Pink
            SKColor.Parse("#FED7AA"), // Light Orange
            SKColor.Parse("#D9F99D"), // Light Lime
            SKColor.Parse("#E9D5FF"), // Light Violet
            SKColor.Parse("#99F6E4"), // Light Teal
        },
        [TreemapColorScheme.Ocean] = new[]
        {
            SKColor.Parse("#0077B6"), // Ocean Blue
            SKColor.Parse("#0096C7"), // Sky Blue
            SKColor.Parse("#00B4D8"), // Bright Cyan
            SKColor.Parse("#48CAE4"), // Light Cyan
            SKColor.Parse("#90E0EF"), // Pale Cyan
            SKColor.Parse("#023E8A"), // Deep Blue
            SKColor.Parse("#0369A1"), // Steel Blue
            SKColor.Parse("#0EA5E9"), // Vivid Blue
            SKColor.Parse("#06B6D4"), // Teal
            SKColor.Parse("#14B8A6"), // Teal Green
        },
        [TreemapColorScheme.Warm] = new[]
        {
            SKColor.Parse("#EA580C"), // Orange
            SKColor.Parse("#DC2626"), // Red
            SKColor.Parse("#D97706"), // Amber
            SKColor.Parse("#CA8A04"), // Yellow
            SKColor.Parse("#F59E0B"), // Light Amber
            SKColor.Parse("#B45309"), // Dark Orange
            SKColor.Parse("#C2410C"), // Deep Orange
            SKColor.Parse("#EF4444"), // Bright Red
            SKColor.Parse("#FBBF24"), // Gold
            SKColor.Parse("#F97316"), // Tangerine
        },
        [TreemapColorScheme.Cool] = new[]
        {
            SKColor.Parse("#7C3AED"), // Violet
            SKColor.Parse("#8B5CF6"), // Purple
            SKColor.Parse("#6366F1"), // Indigo
            SKColor.Parse("#0EA5E9"), // Sky Blue
            SKColor.Parse("#14B8A6"), // Teal
            SKColor.Parse("#A855F7"), // Light Purple
            SKColor.Parse("#818CF8"), // Light Indigo
            SKColor.Parse("#22D3EE"), // Cyan
            SKColor.Parse("#2DD4BF"), // Light Teal
            SKColor.Parse("#C084FC"), // Light Violet
        }
    };

    private static readonly Dictionary<TreemapColorScheme, Dictionary<ItemCategory, SKColor>> CategoryColorSchemes = new()
    {
        [TreemapColorScheme.Vivid] = new()
        {
            { ItemCategory.Document, SKColor.Parse("#2563EB") },   // Blue
            { ItemCategory.Image, SKColor.Parse("#DB2777") },      // Pink
            { ItemCategory.Video, SKColor.Parse("#DC2626") },      // Red
            { ItemCategory.Audio, SKColor.Parse("#D97706") },      // Amber
            { ItemCategory.Archive, SKColor.Parse("#7C3AED") },    // Violet
            { ItemCategory.Code, SKColor.Parse("#059669") },       // Green
            { ItemCategory.Executable, SKColor.Parse("#6366F1") }, // Indigo
            { ItemCategory.Game, SKColor.Parse("#EA580C") },       // Orange
            { ItemCategory.System, SKColor.Parse("#475569") },     // Slate
            { ItemCategory.Temporary, SKColor.Parse("#64748B") },  // Gray
            { ItemCategory.Other, SKColor.Parse("#78716C") },      // Stone
        },
        [TreemapColorScheme.Pastel] = new()
        {
            { ItemCategory.Document, SKColor.Parse("#93C5FD") },
            { ItemCategory.Image, SKColor.Parse("#F9A8D4") },
            { ItemCategory.Video, SKColor.Parse("#FCA5A5") },
            { ItemCategory.Audio, SKColor.Parse("#FCD34D") },
            { ItemCategory.Archive, SKColor.Parse("#C4B5FD") },
            { ItemCategory.Code, SKColor.Parse("#86EFAC") },
            { ItemCategory.Executable, SKColor.Parse("#A5B4FC") },
            { ItemCategory.Game, SKColor.Parse("#FDBA74") },
            { ItemCategory.System, SKColor.Parse("#94A3B8") },
            { ItemCategory.Temporary, SKColor.Parse("#CBD5E1") },
            { ItemCategory.Other, SKColor.Parse("#D6D3D1") },
        },
        [TreemapColorScheme.Ocean] = new()
        {
            { ItemCategory.Document, SKColor.Parse("#0077B6") },
            { ItemCategory.Image, SKColor.Parse("#48CAE4") },
            { ItemCategory.Video, SKColor.Parse("#023E8A") },
            { ItemCategory.Audio, SKColor.Parse("#00B4D8") },
            { ItemCategory.Archive, SKColor.Parse("#0096C7") },
            { ItemCategory.Code, SKColor.Parse("#14B8A6") },
            { ItemCategory.Executable, SKColor.Parse("#0369A1") },
            { ItemCategory.Game, SKColor.Parse("#06B6D4") },
            { ItemCategory.System, SKColor.Parse("#334155") },
            { ItemCategory.Temporary, SKColor.Parse("#64748B") },
            { ItemCategory.Other, SKColor.Parse("#475569") },
        },
        [TreemapColorScheme.Warm] = new()
        {
            { ItemCategory.Document, SKColor.Parse("#D97706") },
            { ItemCategory.Image, SKColor.Parse("#F59E0B") },
            { ItemCategory.Video, SKColor.Parse("#DC2626") },
            { ItemCategory.Audio, SKColor.Parse("#FBBF24") },
            { ItemCategory.Archive, SKColor.Parse("#B45309") },
            { ItemCategory.Code, SKColor.Parse("#CA8A04") },
            { ItemCategory.Executable, SKColor.Parse("#EA580C") },
            { ItemCategory.Game, SKColor.Parse("#EF4444") },
            { ItemCategory.System, SKColor.Parse("#78716C") },
            { ItemCategory.Temporary, SKColor.Parse("#A8A29E") },
            { ItemCategory.Other, SKColor.Parse("#D6D3D1") },
        },
        [TreemapColorScheme.Cool] = new()
        {
            { ItemCategory.Document, SKColor.Parse("#6366F1") },
            { ItemCategory.Image, SKColor.Parse("#A855F7") },
            { ItemCategory.Video, SKColor.Parse("#7C3AED") },
            { ItemCategory.Audio, SKColor.Parse("#22D3EE") },
            { ItemCategory.Archive, SKColor.Parse("#8B5CF6") },
            { ItemCategory.Code, SKColor.Parse("#14B8A6") },
            { ItemCategory.Executable, SKColor.Parse("#818CF8") },
            { ItemCategory.Game, SKColor.Parse("#C084FC") },
            { ItemCategory.System, SKColor.Parse("#475569") },
            { ItemCategory.Temporary, SKColor.Parse("#64748B") },
            { ItemCategory.Other, SKColor.Parse("#78716C") },
        }
    };

    /// <summary>
    /// Internal class for layout calculations - keeps pixel area separate from file size
    /// </summary>
    private class LayoutElement
    {
        public TreemapTile Tile { get; set; } = null!;
        public double Area { get; set; }  // Calculated pixel area (NOT file size)
    }

    public void SetColorScheme(TreemapColorScheme scheme)
    {
        _colorScheme = scheme;
    }

    public void SetColorMode(TreemapColorMode mode)
    {
        _colorMode = mode;
    }

    /// <summary>
    /// Get legend items for the current color mode
    /// </summary>
    public Dictionary<string, SKColor> GetLegendItems()
    {
        var items = new Dictionary<string, SKColor>();
        
        switch (_colorMode)
        {
            case TreemapColorMode.Category:
                var categoryColors = CategoryColorSchemes[_colorScheme];
                foreach (var kvp in categoryColors)
                {
                    items[kvp.Key.ToString()] = kvp.Value;
                }
                break;
                
            case TreemapColorMode.Age:
                items["Used < 7 days"] = SKColor.Parse("#EF4444");       // Red
                items["Used < 30 days"] = SKColor.Parse("#F97316");      // Orange
                items["1-3 months ago"] = SKColor.Parse("#EAB308");      // Yellow
                items["3-6 months ago"] = SKColor.Parse("#22C55E");      // Green
                items["6-12 months ago"] = SKColor.Parse("#06B6D4");     // Cyan
                items["1+ year stale"] = SKColor.Parse("#3B82F6");       // Blue
                items["2+ years stale"] = SKColor.Parse("#6366F1");      // Indigo
                break;
                
            case TreemapColorMode.FileType:
                items[".exe/.dll"] = SKColor.Parse("#6366F1");
                items[".mp4/.mkv"] = SKColor.Parse("#DC2626");
                items[".mp3/.wav"] = SKColor.Parse("#F59E0B");
                items[".jpg/.png"] = SKColor.Parse("#EC4899");
                items[".pdf/.doc"] = SKColor.Parse("#2563EB");
                items[".zip/.rar"] = SKColor.Parse("#8B5CF6");
                items["Code files"] = SKColor.Parse("#10B981");
                items["Other"] = SKColor.Parse("#6B7280");
                break;
                
            case TreemapColorMode.Depth:
            default:
                var depthColors = DepthColorSchemes[_colorScheme];
                for (int i = 0; i < Math.Min(5, depthColors.Length); i++)
                {
                    items[$"Level {i + 1}"] = depthColors[i];
                }
                break;
        }
        
        return items;
    }

    public TreemapTile BuildTreemap(FileSystemItem root, float width, float height, int maxDepth = 3)
    {
        var depthColors = DepthColorSchemes[_colorScheme];
        
        var rootTile = new TreemapTile
        {
            Name = root.Name,
            FullPath = root.FullPath,
            Size = root.Size,
            SizeFormatted = root.SizeFormatted,
            IsFolder = root.IsFolder,
            Depth = 0,
            Bounds = new SKRect(0, 0, width, height),
            Color = depthColors[0],
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

        // Apply squarified layout algorithm (iterative to avoid stack overflow)
        Squarify(elements, layoutBounds);

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

    /// <summary>
    /// Iterative squarify algorithm - avoids stack overflow on large datasets
    /// </summary>
    private void Squarify(List<LayoutElement> elements, SKRect bounds)
    {
        if (!elements.Any() || bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var remaining = new List<LayoutElement>(elements);
        var currentBounds = bounds;

        while (remaining.Count > 0)
        {
            double shortestSide = Math.Min(currentBounds.Width, currentBounds.Height);
            if (shortestSide <= 0) break;

            var row = new List<LayoutElement>();
            
            // Build up the current row
            while (remaining.Count > 0)
            {
                var next = remaining[0];
                var rowWithNext = new List<LayoutElement>(row) { next };

                if (row.Count == 0 || WorstRatio(row, shortestSide) >= WorstRatio(rowWithNext, shortestSide))
                {
                    // Add to current row - improves or maintains aspect ratio
                    row.Add(next);
                    remaining.RemoveAt(0);
                }
                else
                {
                    // Row is complete, break to layout it
                    break;
                }
            }

            // Layout the completed row and get remaining bounds
            if (row.Count > 0)
            {
                currentBounds = LayoutRow(row, currentBounds, shortestSide);
            }
        }
    }

    private SKRect LayoutRow(List<LayoutElement> row, SKRect bounds, double shortestSide)
    {
        if (!row.Any() || shortestSide <= 0)
            return bounds;

        double totalArea = row.Sum(e => e.Area);
        if (totalArea <= 0)
            return bounds;
            
        double rowThickness = totalArea / shortestSide;  // Width of the strip we're laying out
        if (rowThickness <= 0)
            return bounds;

        bool layoutVertically = bounds.Width >= bounds.Height;  // Stack items in the shorter dimension
        double offset = 0;

        foreach (var element in row)
        {
            double itemLength = rowThickness > 0 ? element.Area / rowThickness : 0;
            if (itemLength <= 0) continue;

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
        var categoryColors = CategoryColorSchemes[_colorScheme];
        var depthColors = DepthColorSchemes[_colorScheme];
        
        switch (_colorMode)
        {
            case TreemapColorMode.Category:
                // Always use category color
                return categoryColors.GetValueOrDefault(item.Category, categoryColors[ItemCategory.Other]);
                
            case TreemapColorMode.Age:
                // Color based on file age
                return GetAgeColor(item);
                
            case TreemapColorMode.FileType:
                // Color based on file extension
                return GetFileTypeColor(item);
                
            case TreemapColorMode.Depth:
            default:
                if (!item.IsFolder)
                {
                    // Use category color for files in depth mode
                    return categoryColors.GetValueOrDefault(item.Category, categoryColors[ItemCategory.Other]);
                }
                // Use depth-based color for folders
                return depthColors[depth % depthColors.Length];
        }
    }

    private SKColor GetAgeColor(FileSystemItem item)
    {
        // Use last accessed date for "staleness" detection
        var daysSinceAccessed = (DateTime.Now - item.LastAccessed).TotalDays;
        
        if (daysSinceAccessed < 7)
            return SKColor.Parse("#EF4444");      // Red - recently used
        if (daysSinceAccessed < 30)
            return SKColor.Parse("#F97316");      // Orange - used this month
        if (daysSinceAccessed < 90)
            return SKColor.Parse("#EAB308");      // Yellow - 1-3 months
        if (daysSinceAccessed < 180)
            return SKColor.Parse("#22C55E");      // Green - 3-6 months
        if (daysSinceAccessed < 365)
            return SKColor.Parse("#06B6D4");      // Cyan - 6-12 months
        if (daysSinceAccessed < 730)
            return SKColor.Parse("#3B82F6");      // Blue - 1-2 years
            
        return SKColor.Parse("#6366F1");          // Indigo - 2+ years (stale)
    }

    private SKColor GetFileTypeColor(FileSystemItem item)
    {
        if (item.IsFolder)
            return SKColor.Parse("#374151");      // Dark gray for folders
            
        var ext = item.Extension?.ToLowerInvariant() ?? "";
        
        return ext switch
        {
            // Executables
            ".exe" or ".dll" or ".msi" or ".bat" or ".cmd" or ".app" => SKColor.Parse("#6366F1"),
            // Video
            ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".flv" or ".webm" => SKColor.Parse("#DC2626"),
            // Audio
            ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".wma" or ".m4a" or ".aiff" => SKColor.Parse("#F59E0B"),
            // Images
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" or ".ico" or ".heic" => SKColor.Parse("#EC4899"),
            // Documents
            ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" or ".rtf" or ".pages" => SKColor.Parse("#2563EB"),
            // Archives
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" or ".dmg" => SKColor.Parse("#8B5CF6"),
            // Code
            ".cs" or ".js" or ".ts" or ".py" or ".java" or ".cpp" or ".c" or ".h" or ".html" or ".css" or ".json" or ".xml" or ".swift" or ".go" or ".rs" => SKColor.Parse("#10B981"),
            // Other
            _ => SKColor.Parse("#6B7280")
        };
    }
}

public interface ITreemapLayoutService
{
    TreemapTile BuildTreemap(FileSystemItem root, float width, float height, int maxDepth = 3);
    void SetColorScheme(TreemapColorScheme scheme);
    void SetColorMode(TreemapColorMode mode);
    TreemapColorMode CurrentColorMode { get; }
    Dictionary<string, SKColor> GetLegendItems();
}

/// <summary>
/// Color schemes for treemap visualization
/// </summary>
public enum TreemapColorScheme
{
    Vivid,
    Pastel,
    Ocean,
    Warm,
    Cool
}

/// <summary>
/// Color modes for treemap visualization
/// </summary>
public enum TreemapColorMode
{
    Depth,
    Category,
    Age,
    FileType
}
