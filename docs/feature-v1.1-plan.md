# WinTrim v1.1 Feature Implementation Plan

## Executive Summary

This document outlines the implementation plan for two major features:
1. **Delete Functionality** - Cross-platform file deletion (Windows/Linux/macOS sandbox-compliant)
2. **Duplicate File Detection** - Cross-platform hash-based duplicate finder

**Implementation Order:** Delete FIRST → Duplicates SECOND (users need delete to remove duplicates)

Both features follow KISS principles and build on existing architecture patterns.

---

## Research Summary

### Avalonia Storage Provider (Security-Scoped Bookmarks)

From official Avalonia docs, the `StorageProvider` API provides:

- **`IStorageBookmarkFile`** / **`IStorageBookmarkFolder`** - Interfaces for bookmarked items
- **`SaveBookmarkAsync()`** - Persist access for future sessions
- **`OpenFolderBookmarkAsync(bookmarkId)`** - Reopen previously granted access
- **`DeleteAsync()`** - Delete files/folders within bookmarked scope
- **`ReleaseBookmarkAsync()`** - Revoke security access when done

**Key insight:** On macOS, when a user selects a folder via the native file picker, you gain temporary security-scoped access to the entire tree under that folder. By saving a bookmark, you can restore this access in future sessions without re-prompting.

### Existing Architecture Analysis

| Component | Current State | Notes |
|-----------|--------------|-------|
| `IPlatformService.MoveToTrash()` | ✅ Exists | Windows + macOS implementations |
| `FileScanner` | ✅ Production | 1800+ lines, highly optimized |
| `IFileScanner` interface | ✅ Clean | Good abstraction point |
| Hash utilities | ❌ Missing | Need to add |
| Delete UI | ❌ Missing | Need context menu + confirmation |
| Bookmark persistence | ❌ Missing | Need settings storage |

---

## Feature 1: Delete Functionality (Cross-Platform)

### Platform Requirements

| Platform | Access Model | Trash Location | Complexity |
|----------|-------------|----------------|------------|
| **Windows** | Full access | Recycle Bin | 🟢 Easy |
| **Linux** | Full access | XDG Trash (~/.local/share/Trash) | 🟢 Easy |
| **macOS (dev)** | Full access | ~/.Trash | 🟢 Easy |
| **macOS (App Store)** | Sandbox | ~/.Trash (via bookmark) | 🟡 Medium |

### The macOS Sandbox Challenge

macOS App Store sandbox restricts file deletion to:
1. Files the app itself created
2. Files within user-granted folder scope (via file picker)

**Current entitlement:** `com.apple.security.files.user-selected.read-write` (already present!)

### The Solution: Platform Abstraction + DaisyDisk Pattern

```
User clicks file in treemap
    ↓
IFileOperationsService.CanDeleteAsync(path)
    ↓
┌─────────────────────────────────────────────────────────┐
│ Windows/Linux: Always true (full access)                │
│ macOS: Check if path within bookmarked scope            │
└─────────────────────────────────────────────────────────┘
    ↓ Yes                           ↓ No (macOS only)
Delete directly        →    Prompt: "Grant access to parent folder?"
    ↓                                    ↓
Success                       User selects folder via picker
    ↓                                    ↓
Update treemap           Save bookmark, then delete
```

### Bookmark Persistence (macOS only)

Store in: `~/Library/Application Support/com.mobius29er.wintrim/bookmarks.json`

```json
{
  "bookmarks": [
    {
      "id": "bookmark_abc123",
      "path": "/Users/john/Downloads",
      "grantedAt": "2025-01-15T10:30:00Z"
    }
  ]
}
```

### Platform Abstraction

```csharp
public interface IFileOperationsService
{
    /// <summary>
    /// Check if we have permission to delete this path
    /// Windows/Linux: Always true
    /// macOS: True if within bookmarked scope
    /// </summary>
    Task<bool> CanDeleteAsync(string path);
    
    /// <summary>
    /// Delete file or folder
    /// </summary>
    Task<DeleteResult> DeleteAsync(string path, DeleteOptions options);
    
    /// <summary>
    /// Request access to a folder (macOS: opens folder picker, saves bookmark)
    /// Windows/Linux: No-op, returns true
    /// </summary>
    Task<bool> RequestAccessAsync(string folderPath);
    
    /// <summary>
    /// Get list of folders we have access to (macOS bookmarks)
    /// Windows/Linux: Returns empty (full access)
    /// </summary>
    IReadOnlyList<string> GetGrantedFolders();
    
    /// <summary>
    /// Reveal file in native file manager
    /// </summary>
    Task RevealInFileManagerAsync(string path);
}

public record DeleteOptions(bool UseTrash = true, bool Recursive = true);
public record DeleteResult(bool Success, string? ErrorMessage = null);
```

