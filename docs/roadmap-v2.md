# WinTrim v2.0 Roadmap: Parity with DaisyDisk & Beyond

*Created: February 14, 2026*  
*Target Completion: August 2026*  
*Author: Jeremy Foxx / Foxxception LLC*

---

## Executive Summary

WinTrim is a cross-platform disk analyzer built with .NET 8 + Avalonia UI that already surpasses DaisyDisk in feature breadth (duplicate finder, developer cache detection, game detection, cleanup recommendations with risk levels, 5 themes). However, DaisyDisk — an 18-year-old macOS-only app at $9.99 with 4.7★ (2,676 ratings) and 3x Apple "Best of the Year" — beats WinTrim on **speed**, **polish**, and **ecosystem features** (cloud scanning, admin mode, APFS snapshots, Quick Look, localization).

This roadmap closes those gaps while maintaining full **Windows, macOS, and Linux** compatibility, positioning WinTrim as the best cross-platform disk tool at $9.99.

---

## Current State (v1.0)

### What We Ship Today
| Feature | Status |
|---------|--------|
| Disk scanning (async, parallel, work-stealing) | ✅ |
| Treemap visualization (SkiaSharp, 5 color schemes) | ✅ |
| Duplicate file finder (xxHash64) | ✅ |
| Delete with macOS sandbox support | ✅ |
| Developer cache detection (npm, NuGet, pip, Maven, Cargo, Gradle, Docker) | ✅ |
| Game detection (Steam, Epic, GOG, Xbox) | ✅ |
| Quick Clean with risk indicators (Safe/Low/Med/High) | ✅ |
| 5 themes + 4 font sizes | ✅ |
| Session persistence (auto-save/restore scans) | ✅ |
| Export (CSV, JSON) | ✅ |
| Time Machine analyzer (backend) | ✅ |
| Cross-platform (Windows, macOS, Linux) | ✅ |

### Known Gaps vs DaisyDisk
| Gap | Severity | DaisyDisk Behavior |
|-----|----------|-------------------|
| Scan speed (macOS ~2 min vs ~15 sec) | 🔴 Critical | `getattrlistbulk` kernel-level enumeration |
| App size (~200MB vs ~5MB) | 🔴 Critical | Native Objective-C, no runtime bundled |
| No progressive rendering during scan | 🟡 High | Treemap builds live as scan progresses |
| No file preview (Quick Look) | 🟡 High | Spacebar → instant file preview |
| No cloud storage scanning | 🟡 High | Dropbox, Google Drive, OneDrive, Box |
| No admin/root scanning | 🟡 High | Reveals hidden system files |
| No APFS snapshot UI | 🟡 Medium | Discover & purge local snapshots |
| No localization | 🟡 Medium | Multiple languages |
| No network/external disk browser | 🟠 Low | Shows all connected volumes |
| MainViewModel is 2,717 lines | 🟠 Low | N/A (internal quality) |
| Treemap redraws every frame | 🟠 Low | N/A (internal quality) |

---

## Phase 1: Performance & App Size (Weeks 1–3)

> **Goal:** Match DaisyDisk's perceived speed. Users should feel "instant."

### 1.1 — IL Trimming & App Size Reduction
**Priority:** 🔴 Critical | **Effort:** Low | **Risk:** Medium  
**Platforms:** All

**Problem:** App is self-contained but NOT trimmed. The published bundle is ~150-250MB (full .NET runtime + SkiaSharp + Avalonia + LiveChartsCore). DaisyDisk is 5MB. Users associate large apps with bloat.

**Implementation:**
1. Add to all publish profiles:
   ```xml
   <PublishTrimmed>true</PublishTrimmed>
   <TrimMode>partial</TrimMode>
   ```
2. Start with `partial` (conservative) — only trims assemblies explicitly marked trimmable
3. Test all features after trimming — LiveChartsCore and SkiaSharp may need `<TrimmerRootAssembly>` entries
4. Add `[DynamicallyAccessedMembers]` attributes where reflection is used (CommunityToolkit.Mvvm source generators should be safe)
5. If `partial` works cleanly, try `full` trim mode for maximum reduction
6. Enable `<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>` for Windows (already set) and Linux
7. For macOS `.app` bundles: trim still applies, but no single-file (incompatible with `.app` structure)

**Expected result:** ~60-80MB app size (60-70% reduction)

