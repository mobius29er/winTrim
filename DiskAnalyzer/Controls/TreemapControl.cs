using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DiskAnalyzer.Models;
using DiskAnalyzer.Services;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace DiskAnalyzer.Controls;

/// <summary>
/// Custom treemap visualization control using SkiaSharp for high-performance rendering
/// </summary>
public class TreemapControl : SKElement
{
    private TreemapTile? _rootTile;
    private TreemapTile? _currentRoot;
    private TreemapTile? _hoveredTile;
    private readonly ITreemapLayoutService _layoutService;
    
    // Navigation history stack for drill-down/back functionality
    private readonly Stack<FileSystemItem> _navigationHistory = new();

    // Dependency Properties
    public static readonly DependencyProperty SourceItemProperty =
        DependencyProperty.Register(nameof(SourceItem), typeof(FileSystemItem), typeof(TreemapControl),
            new PropertyMetadata(null, OnSourceItemChanged));

    public static readonly DependencyProperty MaxDepthProperty =
        DependencyProperty.Register(nameof(MaxDepth), typeof(int), typeof(TreemapControl),
            new PropertyMetadata(3, OnSourceItemChanged));

    public static readonly DependencyProperty SelectedThemeProperty =
        DependencyProperty.Register(nameof(SelectedTheme), typeof(AppTheme), typeof(TreemapControl),
            new PropertyMetadata(AppTheme.Default, OnThemeChanged));

    public static readonly DependencyProperty ColorSchemeProperty =
        DependencyProperty.Register(nameof(ColorScheme), typeof(TreemapColorScheme), typeof(TreemapControl),
            new PropertyMetadata(TreemapColorScheme.Vivid, OnColorSchemeChanged));

    public static readonly DependencyProperty ColorModeProperty =
        DependencyProperty.Register(nameof(ColorMode), typeof(TreemapColorMode), typeof(TreemapControl),
            new PropertyMetadata(TreemapColorMode.Depth, OnColorModeChanged));

    public FileSystemItem? SourceItem
    {
        get => (FileSystemItem?)GetValue(SourceItemProperty);
        set => SetValue(SourceItemProperty, value);
    }

    public int MaxDepth
    {
        get => (int)GetValue(MaxDepthProperty);
        set => SetValue(MaxDepthProperty, value);
    }

    public TreemapColorScheme ColorScheme
    {
        get => (TreemapColorScheme)GetValue(ColorSchemeProperty);
        set => SetValue(ColorSchemeProperty, value);
    }

    public TreemapColorMode ColorMode
    {
        get => (TreemapColorMode)GetValue(ColorModeProperty);
        set => SetValue(ColorModeProperty, value);
    }

    public AppTheme SelectedTheme
    {
        get => (AppTheme)GetValue(SelectedThemeProperty);
        set => SetValue(SelectedThemeProperty, value);
    }

    // Theme colors - now based on SelectedTheme
    private bool IsDarkTheme => SelectedTheme != AppTheme.Enterprise;
    
    private SKColor BackgroundColor => SelectedTheme switch
    {
        AppTheme.Default => SKColor.Parse("#030A0D"),       // Deep teal-black
        AppTheme.Tech => SKColor.Parse("#050505"),          // Void Black
        AppTheme.Enterprise => SKColors.White,
        AppTheme.TerminalGreen => SKColors.Black,
        AppTheme.TerminalRed => SKColors.Black,
        _ => SKColor.Parse("#030A0D")
    };
    
    private SKColor BorderColor => SelectedTheme switch
    {
        AppTheme.Default => SKColor.Parse("#1A3A40"),       // Teal border
        AppTheme.Tech => SKColor.Parse("#1F2937"),          // Off-World Gray
        AppTheme.Enterprise => SKColors.White,
        AppTheme.TerminalGreen => SKColor.Parse("#1A1A1A"),
        AppTheme.TerminalRed => SKColor.Parse("#1A1A1A"),
        _ => SKColor.Parse("#1A3A40")
    };
    
    private SKColor TextColor => SelectedTheme switch
    {
        AppTheme.Default => SKColor.Parse("#00D4E5"),       // Cyan
        AppTheme.Tech => SKColor.Parse("#00F3FF"),          // K Teal
        AppTheme.Enterprise => SKColors.White,
        AppTheme.TerminalGreen => SKColor.Parse("#00FF00"),
        AppTheme.TerminalRed => SKColor.Parse("#FF3333"),
        _ => SKColor.Parse("#00D4E5")
    };
    
