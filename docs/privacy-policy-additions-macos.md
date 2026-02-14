# Privacy Policy Additions for macOS

Add this section to your existing privacy policy at https://www.wintrim.io/privacy

---

## macOS-Specific Permissions

WinTrim for macOS may request the following system permissions to provide its disk analysis features:

### Full Disk Access (Optional)
WinTrim may request Full Disk Access to enable comprehensive disk scanning. This permission allows the app to:
- Analyze your complete file system for accurate disk usage reports
- Detect hidden system files and caches that consume space
- Analyze Time Machine backups (if enabled)

**You are not required to grant this permission.** WinTrim will work with user-selected folders if Full Disk Access is denied. No data from scanned files is transmitted or collected.

### Folder Access Permissions
When you choose to scan a specific folder, WinTrim requests access only to that folder and its contents. These permissions are managed by macOS's security-scoped bookmarks system and can be revoked at any time through System Settings.

### Time Machine Analysis (macOS Only)
If you use the Time Machine analysis feature, WinTrim reads metadata about your backup snapshots to identify large files and suggest exclusions. This analysis:
- Runs entirely on your local machine
- Does not modify your backups
- Does not access backup file contents
- Does not transmit backup information anywhere

### Desktop Folder Access
WinTrim creates a "DeleteMe" folder on your Desktop to safely stage files for deletion. This is a standard folder you can see and control. Files moved to this folder remain on your computer until you manually delete them.

### Privacy Controls
All file access requests are handled through macOS's standard permission dialogs. You can:
- Grant or deny permissions at any time
- Revoke access through System Settings → Privacy & Security
- Choose which folders WinTrim can scan
- Delete the WinTrim app to remove all bookmarks and preferences

---

## Important Notes

1. **Fix GitHub Link**: Change "https://github.com/mobius29er/winLose" to "https://github.com/mobius29er/winTrim" in your current policy

2. **App Store Contact Info**: Make sure your policy includes a support email (required by Apple)

3. **Last Updated Date**: Add or update the "Last Updated" date when you make these changes