**Files to modify:**
- `WinTrim.Avalonia/Properties/PublishProfiles/macos-arm64.pubxml`
- `WinTrim.Avalonia/Properties/PublishProfiles/macos-x64.pubxml`
- `WinTrim.Avalonia/Properties/PublishProfiles/win-x64.pubxml`
- Add new: `linux-x64.pubxml`
- `WinTrim.Avalonia/WinTrim.Avalonia.csproj` (add trim compatibility attributes)

**Validation:**
- [ ] All 5 themes load correctly
- [ ] Treemap renders with all color schemes
- [ ] LiveChartsCore pie/bar charts render
- [ ] Duplicate scanner completes successfully
- [ ] Quick Clean dialog opens and functions
- [ ] Export CSV/JSON produces valid output
- [ ] macOS sandbox file access works
- [ ] Game + dev tool detection works

---

### 1.2 — macOS Scan Speed: `getattrlistbulk` P/Invoke
**Priority:** 🔴 Critical | **Effort:** High | **Risk:** Medium  
**Platforms:** macOS (Windows/Linux unchanged)

**Problem:** `FileScanner` uses `Directory.EnumerateFileSystemEntries` which calls `stat()` per file. On a 1TB drive with 1M+ files, that's 1M+ syscalls. DaisyDisk uses `getattrlistbulk` which returns metadata for hundreds of files per syscall.

**Current scanner architecture:**
```
FileScanner.ScanAsync()
  → ParallelBreadthFirstScan() [32 workers on SSD]
    → ProcessDirectory()
      → Directory.EnumerateFileSystemEntries()  ← BOTTLENECK
        → new FileInfo(path)  ← per-file stat() call
          → new FileSystemItem()
```

**Implementation — Platform-abstracted fast enumeration:**

1. Create `IFastFileEnumerator` interface in `WinTrim.Core`:
   ```csharp
   public interface IFastFileEnumerator
   {
       IEnumerable<BulkFileInfo> EnumerateDirectory(string path);
   }
   
   public readonly struct BulkFileInfo
   {
       public string Name { get; init; }
       public string FullPath { get; init; }
       public long Size { get; init; }
       public DateTime LastAccessed { get; init; }
       public DateTime LastModified { get; init; }
       public DateTime Created { get; init; }
       public bool IsDirectory { get; init; }
       public ulong InodeNumber { get; init; }  // for hardlink dedup
       public uint DeviceId { get; init; }       // for mount-point detection
   }
   ```

2. **macOS implementation** — `MacFastFileEnumerator`:
   - P/Invoke to `getattrlistbulk(int dirfd, struct attrlist*, void* buf, size_t bufsize, uint64_t options)`
   - Open directory with `open(path, O_RDONLY)`
   - Request attributes: `ATTR_CMN_NAME | ATTR_CMN_OBJTYPE | ATTR_FILE_TOTALSIZE | ATTR_CMN_ACCTIME | ATTR_CMN_MODTIME | ATTR_CMN_CRTIME | ATTR_CMN_FILEID | ATTR_CMN_DEVID`
   - Allocate 256KB buffer, iterate with `getattrlistbulk` until it returns 0
   - Parse variable-length attribute buffer into `BulkFileInfo` structs
   - Fall back to `Directory.EnumerateFileSystemEntries` on error

3. **Windows implementation** — `WindowsFastFileEnumerator`:
   - Use `NtQueryDirectoryFile` or simply `Directory.EnumerateFileSystemEntries` with `FileInfo` (Windows is already fast with NTFS MFT caching)
   - Future: Consider MFT direct read for extreme speed (matches WizTree)

4. **Linux implementation** — `LinuxFastFileEnumerator`:
   - Use `getdents64` syscall via P/Invoke for bulk directory reading
   - Still needs `stat()` for metadata, but `getdents64` is faster than `readdir()`
   - Alternative: Use `/proc/self/mountinfo` to detect filesystem type, use `btrfs` subvolume queries for btrfs

5. **Inject into FileScanner** via constructor:
   ```csharp
   public FileScanner(
       ICategoryClassifier classifier,
       IGameDetector gameDetector,
       IDevToolDetector devToolDetector,
       ICleanupAdvisor cleanupAdvisor,
       IAppLogger logger,
       IFastFileEnumerator? fastEnumerator = null  // NEW, optional
   )
   ```

6. Modify `ProcessDirectory()` to use `IFastFileEnumerator` when available, falling back to current `Directory.EnumerateFileSystemEntries` code

**Expected result:** macOS scan time from ~2 min → ~20-30 sec for 1TB

