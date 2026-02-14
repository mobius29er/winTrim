# macOS Testing Checklist for WinTrim

## Pre-Submission Testing Requirements

Before submitting to the Mac App Store, test ALL functionality on real macOS hardware.

---

## 1. App Installation & Launch

### Test on Both Architectures
- [ ] **Apple Silicon (M1/M2/M3)**: Test on ARM64 Mac
- [ ] **Intel Mac**: Test on x64 Mac (if available)

### First Launch
- [ ] App launches without errors
- [ ] No crash on startup
- [ ] EULA dialog appears (if implemented)
- [ ] Main window displays correctly
- [ ] All UI elements render properly (no missing icons/images)

---

## 2. Permissions Testing

### Folder Access (Security-Scoped Bookmarks)
- [ ] **Test folder picker**: Click "Choose Folder" or similar button
- [ ] System permission dialog appears
- [ ] After granting access, folder scans successfully
- [ ] Access persists across app restarts (bookmark saved)
- [ ] Scan different folder types:
  - [ ] Desktop folder
  - [ ] Documents folder
  - [ ] Downloads folder
  - [ ] External USB drive
  - [ ] Network drive (if available)

### Full Disk Access (Optional)
- [ ] App prompts for Full Disk Access when needed
- [ ] Instructions are clear for enabling in System Settings
- [ ] App works correctly when FDA is **granted**
- [ ] App degrades gracefully when FDA is **denied**
- [ ] Time Machine analysis requires FDA (expected behavior)

### Desktop Folder Access
- [ ] DeleteMe folder is created on Desktop automatically
- [ ] No errors when creating DeleteMe folder
- [ ] Folder is visible in Finder

---

## 3. Core Functionality Testing

### Disk Scanning
- [ ] **Scan Home Folder**: Scan ~/
  - [ ] Progress bar updates smoothly
  - [ ] File count increases during scan
  - [ ] No crashes during scan
  - [ ] Scan completes successfully
  - [ ] Results display correctly
- [ ] **Scan External Drive**: Test with USB drive
- [ ] **Pause/Resume**: Test pause and resume functionality
- [ ] **Cancel Scan**: Cancel mid-scan, verify cleanup

### Data Visualization
- [ ] **Treemap View**:
  - [ ] Renders correctly with data
  - [ ] Click on blocks navigates into folders
  - [ ] Breadcrumb navigation works
  - [ ] All 5 color schemes work:
    - [ ] Retrofuturistic
    - [ ] Tech
    - [ ] Enterprise
    - [ ] TerminalGreen
    - [ ] TerminalRed
  - [ ] Zoom depth slider works (1-10 levels)
- [ ] **Category Pie Chart**:
  - [ ] Displays categories correctly
  - [ ] Clicking categories filters treemap
  - [ ] Legend shows accurate sizes
- [ ] **Largest Files**:
  - [ ] Shows top 50 files
  - [ ] Sizes are accurate
  - [ ] Can sort by name/size/date
  - [ ] Double-click opens file in Finder

### Cleanup Features
- [ ] **Cleanup Suggestions**:
  - [ ] Suggestions appear after scan
  - [ ] Risk levels are color-coded (Safe/Low/Medium/High)
  - [ ] Sizes are accurate
  - [ ] Categories make sense (Browser Cache, Logs, etc.)
- [ ] **Collector**:
  - [ ] Add items to Collector from various views
  - [ ] Remove items from Collector
  - [ ] Collector shows accurate count and size
  - [ ] "Clear All" removes all items
- [ ] **DeleteMe Folder Workflow**:
  - [ ] Click "Move to Cleanup Folder"
  - [ ] Files/folders move to ~/Desktop/DeleteMe successfully
  - [ ] Verify moved files exist in DeleteMe folder
  - [ ] Original files are gone from original location
  - [ ] No permission errors
  - [ ] Works with:
    - [ ] Individual files
    - [ ] Folders
    - [ ] Mix of both
    - [ ] Large files (>1GB)
- [ ] **QuickClean Dialog**:
  - [ ] Opens when clicking "⚡ Quick Clean" button
  - [ ] Shows safe/low-risk items only
  - [ ] Can select/deselect individual files
  - [ ] "Select All" / "Select None" works
  - [ ] "Expand All" / "Collapse All" works
  - [ ] Confirmation dialog appears before deletion
  - [ ] **⚠️ WARNING MESSAGE IS CLEAR** about permanent deletion
  - [ ] Actually deletes files when confirmed
  - [ ] Shows accurate completion message
  - [ ] Dialog closes when all items cleaned

