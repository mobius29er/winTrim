using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using WinTrim.Avalonia.Controls;
using WinTrim.Core.Models;

namespace WinTrim.Avalonia.Views;

public partial class MainWindow : Window
{
    private TreemapTile? _rightClickedTile;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Wire up treemap events after initialization
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        // Find the TreemapControl by name and wire up events
        var treemapControl = this.FindControl<TreemapControl>("TreemapView");
        if (treemapControl != null)
        {
            treemapControl.TileClicked += TreemapControl_TileClicked;
            treemapControl.TileDoubleClicked += TreemapControl_TileDoubleClicked;
            treemapControl.TileRightClicked += TreemapControl_TileRightClicked;
        }
    }

    private void TreemapControl_TileClicked(object? sender, TreemapTile tile)
    {
        // Single click just selects the item - no tab navigation
        // User stays on treemap tab and can see item details
        if (DataContext is ViewModels.MainWindowViewModel vm && tile?.SourceItem != null)
        {
            vm.SelectedItem = tile.SourceItem;
        }
    }

    private void TreemapControl_TileDoubleClicked(object? sender, TreemapTile tile)
    {
        // Double-click on folder drills down into it (handled by TreemapControl internally)
        // Double-click on file opens File Explorer tab to show context
        if (DataContext is ViewModels.MainWindowViewModel vm && tile?.SourceItem != null)
        {
            vm.SelectedItem = tile.SourceItem;
            
            // If it's a file, navigate to File Explorer to show it in context
            if (!tile.IsFolder)
            {
                var tabControl = this.FindControl<TabControl>("MainTabControl");
                if (tabControl != null)
                {
                    tabControl.SelectedIndex = 2; // File Explorer tab
                }
            }
            // If it's a folder, TreemapControl.NavigateToTile() already handles drill-down
        }
    }

    private void TreemapControl_TileRightClicked(object? sender, TreemapTile tile)
    {
        // Right-click: show context menu at mouse position
        _rightClickedTile = tile;
        
        var treemapControl = sender as TreemapControl;
        if (treemapControl == null || tile == null) return;
        
        // Find the context menu from resources and show at mouse position
        if (this.TryFindResource("TreemapContextMenu", out var resource) && resource is MenuFlyout contextMenu)
        {
            contextMenu.ShowAt(treemapControl, true);
        }
    }

    private void TreemapCopyPath_Click(object? sender, RoutedEventArgs e)
    {
        if (_rightClickedTile?.FullPath != null && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            clipboard.SetTextAsync(_rightClickedTile.FullPath);
        }
    }

    private void TreemapOpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (_rightClickedTile?.FullPath != null)
        {
            try
            {
                var path = _rightClickedTile.IsFolder 
                    ? _rightClickedTile.FullPath 
                    : System.IO.Path.GetDirectoryName(_rightClickedTile.FullPath);
                    
                if (!string.IsNullOrEmpty(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
            }
            catch { /* Ignore errors */ }
        }
    }

    private void TreemapNavigateBack_Click(object? sender, RoutedEventArgs e)
    {
        var treemapControl = this.FindControl<TreemapControl>("TreemapView");
        treemapControl?.NavigateUp();
    }

    /// <summary>
    /// Handles click outside the settings panel to close it
    /// </summary>
    private void SettingsOverlay_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Only close if clicked on the overlay itself (not the settings panel)
        if (e.Source == sender && DataContext is ViewModels.MainWindowViewModel vm)
        {
            vm.ToggleSettingsCommand.Execute(null);
        }
    }
}