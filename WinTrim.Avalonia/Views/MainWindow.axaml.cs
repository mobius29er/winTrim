using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
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
        
        // Dialog handles cleanup internally now
        var dialog = new QuickCleanDialog(safeItems);
        await dialog.ShowDialog<bool>(this);
        
        // After dialog closes, refresh cleanup suggestions to reflect any deletions
        // Remove items that no longer have any files
        var toRemove = vm.CleanupSuggestions
            .Where(s => s.AffectedFiles.All(f => 
                !System.IO.File.Exists(f) && !System.IO.Directory.Exists(f)))
            .ToList();
        
        foreach (var item in toRemove)
        {
            vm.CleanupSuggestions.Remove(item);
        }
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

    #region Context Menu Support - Track selected items for right-click actions
    
    private FileSystemItem? _rightClickedFileItem;
    private GameInstallation? _rightClickedGame;
    private CleanupItem? _rightClickedDevTool;

    /// <summary>
    /// Tracks selection for FileSystemItem DataGrids (CategoryFiles, FolderContents, FilteredChildren, LargestFiles)
    /// </summary>
    private void FileItem_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid dataGrid && dataGrid.SelectedItem is FileSystemItem item)
        {
            _rightClickedFileItem = item;
        }
    }

    private void FileItemOpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (_rightClickedFileItem?.FullPath != null)
        {
            try
            {
                var folder = _rightClickedFileItem.IsFolder 
                    ? _rightClickedFileItem.FullPath 
                    : System.IO.Path.GetDirectoryName(_rightClickedFileItem.FullPath);
                if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder))
                {
                    Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
                }
            }
            catch { /* Ignore errors */ }
        }
    }

    private void FileItemCopyPath_Click(object? sender, RoutedEventArgs e)
    {
        if (_rightClickedFileItem?.FullPath != null && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            clipboard.SetTextAsync(_rightClickedFileItem.FullPath);
        }
    }

    /// <summary>
    /// Tracks selection for Game DataGrid
    /// </summary>
    private void Game_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid dataGrid && dataGrid.SelectedItem is GameInstallation game)
        {
            _rightClickedGame = game;
        }
    }

    private void GameOpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (_rightClickedGame?.Path != null)
        {
            try
            {
                if (System.IO.Directory.Exists(_rightClickedGame.Path))
                {
                    Process.Start(new ProcessStartInfo { FileName = _rightClickedGame.Path, UseShellExecute = true });
                }
            }
            catch { /* Ignore errors */ }
        }
    }

    private void GameCopyPath_Click(object? sender, RoutedEventArgs e)
    {
        if (_rightClickedGame?.Path != null && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            clipboard.SetTextAsync(_rightClickedGame.Path);
        }
    }

    /// <summary>
    /// Tracks selection for DevTool DataGrid
    /// </summary>
    private void DevTool_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid dataGrid && dataGrid.SelectedItem is CleanupItem item)
        {
            _rightClickedDevTool = item;
        }
    }

    private void DevToolOpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (_rightClickedDevTool?.Path != null)
        {
            try
            {
                var folder = System.IO.Directory.Exists(_rightClickedDevTool.Path) 
                    ? _rightClickedDevTool.Path 
                    : System.IO.Path.GetDirectoryName(_rightClickedDevTool.Path);
                if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder))
                {
                    Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
                }
            }
            catch { /* Ignore errors */ }
        }
    }

    private void DevToolCopyPath_Click(object? sender, RoutedEventArgs e)
    {
        if (_rightClickedDevTool?.Path != null && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            clipboard.SetTextAsync(_rightClickedDevTool.Path);
        }
    }

    #endregion

    /// <summary>
    /// Handles row selection in the Largest Folders DataGrid to populate folder contents
    /// </summary>
    private void LargestFolders_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid dataGrid && 
            dataGrid.SelectedItem is WinTrim.Core.Models.FileSystemItem folder &&
            DataContext is ViewModels.MainWindowViewModel vm)
        {
            vm.SelectFolderCommand.Execute(folder);
            _rightClickedFileItem = folder;  // Track for context menu
        }
    }

    /// <summary>
    /// Handles row selection in the Cleanup Suggestions DataGrid to populate affected files
    /// </summary>
    private void CleanupSuggestions_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid dataGrid && 
            dataGrid.SelectedItem is WinTrim.Core.Models.CleanupSuggestion suggestion &&
            DataContext is ViewModels.MainWindowViewModel vm)
        {
            vm.SelectCleanupCommand.Execute(suggestion);
            _rightClickedSuggestion = suggestion;  // Track for context menu
        }
    }

    private WinTrim.Core.Models.CleanupFileInfo? _selectedCleanupFile;

    /// <summary>
    /// Tracks selection in the Cleanup Files DataGrid for context menu actions
    /// </summary>
    private void CleanupFilesGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid dataGrid && dataGrid.SelectedItem is WinTrim.Core.Models.CleanupFileInfo fileInfo)
        {
            _selectedCleanupFile = fileInfo;
        }
    }

    private void CleanupFileCopyPath_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedCleanupFile?.FilePath != null && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            clipboard.SetTextAsync(_selectedCleanupFile.FilePath);
        }
    }

    private void CleanupFileOpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedCleanupFile?.FilePath != null)
        {
            OpenFileLocation(_selectedCleanupFile.FilePath);
        }
    }

    /// <summary>
    /// Handler for the Open button in the Cleanup Files DataGrid
    /// </summary>
    private void CleanupFileOpenButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is WinTrim.Core.Models.CleanupFileInfo fileInfo)
        {
            OpenFileLocation(fileInfo.FilePath);
        }
    }

    /// <summary>
    /// Opens the containing folder for a file path
    /// </summary>
    private void OpenFileLocation(string filePath)
    {
        try
        {
            var folder = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
        }
        catch { /* Ignore errors */ }
    }

    private WinTrim.Core.Models.CleanupSuggestion? _rightClickedSuggestion;

    private void CleanupSuggestionCopyPath_Click(object? sender, RoutedEventArgs e)
    {
        if (_rightClickedSuggestion?.Path != null && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            clipboard.SetTextAsync(_rightClickedSuggestion.Path);
        }
    }

    private void CleanupSuggestionOpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (_rightClickedSuggestion?.Path != null)
        {
            try
            {
                var folder = System.IO.Directory.Exists(_rightClickedSuggestion.Path) 
                    ? _rightClickedSuggestion.Path 
                    : System.IO.Path.GetDirectoryName(_rightClickedSuggestion.Path);
                if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = folder,
                        UseShellExecute = true
                    });
                }
            }
            catch { /* Ignore errors */ }
        }
    }

    /// <summary>
    /// Handles double-click on DataGrid column headers to auto-size columns
    /// </summary>
    private void DataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not DataGrid dataGrid) return;
        
        // Check if double-click was on a column header
        if (e.Source is Control source)
        {
            var columnHeader = source.FindAncestorOfType<DataGridColumnHeader>();
            if (columnHeader != null)
            {
                // Find the column by matching the header content
                var column = dataGrid.Columns.FirstOrDefault(c => 
                    c.Header?.ToString() == columnHeader.Content?.ToString());
                
                if (column != null)
                {
                    AutoSizeColumn(dataGrid, column);
                }
            }
        }
    }

    /// <summary>
    /// Auto-sizes a column to fit its content
    /// </summary>
    private void AutoSizeColumn(DataGrid dataGrid, DataGridColumn column)
    {
        // Calculate the maximum width needed for this column
        double maxWidth = 50; // Minimum width
        
        // Measure header
        if (column.Header is string headerText)
        {
            var formattedText = new FormattedText(
                headerText,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default),
                12,
                Brushes.Black);
            maxWidth = Math.Max(maxWidth, formattedText.Width + 20); // Add padding
        }

        // Measure cell content by iterating through visible rows
        if (dataGrid.ItemsSource != null)
        {
            foreach (var item in dataGrid.ItemsSource)
            {
                // Get the cell value for this column
                string? cellText = null;
                
                if (column is DataGridTextColumn textColumn && textColumn.Binding is Binding binding)
                {
                    var propertyPath = binding.Path;
                    if (!string.IsNullOrEmpty(propertyPath))
                    {
                        var property = item.GetType().GetProperty(propertyPath);
                        if (property != null)
                        {
                            var value = property.GetValue(item);
                            cellText = value?.ToString();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(cellText))
                {
                    var formattedText = new FormattedText(
                        cellText,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(FontFamily.Default),
                        12,
                        Brushes.Black);
                    maxWidth = Math.Max(maxWidth, formattedText.Width + 16); // Add cell padding
                }
            }
        }

        // Set the column width (cap at reasonable max)
        maxWidth = Math.Min(maxWidth, 400);
        column.Width = new DataGridLength(maxWidth);
    }
}