using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
    /// Shows the Quick Clean dialog
    /// </summary>
    private async void QuickCleanButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm) return;
        
        // Get safe/low risk suggestions
        var safeItems = vm.CleanupSuggestions
            .Where(s => s.RiskLevel <= WinTrim.Core.Models.CleanupRisk.Low)
            .ToList();
        
        if (!safeItems.Any())
        {
            // Show message that there's nothing to clean
            return;
        }
        
        var dialog = new QuickCleanDialog(safeItems);
        var result = await dialog.ShowDialog<bool>(this);
        
        if (result && dialog.Confirmed)
        {
            var selectedItems = dialog.GetSelectedItems().ToList();
            if (selectedItems.Any())
            {
                // Perform the cleanup
                await PerformCleanup(selectedItems);
            }
        }
    }

    private async Task PerformCleanup(List<WinTrim.Core.Models.CleanupSuggestion> items)
    {
        var totalDeleted = 0L;
        var filesDeleted = 0;
        
        foreach (var item in items)
        {
            foreach (var filePath in item.AffectedFiles)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        var fileInfo = new FileInfo(filePath);
                        var size = fileInfo.Length;
                        File.Delete(filePath);
                        totalDeleted += size;
                        filesDeleted++;
                    }
                    else if (Directory.Exists(filePath))
                    {
                        var dirInfo = new DirectoryInfo(filePath);
                        var size = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                        Directory.Delete(filePath, true);
                        totalDeleted += size;
                        filesDeleted++;
                    }
                }
                catch
                {
                    // Skip files that can't be deleted
                }
            }
        }
        
        // Show completion message (could use a dialog or status bar)
        Console.WriteLine($"[QuickClean] Deleted {filesDeleted} items, freed {FormatBytes(totalDeleted)}");
        
        // Refresh the scan to update the UI
        if (DataContext is ViewModels.MainWindowViewModel vm && vm.CanStart)
        {
            // Optionally trigger a rescan
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:N2} {suffixes[suffixIndex]}";
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