using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DiskAnalyzer.Controls;
using DiskAnalyzer.Models;
using DiskAnalyzer.ViewModels;
using LiveChartsCore.Kernel.Sketches;

namespace DiskAnalyzer.Views;

/// <summary>
/// Main application window
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainViewModel vm && e.NewValue is FileSystemItem item)
        {
            vm.SelectedItem = item;
            vm.UpdateSelectedItemChildren(); // Apply filters to new selection
        }
    }

    private void TreemapView_TileClicked(object? sender, TreemapTile tile)
    {
        if (DataContext is MainViewModel vm && tile.SourceItem != null)
        {
            vm.SelectedItem = tile.SourceItem;
            
            // Show tooltip with details
            vm.StatusText = $"{tile.Name} - {tile.SizeFormatted} ({tile.FullPath})";
        }
    }

    private void PieChart_ChartPointPointerDown(IChartView chart, LiveChartsCore.Kernel.ChartPoint? point)
    {
        if (point == null) return;
        
        // Extract category name from series name (format: "Category (size)")
        var seriesName = point.Context.Series.Name;
        if (seriesName != null && DataContext is MainViewModel vm)
        {
            // Parse category name (before the " (" part)
            var parenIndex = seriesName.IndexOf(" (");
            var categoryName = parenIndex > 0 ? seriesName.Substring(0, parenIndex) : seriesName;
            
            // Use the existing SelectCategory command
            if (vm.SelectCategoryCommand.CanExecute(categoryName))
            {
                vm.SelectCategoryCommand.Execute(categoryName);
            }
        }
    }

    private void SettingsOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Close settings when clicking outside the settings panel
        if (DataContext is MainViewModel vm && e.OriginalSource == sender)
        {
            vm.IsSettingsOpen = false;
        }
    }

    private void TreemapContextMenu_OpenLocation(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.SelectedItem != null)
        {
            try
            {
                var path = vm.SelectedItem.FullPath;
                if (vm.SelectedItem.IsFolder)
                {
                    System.Diagnostics.Process.Start("explorer.exe", path);
                }
                else
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
            }
            catch (Exception ex)
            {
                vm.StatusText = $"Error opening location: {ex.Message}";
            }
        }
    }

    private void TreemapContextMenu_CopyPath(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.SelectedItem != null)
        {
            try
            {
                Clipboard.SetText(vm.SelectedItem.FullPath);
                vm.StatusText = "Path copied to clipboard";
            }
            catch (Exception ex)
            {
                vm.StatusText = $"Error copying path: {ex.Message}";
            }
        }
    }

    private void TreemapContextMenu_GoBack(object sender, RoutedEventArgs e)
    {
        TreemapView.NavigateUp();
    }
}
