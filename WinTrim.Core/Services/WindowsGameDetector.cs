using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// Windows-specific game detector.
/// Handles Steam, Epic Games, GOG, Xbox Game Pass, and Windows Registry lookups.
/// </summary>
public class WindowsGameDetector : GameDetectorBase
{
    public WindowsGameDetector(IPlatformService platformService) : base(platformService)
    {
    }
    
    protected override IEnumerable<Task<List<GameInstallation>>> GetDetectionTasks(string rootPath, CancellationToken cancellationToken)
    {
        return new[]
        {
            DetectWindowsSteamGamesAsync(rootPath, cancellationToken),
            DetectEpicGamesAsync(cancellationToken),
            DetectWindowsGOGGamesAsync(cancellationToken),
            DetectXboxGamesAsync(rootPath, cancellationToken)
        };
    }
    
    private async Task<List<GameInstallation>> DetectWindowsSteamGamesAsync(string rootPath, CancellationToken cancellationToken)
    {
        var steamPaths = new List<string>
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
            @"C:\Steam",
            @"D:\Steam",
            @"D:\SteamLibrary"
        };

        // Try to get Steam path from registry (Windows-specific)
        try
        {
            var steamPath = GetSteamPathFromRegistry();
            if (!string.IsNullOrEmpty(steamPath))
            {
                steamPaths.Insert(0, steamPath);
            }
        }
        catch { }

        return await DetectSteamGamesAsync(steamPaths, cancellationToken);
    }
    
    private static string? GetSteamPathFromRegistry()
    {
        // Use reflection to avoid compile-time dependency on Windows Registry
        try
        {
            var registryType = Type.GetType("Microsoft.Win32.Registry, Microsoft.Win32.Registry");
            if (registryType == null) return null;
            
            var currentUser = registryType.GetProperty("CurrentUser")?.GetValue(null);
            if (currentUser == null) return null;
            
            var openSubKeyMethod = currentUser.GetType().GetMethod("OpenSubKey", new[] { typeof(string) });
            if (openSubKeyMethod == null) return null;
            
            using var key = openSubKeyMethod.Invoke(currentUser, new object[] { @"Software\Valve\Steam" }) as IDisposable;
            if (key == null) return null;
            
            var getValueMethod = key.GetType().GetMethod("GetValue", new[] { typeof(string) });
            return getValueMethod?.Invoke(key, new object[] { "SteamPath" })?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private async Task<List<GameInstallation>> DetectEpicGamesAsync(CancellationToken cancellationToken)
    {
        var games = new List<GameInstallation>();

        await Task.Run(() =>
        {
            var manifestPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Epic", "EpicGamesLauncher", "Data", "Manifests");

            if (!Directory.Exists(manifestPath))
                return;

            try
            {
                foreach (var manifest in Directory.GetFiles(manifestPath, "*.item"))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var json = File.ReadAllText(manifest);
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        var displayName = root.GetProperty("DisplayName").GetString() ?? "Unknown";
                        var installLocation = root.GetProperty("InstallLocation").GetString();

                        if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                        {
                            var size = CalculateDirectorySize(installLocation, cancellationToken);
                            var dirInfo = new DirectoryInfo(installLocation);

                            games.Add(new GameInstallation
                            {
                                Name = displayName,
                                Path = installLocation,
                                Size = size,
                                Platform = GamePlatform.EpicGames,
                                LastPlayed = dirInfo.LastAccessTime
                            });
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }, cancellationToken);

        return games;
    }

    private async Task<List<GameInstallation>> DetectWindowsGOGGamesAsync(CancellationToken cancellationToken)
    {
        var gogPaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "GOG Galaxy", "Games"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GOG Galaxy", "Games"),
            @"C:\GOG Games",
            @"D:\GOG Games"
        };

        return await DetectGOGGamesAsync(gogPaths, cancellationToken);
    }

    private async Task<List<GameInstallation>> DetectXboxGamesAsync(string rootPath, CancellationToken cancellationToken)
    {
        var games = new List<GameInstallation>();

        await Task.Run(() =>
        {
            var xboxPaths = new[]
            {
                @"C:\XboxGames",
                @"D:\XboxGames",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps")
            };

            foreach (var xboxPath in xboxPaths)
            {
                if (!Directory.Exists(xboxPath))
                    continue;

                try
                {
                    foreach (var gameDir in Directory.GetDirectories(xboxPath))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var dirName = Path.GetFileName(gameDir);
                        if (dirName.StartsWith("Microsoft.") && !dirName.Contains("Game"))
                            continue;

                        try
                        {
                            var dirInfo = new DirectoryInfo(gameDir);
                            var size = CalculateDirectorySize(gameDir, cancellationToken);

                            if (size > 100 * 1024 * 1024) // > 100MB
                            {
                                games.Add(new GameInstallation
                                {
                                    Name = dirInfo.Name,
                                    Path = gameDir,
                                    Size = size,
                                    Platform = GamePlatform.Xbox,
                                    LastPlayed = dirInfo.LastAccessTime
                                });
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }, cancellationToken);

        return games;
    }
}
