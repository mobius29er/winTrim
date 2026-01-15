using System;
using System.Collections.Generic;
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
            new PropertyMetadata(AppTheme.Tech, OnThemeChanged));

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

        // Draw label if tile is big enough
        if (rect.Width > 40 && rect.Height > 20)
        {
            DrawLabel(canvas, tile, rect);
        }

        // Draw children (only if not too deep)
        if (tile.Depth < MaxDepth)
        {
            foreach (var child in tile.Children)
            {
                DrawTile(canvas, child);
            }
        }
    }

    private void DrawLabel(SKCanvas canvas, TreemapTile tile, SKRect rect)
    {
        var textSize = Math.Min(14, Math.Max(9, rect.Height / 4));
        
        using var textPaint = new SKPaint
        {
            Color = TextColor,
            TextSize = textSize,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
        };

        // Truncate text to fit
        var text = tile.Name;
        var maxWidth = rect.Width - 8;
        
        while (textPaint.MeasureText(text) > maxWidth && text.Length > 3)
        {
            text = text.Substring(0, text.Length - 4) + "...";
        }

        var x = rect.Left + 4;
        var y = rect.Top + textSize + 2;
        
        // Draw text shadow for readability
        using var shadowPaint = new SKPaint
        {
            Color = TextShadowColor,
            TextSize = textSize,
            IsAntialias = true,
            Typeface = textPaint.Typeface
        };
        canvas.DrawText(text, x + 1, y + 1, shadowPaint);
        canvas.DrawText(text, x, y, textPaint);

        // Draw size if room permits
        if (rect.Height > 35)
        {
            using var sizePaint = new SKPaint
            {
                Color = TextColor.WithAlpha(200),
                TextSize = Math.Max(8, textSize - 2),
                IsAntialias = true
            };
            canvas.DrawText(tile.SizeFormatted, x, y + textSize + 2, sizePaint);
        }
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
        NavigateUp();
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