**Files to create:**
- `WinTrim.Core/Services/Interfaces/IFastFileEnumerator.cs`
- `WinTrim.Core/Services/MacFastFileEnumerator.cs`
- `WinTrim.Core/Services/WindowsFastFileEnumerator.cs`
- `WinTrim.Core/Services/LinuxFastFileEnumerator.cs`

**Files to modify:**
- `WinTrim.Core/Services/FileScanner.cs` — inject enumerator, modify `ProcessDirectory()`
- `WinTrim.Avalonia/ServiceCollectionExtensions.cs` — register platform-specific enumerator

**Validation:**
- [ ] macOS: Scan 1TB drive in < 30 sec
- [ ] Windows: No regression (should be same speed or faster)
- [ ] Linux: No regression
- [ ] All scan results identical (file counts, sizes, categories)
- [ ] Inode dedup still works with new enumerator
- [ ] Mount-point boundary detection still works
- [ ] Game/dev tool detection still fires during scan

---

### 1.3 — Work-Stealing Loop Optimization
**Priority:** 🟡 Medium | **Effort:** Low | **Risk:** Low  
**Platforms:** All

**Problem:** The parallel scan work-stealing loop uses `Thread.Sleep(1)` for idle workers, which introduces unnecessary latency and CPU spin.

**Implementation:**
1. Replace `Thread.Sleep(1)` with `ManualResetEventSlim`:
   ```csharp
   private ManualResetEventSlim _workAvailable = new(false);
   
   // In work-stealing loop (idle path):
   _workAvailable.Wait(10, cancellationToken);  // 10ms timeout
   
   // When enqueueing work:
   _workQueue.Enqueue(directory);
   _workAvailable.Set();
   ```
2. This eliminates CPU spin while maintaining responsiveness

**Files to modify:**
- `WinTrim.Core/Services/FileScanner.cs` — `ParallelBreadthFirstScan()` method

---

### 1.4 — Lazy Hierarchy Building
**Priority:** 🟡 Medium | **Effort:** Medium | **Risk:** Low  
**Platforms:** All

**Problem:** `BuildHierarchy()` runs as a synchronous tree walk after all parallel scanning completes. On large drives, this adds seconds before the user sees results.

**Implementation:**
1. Move `BuildHierarchy()` off the scan hot path
2. Build hierarchy lazily on first treemap render or tree view expansion
3. Show scan results (largest files, categories, cleanup) immediately after scan
4. Treemap requests hierarchy → triggers background build → renders when ready

**Files to modify:**
- `WinTrim.Core/Services/FileScanner.cs` — make `BuildHierarchy()` callable separately
- `WinTrim.Avalonia/ViewModels/MainWindowViewModel.cs` — trigger hierarchy build before treemap render

---

## Phase 2: Visual Polish & UX (Weeks 3–5)

> **Goal:** Make WinTrim *feel* premium. Smooth, responsive, delightful.

### 2.1 — Treemap Dirty-Flag Rendering
**Priority:** 🟡 High | **Effort:** Low | **Risk:** Low  
**Platforms:** All

**Problem:** `TreemapControl.NeedsRendering` always returns `false` (inverted logic or unimplemented), causing the treemap to redraw every frame. This wastes CPU/GPU and drains battery on laptops.

**Implementation:**
1. Add a `_isDirty` flag to `TreemapControl`
2. Set `_isDirty = true` when: source data changes, depth changes, color scheme changes, size changes > 1px
3. In the render callback, only redraw when `_isDirty` is true; otherwise return cached bitmap
4. Consider double-buffering: render to off-screen `SKBitmap`, blit to screen on clean frames

**Expected result:** 90%+ reduction in unnecessary redraws, smoother scrolling, lower CPU usage

**Files to modify:**
- `WinTrim.Avalonia/Controls/TreemapControl.cs` — render loop, add dirty flag + bitmap cache

---

### 2.2 — Progressive Treemap Rendering During Scan
**Priority:** 🟡 High | **Effort:** High | **Risk:** Medium  
**Platforms:** All

**Problem:** Users stare at a progress bar during the entire scan. DaisyDisk shows the treemap building live as files are discovered, which feels dramatically faster even if total scan time is similar.

**Implementation:**
1. During scan, emit partial `FileSystemItem` trees every 2-3 seconds via a new progress channel
2. TreemapControl accepts partial data and re-layouts incrementally
3. Use a throttled `IProgress<ScanResult>` that sends snapshots:
   ```csharp
   // In FileScanner - new partial result emitter
   public IProgress<ScanResult>? PartialResultCallback { get; set; }
   
   // Every 2-3 sec during scan:
   if (partialTimer.Elapsed > TimeSpan.FromSeconds(2.5))
   {
       var partial = BuildPartialResult();
       PartialResultCallback?.Report(partial);
       partialTimer.Restart();
   }
   ```
