using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// macOS-specific game detector.
/// Handles Steam, GOG, and Mac App Store games.
/// </summary>
public class MacGameDetector : GameDetectorBase
{
    private readonly string _userHome;
    private readonly string _libraryPath;
    
    public MacGameDetector(IPlatformService platformService) : base(platformService)
    {
        _userHome = platformService.GetUserProfilePath();
        _libraryPath = Path.Combine(_userHome, "Library");
    }
    
    protected override IEnumerable<Task<List<GameInstallation>>> GetDetectionTasks(string rootPath, CancellationToken cancellationToken)
    {
        return new[]
        {
            DetectMacSteamGamesAsync(cancellationToken),
            DetectMacGOGGamesAsync(cancellationToken),
            DetectMacAppStoreGamesAsync(cancellationToken)
        };
    }
    
    private async Task<List<GameInstallation>> DetectMacSteamGamesAsync(CancellationToken cancellationToken)
    {
        var steamPaths = new List<string>
        {
            // Steam on Mac stores games here
            Path.Combine(_libraryPath, "Application Support", "Steam", "steamapps", "common"),
            // Alternative location if user moved library
            Path.Combine(_userHome, "Steam", "steamapps", "common"),
        };
        
        // Check for additional library folders from libraryfolders.vdf
        var libraryFoldersPath = Path.Combine(_libraryPath, "Application Support", "Steam", "steamapps", "libraryfolders.vdf");
        if (File.Exists(libraryFoldersPath))
        {
            try
            {
                var content = File.ReadAllText(libraryFoldersPath);
                // Simple parsing - look for "path" entries
                foreach (var line in content.Split('\n'))
                {
                    if (line.Contains("\"path\""))
                    {
                        var parts = line.Split('"');
                        if (parts.Length >= 4)
                        {
                            var additionalPath = Path.Combine(parts[3], "steamapps", "common");
                            if (Directory.Exists(additionalPath))
                            {
                                steamPaths.Add(additionalPath);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // Steam paths already include steamapps/common, so pass parent directories
        var baseSteamPaths = new List<string>
        {
            Path.Combine(_libraryPath, "Application Support", "Steam"),
            Path.Combine(_userHome, "Steam"),
        };
        
        return await DetectSteamGamesAsync(baseSteamPaths, cancellationToken);
    }

    private async Task<List<GameInstallation>> DetectMacGOGGamesAsync(CancellationToken cancellationToken)
    {
        var gogPaths = new[]
        {
            // GOG Galaxy on Mac
            Path.Combine(_libraryPath, "Application Support", "GOG.com", "Galaxy", "Games"),
            // GOG installers sometimes put games here
            Path.Combine("/Applications", "Games"),
            Path.Combine(_userHome, "Games"),
        };

        return await DetectGOGGamesAsync(gogPaths, cancellationToken);
    }

    private async Task<List<GameInstallation>> DetectMacAppStoreGamesAsync(CancellationToken cancellationToken)
    {
        var games = new List<GameInstallation>();

        await Task.Run(() =>
        {
            // Mac App Store apps are in /Applications
            var applicationsPath = "/Applications";
            
            if (!Directory.Exists(applicationsPath))
                return;

            try
            {
                // Look for .app bundles that are likely games
                var gameIndicators = new[] { "game", "adventure", "puzzle", "arcade", "rpg", "simulator" };
                
                foreach (var appPath in Directory.GetDirectories(applicationsPath, "*.app"))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var dirInfo = new DirectoryInfo(appPath);
                        var appName = dirInfo.Name.Replace(".app", "");
                        
                        // Check if it's likely a game by size (games are usually > 500MB)
                        var size = CalculateDirectorySize(appPath, cancellationToken);
                        
                        if (size > 500 * 1024 * 1024) // > 500MB
                        {
                            // Check Info.plist for game category or known game names
                            var plistPath = Path.Combine(appPath, "Contents", "Info.plist");
                            var isLikelyGame = false;
                            
                            if (File.Exists(plistPath))
                            {
                                try
                                {
                                    var plistContent = File.ReadAllText(plistPath).ToLowerInvariant();
                                    isLikelyGame = gameIndicators.Any(g => plistContent.Contains(g)) ||
                                                   plistContent.Contains("lsapplicationcategorytype") && 
                                                   plistContent.Contains("games");
                                }
                                catch { }
                            }
                            
                            // If large app and possibly a game, include it
                            if (isLikelyGame || size > 1L * 1024 * 1024 * 1024) // > 1GB
                            {
                                games.Add(new GameInstallation
                                {
                                    Name = appName,
                                    Path = appPath,
                                    Size = size,
                                    Platform = GamePlatform.Other,
                                    LastPlayed = dirInfo.LastAccessTime
                                });
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }, cancellationToken);

        return games;
    }
}
