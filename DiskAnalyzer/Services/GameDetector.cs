using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DiskAnalyzer.Models;
using Microsoft.Win32;

namespace DiskAnalyzer.Services;

/// <summary>
/// Detects game installations from various platforms
/// </summary>
public sealed class GameDetector : IGameDetector
{
    public async Task<List<GameInstallation>> DetectGamesAsync(string rootPath, CancellationToken cancellationToken)
    {
        var games = new List<GameInstallation>();

        // Run detection in parallel
        var tasks = new[]
        {
            DetectSteamGamesAsync(rootPath, cancellationToken),
            DetectEpicGamesAsync(cancellationToken),
            DetectGOGGamesAsync(cancellationToken),
            DetectXboxGamesAsync(rootPath, cancellationToken)
        };

        var results = await Task.WhenAll(tasks);
        
        foreach (var result in results)
        {
            games.AddRange(result);
        }

        // Deduplicate games:
        // 1. First by exact path (case-insensitive) 
        // 2. Then by name (same game in different library folders)
        // Keep the largest installation when names match
        return games
            .GroupBy(g => g.Path.ToLowerInvariant())
            .Select(g => g.First())
            .GroupBy(g => g.Name.ToLowerInvariant())
            .Select(g => g.OrderByDescending(x => x.Size).First())
            .OrderByDescending(g => g.Size)
            .ToList();
    }

    private async Task<List<GameInstallation>> DetectSteamGamesAsync(string rootPath, CancellationToken cancellationToken)
    {
        var games = new List<GameInstallation>();

        await Task.Run(() =>
        {
            // Common Steam installation paths
            var steamPaths = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
                @"C:\Steam",
                @"D:\Steam",
                @"D:\SteamLibrary"
            };

            // Try to get Steam path from registry
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                var steamPath = key?.GetValue("SteamPath")?.ToString();
                if (!string.IsNullOrEmpty(steamPath))
                {
                    steamPaths.Insert(0, steamPath);
                }
            }
            catch { }

            foreach (var steamPath in steamPaths.Distinct())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var steamAppsPath = Path.Combine(steamPath, "steamapps", "common");
                if (!Directory.Exists(steamAppsPath))
                    continue;

                try
                {
                    foreach (var gameDir in Directory.GetDirectories(steamAppsPath))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            var dirInfo = new DirectoryInfo(gameDir);
                            var size = CalculateDirectorySize(gameDir, cancellationToken);

                            games.Add(new GameInstallation
                            {
                                Name = dirInfo.Name,
                                Path = gameDir,
                                Size = size,
                                Platform = GamePlatform.Steam,
                                LastPlayed = dirInfo.LastAccessTime
                            });
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }, cancellationToken);

        return games;
    }

    private async Task<List<GameInstallation>> DetectEpicGamesAsync(CancellationToken cancellationToken)
    {
        var games = new List<GameInstallation>();

        await Task.Run(() =>
        {
            // Epic Games manifest location
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

    private async Task<List<GameInstallation>> DetectGOGGamesAsync(CancellationToken cancellationToken)
    {
        var games = new List<GameInstallation>();

        await Task.Run(() =>
        {
            // GOG games are typically in Program Files (x86)\GOG Galaxy\Games
            var gogPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "GOG Galaxy", "Games"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GOG Galaxy", "Games"),
                @"C:\GOG Games",
                @"D:\GOG Games"
            };

            foreach (var gogPath in gogPaths)
            {
                if (!Directory.Exists(gogPath))
                    continue;

                try
                {
                    foreach (var gameDir in Directory.GetDirectories(gogPath))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            var dirInfo = new DirectoryInfo(gameDir);
                            var size = CalculateDirectorySize(gameDir, cancellationToken);

                            games.Add(new GameInstallation
                            {
                                Name = dirInfo.Name,
                                Path = gameDir,
                                Size = size,
                                Platform = GamePlatform.GOG,
                                LastPlayed = dirInfo.LastAccessTime
                            });
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }, cancellationToken);

        return games;
    }

    private async Task<List<GameInstallation>> DetectXboxGamesAsync(string rootPath, CancellationToken cancellationToken)
    {
        var games = new List<GameInstallation>();

        await Task.Run(() =>
        {
            // Xbox games are in WindowsApps or XboxGames folder
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

                        // Skip system/framework folders
                        var dirName = Path.GetFileName(gameDir);
                        if (dirName.StartsWith("Microsoft.") && !dirName.Contains("Game"))
                            continue;

                        try
                        {
                            var dirInfo = new DirectoryInfo(gameDir);
                            var size = CalculateDirectorySize(gameDir, cancellationToken);

                            // Only include if significant size (likely a game)
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

    private static long CalculateDirectorySize(string path, CancellationToken cancellationToken)
    {
        long size = 0;
        try
        {
            var dir = new DirectoryInfo(path);
            foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    size += file.Length;
                }
                catch { }
            }
        }
        catch { }
        return size;
    }
}