4. ViewModel subscribes and updates treemap source on UI thread
5. Treemap shows "Scanning…" overlay with live visualization underneath

**Marketing value:** This is DaisyDisk's signature UX moment. Implementing it makes WinTrim feel alive.

**Files to modify:**
- `WinTrim.Core/Services/FileScanner.cs` — add partial result emission
- `WinTrim.Core/Services/IFileScanner.cs` — add partial result callback
- `WinTrim.Avalonia/ViewModels/MainWindowViewModel.cs` — subscribe to partial results
- `WinTrim.Avalonia/Controls/TreemapControl.cs` — accept partial data gracefully

---

### 2.3 — Animations & Transitions
**Priority:** 🟠 Medium | **Effort:** Medium | **Risk:** Low  
**Platforms:** All

**Problem:** UI transitions are instant/jarring. Premium apps have smooth transitions.

**Implementation:**
1. **Tab transitions** — fade or slide when switching between tabs (Treemap, Files, Duplicates, etc.)
   - Avalonia supports `PageSlide`, `CrossFade` transitions on `TransitioningContentControl`
2. **Scan progress** — animate the progress bar smoothly (already partially done)
3. **Treemap drill-down** — zoom animation when double-clicking a folder:
   - Capture current render as bitmap
   - Animate scale + position to zoom into clicked tile
   - Cross-fade to new treemap at target depth
4. **Panel open/close** — settings panel, collector panel slide in/out
5. **Button hover states** — subtle scale or glow effects

**Implementation approach (Avalonia):**
```xml
<!-- In Styles.axaml -->
<Style Selector="Button">
    <Style.Animations>
        <Animation Duration="0:0:0.15">
            <KeyFrame Cue="0%"><Setter Property="Opacity" Value="1"/></KeyFrame>
            <KeyFrame Cue="100%"><Setter Property="Opacity" Value="0.8"/></KeyFrame>
        </Animation>
    </Style.Animations>
</Style>
```

**Files to modify:**
- `WinTrim.Avalonia/Themes/Styles.axaml` — add global transitions
- `WinTrim.Avalonia/Views/MainWindow.axaml` — add `TransitioningContentControl` for tab content
- `WinTrim.Avalonia/Controls/TreemapControl.cs` — drill-down animation

---

### 2.4 — Quick Look / File Preview
**Priority:** 🟡 High | **Effort:** Medium | **Risk:** Low  
**Platforms:** macOS (primary), Windows (bonus), Linux (bonus)

**Problem:** DaisyDisk lets users press Spacebar to instantly preview any file. WinTrim requires opening Finder/Explorer.

**Implementation:**

| Platform | Method | Command |
|----------|--------|---------|
| **macOS** | `qlmanage -p <path>` | Shell out to Quick Look CLI |
| **Windows** | `Process.Start(path)` with preview verb, or embed `PreviewHandler` COM | Medium complexity |
| **Linux** | `xdg-open <path>` or `gnome-sushi` if available | Best effort |

1. Add to `IPlatformService`:
   ```csharp
   Task PreviewFileAsync(string path);
   ```
2. macOS: `Process.Start("qlmanage", $"-p \"{path}\"")` — opens native Quick Look panel
3. Bind to Spacebar key in treemap, file list, and duplicate views
4. Also add "Preview" to all context menus

**Files to create:**
- None (add to existing `IPlatformService` implementations)

**Files to modify:**
- `WinTrim.Core/Services/Interfaces/IPlatformService.cs` (if exists) or appropriate platform service
- `WinTrim.Core/Services/MacPlatformService.cs`
- `WinTrim.Core/Services/WindowsPlatformService.cs`
- `WinTrim.Avalonia/Controls/TreemapControl.cs` — keyboard handler
- `WinTrim.Avalonia/Views/MainWindow.axaml` — context menu additions + key bindings

---

## Phase 3: Feature Parity (Weeks 5–9)

> **Goal:** Close every feature gap with DaisyDisk.

### 3.1 — Cloud Storage Folder Detection
**Priority:** 🟡 High | **Effort:** Low | **Risk:** Low  
**Platforms:** All

**Problem:** DaisyDisk scans Dropbox, Google Drive, OneDrive, Box. These all sync to local folders.