---

## Feature 2: Duplicate File Detection (Cross-Platform)

Works identically on all platforms - no sandbox restrictions for reading files.

### Approach: Size-First, Hash-Second (DaisyDisk Pattern)

```
Step 1: Group files by size → O(n), eliminates 99%
Step 2: Partial hash (first 4KB) → O(duplicates)
Step 3: Full hash on matches → O(likely_duplicates)
```

**Why this is optimal:**
- 99% of files eliminated in step 1 (unique sizes)
- Partial hash catches most false positives cheaply
- Full hash only on high-probability matches

### Algorithm Choice

| Algorithm | Speed | Built-in | Use Case |
|-----------|-------|----------|----------|
| xxHash | ⚡ Fastest | ❌ NuGet | Large files, batch |
| SHA256 | 🔒 Secure | ✅ Yes | Cross-session cache |
| MD5 | 🚀 Fast | ✅ Yes | In-memory only |

**Recommendation:** `XxHash64` from `System.IO.Hashing` (.NET 7+) for speed, fallback to SHA256 for persistence.

### Data Model

```csharp
public class DuplicateGroup
{
    public long FileSize { get; set; }
    public string Hash { get; set; }
    public List<FileSystemItem> Files { get; set; }
    public int Count => Files.Count;
    public long WastedSpace => FileSize * (Count - 1);
}
```

---

## Cost-Benefit Analysis

### Delete Functionality (IMPLEMENT FIRST)

| Benefit | Cost |
|---------|------|
| **Prerequisite for duplicates** | Sandbox complexity on macOS |
| Critical UX ("Why can't I delete?") | Bookmark management overhead |
| Competitive parity | Windows/Linux: trivial |
| Cross-platform abstraction | macOS: ~3-5 days |

**Verdict:** ✅ IMPLEMENT FIRST - Required foundation

### Duplicate Detection (IMPLEMENT SECOND)

| Benefit | Cost |
|---------|------|
| High user value (reclaim wasted space) | Medium complexity |
| Differentiator from competitors | ~5-7 days implementation |
| Cross-platform (no restrictions) | Memory for hash table |
| **Requires delete to be useful** | - |

**Verdict:** ✅ HIGH VALUE, implement after delete

---

## Task Breakdown

### Phase 1: Delete Functionality (3-5 days) - IMPLEMENT FIRST

#### 1.1 File Operations Abstraction (Cross-Platform)
- [ ] **Task 1.1.1**: Create `IFileOperationsService` interface in WinTrim.Core
- [ ] **Task 1.1.2**: Implement `WindowsFileOperationsService` (direct delete + recycle bin)
- [ ] **Task 1.1.3**: Implement `LinuxFileOperationsService` (direct delete + trash)
- [ ] **Task 1.1.4**: Implement `MacFileOperationsService` (security-scoped bookmarks)
- [ ] **Task 1.1.5**: Register platform services in DI

#### 1.2 Bookmark Management (macOS Sandbox)
- [ ] **Task 1.2.1**: Create `IBookmarkService` interface
- [ ] **Task 1.2.2**: Implement `BookmarkService` with JSON persistence
- [ ] **Task 1.2.3**: Integrate with Avalonia `StorageProvider` API
- [ ] **Task 1.2.4**: Add "Manage Granted Folders" settings UI

#### 1.3 UI Integration
- [ ] **Task 1.3.1**: Add context menu to treemap tiles (right-click)
- [ ] **Task 1.3.2**: Add context menu to file/folder lists
- [ ] **Task 1.3.3**: Create deletion confirmation dialog
- [ ] **Task 1.3.4**: Implement "Request Access" flow for macOS
- [ ] **Task 1.3.5**: Add "Reveal in Finder/Explorer" option
- [ ] **Task 1.3.6**: Update treemap/lists after deletion

#### 1.4 Safety & Polish
- [ ] **Task 1.4.1**: Add undo support (within session, optional)
- [ ] **Task 1.4.2**: Add delete animation/feedback
- [ ] **Task 1.4.3**: Handle edge cases (read-only, in-use files)
- [ ] **Task 1.4.4**: Unit + integration tests

---

### Phase 2: Duplicate Detection (5-7 days) - IMPLEMENT SECOND

#### 2.1 Core Hashing Infrastructure
- [ ] **Task 2.1.1**: Add `System.IO.Hashing` package to WinTrim.Core
- [ ] **Task 2.1.2**: Create `IHashService` interface
- [ ] **Task 2.1.3**: Implement `HashService` with XxHash64 + SHA256 fallback
- [ ] **Task 2.1.4**: Add streaming hash computation for large files
- [ ] **Task 2.1.5**: Unit tests for hash service

