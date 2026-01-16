using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using WinTrim.Avalonia.ViewModels;
using WinTrim.Avalonia.Views;

namespace WinTrim.Avalonia;

public partial class App : Application
{
    /// <summary>
    /// Gets the service provider for dependency injection
    /// </summary>
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        Console.WriteLine("[App] Initializing Avalonia XAML...");
        AvaloniaXamlLoader.Load(this);
        Console.WriteLine("[App] Avalonia XAML loaded.");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Console.WriteLine("[App] Framework initialization completed.");
        // Configure dependency injection
        var services = new ServiceCollection();
        services.AddWinTrimServices();
        Services = services.BuildServiceProvider();
        Console.WriteLine("[App] Services built.");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            
            // Resolve MainWindowViewModel from DI container
            Console.WriteLine("[App] Resolving MainWindowViewModel...");
            var mainViewModel = Services.GetRequiredService<MainWindowViewModel>();
            Console.WriteLine("[App] MainWindowViewModel resolved.");
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };
            Console.WriteLine("[App] MainWindow created with DataContext.");
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}