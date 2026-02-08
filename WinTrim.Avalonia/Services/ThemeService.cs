using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using WinTrim.Avalonia.Themes;
using WinTrim.Core.Services;

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
    private readonly IAppLogger _logger;
    
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

    public ThemeService(IAppLogger logger)
    {
        _logger = logger;
        _application = Application.Current ?? throw new InvalidOperationException("Application.Current is null");
        
        // Debug: Print theme dictionary keys
        if (_application.Resources is global::Avalonia.Controls.ResourceDictionary rd)
        {
            _logger.LogDebug($"ThemeDictionaries count: {rd.ThemeDictionaries.Count}");
            foreach (var kvp in rd.ThemeDictionaries)
            {
                _logger.LogDebug($"ThemeDictionary Key: {kvp.Key}");
            }
        }
    }

    public void ApplyTheme(string themeName)
    {
        _logger.LogDebug($"ApplyTheme called: {themeName}, current: {CurrentTheme}");
        
        if (string.IsNullOrEmpty(themeName))
        {
            _logger.LogWarning("Skipping - empty theme name");
            return;
        }
        
        if (themeName == CurrentTheme)
        {
            _logger.LogDebug("Same theme requested, skipping");
            return;
        }

        try
        {
            // Get the ThemeVariant for this theme name
            if (!ThemeVariantMap.TryGetValue(themeName, out var themeVariant))
            {
                _logger.LogWarning($"Unknown theme: {themeName}, defaulting to Dark");
                themeVariant = ThemeVariant.Dark;
            }
            
            _logger.LogDebug($"Setting RequestedThemeVariant to: {themeVariant}");
            
            // This is the key - changing RequestedThemeVariant causes all
            // DynamicResource bindings to automatically re-evaluate
            _application.RequestedThemeVariant = themeVariant;
            
            CurrentTheme = themeName;
            _logger.LogInfo($"Theme applied: {themeName}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to apply theme: {themeName}", ex);
        }
    }
    
    public void ApplyFontSize(int fontSize)
    {
        _logger.LogDebug($"ApplyFontSize called: {fontSize}, current: {CurrentFontSize}");
        
        if (fontSize < 10 || fontSize > 24 || fontSize == CurrentFontSize)
        {
            _logger.LogDebug("Skipping font size change");
            return;
        }
            
        try
        {
            // Update font size resource
            _application.Resources["BaseFontSize"] = (double)fontSize;
            CurrentFontSize = fontSize;
            _logger.LogInfo($"Font size applied: {fontSize}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to apply font size: {fontSize}", ex);
        }
    }
}