**Implementation:**
1. Create `CloudStorageDetector` service:
   ```csharp
   public class CloudStorageDetector
   {
       public List<CloudFolder> DetectCloudFolders()
       {
           var folders = new List<CloudFolder>();
           
           // macOS
           ScanPath("~/Library/CloudStorage", folders);          // iCloud, Google, OneDrive, Dropbox, Box
           ScanPath("~/Dropbox", folders);                        // Legacy Dropbox
           ScanPath("~/Google Drive", folders);                   // Legacy Google Drive
           ScanPath("~/OneDrive", folders);                       // Legacy OneDrive
           
           // Windows
           ScanPath(Environment.GetFolderPath(SpecialFolder.UserProfile) + "/Dropbox", folders);
           ScanPath(Environment.GetFolderPath(SpecialFolder.UserProfile) + "/OneDrive", folders);
           ScanPath(Environment.GetFolderPath(SpecialFolder.UserProfile) + "/Google Drive", folders);
           // Also check registry: HKCU\Software\Dropbox, HKCU\Software\Google\DriveFS
           
           // Linux
           ScanPath("~/Dropbox", folders);
           ScanPath("~/.local/share/Google/DriveFS", folders);
           
           return folders;
       }
   }
   ```
2. Add cloud folder icons/labels in the category breakdown
3. Show cloud folders as a filterable category in the file list
4. Add "Cloud Storage" section to scan results

**Files to create:**
- `WinTrim.Core/Services/CloudStorageDetector.cs`
- `WinTrim.Core/Models/CloudFolder.cs`

**Files to modify:**
- `WinTrim.Core/Services/FileScanner.cs` — detect cloud folders during scan
- `WinTrim.Core/Models/ScanResult.cs` — add cloud folder results
- `WinTrim.Avalonia/ViewModels/MainWindowViewModel.cs` — display cloud info

---

### 3.2 — Admin/Root Scanning Mode
**Priority:** 🟡 Medium | **Effort:** Medium | **Risk:** Medium  
**Platforms:** All (different mechanisms per OS)

**Problem:** DaisyDisk can scan as administrator to reveal hidden/system files and purgeable space. WinTrim only scans user-accessible files.

**Implementation:**

| Platform | Elevation Method | Details |
|----------|-----------------|---------|
| **macOS** | `osascript -e 'do shell script "command" with administrator privileges'` | Opens native password dialog |
| **macOS (App Store)** | Cannot elevate from sandbox | Show informational message; suggest using non-App Store version for admin scan |
| **Windows** | `ProcessStartInfo.Verb = "runas"` | UAC prompt, relaunch app elevated |
| **Linux** | `pkexec` or `sudo -A` with `SSH_ASKPASS` | Graphical sudo prompt |

1. Add "Scan as Administrator" button/menu item
2. On click: relaunch the scan process with elevated privileges
3. For macOS non-sandboxed: use `osascript` to run the scan helper with admin rights
4. Show additional system files that were previously inaccessible
5. Mark elevated scan results with a badge/indicator

**Compatibility note:** The macOS App Store build CANNOT use admin scanning (sandbox restriction). The direct-download version can. Consider offering both distribution channels.

**Files to create:**
- `WinTrim.Core/Services/Interfaces/IElevationService.cs`
- `WinTrim.Core/Services/MacElevationService.cs`
- `WinTrim.Core/Services/WindowsElevationService.cs`
- `WinTrim.Core/Services/LinuxElevationService.cs`

**Files to modify:**
- `WinTrim.Avalonia/Views/MainWindow.axaml` — add admin scan button
- `WinTrim.Avalonia/ViewModels/MainWindowViewModel.cs` — admin scan command

---

### 3.3 — APFS Snapshot Management UI
**Priority:** 🟡 Medium | **Effort:** Medium | **Risk:** Low  
**Platforms:** macOS only (hidden on Windows/Linux)

**Problem:** `TimeMachineAnalyzer` already has robust backend logic (632 lines) but it's not surfaced in the UI. DaisyDisk shows APFS snapshots and lets users purge them.

**Current backend capabilities (already built):**
- ✅ Parse `tmutil destinationinfo` (local + network)
- ✅ List backups with dates
- ✅ Scan latest backup for large files
- ✅ 18+ exclusion suggestions with risk levels
- ✅ Add exclusions via `tmutil addexclusion`
- ✅ Detect VM images, large node_modules

**What needs to be added:**

