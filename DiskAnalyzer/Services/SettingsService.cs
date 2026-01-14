using System;
using System.IO;
using System.Text.Json;

namespace DiskAnalyzer.Services;

/// <summary>
/// Theme accent color options for terminal theme
/// </summary>
public enum ThemeAccent
{
    Green,
    Red
}

/// <summary>
/// Manages user settings with persistence to JSON file
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private UserSettings _settings;

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinLose", "DiskAnalyzer");
        
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");
        _settings = Load();
    }

    public bool IsDarkMode
    {
        get => _settings.IsDarkMode;
        set
        {
            _settings.IsDarkMode = value;
            Save();
        }
    }

    public ThemeAccent Accent
    {
        get => _settings.Accent;
        set
        {
            _settings.Accent = value;
            Save();
        }
    }

    public string? LastScanPath
    {
        get => _settings.LastScanPath;
        set
        {
            _settings.LastScanPath = value;
            Save();
        }
    }

    private UserSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
        }
        catch { }
        
        return new UserSettings();
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch { }
    }
}

public interface ISettingsService
{
    bool IsDarkMode { get; set; }
    ThemeAccent Accent { get; set; }
    string? LastScanPath { get; set; }
}

public class UserSettings
{
    public bool IsDarkMode { get; set; } = false;
    public ThemeAccent Accent { get; set; } = ThemeAccent.Green;
    public string? LastScanPath { get; set; }
}
