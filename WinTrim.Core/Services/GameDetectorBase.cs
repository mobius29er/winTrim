using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// Base class for cross-platform game detection.
/// Contains shared logic; platform-specific paths are provided by derived classes.
/// </summary>
public abstract class GameDetectorBase : IGameDetector
{
    protected readonly IPlatformService PlatformService;
    
    protected GameDetectorBase(IPlatformService platformService)
    {
        PlatformService = platformService;
    }
    
    public virtual async Task<List<GameInstallation>> DetectGamesAsync(string rootPath, CancellationToken cancellationToken)
    {
        var games = new List<GameInstallation>();

        var tasks = GetDetectionTasks(rootPath, cancellationToken);
        var results = await Task.WhenAll(tasks);
        
        foreach (var result in results)
        {
            games.AddRange(result);
        }

        // Deduplicate games
        return games
            .GroupBy(g => g.Path.ToLowerInvariant())
            .Select(g => g.First())
            .GroupBy(g => g.Name.ToLowerInvariant())
            .Select(g => g.OrderByDescending(x => x.Size).First())
            .OrderByDescending(g => g.Size)
            .ToList();
    }
    
    /// <summary>
    /// Override to provide platform-specific detection tasks
    /// </summary>
    protected abstract IEnumerable<Task<List<GameInstallation>>> GetDetectionTasks(string rootPath, CancellationToken cancellationToken);
    
    /// <summary>
    /// Detect Steam games - cross-platform with platform-specific paths
    /// </summary>
    protected async Task<List<GameInstallation>> DetectSteamGamesAsync(IEnumerable<string> steamPaths, CancellationToken cancellationToken)
    {
        var games = new List<GameInstallation>();

        await Task.Run(() =>
        {
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
    
    /// <summary>
    /// Detect GOG games - cross-platform with platform-specific paths
    /// </summary>
    protected async Task<List<GameInstallation>> DetectGOGGamesAsync(IEnumerable<string> gogPaths, CancellationToken cancellationToken)
    {
        var games = new List<GameInstallation>();

        await Task.Run(() =>
        {
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

    protected static long CalculateDirectorySize(string path, CancellationToken cancellationToken)
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
