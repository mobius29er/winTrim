using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace WinTrim.Avalonia.Services;

/// <summary>
/// Service for managing runtime theme switching in Avalonia
/// </summary>
public interface IThemeService
{
    string CurrentTheme { get; }
    int CurrentFontSize { get; }
    void ApplyTheme(string themeName);
    void ApplyFontSize(int fontSize);
}

/// <summary>
/// Implementation of theme switching using Avalonia's ResourceDictionary
/// </summary>
public class ThemeService : IThemeService
{
    private readonly Application _application;
    
    public string CurrentTheme { get; private set; } = "Default";
    public int CurrentFontSize { get; private set; } = 14;

    public ThemeService()
    {
        _application = Application.Current ?? throw new InvalidOperationException("Application.Current is null");
    }

    public void ApplyTheme(string themeName)
    {
        Console.WriteLine($"[ThemeService] ApplyTheme called with: {themeName}, current: {CurrentTheme}");
        
        if (string.IsNullOrEmpty(themeName) || themeName == CurrentTheme)
        {
            Console.WriteLine($"[ThemeService] Skipping - same theme or empty");
            return;
        }

        var themeUri = GetThemeUri(themeName);
        Console.WriteLine($"[ThemeService] Theme URI: {themeUri}");

        try
        {
            // In Avalonia, Application.Resources is a ResourceDictionary that can contain:
            // 1. Direct resources (like BaseFontSize)
            // 2. MergedDictionaries - which is where our theme ResourceInclude lives
            
            var appResources = _application.Resources;
            Console.WriteLine($"[ThemeService] App.Resources type: {appResources.GetType().Name}");
            Console.WriteLine($"[ThemeService] Direct MergedDictionaries count: {appResources.MergedDictionaries.Count}");
            
            // Get the MergedDictionaries collection
            var mergedDicts = appResources.MergedDictionaries;
            
            // Log all current merged dictionaries
            Console.WriteLine($"[ThemeService] Current merged dictionaries:");
            for (int i = 0; i < mergedDicts.Count; i++)
            {
                var dict = mergedDicts[i];
                if (dict is ResourceInclude ri)
                {
                    Console.WriteLine($"[ThemeService]   [{i}] ResourceInclude: {ri.Source}");
                }
                else if (dict is ResourceDictionary rd)
                {
                    Console.WriteLine($"[ThemeService]   [{i}] ResourceDictionary with {rd.Count} items, {rd.MergedDictionaries.Count} merged");
                }
                else
                {
                    Console.WriteLine($"[ThemeService]   [{i}] {dict?.GetType().Name ?? "null"}");
                }
            }
            
            // Find and remove existing theme (Colors.axaml files)
            var existingThemes = mergedDicts
                .OfType<ResourceInclude>()
                .Where(r => r.Source?.ToString().Contains("Colors.axaml") == true)
                .ToList();
            
            Console.WriteLine($"[ThemeService] Found {existingThemes.Count} existing color themes to remove");
            
            foreach (var oldTheme in existingThemes)
            {
                Console.WriteLine($"[ThemeService] Removing: {oldTheme.Source}");
                mergedDicts.Remove(oldTheme);
            }

            // Create and add new theme resource at the beginning
            var newThemeResource = new ResourceInclude(themeUri) { Source = themeUri };
            mergedDicts.Insert(0, newThemeResource);
            Console.WriteLine($"[ThemeService] Added new theme: {themeUri}");
            
            CurrentTheme = themeName;
            Console.WriteLine($"[ThemeService] Theme successfully applied: {themeName}");
            
            // Force resource refresh by triggering property changed on resources
            // This helps Avalonia recognize the resource changes
            if (_application.Resources is ResourceDictionary rd2)
            {
                // Workaround: Force a refresh by temporarily modifying a resource
                if (rd2.TryGetResource("BaseFontSize", null, out var fontSize))
                {
                    rd2["BaseFontSize"] = fontSize;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThemeService] ERROR: {ex.Message}");
            Console.WriteLine($"[ThemeService] Stack: {ex.StackTrace}");
        }
    }
    
    public void ApplyFontSize(int fontSize)
    {
        Console.WriteLine($"[ThemeService] ApplyFontSize called with: {fontSize}, current: {CurrentFontSize}");
        
        if (fontSize < 10 || fontSize > 24 || fontSize == CurrentFontSize)
        {
            Console.WriteLine($"[ThemeService] Skipping font size change");
            return;
        }
            
        try
        {
            // Update font size resource
            _application.Resources["BaseFontSize"] = (double)fontSize;
            CurrentFontSize = fontSize;
            Console.WriteLine($"[ThemeService] Font size successfully applied: {fontSize}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThemeService] Font size ERROR: {ex.Message}");
        }
    }
    
    private static Uri GetThemeUri(string themeName) => themeName switch
    {
        "Default" => new Uri("avares://WinTrim/Themes/DefaultColors.axaml"),
        "Tech" => new Uri("avares://WinTrim/Themes/TechColors.axaml"),
        "Enterprise" => new Uri("avares://WinTrim/Themes/EnterpriseColors.axaml"),
        "TerminalGreen" => new Uri("avares://WinTrim/Themes/TerminalGreenColors.axaml"),
        "TerminalRed" => new Uri("avares://WinTrim/Themes/TerminalRedColors.axaml"),
        _ => new Uri("avares://WinTrim/Themes/DefaultColors.axaml")
    };
}