### Advanced Features
- [ ] **Time Machine Analysis** (macOS-specific):
  - [ ] Opens Time Machine analysis view
  - [ ] Detects local Time Machine backups
  - [ ] Detects network Time Machine backups
  - [ ] Shows backup snapshots with dates
  - [ ] Analyzes latest backup for large files
  - [ ] Suggests exclusion candidates
  - [ ] Can add exclusions to Time Machine
  - [ ] Requires Full Disk Access (expected)
- [ ] **Duplicate Finder**:
  - [ ] Scans for duplicate files
  - [ ] Groups duplicates correctly (by hash)
  - [ ] Shows file size and count
  - [ ] Can add duplicates to Collector
- [ ] **Game Detection**:
  - [ ] Detects Steam games (if installed)
  - [ ] Detects Epic Games (if installed)
  - [ ] Detects GOG games (if installed)
  - [ ] Shows game sizes accurately
- [ ] **Developer Tools Detection**:
  - [ ] Detects npm cache
  - [ ] Detects NuGet cache
  - [ ] Detects pip cache
  - [ ] Detects Xcode DerivedData
  - [ ] Detects Docker volumes

---

## 4. UI/UX Testing

### Themes
- [ ] All 5 themes load correctly
- [ ] Theme switching works without restart
- [ ] All UI elements are readable in each theme
- [ ] No visual glitches when switching themes

### Settings
- [ ] Settings panel opens
- [ ] Font size adjustment works (80% to 120%)
- [ ] Settings persist across app restarts
- [ ] Cleanup folder path can be changed
- [ ] Express scan toggle works

### Responsiveness
- [ ] Window resize works smoothly
- [ ] Minimum window size is reasonable
- [ ] All panels scroll properly when content overflows
- [ ] No UI freezing during long operations
- [ ] Status bar updates in real-time during scans

### Keyboard Shortcuts
- [ ] Test all keyboard shortcuts (if any)
- [ ] Cmd+Q quits the app
- [ ] Cmd+W closes window

---

## 5. Error Handling

### Permission Denials
- [ ] Deny folder access → app shows helpful message
- [ ] Deny Full Disk Access → Time Machine analysis gracefully fails
- [ ] Access inaccessible system files → no crash

### Edge Cases
- [ ] Scan empty folder → shows "no data" gracefully
- [ ] Scan folder with 0 files → no errors
- [ ] Scan while files are being deleted → handles gracefully
- [ ] Delete files that no longer exist → shows clear error
- [ ] Try to move file to DeleteMe when it's deleted → handles error
- [ ] Fill up disk during scan → shows disk full error

### File System Edge Cases
- [ ] Files with special characters in names (é, ñ, 中文)
- [ ] Very long file paths (>255 characters)
- [ ] Symbolic links (should handle or skip)
- [ ] Hidden files (files starting with .)
- [ ] Files without extensions
- [ ] Read-only files

---

## 6. Performance Testing

### Large Scans
- [ ] Scan folder with 100,000+ files
  - [ ] No crash
  - [ ] Memory usage stays reasonable (<2GB)
  - [ ] UI remains responsive
  - [ ] Progress updates smoothly
- [ ] Scan 500GB+ folder
  - [ ] Completes successfully
  - [ ] Shows accurate sizes
- [ ] Open large duplicate group (100+ duplicates)
  - [ ] Loads without lag

### Memory Leaks
- [ ] Run app for 30+ minutes
- [ ] Check Activity Monitor for memory growth
- [ ] Perform multiple scans (5-10)
- [ ] Memory usage should stabilize, not grow indefinitely

---

## 7. Session Persistence

### Auto-Save
- [ ] Complete a scan
- [ ] Quit app (Cmd+Q)
- [ ] Relaunch app
- [ ] Previous scan results are restored
- [ ] Treemap shows same data
- [ ] Cleanup suggestions are still there

### Settings Persistence
- [ ] Change theme
- [ ] Change font size
- [ ] Set cleanup folder path
- [ ] Quit and relaunch
- [ ] All settings are preserved

---

## 8. macOS Integration

### Finder Integration
- [ ] Right-click file → "Reveal in Finder" works (if implemented)
- [ ] Double-click file opens in default app
- [ ] Can drag files from app to Finder

