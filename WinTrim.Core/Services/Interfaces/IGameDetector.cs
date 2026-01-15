using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// Interface for detecting game installations
/// </summary>
public interface IGameDetector
{
    Task<List<GameInstallation>> DetectGamesAsync(string rootPath, CancellationToken cancellationToken);
}
