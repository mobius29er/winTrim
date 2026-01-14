# WinLose - Disk Analyzer

A clean, safe, and powerful Windows 10/11 disk analyzer application to view and analyze file contents and storage allocation.

## 🎯 Features

### Core Functionality
- ✅ **Disk Scanning** - Fast recursive scanning with async processing
- ✅ **Storage Analytics** - Visual breakdown of how data is allocated by category
- ✅ **Cleanup Recommendations** - Safe suggestions for freeing disk space
- ✅ **Largest Files Finder** - Top 50 largest files with quick access
- ✅ **Game Detection** - Auto-detect Steam, Epic, GOG, and Xbox game installations
- ✅ **File Age Analysis** - Identify files not accessed in 90+ days

### Data Provided
- 📅 Date last accessed/modified
- 📊 Size of folders and files (human-readable)
- 📍 Full path locations
- 🏷️ File type categorization

### UI/UX Features
- 🎨 Clean, modern interface
- ▶️ Start/Stop/Pause scan controls
- 📈 Interactive charts (pie charts for categories, bar charts for folders)
- 🌲 Tree view folder navigation
- 📋 Data grids with sorting
- 📂 Quick "Open in Explorer" buttons

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
├── Services/         # Business logic services
│   ├── FileScanner   # Core scanning engine
│   ├── GameDetector  # Steam/Epic/GOG detection
│   ├── CleanupAdvisor # Cleanup recommendations
│   └── CategoryClassifier # File type classification
├── Converters/       # Value converters for UI
└── Themes/           # Colors and styles
```

## 🛡️ Safety Features

- **Read-only scanning** - No files are modified or deleted
- **Risk levels** for cleanup suggestions (Safe/Low/Medium/High)
- **Graceful error handling** for inaccessible folders
- **Memory efficient** - Processes files in batches

## 🔧 Technical Details

- **Framework:** .NET 8.0 + WPF
- **Architecture:** MVVM with CommunityToolkit.Mvvm
- **Charts:** LiveCharts2 (SkiaSharp)
- **Async:** Full async/await with CancellationToken support

## 📋 Original Requirements

Purpose of this software is to download locally a clean and safe application to view and analyze the contents and location of the files on your harddrive in windows 10.

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
- Ability to Start/stop/pause analysis and maintain the current results so user could start it, see items they want to address, and then either continue or stop# winLose
