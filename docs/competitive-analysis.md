# WinTrim Competitive Analysis

*Last Updated: January 2026*

## Executive Summary

WinTrim is a cross-platform disk analyzer built with .NET 8 + Avalonia UI. This document compares WinTrim against major competitors in the disk cleanup and system optimization space.

---

## 🏆 Competitor Overview

| Product | Company | Primary Platform | Pricing | Open Source |
|---------|---------|------------------|---------|-------------|
| **WinTrim** | (You) | Windows, macOS, Linux | Free | ✅ Yes (MIT) |
| **CleanMyMac X** | MacPaw | macOS only | $39.95/yr or $89.95 lifetime | ❌ No |
| **CCleaner** | Gen Digital | Windows, Mac, Android, iOS | Free / $29.95/yr Pro | ❌ No |
| **WizTree** | Antibody Software | Windows only | Free (personal) / $20 Pro | ❌ No |
| **WinDirStat** | Open Source | Windows only | Free | ✅ Yes (GPL) |
| **Intego Mac Washing Machine** | Intego | macOS only | $29.99/yr (bundle $69.99) | ❌ No |

---

## 📊 Feature Comparison Matrix

### Core Disk Analysis Features

| Feature | WinTrim | CleanMyMac | CCleaner | WizTree | WinDirStat |
|---------|:-------:|:----------:|:--------:|:-------:|:----------:|
| **Disk Scanning** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Treemap Visualization** | ✅ | ❌ | ❌ | ✅ | ✅ |
| **Largest Files Finder** | ✅ (Top 50) | ✅ | ❌ | ✅ | ✅ |
| **Folder Size Analysis** | ✅ | ✅ | ❌ | ✅ | ✅ |
| **Category Breakdown** | ✅ (Pie charts) | ✅ | ❌ | ❌ | ✅ (by extension) |
| **File Age Analysis** | ✅ (90+ days) | ✅ | ❌ | ❌ | ❌ |
| **Duplicate File Finder** | 🔜 Planned | ✅ | ✅ (Pro) | ❌ | ❌ |
| **MFT Direct Read (NTFS)** | ❌ | N/A | ❌ | ✅ | ❌ |

### Cleanup & Optimization Features

| Feature | WinTrim | CleanMyMac | CCleaner | WizTree | WinDirStat |
|---------|:-------:|:----------:|:--------:|:-------:|:----------:|
| **Cleanup Recommendations** | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Quick Clean (One-click)** | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Risk Level Indicators** | ✅ (Safe/Low/Med/High) | ⚠️ Limited | ❌ | ❌ | ❌ |
| **Preview Before Delete** | ✅ | ✅ | ✅ | ❌ | ❌ |
| **File-Level Selection** | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Browser Cache Cleanup** | ✅ | ✅ | ✅ | ❌ | ❌ |
| **System Cache Cleanup** | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Temp Files Cleanup** | ✅ | ✅ | ✅ | ❌ | ❌ |

### Developer-Focused Features

| Feature | WinTrim | CleanMyMac | CCleaner | WizTree | WinDirStat |
|---------|:-------:|:----------:|:--------:|:-------:|:----------:|
| **npm/node_modules Detection** | ✅ | ❌ | ❌ | ❌ | ❌ |
| **NuGet Cache Detection** | ✅ | ❌ | ❌ | ❌ | ❌ |
| **pip Cache Detection** | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Maven .m2 Detection** | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Cargo (Rust) Detection** | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Gradle Cache Detection** | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Xcode DerivedData (Mac)** | ✅ | ✅ | ❌ | N/A | N/A |
| **Docker Images Detection** | ✅ | ❌ | ❌ | ❌ | ❌ |

### Gaming Features

