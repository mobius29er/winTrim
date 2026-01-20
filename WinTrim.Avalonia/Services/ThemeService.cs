using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using WinTrim.Avalonia.Themes;

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
/// Implementation of theme switching using Avalonia's ThemeVariant system.
/// This is the official Avalonia approach - change RequestedThemeVariant and
/// DynamicResource bindings automatically update.
/// 
/// Custom ThemeVariants are defined in ThemeVariants.cs and registered as keys
/// in App.axaml's ThemeDictionaries. Each theme has its own color palette.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly Application _application;
    
    // Map theme names to our custom ThemeVariant values
    // Each maps to a unique ThemeDictionary in App.axaml
    private static readonly Dictionary<string, ThemeVariant> ThemeVariantMap = new()
    {
        { "Retrofuturistic", ThemeVariants.Retrofuturistic },
        { "Tech", ThemeVariants.Tech },
        { "Enterprise", ThemeVariants.Enterprise },
        { "TerminalGreen", ThemeVariants.TerminalGreen },
        { "TerminalRed", ThemeVariants.TerminalRed }
    };
    
    public string CurrentTheme { get; private set; } = "Retrofuturistic";
    public int CurrentFontSize { get; private set; } = 14;

    public ThemeService()
    {
        _application = Application.Current ?? throw new InvalidOperationException("Application.Current is null");
        
        // Debug: Print theme dictionary keys
        if (_application.Resources is global::Avalonia.Controls.ResourceDictionary rd)
        {
            Console.WriteLine($"[ThemeService] ThemeDictionaries count: {rd.ThemeDictionaries.Count}");
            foreach (var kvp in rd.ThemeDictionaries)
            {
                Console.WriteLine($"[ThemeService]   Key: {kvp.Key} (Type: {kvp.Key.GetType().Name}, HashCode: {kvp.Key.GetHashCode()})");
            }
            Console.WriteLine($"[ThemeService] ThemeVariants.Retrofuturistic: {ThemeVariants.Retrofuturistic} (HashCode: {ThemeVariants.Retrofuturistic.GetHashCode()})");
        }
    }

    public void ApplyTheme(string themeName)
    {
        Console.WriteLine($"[ThemeService] ApplyTheme called with: {themeName}, current: {CurrentTheme}");
        
        if (string.IsNullOrEmpty(themeName))
        {
            Console.WriteLine($"[ThemeService] Skipping - empty theme name");
            return;
        }
        
        if (themeName == CurrentTheme)
        {
            Console.WriteLine($"[ThemeService] Same theme requested, skipping");
            return;
        }

        try
        {
            // Get the ThemeVariant for this theme name
            if (!ThemeVariantMap.TryGetValue(themeName, out var themeVariant))
            {
                Console.WriteLine($"[ThemeService] Unknown theme: {themeName}, defaulting to Dark");
                themeVariant = ThemeVariant.Dark;
            }
            
            Console.WriteLine($"[ThemeService] Setting RequestedThemeVariant to: {themeVariant}");
            
            // This is the key - changing RequestedThemeVariant causes all
            // DynamicResource bindings to automatically re-evaluate
            _application.RequestedThemeVariant = themeVariant;
            
            CurrentTheme = themeName;
            Console.WriteLine($"[ThemeService] Theme successfully applied: {themeName} -> {themeVariant}");
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
}
