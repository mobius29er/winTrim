using System;
using Microsoft.Extensions.DependencyInjection;
using WinTrim.Avalonia.Services;
using WinTrim.Avalonia.ViewModels;
using WinTrim.Core.Services;

namespace WinTrim.Avalonia;

/// <summary>
/// Extension methods for configuring dependency injection.
/// Uses runtime platform detection per Avalonia best practices.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all WinTrim services with the DI container.
    /// Automatically selects platform-specific implementations at runtime.
    /// </summary>
    public static IServiceCollection AddWinTrimServices(this IServiceCollection services)
    {
        // Register platform-specific services using runtime detection
        // This is the recommended Avalonia approach per docs
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<IPlatformService, WindowsPlatformService>();
            services.AddSingleton<IDevToolDetector, WindowsDevToolDetector>();
            services.AddSingleton<IGameDetector, WindowsGameDetector>();
        }
        else if (OperatingSystem.IsMacOS())
        {
            services.AddSingleton<IPlatformService, MacPlatformService>();
            services.AddSingleton<IDevToolDetector, MacDevToolDetector>();
            services.AddSingleton<IGameDetector, MacGameDetector>();
        }
        else
        {
            // Linux fallback - use Mac implementations as they're closer to Linux behavior
            // TODO: Create Linux-specific implementations when Linux support is added
            services.AddSingleton<IPlatformService, MacPlatformService>();
            services.AddSingleton<IDevToolDetector, MacDevToolDetector>();
            services.AddSingleton<IGameDetector, MacGameDetector>();
        }

        // Register cross-platform services
        services.AddSingleton<ICategoryClassifier, CategoryClassifier>();
        services.AddSingleton<ICleanupAdvisor, CleanupAdvisor>();
        services.AddSingleton<IFileScanner, FileScanner>();
        services.AddSingleton<TreemapLayoutService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        
        // Register Avalonia-specific services
        services.AddSingleton<IThemeService, ThemeService>();

        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();

        return services;
    }
}
