using System.Windows;
using System.Windows.Controls;
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
}