### System Integration
- [ ] App appears in Applications folder correctly
- [ ] App icon shows in Dock
- [ ] App icon shows in Finder
- [ ] About dialog shows correct version
- [ ] Copyright year is current

### Sandbox Compliance
- [ ] App runs in sandbox (App Store build)
- [ ] No unauthorized file system access
- [ ] No network requests (WinTrim is offline-only)
- [ ] No crashes due to sandbox restrictions

---

## 9. Localization (If Implemented)

- [ ] App displays in system language (if supported)
- [ ] English strings are correct
- [ ] No untranslated strings visible
- [ ] Number/date formatting matches system locale

---

## 10. Stability Testing

### Crash Testing
- [ ] Force quit during scan → restarts without corruption
- [ ] Kill app via Activity Monitor → restarts cleanly
- [ ] Scan extremely large file (10GB+) → no crash
- [ ] Rapid clicking buttons → no crash
- [ ] Spam scan/cancel repeatedly → no crash

### Long-Running Tests
- [ ] Leave app open overnight → no crash
- [ ] Complete 10 consecutive scans → no crash
- [ ] Delete 1000+ files via QuickClean → completes successfully

---

## 11. Specific macOS Versions

Test on multiple macOS versions if possible:
- [ ] **macOS 10.15 Catalina** (minimum version in Info.plist)
- [ ] **macOS 11 Big Sur**
- [ ] **macOS 12 Monterey**
- [ ] **macOS 13 Ventura**
- [ ] **macOS 14 Sonoma**
- [ ] **macOS 15 Sequoia** (latest as of 2026)

---

## 12. Accessibility (Optional but Recommended)

- [ ] VoiceOver reads UI elements correctly
- [ ] Keyboard navigation works
- [ ] Contrast ratios are acceptable
- [ ] Text scales properly with system font size

---

## 13. Logging and Debugging

- [ ] Check Console.app for errors while running WinTrim
- [ ] No excessive logging
- [ ] No sensitive data in logs (file paths should be anonymized if logged)
- [ ] Error messages are helpful for debugging

---

## Critical Issues That Would Block App Store Approval

### Must Fix Before Submission
- ❌ App crashes on launch
- ❌ App requests permissions not listed in Info.plist
- ❌ App violates sandbox (accesses files without permission)
- ❌ App makes network requests (WinTrim should be 100% offline)
- ❌ App contains malware or security vulnerabilities
- ❌ Delete functionality doesn't work (core feature)
- ❌ Privacy policy URL is broken or missing
- ❌ App crashes when denying permissions

### Should Fix (High Priority)
- ⚠️ Confusing error messages
- ⚠️ UI elements not visible in some themes
- ⚠️ Memory leaks during long scans
- ⚠️ Incorrect file sizes displayed
- ⚠️ Settings don't persist

---

## Testing Notes Template

Use this template to document test results:

```
### Test Session: [Date]
**Tester**: [Your Name]
**macOS Version**: [e.g., macOS 14.2 Sonoma]
**Hardware**: [e.g., MacBook Pro M1, 16GB RAM]
**Build**: [e.g., WinTrim-1.0.0.pkg]

#### Tests Passed ✅
- [List passed tests]

#### Tests Failed ❌
- [List failed tests with details]

#### Bugs Found 🐛
1. [Bug description]
   - Steps to reproduce
   - Expected behavior
   - Actual behavior
   - Severity: Critical/High/Medium/Low

#### Performance Notes
- Scanned [X] files in [Y] seconds
- Memory usage: [Z] MB
- CPU usage: [%]

#### Notes
- [Any additional observations]
```

---

## Final Checklist Before Submission

- [ ] All critical tests passed
- [ ] No known crash bugs
- [ ] Privacy policy updated and live at https://wintrim.io/privacy
- [ ] Screenshots created (1280×800 or 1440×900)
- [ ] App metadata prepared (description, keywords, category)
- [ ] Version number is correct in Info.plist and build
- [ ] Copyright year is 2026
- [ ] Bundle ID matches provisioning profile: `com.mobius29er.wintrim`
- [ ] Signing certificate is valid
- [ ] Provisioning profile is valid
- [ ] App notarized successfully
- [ ] Tested on both Apple Silicon and Intel (if possible)

---

## Test Result Summary

**Overall Status**: ⬜ Pass / ⬜ Pass with Minor Issues / ⬜ Fail

**Ready for App Store Submission?**: ⬜ Yes / ⬜ No / ⬜ With Fixes

**Tester Sign-Off**: _____________________ Date: __________