#### 2.2 Duplicate Detection Service
- [ ] **Task 2.2.1**: Create `IDuplicateDetector` interface
- [ ] **Task 2.2.2**: Implement size-grouping algorithm
- [ ] **Task 2.2.3**: Implement partial hash (first 4KB) phase
- [ ] **Task 2.2.4**: Implement full hash phase
- [ ] **Task 2.2.5**: Add progress reporting with cancellation
- [ ] **Task 2.2.6**: Unit tests for duplicate detection

#### 2.3 Data Models
- [ ] **Task 2.3.1**: Create `DuplicateGroup` model
- [ ] **Task 2.3.2**: Create `DuplicateScanResult` model
- [ ] **Task 2.3.3**: Add duplicate stats to `ScanResult`

#### 2.4 UI Integration
- [ ] **Task 2.4.1**: Add "Duplicates" tab to MainWindow
- [ ] **Task 2.4.2**: Create DuplicatesViewModel
- [ ] **Task 2.4.3**: Design duplicate group list UI
- [ ] **Task 2.4.4**: Add "Find Duplicates" button to toolbar
- [ ] **Task 2.4.5**: Implement duplicate selection UI (keep one, delete rest)
- [ ] **Task 2.4.6**: Wire up delete action (uses Phase 1 infrastructure)

#### 2.5 Polish
- [ ] **Task 2.5.1**: Add duplicate detection to scan options
- [ ] **Task 2.5.2**: Performance optimization (parallel hashing)
- [ ] **Task 2.5.3**: Integration tests

---

## Implementation Order & Dependencies

```
Week 1-2: Phase 1 (Delete Functionality - Foundation)
Week 3-4: Phase 2 (Duplicate Detection - Uses Delete)
```

### Critical Path
```
IFileOperationsService → Platform Implementations → Context Menu → Confirm Dialog
    ↓ (Phase 1 complete)
HashService → DuplicateDetector → DuplicatesViewModel → UI → Delete Duplicates
```

### Platform-Specific Implementation Matrix

| Platform | Delete Method | Trash Support | Access Model |
|----------|--------------|---------------|--------------|
| **Windows** | `File.Delete()` / `FileSystem.DeleteFile()` | ✅ RecycleBin | Full access |
| **Linux** | `File.Delete()` / XDG Trash | ✅ ~/.local/share/Trash | Full access |
| **macOS** | AppleScript / NSFileManager | ✅ ~/.Trash | Sandbox + Bookmarks |

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Hash collisions | SHA256 for final comparison |
| Large file memory | Streaming hash, 4KB chunks |
| Sandbox rejection | Already have correct entitlement |
| Bookmark expiry | Handle gracefully, re-prompt user |
| Delete wrong file | Confirm dialog with full path |

---

## Success Criteria

### Delete Functionality (Phase 1)
- [ ] Works on Windows without prompts (direct access)
- [ ] Works on Linux without prompts (direct access)
- [ ] macOS: Seamless within granted scope
- [ ] macOS: Clear prompt when access needed (folder picker)
- [ ] Move to Trash by default (recoverable)
- [ ] Permanent delete option available
- [ ] Treemap/lists update after deletion

### Duplicate Detection (Phase 2)
- [ ] Find all duplicates in <30s for 100K files
- [ ] Memory usage <500MB for 1M files
- [ ] No false positives (hash collisions)
- [ ] Progress indication with ETA
- [ ] Can delete selected duplicates (uses Phase 1)

---

## Next Steps

1. Review this plan and adjust estimates
2. **Start with Task 1.1.1** - Create `IFileOperationsService` interface
3. Implement in small PRs with code review at each phase gate

---

## Appendix: Cross-Platform Delete Implementation Details

### Windows Implementation
```csharp
// Direct delete (full access, no sandbox)
File.Delete(path);  // Permanent
Directory.Delete(path, recursive: true);  // Permanent

// Recycle Bin (requires Microsoft.VisualBasic reference)
FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
```

### Linux Implementation
```csharp
// Direct delete (full access)
File.Delete(path);

// XDG Trash (freedesktop.org standard)
// Move to: ~/.local/share/Trash/files/
// Create info file: ~/.local/share/Trash/info/{filename}.trashinfo
```

### macOS Implementation (Sandbox)
```csharp
// Within granted scope (user selected folder via picker)
await storageItem.DeleteAsync();  // Avalonia StorageProvider

// AppleScript fallback for Trash
osascript -e 'tell application "Finder" to delete POSIX file "/path/to/file"'

// NSFileManager (requires P/Invoke or Swift interop)
[[NSFileManager defaultManager] trashItemAtURL:url resultingItemURL:nil error:nil];
```
