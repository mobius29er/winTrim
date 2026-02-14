# Mac App Store Submission Checklist

## Pre-Submission Requirements

This checklist covers everything needed to submit WinTrim to the Mac App Store.

---

## ✅ 1. App Preparation

### Code & Build
- [x] **Privacy policy updated** with macOS-specific permissions
  - URL: https://wintrim.io/privacy
  - Updated: February 2026
  - Covers: Full Disk Access, Folder Access, Time Machine, DeleteMe folder
- [x] **GitHub links fixed** (winLose → winTrim)
- [x] **QuickClean functionality verified** (fully implemented)
- [x] **DeleteMe folder workflow implemented**
- [x] **Time Machine analysis implemented**
- [x] **.NET SDK pinned** in global.json (8.0.0)
- [x] **App builds successfully** for both ARM64 and x64
- [ ] **All tests passed** (see macos-testing-checklist.md)

### App Metadata
- [x] **Bundle ID**: `com.mobius29er.wintrim`
- [x] **Version**: 1.0.0
- [x] **Build Number**: 4
- [x] **Minimum macOS Version**: 10.15 (Catalina)
- [x] **Category**: public.app-category.utilities
- [x] **Copyright**: © 2026 Foxxception. Open Source.

### Provisioning & Signing
- [x] **Team ID**: BVPSR4AWJD
- [x] **Distribution Certificate**: mac_app.cer (expires: check date)
- [x] **Installer Certificate**: mac_installer.cer (expires: check date)
- [x] **Provisioning Profile**: WinTrim_Distribution.provisionprofile
- [ ] **Verify certificates are valid** (not expired)
- [ ] **Verify provisioning profile is valid** (not expired)

### Entitlements
- [x] App Sandbox enabled (`com.apple.security.app-sandbox`)
- [x] User Selected File Read/Write (`com.apple.security.files.user-selected.read-write`)
- [x] Downloads Folder Read-Only (`com.apple.security.files.downloads.read-only`)
- [x] Security-Scoped Bookmarks (`com.apple.security.files.bookmarks.app-scope`)
- [x] Hardened Runtime enabled
- [x] Temporary exceptions configured (JIT, unsigned memory, dyld)

### Build Artifacts
- [x] **Signed .app bundle**: publish/WinTrim.app
- [x] **Signed .pkg installer**: publish/macos-appstore/WinTrim-1.0.0.pkg (51MB)
- [ ] **Notarized build** (verify notarization status)

---

## 📸 2. Screenshots & Media

### Required Screenshots
- [ ] **Screenshot 1**: Main window with treemap visualization
  - Resolution: 1280×800 or 1440×900
  - Format: PNG or JPG
  - Shows: Treemap with data, colorful visualization
- [ ] **Screenshot 2**: Cleanup suggestions view
  - Shows: Low-risk cleanup items, category breakdown
- [ ] **Screenshot 3** (optional): Time Machine analysis
  - Shows: macOS-specific feature, backup snapshots

### Tips for Great Screenshots
- Use the "Retrofuturistic" or "Tech" theme (most visually appealing)
- Scan a real folder with interesting data
- Show a reasonable amount of data (not empty, not overwhelming)
- Make sure all text is readable at thumbnail size
- No personal/sensitive file names visible

### App Icon
- [x] App icon exists in project
- [ ] Icon looks good at all sizes (16×16 to 1024×1024)
- [ ] Icon follows macOS design guidelines

---

## 📝 3. App Store Connect Metadata

### Basic Information
- **App Name**: WinTrim
- **Subtitle** (70 chars max): Cross-Platform Disk Analyzer & Cleanup Tool
- **Category**: Utilities
- **Secondary Category** (optional): Developer Tools

### Description (4000 chars max)
```
WinTrim is a powerful, privacy-focused disk analyzer that helps you reclaim storage space on your Mac. Discover what's consuming your disk, visualize your file system with interactive treemaps, and safely clean up unwanted files.

KEY FEATURES:
• Interactive Treemap Visualization - See your disk usage at a glance with beautiful, color-coded blocks
• Time Machine Analysis - Identify large files in your backups and optimize Time Machine exclusions (macOS only)
• Intelligent Cleanup Suggestions - Safe, low-risk recommendations for freeing up space
• Developer Tools Detection - Find and clean npm, NuGet, pip, Xcode, and Docker caches
• Game Library Scanning - Detect and manage Steam, Epic, GOG, and Xbox games
• Duplicate File Finder - Locate and remove duplicate files using hash-based detection
• DeleteMe Staging Folder - Safely review files before permanent deletion
• 100% Private - All processing happens on your device. Zero data collection.

PRIVACY & SECURITY:
WinTrim collects ZERO data. No file names, paths, or usage analytics are ever transmitted. The app runs entirely offline and is fully open source.

BEAUTIFUL THEMES:
Choose from 5 stunning themes: Retrofuturistic, Tech, Enterprise, TerminalGreen, and TerminalRed.

CROSS-PLATFORM:
Also available for Windows and Linux. Your data stays on your device, always.

Perfect for developers, photographers, gamers, and anyone looking to optimize their Mac's storage.
```

