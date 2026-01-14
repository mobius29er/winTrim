using System.Windows;

namespace DiskAnalyzer;

/// <summary>
/// Main application entry point
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Set up global exception handling
        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show($"An unexpected error occurred: {ex.Exception.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };
    }
}
