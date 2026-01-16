using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using WinTrim.Core.Models;
using WinTrim.Core.Services;

namespace WinTrim.Avalonia.Controls;

/// <summary>
/// Custom treemap visualization control using SkiaSharp for high-performance rendering
/// Based on official Avalonia ICustomDrawOperation pattern from RenderDemo samples
/// </summary>
public class TreemapControl : Control
{
    private TreemapTile? _rootTile;
    private TreemapTile? _currentRoot;
    private TreemapTile? _hoveredTile;
    private readonly TreemapLayoutService _layoutService;
    private Size _lastSize;
    
    // Navigation history stack for drill-down/back functionality
    private readonly Stack<FileSystemItem> _navigationHistory = new();

    #region Styled Properties

    public static readonly StyledProperty<FileSystemItem?> SourceItemProperty =
        AvaloniaProperty.Register<TreemapControl, FileSystemItem?>(nameof(SourceItem));

    public static readonly StyledProperty<int> MaxDepthProperty =
        AvaloniaProperty.Register<TreemapControl, int>(nameof(MaxDepth), 3);

    public static readonly StyledProperty<string> SelectedThemeProperty =
        AvaloniaProperty.Register<TreemapControl, string>(nameof(SelectedTheme), "Default");

    public static readonly StyledProperty<string> ColorSchemeProperty =
        AvaloniaProperty.Register<TreemapControl, string>(nameof(ColorScheme), "Vivid");

    public static readonly StyledProperty<string> ColorModeProperty =
        AvaloniaProperty.Register<TreemapControl, string>(nameof(ColorMode), "Depth");

    public FileSystemItem? SourceItem
    {
        get => GetValue(SourceItemProperty);
        set => SetValue(SourceItemProperty, value);
    }

    public int MaxDepth
    {
        get => GetValue(MaxDepthProperty);
        set => SetValue(MaxDepthProperty, value);
    }

    public string SelectedTheme
    {
        get => GetValue(SelectedThemeProperty);
        set => SetValue(SelectedThemeProperty, value);
    }

    public string ColorScheme
    {
        get => GetValue(ColorSchemeProperty);
        set => SetValue(ColorSchemeProperty, value);
    }

    public string ColorMode
    {
        get => GetValue(ColorModeProperty);
        set => SetValue(ColorModeProperty, value);
    }

    #endregion

    // Theme colors
    private SKColor BackgroundColor => SelectedTheme switch
    {
        "Default" => SKColor.Parse("#030A0D"),
        "Tech" => SKColor.Parse("#050505"),
        "Enterprise" => SKColors.White,
        "TerminalGreen" => SKColors.Black,
        "TerminalRed" => SKColors.Black,
        _ => SKColor.Parse("#030A0D")
    };
    
    private SKColor BorderColor => SelectedTheme switch
    {
        "Default" => SKColor.Parse("#1A3A40"),
        "Tech" => SKColor.Parse("#1F2937"),
        "Enterprise" => SKColors.White,
        "TerminalGreen" => SKColor.Parse("#1A1A1A"),
        "TerminalRed" => SKColor.Parse("#1A1A1A"),
        _ => SKColor.Parse("#1A3A40")
    };
    
    private SKColor TextColor => SelectedTheme switch
    {
        "Default" => SKColor.Parse("#00D4E5"),
        "Tech" => SKColor.Parse("#00F3FF"),
        "Enterprise" => SKColors.White,
        "TerminalGreen" => SKColor.Parse("#00FF00"),
        "TerminalRed" => SKColor.Parse("#FF3333"),
        _ => SKColor.Parse("#00D4E5")
    };

    // Events
    public event EventHandler<TreemapTile>? TileClicked;
    public event EventHandler<TreemapTile>? TileDoubleClicked;
    public event EventHandler<TreemapTile>? TileRightClicked;  // For context menu
    public event EventHandler<TreemapTile?>? TileHovered;
    public event EventHandler? NavigateBack;
    
    /// <summary>
    /// The position of the last right-click event, for positioning context menus
    /// </summary>
    public Point LastRightClickPosition { get; private set; }

    static TreemapControl()
    {
        AffectsRender<TreemapControl>(
            SourceItemProperty, 
            MaxDepthProperty, 
            SelectedThemeProperty, 
            ColorSchemeProperty, 
            ColorModeProperty);
    }

