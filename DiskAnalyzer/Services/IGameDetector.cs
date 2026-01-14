using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiskAnalyzer.Models;

namespace DiskAnalyzer.Services;

/// <summary>
/// Interface for detecting game installations
/// </summary>
public interface IGameDetector
{
    Task<List<GameInstallation>> DetectGamesAsync(string rootPath, CancellationToken cancellationToken);
}