### Keywords (100 chars max, comma-separated)
```
disk,analyzer,cleanup,storage,space,cache,duplicate,developer,time machine,utility
```

### Support URL
- **URL**: https://github.com/mobius29er/winTrim/issues
- **Alternative**: Create a dedicated support page on wintrim.io

### Marketing URL (optional)
- **URL**: https://wintrim.io

### Privacy Policy URL (REQUIRED)
- **URL**: https://wintrim.io/privacy
- [ ] **Verify URL is accessible** (test in browser)
- [x] **Content updated** with macOS permissions

---

## 🔒 4. Privacy & Data Collection

### Privacy Questionnaire (App Store Connect)
Answer these questions in App Store Connect:

**Does this app collect data from users?**
- ✅ **NO** - WinTrim collects zero data

**Do you or your third-party partners collect data from this app?**
- ✅ **NO**

**Data Types** (all should be NO):
- Contact Info: ❌ No
- Health & Fitness: ❌ No
- Financial Info: ❌ No
- Location: ❌ No
- Sensitive Info: ❌ No
- Contacts: ❌ No
- User Content: ❌ No
- Browsing History: ❌ No
- Search History: ❌ No
- Identifiers: ❌ No
- Purchases: ❌ No
- Usage Data: ❌ No
- Diagnostics: ❌ No
- Other Data: ❌ No

---

## 🔧 5. Technical Information

### Export Compliance
- **Does your app use encryption?**
  - ✅ **NO** - WinTrim does not use encryption beyond standard HTTPS (if website accessed)
  - If you later add encryption, you may need to file CCATS or qualify for exemption

### App Sandbox Capabilities
Explain why each permission is needed:

1. **App Sandbox**: Required for App Store distribution
2. **User Selected Files**: Allows users to choose folders to scan
3. **Downloads Folder Read-Only**: Enables scanning Downloads for large files
4. **Security-Scoped Bookmarks**: Remembers user-selected folders across app launches
5. **Full Disk Access** (optional, user-granted): Enables comprehensive disk scanning and Time Machine analysis

### macOS Feature Usage
- **Time Machine**: Used for backup analysis feature (macOS 10.15+)
- **Finder Integration**: File reveal, double-click to open
- **NSFileManager**: For file operations (move to DeleteMe folder)
- **SkiaSharp**: For treemap rendering
- **Avalonia UI**: Cross-platform UI framework

---

## 📦 6. Upload & Submission

### Notarization (Required Before Upload)
```bash
# 1. Build and sign the app
./build.sh

# 2. Create a ZIP for notarization
cd publish/macos-appstore
ditto -c -k --keepParent WinTrim.app WinTrim.zip

# 3. Submit for notarization
xcrun notarytool submit WinTrim.zip \
  --apple-id "your-apple-id@example.com" \
  --team-id BVPSR4AWJD \
  --password "app-specific-password" \
  --wait

# 4. Check status
xcrun notarytool log <submission-id> --apple-id "your-apple-id@example.com" --team-id BVPSR4AWJD

# 5. Staple the notarization ticket
xcrun stapler staple WinTrim.app
```

- [ ] **Notarization successful**
- [ ] **Notarization ticket stapled**

### Upload to App Store Connect
```bash
# Using Transporter app (GUI)
# 1. Open Transporter app
# 2. Sign in with Apple ID
# 3. Drag WinTrim-1.0.0.pkg
# 4. Click "Deliver"

# OR using command line
xcrun altool --upload-app \
  --type osx \
  --file publish/macos-appstore/WinTrim-1.0.0.pkg \
  --username "your-apple-id@example.com" \
  --password "app-specific-password"
```

- [ ] **Upload successful**
- [ ] **Build appears in App Store Connect**
- [ ] **Build processing complete** (usually 15-30 minutes)

---

## 📋 7. App Store Connect Configuration