| Feature | Implementation | Effort |
|---------|---------------|--------|
| List local APFS snapshots | `tmutil listlocalsnapshots /` → parse dates | Low |
| Show snapshot sizes | `diskutil apfs listSnapshots disk1s1` → parse size | Low |
| Delete individual snapshots | `tmutil deletelocalsnapshots <date>` (requires admin) | Low |
| Show purgeable space | `diskutil apfs list` → parse `FileVault: Yes/No`, `Purgeable: X GB` | Low |
| UI panel for snapshots | New tab/section in MainWindow | Medium |
| Surface TM exclusion suggestions | Connect existing suggestion engine to UI | Medium |

**Files to modify:**
- `WinTrim.Core/Services/TimeMachineAnalyzer.cs` — add `ListLocalSnapshots()`, `GetPurgeableSpace()`, `DeleteSnapshot()`
- `WinTrim.Avalonia/Views/MainWindow.axaml` — add Snapshots/Time Machine section
- `WinTrim.Avalonia/ViewModels/MainWindowViewModel.cs` — TM commands + snapshot display

---

### 3.4 — Volume/Disk Browser
**Priority:** 🟠 Low | **Effort:** Medium | **Risk:** Low  
**Platforms:** All

**Problem:** DaisyDisk shows all connected disks (internal, external, network) with capacity bars. WinTrim uses a simple drive dropdown.

**Implementation:**
1. Create a disk browser panel (sidebar or header widget):
   ```
   ┌─────────────────────────────────────┐
   │ 💾 Macintosh HD    450GB / 1TB  [▓▓▓▓▓░░░░░] │
   │ 💾 External SSD    180GB / 500GB [▓▓▓░░░░░░░] │
   │ 🌐 NAS Backup      2.1TB / 4TB  [▓▓▓▓▓░░░░░] │
   │ ☁️ iCloud Drive    12GB / 50GB   [▓▓░░░░░░░░] │
   └─────────────────────────────────────┘
   ```
2. Auto-detect volumes:
   - **macOS:** `DriveInfo.GetDrives()` + parse `/Volumes/*` + cloud folders
   - **Windows:** `DriveInfo.GetDrives()` (includes mapped network drives)
   - **Linux:** Parse `/proc/mounts` or `df -h`
3. Click a disk → starts scanning that volume
4. Show eject button for removable media (`diskutil eject` / `udisksctl unmount`)
5. Real-time capacity updates (poll every 30 sec)

**Files to create:**
- `WinTrim.Core/Services/VolumeDetector.cs`
- `WinTrim.Core/Models/VolumeInfo.cs`
- `WinTrim.Avalonia/Controls/DiskBrowserControl.axaml` + `.cs`

---

## Phase 4: Localization (Weeks 9–11)

> **Goal:** Support 5+ languages for international App Store presence.

### 4.1 — Localization Infrastructure
**Priority:** 🟡 Medium | **Effort:** High (initial setup) | **Risk:** Low  
**Platforms:** All

**Problem:** All strings are hardcoded. DaisyDisk supports multiple languages. The Mac App Store serves 175 countries — localization directly impacts downloads.

**Implementation approach — .resx resource files:**

1. Create resource infrastructure:
   ```
   WinTrim.Core/
     Resources/
       Strings.resx              (English - default)
       Strings.es.resx           (Spanish)
       Strings.fr.resx           (French)
       Strings.de.resx           (German)
       Strings.ja.resx           (Japanese)
       Strings.zh-Hans.resx      (Simplified Chinese)
       Strings.pt-BR.resx        (Portuguese - Brazil)
   ```

2. Create `ILocalizationService`:
   ```csharp
   public interface ILocalizationService
   {
       string Get(string key);
       string Get(string key, params object[] args);
       CultureInfo CurrentCulture { get; set; }
       event Action CultureChanged;
   }
   ```

3. Replace all hardcoded strings systematically:
   - UI labels in `.axaml` files → use `{x:Static}` or custom markup extension
   - Status messages in ViewModels → inject `ILocalizationService`
   - Category names, risk levels, cleanup descriptions in Core services

4. Add language selector in Settings panel

**String count estimate:** ~200-300 translatable strings across UI + services

**Priority languages (by Mac App Store revenue):**
1. 🇺🇸 English (default)
2. 🇪🇸 Spanish (largest non-English market)
3. 🇫🇷 French
4. 🇩🇪 German
5. 🇯🇵 Japanese (high App Store spend)
6. 🇨🇳 Chinese Simplified
7. 🇧🇷 Portuguese (Brazil)

**Community translation strategy:** Once infrastructure is in place, publish a translation guide and accept PRs for additional languages. MIT license makes community contributions easy.

---

## Phase 5: Architecture & Code Quality (Ongoing)