| Feature | WinTrim | CleanMyMac | CCleaner | WizTree | WinDirStat |
|---------|:-------:|:----------:|:--------:|:-------:|:----------:|
| **Steam Detection** | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Epic Games Detection** | ✅ | ❌ | ❌ | ❌ | ❌ |
| **GOG Detection** | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Xbox/Microsoft Store** | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Game Size Breakdown** | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Last Played Tracking** | ✅ | ❌ | ❌ | ❌ | ❌ |

### Platform Support

| Platform | WinTrim | CleanMyMac | CCleaner | WizTree | WinDirStat |
|----------|:-------:|:----------:|:--------:|:-------:|:----------:|
| **Windows 10/11** | ✅ | ❌ | ✅ | ✅ | ✅ |
| **macOS** | ✅ (Avalonia) | ✅ | ✅ | ❌ | ❌ |
| **Linux** | ✅ (Avalonia) | ❌ | ❌ | ❌ | ❌ |
| **Android** | ❌ | ❌ | ✅ | ❌ | ❌ |
| **iOS** | ❌ | ❌ | ✅ | ❌ | ❌ |

### UI/UX Features

| Feature | WinTrim | CleanMyMac | CCleaner | WizTree | WinDirStat |
|---------|:-------:|:----------:|:--------:|:-------:|:----------:|
| **Multiple Themes** | ✅ (5 themes) | ✅ (Dark/Light) | ❌ | ❌ | ❌ |
| **Font Size Settings** | ✅ (4 sizes) | ❌ | ❌ | ❌ | ❌ |
| **Sortable DataGrids** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Context Menus** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Drill-down Navigation** | ✅ | ✅ | ❌ | ✅ | ✅ |
| **Progress Tracking** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Start/Stop/Pause** | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Session Persistence** | ✅ | ✅ | ✅ | ❌ | ❌ |

### Additional Features (CCleaner/CleanMyMac exclusive)

| Feature | WinTrim | CleanMyMac | CCleaner |
|---------|:-------:|:----------:|:--------:|
| **Malware Scanning** | ❌ | ✅ | ❌ |
| **App Uninstaller** | ❌ | ✅ | ✅ (Pro) |
| **Registry Cleaner** | ❌ | N/A | ✅ |
| **Driver Updater** | ❌ | ❌ | ✅ (Pro) |
| **Startup Manager** | ❌ | ✅ | ✅ |
| **Privacy Cleaner** | ❌ | ✅ | ✅ |
| **Performance Optimizer** | ❌ | ✅ | ✅ (Pro) |
| **Cloud Storage Cleanup** | ❌ | ✅ | ✅ (Pro) |
| **File Recovery** | ❌ | ❌ | ✅ (Pro) |

---

## 💰 Pricing Comparison

| Product | Free Tier | Pro/Premium | Lifetime |
|---------|-----------|-------------|----------|
| **WinTrim** | ✅ Full features | N/A (Free) | Free (Open Source) |
| **CleanMyMac X** | Trial only | $39.95/year | $89.95 one-time |
| **CCleaner** | Basic (limited) | $29.95/year | Not available |
| **WizTree** | Personal use | $20 (supporter) | $20 one-time |
| **WinDirStat** | ✅ Full features | N/A (Free) | Free (GPL) |
| **Intego Mac Washing Machine** | Trial only | $29.99/year | Not available |

---

## ⚡ Performance Comparison

| Metric | WinTrim | WizTree | WinDirStat | CleanMyMac |
|--------|---------|---------|------------|------------|
| **Scan Speed (1TB) - Windows** | ~20-30 sec | ~10 sec* | ~5-10 min | N/A |
| **Scan Speed (1TB) - macOS** | ~2 min | N/A | N/A | ~60-120 sec |
| **Scan Speed (1TB) - Linux** | ~60-90 sec | N/A | N/A | N/A |
| **Memory Usage** | Moderate | Low | Moderate | High |
| **MFT Direct Read** | ❌ | ✅ | ❌ | ❌ |
| **Parallel Processing** | ✅ (32 workers) | ✅ | ❌ | ✅ |

