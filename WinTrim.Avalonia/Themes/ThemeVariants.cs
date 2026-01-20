using Avalonia.Styling;

namespace WinTrim.Avalonia.Themes;

/// <summary>
/// Custom ThemeVariant definitions for WinTrim themes.
/// Avalonia supports custom ThemeVariants beyond just Light/Dark.
/// Each variant inherits from a base (Light or Dark) for FluentTheme compatibility.
/// </summary>
public static class ThemeVariants
{
    /// <summary>
    /// Retrofuturistic theme - Blade Runner style dark theme (inherits Dark)
    /// </summary>
    public static ThemeVariant Retrofuturistic { get; } = new("Retrofuturistic", ThemeVariant.Dark);
    
    /// <summary>
    /// Tech theme - Tech-Noir cyberpunk style (inherits Dark)
    /// </summary>
    public static ThemeVariant Tech { get; } = new("Tech", ThemeVariant.Dark);
    
    /// <summary>
    /// Enterprise theme - Professional light mode (inherits Light)
    /// </summary>
    public static ThemeVariant Enterprise { get; } = new("Enterprise", ThemeVariant.Light);
    
    /// <summary>
    /// Terminal Green theme - Classic green-on-black terminal (inherits Dark)
    /// </summary>
    public static ThemeVariant TerminalGreen { get; } = new("TerminalGreen", ThemeVariant.Dark);
    
    /// <summary>
    /// Terminal Red theme - Red-on-black terminal style (inherits Dark)
    /// </summary>
    public static ThemeVariant TerminalRed { get; } = new("TerminalRed", ThemeVariant.Dark);
}