### Version Information
1. Go to [App Store Connect](https://appstoreconnect.apple.com)
2. Select your app (or create new app if first submission)
3. Create new version: 1.0.0
4. Select build (uploaded via Transporter)

### Pricing & Availability
- **Price**: $0.00 (Free)
- **Availability**: All territories
- **Pre-Order**: No

### App Review Information
- **Sign-In Required?**: No
- **Demo Account**: N/A
- **Notes for Reviewer**:
```
WinTrim is a disk analyzer and cleanup utility for macOS.

TESTING NOTES:
1. Full Disk Access: The app will request Full Disk Access for comprehensive scanning. You can deny this—the app will still work with user-selected folders.

2. Time Machine Analysis: This feature requires Full Disk Access and a Mac with Time Machine backups configured. If denied or no backups exist, the feature gracefully shows an empty state.

3. DeleteMe Folder: The app creates a folder on the Desktop called "DeleteMe" to stage files for cleanup. This is a standard folder visible in Finder.

4. Privacy: The app makes ZERO network requests and collects ZERO data. All processing is local. You can verify this by monitoring network activity.

5. Open Source: Source code is available at https://github.com/mobius29er/winTrim for verification.

Thank you for reviewing WinTrim!
```

- **Contact Information**:
  - First Name: Jeremy
  - Last Name: Foxx
  - Email: support@foxxception.com
  - Phone: [Your phone number]

### Age Rating
- **Age Rating**: 4+ (No objectionable content)
- All content descriptors: None

---

## 🚀 8. Submit for Review

### Pre-Submission Checklist
- [ ] All metadata entered
- [ ] Screenshots uploaded (1-3 required)
- [ ] Privacy policy URL working
- [ ] Build selected and processing complete
- [ ] Pricing set
- [ ] Age rating completed
- [ ] App review information filled out
- [ ] Export compliance answered

### Submit
1. Click "Submit for Review" in App Store Connect
2. Answer any additional questions
3. Confirm submission

### Review Timeline
- **Initial Review**: Typically 24-48 hours
- **Possible Outcomes**:
  - ✅ **Approved**: App goes live automatically (or on your release date)
  - ⚠️ **Metadata Rejected**: Fix issues and resubmit (no new build needed)
  - ❌ **Binary Rejected**: Fix code issues, upload new build, resubmit

### Common Rejection Reasons
1. **Missing functionality**: Reviewers couldn't get a feature to work
2. **Misleading screenshots**: Screenshots don't match actual app
3. **Privacy policy issues**: Policy doesn't cover requested permissions
4. **Crashes**: App crashed during review
5. **Missing entitlement justifications**: Didn't explain why permissions are needed
6. **Sandbox violations**: App accessed files without permission

---

## 📞 9. Post-Submission

### Monitor App Status
- Check App Store Connect daily for status updates
- Respond quickly to any reviewer questions (in "Resolution Center")

### If Approved ✅
- [ ] Verify app appears in Mac App Store
- [ ] Test downloading and installing from App Store
- [ ] Update website with "Available on Mac App Store" badge
- [ ] Announce launch on social media/GitHub

### If Rejected ❌
1. Read rejection reason carefully
2. Address ALL issues mentioned
3. Upload new build if needed (increment build number to 5)
4. Respond in Resolution Center explaining changes
5. Resubmit

---

## 📊 10. Post-Launch Monitoring

### First Week
- [ ] Monitor crash reports in App Store Connect
- [ ] Check user reviews and ratings
- [ ] Respond to user feedback
- [ ] Fix any critical bugs discovered

### Ongoing
- [ ] Plan updates based on user feedback
- [ ] Monitor macOS updates for compatibility
- [ ] Keep certificates and provisioning profiles renewed

---

## 📧 11. Support & Contact

### Support Channels
- **Email**: support@foxxception.com
- **GitHub Issues**: https://github.com/mobius29er/winTrim/issues
- **Website**: https://wintrim.io

### For App Store Questions
- **Apple Developer Support**: https://developer.apple.com/support/
- **App Review**: Use Resolution Center in App Store Connect

---

## ✅ Final Pre-Flight Check

Before clicking "Submit for Review":

- [ ] App builds and runs on real Mac hardware
- [ ] All features tested and working
- [ ] No known crashes
- [ ] Privacy policy is accurate and accessible
- [ ] Screenshots are high-quality and representative
- [ ] All metadata is accurate (no typos!)
- [ ] Contact information is correct
- [ ] Build is notarized
- [ ] You're mentally prepared for potential rejection 😅
- [ ] You have time to respond to reviewer questions within 24 hours

---

## 🎉 Submission Complete!

**Submitted on**: _______________
**Build Number**: 4
**Version**: 1.0.0
**Status**: ⬜ Waiting for Review / ⬜ In Review / ⬜ Approved / ⬜ Rejected

**Good luck! 🚀**

---

## Appendix: Useful Commands

### Check certificate expiration
```bash
security find-identity -v -p codesigning
```

### Verify code signature
```bash
codesign -vvv --deep --strict publish/macos-appstore/WinTrim.app
```

### Check entitlements
```bash
codesign -d --entitlements - publish/macos-appstore/WinTrim.app
```

### Verify provisioning profile
```bash
security cms -D -i WinTrim_Distribution.provisionprofile
```

### Check notarization status
```bash
xcrun notarytool log <submission-id> --apple-id your-id --team-id BVPSR4AWJD
```

### Validate app before upload
```bash
xcrun altool --validate-app \
  --type osx \
  --file WinTrim-1.0.0.pkg \
  --username your-apple-id \
  --password app-specific-password
```
