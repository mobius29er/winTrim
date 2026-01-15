# WinTrim - Disk Analyzer

A clean, safe, and powerful Windows 10/11 disk analyzer application to view and analyze file contents and storage allocation.

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform](https://img.shields.io/badge/Platform-Windows-0078D6)
![License](https://img.shields.io/badge/License-MIT-green)

## 🎯 Features

### Core Functionality
- ✅ **Disk Scanning** - Fast recursive scanning with async processing
- ✅ **Storage Analytics** - Visual breakdown of how data is allocated by category
- ✅ **Cleanup Recommendations** - Safe suggestions for freeing disk space
- ✅ **Quick Clean** - One-click cleanup with preview and file-level selection
- ✅ **Largest Files Finder** - Top 50 largest files with quick access
- ✅ **Game Detection** - Auto-detect Steam, Epic, GOG, and Xbox game installations
- ✅ **File Age Analysis** - Identify files not accessed in 90+ days
- ✅ **Scan Caching** - Automatically saves and restores your last scan

### Interactive Visualization
- 🗺️ **Treemap View** - Visual representation of disk usage with drill-down navigation
- 📊 **Pie Charts** - Category breakdown (Documents, Media, Games, etc.)
- 📈 **Bar Charts** - Largest folders at a glance
- 🌲 **Tree View** - Hierarchical folder navigation

### Quick Clean Features
- 🧹 **Preview Before Delete** - See exactly what will be removed
- ☑️ **File-Level Selection** - Expand categories to select individual files
- 📁 **Smart Detection** - Finds temp files, browser cache, Windows Update cache, old logs
- ⚠️ **Risk Indicators** - Safe/Low/Medium/High risk levels for each item

### Data Provided
- 📅 Date last accessed/modified
- 📊 Size of folders and files (human-readable)
- 📍 Full path locations
- 🏷️ File type categorization
- 🎮 Game platform detection (Steam, Epic, GOG, Xbox)

### UI/UX Features
- 🎨 **Multiple Themes** - Light, Dark, and Terminal (Red/Green) modes
- ▶️ **Scan Controls** - Start/Stop/Pause with progress tracking
- 📋 **Sortable Data Grids** - Click headers to sort by name, size, date
- 🖱️ **Context Menus** - Right-click to open location or copy path
- 📂 **Quick Actions** - Open in Explorer buttons throughout

## 🚀 Getting Started

### Prerequisites
- Windows 10/11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

### Installation

1. Clone or download this repository
2. Open terminal in the project folder
3. Build and run:

```bash
cd DiskAnalyzer
dotnet restore
dotnet build
dotnet run
```

Or build a standalone executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

## 📁 Project Structure

```
DiskAnalyzer/
├── Models/           # Data models (FileSystemItem, ScanResult, etc.)
├── ViewModels/       # MVVM ViewModels with commands
├── Views/            # XAML UI files
├── Controls/         # Custom controls (TreemapControl)
├── Services/         # Business logic services
│   ├── FileScanner   # Core scanning engine
│   ├── GameDetector  # Steam/Epic/GOG/Xbox detection
│   ├── CleanupAdvisor # Cleanup recommendations
│   ├── CleanupService # Execute cleanup operations
│   └── CategoryClassifier # File type classification
├── Converters/       # Value converters for UI
└── Themes/           # Colors and styles (Light/Dark/Terminal)
```

## 🛡️ Safety Features

- **Read-only scanning** - No files are modified during analysis
- **Preview before delete** - Quick Clean shows exactly what will be removed
- **Risk levels** for cleanup suggestions (Safe/Low/Medium/High)
- **Graceful error handling** for inaccessible folders
- **Memory efficient** - Processes files in batches

## 🔧 Technical Details

- **Framework:** .NET 8.0 + WPF
- **Architecture:** MVVM with CommunityToolkit.Mvvm
- **Charts:** LiveCharts2 (SkiaSharp)
- **Treemap:** Custom SkiaSharp-based control
- **Async:** Full async/await with CancellationToken support
- **Persistence:** JSON-based scan caching

## 📸 Screenshots

### Dashboard with Category Breakdown
*Pie chart showing storage allocation by file type*

### Treemap Visualization  
*Interactive treemap with double-click drill-down*

### Quick Clean Dialog
*Preview and select individual files before cleanup*

## 📋 Original Requirements

Purpose of this software is to download locally a clean and safe application to view and analyze the contents and location of the files on your harddrive in Windows 10/11.

The application will:
- Read the designated drive
- Provide analytics on how data is allocated
- Provide recommendations on what could be cleaned
- Provide a top hits of the largest files
- Analyze Steam and other applications with large files

It should provide:
- Date used last
- Size of the folders/files
- Location of them

UI/UX:
- Simple and friendly to use
- Ability to Start/stop/pause analysis and maintain the current results

## 📄 License

MIT License - feel free to use and modify as needed.
