using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// Interface for classifying files into categories
/// </summary>
public interface ICategoryClassifier
{
    ItemCategory Classify(string extension);
}
