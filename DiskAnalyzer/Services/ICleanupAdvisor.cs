using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiskAnalyzer.Models;

namespace DiskAnalyzer.Services;

/// <summary>
/// Interface for generating cleanup recommendations
/// </summary>
public interface ICleanupAdvisor
{
    Task<List<CleanupSuggestion>> GetSuggestionsAsync(FileSystemItem rootItem, CancellationToken cancellationToken);
}
