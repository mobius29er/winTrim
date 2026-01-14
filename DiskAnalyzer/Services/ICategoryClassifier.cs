using DiskAnalyzer.Models;

namespace DiskAnalyzer.Services;

/// <summary>
/// Interface for classifying files into categories
/// </summary>
public interface ICategoryClassifier
{
    ItemCategory Classify(string extension);
}