    private SKColor TextShadowColor => IsDarkTheme ? SKColors.Black : SKColors.Black.WithAlpha(100);

    // Events
    public event EventHandler<TreemapTile>? TileClicked;
    public event EventHandler<TreemapTile>? TileDoubleClicked;
    public event EventHandler<TreemapTile?>? TileHovered;
    public event EventHandler? NavigateBack;

    public TreemapControl()
    {
        _layoutService = new TreemapLayoutService();
        
        // Enable mouse events - use Preview events for better capture
        this.MouseMove += OnMouseMove;
        this.PreviewMouseLeftButtonUp += OnMouseClick;
        this.PreviewMouseRightButtonUp += OnRightClick;
        this.Focusable = true;  // Allow focus for keyboard events

        // Set minimum size
        this.MinHeight = 200;
        this.MinWidth = 200;
    }

    private DateTime _lastClickTime;
    private Point _lastClickPos;
    private const double DoubleClickTimeMs = 500;
    private const double DoubleClickDistanceThreshold = 5;

    private void OnMouseClick(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(this);
        var now = DateTime.Now;
        
        // Check for double-click manually
        var timeSinceLastClick = (now - _lastClickTime).TotalMilliseconds;
        var distance = Math.Sqrt(Math.Pow(pos.X - _lastClickPos.X, 2) + Math.Pow(pos.Y - _lastClickPos.Y, 2));
        
        var tile = FindTileAtPoint((float)pos.X, (float)pos.Y);
        
        if (timeSinceLastClick < DoubleClickTimeMs && distance < DoubleClickDistanceThreshold)
        {
            // Double-click detected
            if (tile != null && tile.IsFolder)
            {
                NavigateToTile(tile);
                TileDoubleClicked?.Invoke(this, tile);
                e.Handled = true;
            }
            _lastClickTime = DateTime.MinValue; // Reset to prevent triple-click
        }
        else
        {
            // Single click
            if (tile != null)
            {
                TileClicked?.Invoke(this, tile);
            }
            _lastClickTime = now;
            _lastClickPos = pos;
        }
    }