    public TreemapControl()
    {
        _layoutService = new TreemapLayoutService();
        ClipToBounds = true;
        Focusable = true;
        MinHeight = 100;
        MinWidth = 100;
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        // Rebuild when attached to visual tree if we have data but no treemap
        if (SourceItem != null && _rootTile == null)
        {
            // Use dispatcher to ensure bounds are calculated first
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (Bounds.Width > 0 && Bounds.Height > 0)
                {
                    RebuildTreemap();
                }
            }, global::Avalonia.Threading.DispatcherPriority.Loaded);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceItemProperty)
        {
            _navigationHistory.Clear();
            RebuildTreemap();
        }
        else if (change.Property == MaxDepthProperty)
        {
            RebuildTreemap();
        }
        else if (change.Property == ColorSchemeProperty)
        {
            if (Enum.TryParse<TreemapColorScheme>(ColorScheme, out var scheme))
            {
                _layoutService.SetColorScheme(scheme);
                RebuildTreemap();
            }
        }
        else if (change.Property == ColorModeProperty)
        {
            if (Enum.TryParse<TreemapColorMode>(ColorMode, out var mode))
            {
                _layoutService.SetColorMode(mode);
                RebuildTreemap();
            }
        }
    }

    private DateTime _lastClickTime;
    private Point _lastClickPos;
    private const double DoubleClickTimeMs = 500;
    private const double DoubleClickDistanceThreshold = 5;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        var pos = e.GetPosition(this);
        var properties = e.GetCurrentPoint(this).Properties;
        
        if (properties.IsLeftButtonPressed)
        {
            var now = DateTime.Now;
            var timeSinceLastClick = (now - _lastClickTime).TotalMilliseconds;
            var distance = Math.Sqrt(Math.Pow(pos.X - _lastClickPos.X, 2) + Math.Pow(pos.Y - _lastClickPos.Y, 2));
            
            var tile = FindTileAtPoint((float)pos.X, (float)pos.Y);
            
            if (timeSinceLastClick < DoubleClickTimeMs && distance < DoubleClickDistanceThreshold)
            {
                if (tile != null && tile.IsFolder)
                {
                    NavigateToTile(tile);
                    TileDoubleClicked?.Invoke(this, tile);
                    e.Handled = true;
                }
                _lastClickTime = DateTime.MinValue;
            }
            else
            {
                if (tile != null)
                {
                    TileClicked?.Invoke(this, tile);
                }
                _lastClickTime = now;
                _lastClickPos = pos;
            }
        }
        else if (properties.IsRightButtonPressed)
        {
            // Right-click: select tile and fire right-click event for context menu
            var tile = FindTileAtPoint((float)pos.X, (float)pos.Y);
            if (tile != null)
            {
                LastRightClickPosition = pos;
                TileClicked?.Invoke(this, tile);
                TileRightClicked?.Invoke(this, tile);
            }
        }
        else if (properties.IsMiddleButtonPressed)
        {
            // Middle-click: navigate back up
            NavigateUp();
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        
        // Backspace or Back button: navigate up
        if (e.Key == Key.Back || e.Key == Key.BrowserBack)
        {
            if (CanNavigateUp)
            {
                NavigateUp();
                e.Handled = true;
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        
        var pos = e.GetPosition(this);
        var tile = FindTileAtPoint((float)pos.X, (float)pos.Y);

        if (tile != _hoveredTile)
        {
            _hoveredTile = tile;
            TileHovered?.Invoke(this, tile);
            InvalidateVisual();
        }
    }

    public void RebuildTreemap()
    {
        if (SourceItem == null || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            _rootTile = null;
            _currentRoot = null;
            InvalidateVisual();
            return;
        }

        if (Enum.TryParse<TreemapColorScheme>(ColorScheme, out var scheme))
        {
            _layoutService.SetColorScheme(scheme);
        }
        
        if (Enum.TryParse<TreemapColorMode>(ColorMode, out var mode))
        {
            _layoutService.SetColorMode(mode);
        }
        
        _rootTile = _layoutService.BuildTreemap(SourceItem, (float)Bounds.Width, (float)Bounds.Height, MaxDepth);
        _currentRoot = _rootTile;
        _navigationHistory.Clear();
        InvalidateVisual();
    }

    public void NavigateToTile(TreemapTile tile)
    {
        if (tile.SourceItem == null || !tile.IsFolder) return;
        
        if (_currentRoot?.SourceItem != null)
        {
            _navigationHistory.Push(_currentRoot.SourceItem);
        }

        _currentRoot = _layoutService.BuildTreemap(tile.SourceItem, (float)Bounds.Width, (float)Bounds.Height, MaxDepth);
        InvalidateVisual();
    }

    public void NavigateUp()
    {
        if (_navigationHistory.Count > 0)
        {
            var previousItem = _navigationHistory.Pop();
            _currentRoot = _layoutService.BuildTreemap(previousItem, (float)Bounds.Width, (float)Bounds.Height, MaxDepth);
            InvalidateVisual();
            NavigateBack?.Invoke(this, EventArgs.Empty);
        }
        else if (_rootTile != null && _currentRoot != _rootTile)
        {
            _currentRoot = _rootTile;
            InvalidateVisual();
            NavigateBack?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public bool CanNavigateUp => _navigationHistory.Count > 0 || (_currentRoot != _rootTile && _rootTile != null);
    
    public int NavigationDepth => _navigationHistory.Count;

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
        context.Custom(new TreemapDrawOperation(bounds, this));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Rebuild if size actually changed OR if we have source data but no treemap yet
        var sizeChanged = Math.Abs(_lastSize.Width - finalSize.Width) > 1 || Math.Abs(_lastSize.Height - finalSize.Height) > 1;
        var needsInitialBuild = SourceItem != null && _rootTile == null && finalSize.Width > 0 && finalSize.Height > 0;
        
        if (sizeChanged || needsInitialBuild)
        {
            _lastSize = finalSize;
            RebuildTreemap();
        }
        return finalSize;
    }

    private TreemapTile? FindTileAtPoint(float x, float y)
    {
        return FindTileRecursive(_currentRoot, x, y);
    }

    private TreemapTile? FindTileRecursive(TreemapTile? tile, float x, float y)
    {
        if (tile == null || !tile.ContainsPoint(x, y))
            return null;

        foreach (var child in tile.Children)
        {
            var found = FindTileRecursive(child, x, y);
            if (found != null)
                return found;
        }

        return tile;
    }

    /// <summary>
    /// Custom drawing operation for SkiaSharp rendering in Avalonia
    /// Based on official Avalonia RenderDemo CustomSkiaPage pattern
    /// </summary>
    private class TreemapDrawOperation : ICustomDrawOperation
    {
        private readonly Rect _bounds;
        private readonly TreemapControl _control;
        private readonly TreemapTile? _currentRoot;
        private readonly TreemapTile? _hoveredTile;
        private readonly SKColor _backgroundColor;
        private readonly SKColor _borderColor;
        private readonly SKColor _textColor;
        private readonly int _maxDepth;

        public TreemapDrawOperation(Rect bounds, TreemapControl control)
        {
            _bounds = bounds;
            _control = control;
            // Capture current state to ensure Equals() works correctly
            _currentRoot = control._currentRoot;
            _hoveredTile = control._hoveredTile;
            _backgroundColor = control.BackgroundColor;
            _borderColor = control.BorderColor;
            _textColor = control.TextColor;
            _maxDepth = control.MaxDepth;
        }

        public Rect Bounds => _bounds;

        public void Dispose() { }

        public bool Equals(ICustomDrawOperation? other)
        {
            // Return false to always redraw - simplest approach
            return false;
        }

        public bool HitTest(Point p) => _bounds.Contains(p);

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null) 
            {
                return;
            }

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            
            canvas.Save();
            canvas.Clear(_backgroundColor);

            if (_currentRoot == null)
            {
                DrawEmptyState(canvas);
            }
            else
            {
                DrawTile(canvas, _currentRoot);
            }
            
            canvas.Restore();
        }

        private void DrawEmptyState(SKCanvas canvas)
        {
            using var paint = new SKPaint
            {
                Color = _textColor,
                TextSize = 16,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center
            };

            canvas.DrawText("Run a scan to see the treemap visualization",
                (float)_bounds.Width / 2, (float)_bounds.Height / 2, paint);
        }

        private void DrawTile(SKCanvas canvas, TreemapTile tile)
        {
            if (tile.Bounds.Width < 2 || tile.Bounds.Height < 2)
                return;

            using var fillPaint = new SKPaint
            {
                Color = tile.Color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

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

            using var borderPaint = new SKPaint
            {
                Color = _borderColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true
            };
            canvas.DrawRect(rect, borderPaint);

            var hasVisibleChildren = tile.Depth < _maxDepth && 
                                     tile.Children.Any(c => c.Bounds.Width >= 2 && c.Bounds.Height >= 2);
            
            if (hasVisibleChildren)
            {
                foreach (var child in tile.Children)
                {
                    DrawTile(canvas, child);
                }
            }
            
            if (!hasVisibleChildren && rect.Width > 40 && rect.Height > 20)
            {
                DrawLabel(canvas, tile, rect);
            }
        }

        private void DrawLabel(SKCanvas canvas, TreemapTile tile, SKRect rect)
        {
            var textSize = Math.Min(18, Math.Max(11, rect.Height / 3.5f));
            
            var textColor = SKColors.White;
            var bgColor = SKColors.Black.WithAlpha(160);
            
            using var textPaint = new SKPaint
            {
                Color = textColor,
                TextSize = (float)textSize,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
            };

            var maxWidth = rect.Width - 8;
            var availableHeight = rect.Height - 8;
            
            var name = tile.Name;
            var lines = new List<string>();
            
            if (textPaint.MeasureText(name) <= maxWidth)
            {
                lines.Add(name);
            }
            else if (availableHeight > textSize * 2.5f && rect.Width > 60)
            {
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
                lines.Add(TruncateText(name, textPaint, maxWidth));
            }

            if (lines.Count == 0) return;

            var lineHeight = (float)textSize + 2;
            var totalTextHeight = lines.Count * lineHeight;
            var showSize = rect.Height > 45 && totalTextHeight + lineHeight < availableHeight;
            if (showSize) totalTextHeight += lineHeight;
            
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

            var x = rect.Left + 5;
            var y = rect.Top + (float)textSize + 4;
            
            foreach (var line in lines)
            {
                canvas.DrawText(line, x, y, textPaint);
                y += lineHeight;
            }

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
    }
}