> **Goal:** Ensure long-term maintainability and stability.

### 5.1 — Split MainWindowViewModel
**Priority:** 🟠 Medium | **Effort:** Medium | **Risk:** Medium  
**Platforms:** All

**Problem:** `MainWindowViewModel.cs` is 2,717 lines with 20+ commands, nested helper classes, and manages scanning, duplicates, collector, exports, settings, and Time Machine — all in one file. This is a "god ViewModel" anti-pattern that makes debugging and feature additions risky.

**Implementation — Partial class split:**

```
ViewModels/
  MainWindowViewModel.cs              (core properties, construction, DI — ~300 lines)
  MainWindowViewModel.Scan.cs         (scan commands, progress, drive selection — ~500 lines)
  MainWindowViewModel.Duplicates.cs   (duplicate scan, display, management — ~400 lines)
  MainWindowViewModel.Collector.cs    (collector/staging, delete operations — ~300 lines)
  MainWindowViewModel.Export.cs       (CSV, JSON export — ~200 lines)
  MainWindowViewModel.Settings.cs     (settings panel, themes, font sizes — ~200 lines)
  MainWindowViewModel.TimeMachine.cs  (TM analysis, snapshots — ~300 lines)
  MainWindowViewModel.Helpers.cs      (DriveDisplayItem, DuplicateDisplayRow, nested types — ~200 lines)
```

All files use `partial class MainWindowViewModel` — no behavior change, just organization. CommunityToolkit.Mvvm source generators work with partial classes.

**Approach:**
1. Do NOT change any logic — pure file reorganization
2. Move related `[RelayCommand]` methods + their backing fields together
3. Move nested classes (`DriveDisplayItem`, `DuplicateDisplayRow`, `ExportFileInfo`) to `Helpers.cs`
4. Run full test pass after split to verify zero behavior change

---

### 5.2 — Unit Test Foundation
**Priority:** 🟠 Medium | **Effort:** Medium | **Risk:** Low  
**Platforms:** All

**Problem:** No test project exists. As features grow, regressions become likely.

**Implementation:**
1. Create `WinTrim.Tests` project (xUnit + Moq)
2. Priority test targets:
   - `FileScanner` — mock filesystem, verify file counts, category classification
   - `DuplicateScanner` — verify hash-based dedup logic
   - `CategoryClassifier` — verify file extension → category mapping
   - `CleanupAdvisor` — verify risk level assignments
   - `TimeMachineAnalyzer` — verify `tmutil` output parsing
   - `ExportService` — verify CSV/JSON output format
3. Target: 70%+ coverage on Core services

---

## Execution Timeline

```
Week 1-2  ┃ Phase 1.1: IL Trimming (Low effort, immediate impact)
          ┃ Phase 1.3: Work-stealing optimization (Low effort)
          ┃ Phase 5.1: Split MainViewModel (reduces risk for all other changes)
          ┃
Week 2-3  ┃ Phase 1.2: getattrlistbulk P/Invoke (High effort, critical)
          ┃ Phase 1.4: Lazy hierarchy building
          ┃
Week 3-4  ┃ Phase 2.1: Treemap dirty-flag rendering (Low effort)
          ┃ Phase 2.4: Quick Look / file preview (Medium effort)
          ┃ Phase 3.1: Cloud storage detection (Low effort)
          ┃
Week 4-5  ┃ Phase 2.2: Progressive treemap rendering (High effort)
          ┃ Phase 2.3: Animations & transitions
          ┃
Week 5-7  ┃ Phase 3.3: APFS Snapshot UI (connect existing backend)
          ┃ Phase 3.2: Admin scanning mode
          ┃ Phase 5.2: Unit test foundation
          ┃
Week 7-9  ┃ Phase 3.4: Volume/disk browser
          ┃ Phase 4.1: Localization infrastructure + English extraction
          ┃
Week 9-11 ┃ Phase 4.1: Translations (Spanish, French, German, Japanese)
          ┃ Polish, bug fixes, App Store submission prep
          ┃
Week 12   ┃ 🚀 v2.0 Release
```

---

## Cross-Platform Compatibility Matrix

Every feature is designed to work on all platforms or gracefully degrade:

| Feature | Windows | macOS | Linux | Notes |
|---------|---------|-------|-------|-------|
| IL Trimming | ✅ | ✅ | ✅ | Test all platforms |
| `getattrlistbulk` speed | N/A (already fast) | ✅ | N/A | Platform-specific P/Invoke |
| `getdents64` speed | N/A | N/A | ✅ | Platform-specific P/Invoke |
| Work-stealing fix | ✅ | ✅ | ✅ | Shared code |
| Lazy hierarchy | ✅ | ✅ | ✅ | Shared code |
| Treemap dirty-flag | ✅ | ✅ | ✅ | Shared code |
| Progressive rendering | ✅ | ✅ | ✅ | Shared code |
| Animations | ✅ | ✅ | ✅ | Avalonia handles |
| Quick Look preview | Shell `start` | `qlmanage -p` | `xdg-open` | Platform service |
| Cloud folder detection | Registry + paths | `~/Library/CloudStorage` | `~/Dropbox` etc. | Platform paths |
| Admin scanning | UAC `runas` | `osascript` | `pkexec` | Platform service |
| APFS Snapshots | Hidden | ✅ `tmutil` | Hidden | macOS only |
| Volume browser | `DriveInfo` | `/Volumes` | `/proc/mounts` | Platform detection |
| Localization | ✅ | ✅ | ✅ | Shared `.resx` |
| ViewModel split | ✅ | ✅ | ✅ | Shared code |

---

## Marketing Positioning Post-v2.0

### Tagline Options
- *"The disk analyzer for developers, gamers, and power users."*
- *"See everything. Clean safely. Every platform."*
- *"DaisyDisk features. Every platform. One price."*

### Key Differentiators to Emphasize
1. **Cross-platform** — "The only disk analyzer for Windows, macOS, AND Linux"
2. **Developer-first** — "Finds your `node_modules`, NuGet cache, Docker images, and Cargo builds"
3. **Gamer-friendly** — "Auto-detects Steam, Epic, GOG, and Xbox games with last-played dates"
4. **Smart cleanup** — "Risk-rated recommendations tell you what's safe to delete"
5. **Duplicate finder** — "Find and remove duplicate files with one click"
6. **Open source** — "MIT licensed. No telemetry. No subscriptions."

### Pricing Strategy
| Channel | Price | Rationale |
|---------|-------|-----------|
| Mac App Store | $9.99 | Matches DaisyDisk, undercuts CleanMyMac ($89.95) |
| Microsoft Store | $9.99 | Undercuts CCleaner Pro ($29.95/yr), WizTree ($20) |
| Direct download | Free (open source) | Drives adoption, GitHub stars, community contributions |
| GitHub Sponsors | Voluntary | Tip jar for power users who want to support development |

### App Store Optimization (ASO) Keywords
- disk analyzer, disk cleaner, storage manager, duplicate finder
- cleanup tool, disk space, storage optimizer, file manager
- developer tools, npm cleanup, docker cleanup, game manager
- treemap, disk usage, storage visualization

### App Store Screenshots (update for v2.0)
1. Live treemap scanning (progressive rendering)
2. Duplicate file finder with delete
3. Developer cache detection (node_modules, NuGet, Docker)
4. Quick Clean with risk indicators
5. Time Machine / APFS snapshot management (macOS)
6. Multiple themes showcase
7. Cloud storage detection
8. Cross-platform comparison (Win/Mac/Linux side by side)

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| IL Trimming breaks LiveChartsCore | Medium | High | Use `partial` trim mode first; add `TrimmerRootAssembly` for reflection-heavy libs |
| `getattrlistbulk` P/Invoke crashes | Medium | High | Wrap in try/catch, fall back to `Directory.Enumerate`; test on macOS 12-15 |
| Progressive rendering causes UI jank | Medium | Medium | Throttle updates to every 2-3 sec; use background thread for layout |
| Admin scanning rejected by App Store review | High | Medium | Only enable in direct-download build; App Store version shows informational message |
| Localization doubles string maintenance burden | Low | Medium | Use tooling (`ResXManager`) for string sync; community translations |
| ViewModel split introduces regressions | Low | High | Pure file reorganization, no logic changes; manual QA pass on all features |

---

## Success Metrics

| Metric | Current | v2.0 Target |
|--------|---------|-------------|
| macOS scan speed (1TB) | ~2 min | < 30 sec |
| App bundle size (macOS) | ~200MB | < 80MB |
| Treemap FPS (idle) | Low (constant redraw) | 60 FPS (dirty-flag) |
| Supported languages | 1 | 5+ |
| Mac App Store rating | N/A | 4.5+ ★ |
| MainViewModel lines | 2,717 | ~300 (core) + partials |
| Unit test coverage | 0% | 70%+ (Core) |

---

*This is a living document. Update as phases complete and priorities shift.*