    private static void OnSourceItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TreemapControl control)
        {
            control.RebuildTreemap();
        }
    }

    private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TreemapControl control)
        {
            control.InvalidateVisual();
        }
    }

    private static void OnColorSchemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TreemapControl control)
        {
            control._layoutService.SetColorScheme((TreemapColorScheme)e.NewValue);
            control.RebuildTreemap();
        }
    }

    private static void OnColorModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TreemapControl control)
        {
            control._layoutService.SetColorMode((TreemapColorMode)e.NewValue);
            control.RebuildTreemap();
        }
    }

    public void RebuildTreemap()
    {
        if (SourceItem == null || ActualWidth <= 0 || ActualHeight <= 0)
        {
            _rootTile = null;
            _currentRoot = null;
            _navigationHistory.Clear();
            InvalidateVisual();
            return;
        }

        _layoutService.SetColorScheme(ColorScheme);
        _rootTile = _layoutService.BuildTreemap(SourceItem, (float)ActualWidth, (float)ActualHeight, MaxDepth);
        _currentRoot = _rootTile;
        _navigationHistory.Clear();
        InvalidateVisual();
    }

    public void NavigateToTile(TreemapTile tile)
    {
        if (tile.SourceItem == null || !tile.IsFolder) return;
        
        // Push current root to history before navigating
        if (_currentRoot?.SourceItem != null)
        {
            _navigationHistory.Push(_currentRoot.SourceItem);
        }

        _currentRoot = _layoutService.BuildTreemap(tile.SourceItem, (float)ActualWidth, (float)ActualHeight, MaxDepth);
        InvalidateVisual();
    }

    public void NavigateUp()
    {
        if (_navigationHistory.Count > 0)
        {
            // Pop from history and rebuild
            var previousItem = _navigationHistory.Pop();
            _currentRoot = _layoutService.BuildTreemap(previousItem, (float)ActualWidth, (float)ActualHeight, MaxDepth);
            InvalidateVisual();
            NavigateBack?.Invoke(this, EventArgs.Empty);
        }
        else if (_rootTile != null && _currentRoot != _rootTile)
        {
            // Go back to original root
            _currentRoot = _rootTile;
            InvalidateVisual();
            NavigateBack?.Invoke(this, EventArgs.Empty);
        }
    }
    
    /// <summary>
    /// Returns true if we can navigate back (have history or not at root)
    /// </summary>
    public bool CanNavigateUp => _navigationHistory.Count > 0 || (_currentRoot != _rootTile && _rootTile != null);
    
    /// <summary>
    /// Get the current navigation depth
    /// </summary>
    public int NavigationDepth => _navigationHistory.Count;

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var canvas = e.Surface.Canvas;
        canvas.Clear(BackgroundColor);

        if (_currentRoot == null)
        {
            DrawEmptyState(canvas, e.Info);
            return;
        }

        // Rebuild if size changed
        if (Math.Abs(_currentRoot.Bounds.Width - e.Info.Width) > 1 ||
            Math.Abs(_currentRoot.Bounds.Height - e.Info.Height) > 1)
        {
            if (_currentRoot.SourceItem != null)
            {
                _currentRoot = _layoutService.BuildTreemap(_currentRoot.SourceItem, e.Info.Width, e.Info.Height, MaxDepth);
            }
        }

        DrawTile(canvas, _currentRoot);
    }

    private void DrawEmptyState(SKCanvas canvas, SKImageInfo info)
    {
        using var paint = new SKPaint
        {
            Color = TextColor,
            TextSize = 16,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };

        canvas.DrawText("Run a scan to see the treemap visualization",
            info.Width / 2, info.Height / 2, paint);
    }

    private void DrawTile(SKCanvas canvas, TreemapTile tile)
    {
        if (tile.Bounds.Width < 2 || tile.Bounds.Height < 2)
            return;

        // Draw tile background
        using var fillPaint = new SKPaint
        {
            Color = tile.Color,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        // Lighten color if hovered
        if (tile == _hoveredTile)
        {
            fillPaint.Color = tile.Color.WithAlpha(200);
        }

        var rect = new SKRect(
            tile.Bounds.Left + 1,
            tile.Bounds.Top + 1,
            tile.Bounds.Right - 1,
            tile.Bounds.Bottom - 1);

        canvas.DrawRect(rect, fillPaint);

        // Draw border - use theme-aware color
        using var borderPaint = new SKPaint
        {
            Color = BorderColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true
        };
        canvas.DrawRect(rect, borderPaint);

        // Draw children first (only if not too deep)
        var hasVisibleChildren = tile.Depth < MaxDepth && tile.Children.Any(c => c.Bounds.Width >= 2 && c.Bounds.Height >= 2);
        
        if (hasVisibleChildren)
        {
            foreach (var child in tile.Children)
            {
                DrawTile(canvas, child);
            }
        }
        
        // Only draw label on leaf tiles (no visible children) to prevent overlap
        if (!hasVisibleChildren && rect.Width > 40 && rect.Height > 20)
        {
            DrawLabel(canvas, tile, rect);
        }
    }

    private void DrawLabel(SKCanvas canvas, TreemapTile tile, SKRect rect)
    {
        var textSize = Math.Min(18, Math.Max(11, rect.Height / 3.5f));
        
        // Calculate text color based on tile brightness for legibility
        var tileBrightness = (tile.Color.Red * 0.299f + tile.Color.Green * 0.587f + tile.Color.Blue * 0.114f) / 255f;
        
        // Use white text on dark background pill for best readability
        var textColor = SKColors.White;
        var bgColor = SKColors.Black.WithAlpha(160);
        
        using var textPaint = new SKPaint
        {
            Color = textColor,
            TextSize = (float)textSize,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
        };

        // Get available space
        var maxWidth = rect.Width - 8;
        var availableHeight = rect.Height - 8;
        
        // Prepare text lines
        var name = tile.Name;
        var lines = new List<string>();
        
        // Try to fit the full name, wrapping if needed
        if (textPaint.MeasureText(name) <= maxWidth)
        {
            lines.Add(name);
        }
        else if (availableHeight > textSize * 2.5f && rect.Width > 60)
        {
            // Try word wrap for 2 lines
            var words = name.Split(new[] { ' ', '_', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
            var line1 = "";
            var line2 = "";
            
            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(line1) ? word : line1 + " " + word;
                if (textPaint.MeasureText(testLine) <= maxWidth)
                {
                    line1 = testLine;
                }
                else if (string.IsNullOrEmpty(line2))
                {
                    line2 = word;
                }
                else
                {
                    var testLine2 = line2 + " " + word;
                    if (textPaint.MeasureText(testLine2) <= maxWidth)
                    {
                        line2 = testLine2;
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(line1)) lines.Add(line1);
            if (!string.IsNullOrEmpty(line2)) lines.Add(TruncateText(line2, textPaint, maxWidth));
        }
        else
        {
            // Single line with truncation
            lines.Add(TruncateText(name, textPaint, maxWidth));
        }

        // Calculate total text block height
        var lineHeight = (float)textSize + 2;
        var totalTextHeight = lines.Count * lineHeight;
        var showSize = rect.Height > 45 && totalTextHeight + lineHeight < availableHeight;
        if (showSize) totalTextHeight += lineHeight;
        
        // Draw semi-transparent background pill for text
        var maxTextWidth = lines.Max(l => textPaint.MeasureText(l));
        if (showSize)
        {
            using var sizePaint = new SKPaint { TextSize = Math.Max(10, (float)textSize - 2) };
            maxTextWidth = Math.Max(maxTextWidth, sizePaint.MeasureText(tile.SizeFormatted));
        }
        
        var bgRect = new SKRect(
            rect.Left + 2,
            rect.Top + 2,
            Math.Min(rect.Left + maxTextWidth + 10, rect.Right - 2),
            Math.Min(rect.Top + totalTextHeight + 8, rect.Bottom - 2)
        );
        
        using var bgPaint = new SKPaint
        {
            Color = bgColor,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawRoundRect(bgRect, 3, 3, bgPaint);

        // Draw text
        var x = rect.Left + 5;
        var y = rect.Top + (float)textSize + 4;
        
        foreach (var line in lines)
        {
            canvas.DrawText(line, x, y, textPaint);
            y += lineHeight;
        }

        // Draw size if room permits
        if (showSize)
        {
            using var sizePaint = new SKPaint
            {
                Color = SKColors.White.WithAlpha(200),
                TextSize = Math.Max(10, (float)textSize - 2),
                IsAntialias = true
            };
            canvas.DrawText(tile.SizeFormatted, x, y, sizePaint);
        }
    }

    private string TruncateText(string text, SKPaint paint, float maxWidth)
    {
        if (paint.MeasureText(text) <= maxWidth)
            return text;
            
        // Binary search for optimal truncation point
        var ellipsis = "…";
        var low = 0;
        var high = text.Length;
        
        while (low < high)
        {
            var mid = (low + high + 1) / 2;
            var testText = text.Substring(0, mid) + ellipsis;
            if (paint.MeasureText(testText) <= maxWidth)
                low = mid;
            else
                high = mid - 1;
        }
        
        return low > 0 ? text.Substring(0, low) + ellipsis : ellipsis;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(this);
        var tile = FindTileAtPoint((float)pos.X, (float)pos.Y);

        if (tile != _hoveredTile)
        {
            _hoveredTile = tile;
            TileHovered?.Invoke(this, tile);
            InvalidateVisual();
        }
    }

    private void OnRightClick(object sender, MouseButtonEventArgs e)
    {
        // Don't auto-navigate - let context menu handle actions
        // Just select the tile under cursor for context menu operations
        var pos = e.GetPosition(this);
        var tile = FindTileAtPoint((float)pos.X, (float)pos.Y);
        if (tile != null)
        {
            TileClicked?.Invoke(this, tile);
        }
    }

    private TreemapTile? FindTileAtPoint(float x, float y)
    {
        return FindTileRecursive(_currentRoot, x, y);
    }

    private TreemapTile? FindTileRecursive(TreemapTile? tile, float x, float y)
    {
        if (tile == null || !tile.ContainsPoint(x, y))
            return null;

        // Check children first (they're on top)
        foreach (var child in tile.Children)
        {
            var found = FindTileRecursive(child, x, y);
            if (found != null)
                return found;
        }

        return tile;
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        RebuildTreemap();
    }
}