*WizTree is exceptionally fast due to direct MFT access on NTFS drives

---

## 🎯 WinTrim Unique Advantages

### 1. **Cross-Platform Native App**
- Only disk analyzer that runs natively on Windows, macOS, AND Linux
- Built with Avalonia UI for consistent experience across platforms

### 2. **Developer Tools Detection**
- **Unique feature**: Detects npm, NuGet, pip, Maven, Cargo, Gradle caches
- No competitor offers this level of developer-focused cleanup
- Can save 10-50GB+ for active developers

### 3. **Gaming Platform Detection**
- Steam, Epic, GOG, Xbox game detection
- Shows game sizes and last played dates
- Helps identify games to uninstall

### 4. **Risk-Based Cleanup**
- Safe/Low/Medium/High risk indicators
- No competitor provides this granular safety information
- Users can make informed decisions

### 5. **5 Built-in Themes**
- Retrofuturistic (Default), Tech (Cyberpunk), Enterprise, Terminal Green/Red
- Font size customization (4 presets)
- No disk analyzer offers this level of theming

### 6. **Open Source & Free**
- MIT License - truly free forever
- No subscription, no upsells, no data collection
- Community can contribute and audit code

### 7. **Session Persistence**
- Automatically saves/restores last scan
- Resume where you left off
- Faster subsequent analysis

---

## 📉 WinTrim Gaps vs Competitors

### vs CleanMyMac X
| Gap | Priority | Difficulty |
|-----|----------|------------|
| Malware scanning | Low | High |
| App uninstaller with leftovers | Medium | Medium |
| Cloud storage cleanup | Low | Medium |
| Similar photo detection | Low | High |

### vs CCleaner
| Gap | Priority | Difficulty |
|-----|----------|------------|
| Registry cleaner | Low | Medium (Windows-only) |
| Startup manager | Medium | Medium |
| Browser history cleanup | Medium | Low |
| Driver updater | Low | High |

### vs WizTree
| Gap | Priority | Difficulty |
|-----|----------|------------|
| MFT direct read (NTFS) | High | High |
| Ultra-fast scanning | High | High |

---

## 🎯 Strategic Recommendations

### Short-term (Next 3 months)
1. **Duplicate file finder** - High demand feature
2. **Browser history/cookie cleanup** - Easy win
3. **Startup manager** - Medium effort, high value
4. **Export reports** (CSV, PDF) - Easy differentiation

### Medium-term (3-6 months)
1. **MFT direct read** for NTFS (Windows) - Match WizTree speed
2. **Similar photo detection** - Unique for cross-platform
3. **Scheduled cleanup** - Automation feature
4. **Cloud storage analysis** (Google Drive, OneDrive, iCloud)

### Long-term (6-12 months)
1. **Plugin system** - Community extensions
2. **Enterprise features** - Multi-machine deployment
3. **Mobile companion app** - Remote storage monitoring

---

## 🏁 Competitive Position Summary

```
                    FEATURE RICHNESS
                         ▲
                         │
    CleanMyMac ●         │         
                         │    
           CCleaner ●    │    ● WinTrim (Target)
                         │         
                         │    ● WinTrim (Current)
                         │
    ─────────────────────┼─────────────────────► CROSS-PLATFORM
                         │
                         │    ● WizTree
    WinDirStat ●         │
                         │
                         │
```

### WinTrim's Sweet Spot
- **Niche**: Developers & Gamers who need cross-platform disk analysis
- **Differentiator**: Only tool with developer cache + game detection + cross-platform
- **Advantage**: Free, open source, no bloat, focused functionality

---

## 📋 Action Items

- [ ] Add duplicate file finder
- [ ] Implement browser cleanup
- [ ] Add startup manager (Windows/Mac)
- [ ] Investigate MFT direct read for Windows
- [ ] Add export functionality
- [ ] Create comparison landing page for marketing

---

*Document maintained by WinTrim team*
