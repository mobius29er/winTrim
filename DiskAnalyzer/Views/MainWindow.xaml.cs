using System.Windows;
using System.Windows.Controls;
using DiskAnalyzer.Controls;
using DiskAnalyzer.Models;
using DiskAnalyzer.ViewModels;

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

    private void CleanupSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is DataGrid dg && dg.SelectedItem is CleanupSuggestion suggestion)
        {
            vm.ViewCleanupDetailsCommand.Execute(suggestion);
        }
    }
}
