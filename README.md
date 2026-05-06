# WinTrim - Cross-Platform Disk Analyzer

A clean, safe, and powerful disk analyzer application to view and analyze file contents and storage allocation. Now available on **Windows**, **macOS**, and **Linux**!

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4)
![Avalonia UI](https://img.shields.io/badge/Avalonia-11.2-8B5CF6)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20macOS%20%7C%20Linux-blue)
![License](https://img.shields.io/badge/License-FSL--1.1-blue)

## 🖥️ Platform Support

| Platform | Status | Scan Speed (1TB) | Notes |
|----------|--------|------------------|-------|
| **Windows 10/11** | ✅ Fully Supported | ~20-30 seconds | Native x64 builds |
| **macOS** | ✅ Fully Supported | ~2 minutes | Apple Silicon (M1/M2/M3) & Intel |
| **Linux** | ✅ Fully Supported | ~60-90 seconds | x64, tested on Ubuntu/Debian |

## 🎯 Features

### Core Functionality
- ✅ **Disk Scanning** - Fast recursive scanning with async processing
- ✅ **Storage Analytics** - Visual breakdown of how data is allocated by category
- ✅ **Cleanup Recommendations** - Safe suggestions for freeing disk space
- ✅ **Quick Clean** - One-click cleanup with preview and file-level selection; files are quarantined first so you stay in control
- ✅ **Largest Files Finder** - Top 50 largest files with quick access
- ✅ **Game Detection** - Auto-detect Steam, Epic, GOG, and Xbox game installations
- ✅ **Developer Tools Scanner** - Detect npm, NuGet, pip, Maven, Cargo caches with cleanup recommendations
- ✅ **File Age Analysis** - Identify files not accessed in 90+ days
- ✅ **Session Persistence** - Automatically saves and restores your last scan (including treemap & dev tools)
- ✅ **Rich Cleanup Details** - View file name, size, last accessed date, and risk level for each cleanup item

### Interactive Visualization
- 🗺️ **Treemap View** - Visual representation of disk usage with drill-down navigation
- 🎨 **5 Treemap Color Schemes** - Vivid, Pastel, Ocean, Warm, and Cool palettes
- 📊 **Pie Charts** - Category breakdown (Documents, Media, Games, etc.)
- 📈 **Bar Charts** - Largest folders at a glance
- 🌲 **Tree View** - Hierarchical folder navigation with search & filters

### Quick Clean Features
- 🧹 **Preview Before Clean** - See exactly what will be moved
- ☑️ **File-Level Selection** - Expand categories to select individual files
- 📁 **Smart Detection** - Finds temp files, browser cache, Windows Update cache, old logs
- ⚠️ **Risk Indicators** - Safe/Low/Medium/High risk levels for each item
- 🗂️ **Quarantine Folder** - Files are moved to **"WinTrim - DELETE ME"** on your Desktop, not deleted outright

### Developer Tools Detection
- 📦 **npm** - node_modules, npm cache
- 📦 **NuGet** - Package cache
- 🐍 **pip** - Python package cache
- ☕ **Maven** - .m2 repository
- 🦀 **Cargo** - Rust package cache
- 📱 **Gradle** - Android/Java build cache

### Data Provided
- 📅 Date last accessed/modified
- 📊 Size of folders and files (human-readable)
- 📍 Full path locations
- 🏷️ File type categorization
- 🎮 Game platform detection (Steam, Epic, GOG, Xbox)

### UI/UX Features
- 🎨 **5 Themes** - Default (Retrofuturistic), Tech (Blade Runner), Enterprise (Light), Terminal Green, Terminal Red
- ⚙️ **Settings Panel** - Font size, treemap colors, treemap depth controls
- 📏 **4 Font Size Presets** - Small, Medium, Large, Extra Large
- ▶️ **Scan Controls** - Start/Stop/Pause with progress tracking
- 📋 **Sortable Data Grids** - Click headers to sort by name, size, date
- 🖱️ **Context Menus** - Right-click to open location or copy path
- 📂 **Quick Actions** - Open in Explorer buttons throughout
- 🔍 **File Explorer Filters** - Search and filter by type, size, age

## 🚀 Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

### Installation

**Option 1: Download Release** (Recommended)
- Visit the [Releases](https://github.com/mobius29er/winTrim/releases) page
- Download for your platform (Windows, macOS, or Linux)

**Option 2: Build from Source**

```bash
# Clone the repository
git clone https://github.com/mobius29er/winTrim.git
cd winTrim

# Build and run
cd WinTrim.Avalonia
dotnet restore
dotnet build
dotnet run
```

**Build Standalone Executables:**

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained true

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained true

# macOS (Intel)
dotnet publish -c Release -r osx-x64 --self-contained true

# Linux
dotnet publish -c Release -r linux-x64 --self-contained true
```

## 📁 Project Structure

```
WinTrim.Avalonia/         # Cross-platform UI (Avalonia)
├── Views/                # AXAML UI files
├── ViewModels/           # MVVM ViewModels
├── Controls/             # Custom controls (TreemapControl)
├── Services/             # Theme service
├── Converters/           # Value converters
└── Themes/               # 6 color themes

WinTrim.Core/             # Shared business logic
├── Models/               # Data models
└── Services/             # Core services
    ├── FileScanner       # Core scanning engine
    ├── GameDetector      # Steam/Epic/GOG/Xbox detection
    ├── DevToolDetector   # Developer cache detection
    ├── CleanupAdvisor    # Cleanup recommendations
    ├── CleanupService    # Execute cleanup operations
    ├── SettingsService   # User preferences & scan caching
    ├── TreemapLayoutService # Squarified treemap algorithm
    └── CategoryClassifier   # File type classification

DiskAnalyzer/             # Legacy WPF version (Windows only)
```

## 🛡️ Safety Features

- **Read-only scanning** - No files are modified during analysis
- **Preview before clean** - Quick Clean shows exactly what will be moved
- **Quarantine instead of delete** - Files are moved to a **"WinTrim - DELETE ME"** folder on your Desktop, giving you a chance to review before permanently deleting
- **Risk levels** for cleanup suggestions (Safe/Low/Medium/High)
- **Graceful error handling** for inaccessible folders
- **Memory efficient** - Processes files in batches

## 🗂️ How File Deletion Works

WinTrim does **not** permanently delete files directly. Instead, it uses a **quarantine (staging) folder** to keep you in control:

1. **Select files** to clean in the Quick Clean dialog and click **"Move to Quarantine"**.
2. WinTrim moves the selected files to a folder called **`WinTrim - DELETE ME`** located on your **Desktop**.
3. A confirmation pop-up shows you exactly where the folder is.
4. **Review** the contents of `WinTrim - DELETE ME` at your own pace.
5. When you are satisfied, **delete the folder yourself** to permanently free the disk space — or move any files back if you change your mind.

> 💡 The quarantine folder is never automatically emptied. You are always the one who decides when files are gone for good.

## 🔧 Technical Details

- **Framework:** .NET 8.0 + Avalonia UI 11.2
- **Platforms:** Windows, macOS, Linux
- **Architecture:** MVVM with CommunityToolkit.Mvvm
- **Charts:** LiveCharts2 (SkiaSharp)
- **Treemap:** Custom SkiaSharp-based squarified treemap with iterative layout algorithm
- **Async:** Full async/await with CancellationToken support
- **Persistence:** JSON-based settings and scan caching
- **Themes:** 6 built-in themes with dynamic resource switching
- **Performance:** Iterative algorithms to prevent stack overflow on large datasets

## 📸 Screenshots

### Dashboard with Category Breakdown
*Pie chart showing storage allocation by file type*

### Treemap Visualization  
*Interactive treemap with double-click drill-down and 5 color schemes*

### Quick Clean Dialog
*Preview and select individual files before cleanup*

### Settings Panel
*Customize font size, treemap colors, and visualization depth*

## 🎨 Themes

| Theme | Description |
|-------|-------------|
| **Default** | Retrofuturistic teal/cyan with orange accents |
| **Tech** | Cyberpunk neon - Cyan/Pink on void black |
| **Enterprise** | Professional Windows-style - Clean blues and grays (light mode) |
| **Terminal Green** | Classic terminal - Green on black |
| **Terminal Red** | Alert terminal - Red on black |

## �️ Roadmap

WinTrim is actively developed! Here's what's coming:

### Near Term
- [ ] Mac App Store release
- [ ] Microsoft Store release
- [ ] Duplicate file detection
- [ ] Browser cache cleanup (Chrome, Firefox, Safari, Edge)
- [ ] System restore point cleanup suggestions

### Future
- [ ] Scheduled scans
- [ ] Custom cleanup rules
- [ ] Cloud storage integration (OneDrive, Dropbox, iCloud)
- [ ] Disk health monitoring
- [ ] Localization (multi-language support)

*Have a feature request? [Open an issue](https://github.com/mobius29er/winTrim/issues)!*

## �📋 Original Requirements

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

## ⚠️ Disclaimer

**USE AT YOUR OWN RISK**

WinTrim is a disk cleanup utility. Please be aware:

- **Quick Clean moves files to a quarantine folder** - Files are placed in **"WinTrim - DELETE ME"** on your Desktop, not deleted immediately
- **You control permanent deletion** - Only when you delete the quarantine folder yourself are files permanently gone
- **Always backup important data** before using cleanup features
- **Review items before deletion** - Use the preview feature to see exactly what will be moved
- **Check risk levels** - Each cleanup suggestion shows Safe/Low/Medium/High risk indicators

This software is provided "AS IS" without warranty of any kind. Foxxception LLC shall not be liable for any data loss or damages arising from the use of this software.

## 📄 License

Functional Source License, Version 1.1 (FSL-1.1-Apache-2.0)

Copyright (c) 2026 Foxxception LLC. All rights reserved.

WinTrim is **source-available** software. You may view, use, and modify the source
code for personal and non-competing purposes. You may **not** use this software to
provide a competing hosted or commercial product.

Two years after each version is first publicly distributed, that version's source
code becomes available under the Apache License, Version 2.0.

See the [LICENSE](./LICENSE) file for full terms. For commercial licensing inquiries,
contact support@foxxception.com.
