using System.Collections.Generic;
using WinTrim.Core.Models;

namespace WinTrim.Core.Services;

/// <summary>
/// Classifies files into categories based on extension
/// </summary>
public sealed class CategoryClassifier : ICategoryClassifier
{
    private static readonly Dictionary<string, ItemCategory> ExtensionMap = new()
    {
        // Documents
        { ".doc", ItemCategory.Document },
        { ".docx", ItemCategory.Document },
        { ".pdf", ItemCategory.Document },
        { ".txt", ItemCategory.Document },
        { ".rtf", ItemCategory.Document },
        { ".odt", ItemCategory.Document },
        { ".xls", ItemCategory.Document },
        { ".xlsx", ItemCategory.Document },
        { ".ppt", ItemCategory.Document },
        { ".pptx", ItemCategory.Document },
        { ".csv", ItemCategory.Document },
        { ".pages", ItemCategory.Document },  // Mac
        { ".numbers", ItemCategory.Document }, // Mac
        { ".keynote", ItemCategory.Document }, // Mac

        // Images
        { ".jpg", ItemCategory.Image },
        { ".jpeg", ItemCategory.Image },
        { ".png", ItemCategory.Image },
        { ".gif", ItemCategory.Image },
        { ".bmp", ItemCategory.Image },
        { ".svg", ItemCategory.Image },
        { ".webp", ItemCategory.Image },
        { ".ico", ItemCategory.Image },
        { ".tiff", ItemCategory.Image },
        { ".raw", ItemCategory.Image },
        { ".psd", ItemCategory.Image },
        { ".heic", ItemCategory.Image },  // Mac/iOS
        { ".heif", ItemCategory.Image },  // Mac/iOS

        // Video
        { ".mp4", ItemCategory.Video },
        { ".mkv", ItemCategory.Video },
        { ".avi", ItemCategory.Video },
        { ".mov", ItemCategory.Video },
        { ".wmv", ItemCategory.Video },
        { ".flv", ItemCategory.Video },
        { ".webm", ItemCategory.Video },
        { ".m4v", ItemCategory.Video },

        // Audio
        { ".mp3", ItemCategory.Audio },
        { ".wav", ItemCategory.Audio },
        { ".flac", ItemCategory.Audio },
        { ".aac", ItemCategory.Audio },
        { ".ogg", ItemCategory.Audio },
        { ".wma", ItemCategory.Audio },
        { ".m4a", ItemCategory.Audio },
        { ".aiff", ItemCategory.Audio },  // Mac

        // Archives
        { ".zip", ItemCategory.Archive },
        { ".rar", ItemCategory.Archive },
        { ".7z", ItemCategory.Archive },
        { ".tar", ItemCategory.Archive },
        { ".gz", ItemCategory.Archive },
        { ".bz2", ItemCategory.Archive },
        { ".xz", ItemCategory.Archive },
        { ".iso", ItemCategory.Archive },
        { ".dmg", ItemCategory.Archive },  // Mac

        // Code
        { ".cs", ItemCategory.Code },
        { ".js", ItemCategory.Code },
        { ".ts", ItemCategory.Code },
        { ".py", ItemCategory.Code },
        { ".java", ItemCategory.Code },
        { ".cpp", ItemCategory.Code },
        { ".c", ItemCategory.Code },
        { ".h", ItemCategory.Code },
        { ".html", ItemCategory.Code },
        { ".css", ItemCategory.Code },
        { ".json", ItemCategory.Code },
        { ".xml", ItemCategory.Code },
        { ".yaml", ItemCategory.Code },
        { ".yml", ItemCategory.Code },
        { ".sql", ItemCategory.Code },
        { ".sh", ItemCategory.Code },
        { ".ps1", ItemCategory.Code },
        { ".swift", ItemCategory.Code },  // Mac/iOS
        { ".m", ItemCategory.Code },      // Objective-C
        { ".mm", ItemCategory.Code },     // Objective-C++
        { ".rs", ItemCategory.Code },     // Rust
        { ".go", ItemCategory.Code },     // Go

        // Executables
        { ".exe", ItemCategory.Executable },
        { ".msi", ItemCategory.Executable },
        { ".dll", ItemCategory.Executable },
        { ".bat", ItemCategory.Executable },
        { ".cmd", ItemCategory.Executable },
        { ".app", ItemCategory.Executable },  // Mac
        { ".pkg", ItemCategory.Executable },  // Mac
        { ".deb", ItemCategory.Executable },  // Linux
        { ".rpm", ItemCategory.Executable },  // Linux
        { ".appimage", ItemCategory.Executable }, // Linux

        // Temporary
        { ".tmp", ItemCategory.Temporary },
        { ".temp", ItemCategory.Temporary },
        { ".bak", ItemCategory.Temporary },
        { ".log", ItemCategory.Temporary },
        { ".cache", ItemCategory.Temporary },

        // System
        { ".sys", ItemCategory.System },
        { ".ini", ItemCategory.System },
        { ".cfg", ItemCategory.System },
        { ".reg", ItemCategory.System },
        { ".dat", ItemCategory.System },
        { ".plist", ItemCategory.System }  // Mac
    };

    public ItemCategory Classify(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return ItemCategory.Other;

        var ext = extension.ToLowerInvariant();
        return ExtensionMap.GetValueOrDefault(ext, ItemCategory.Other);
    }
}
