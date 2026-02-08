# Mac App Store Distribution Guide

## Overview

This guide covers building and submitting WinTrim to the Mac App Store.

## Prerequisites

### 1. Apple Developer Account
- Sign up at https://developer.apple.com ($99/year)
- Enroll in the Apple Developer Program

### 2. Create App in App Store Connect
1. Go to https://appstoreconnect.apple.com
2. Click "My Apps" → "+" → "New App"
3. Fill in:
   - Platform: macOS
   - Name: WinTrim
   - Bundle ID: com.mobius29er.wintrim
   - SKU: wintrim-1
   - Primary Language: English (U.S.)

### 3. Create Certificates & Profiles

#### In Apple Developer Portal (https://developer.apple.com/account):

**Certificates needed:**
1. **Mac App Distribution** - Signs the app for App Store
2. **Mac Installer Distribution** - Signs the .pkg installer

To create:
1. Go to Certificates, Identifiers & Profiles
2. Click "+" to create new certificates
3. Download and install in Keychain Access

**App ID:**
1. Go to Identifiers → "+"
2. Select "App IDs" → Continue
3. Select "App" → Continue
4. Description: WinTrim
5. Bundle ID: Explicit → com.mobius29er.wintrim
6. Capabilities: None needed (we're sandboxed)

**Provisioning Profile:**
1. Go to Profiles → "+"
2. Select "Mac App Store" → Continue
3. Select App ID: com.mobius29er.wintrim
4. Select your Mac App Distribution certificate
5. Name: WinTrim Mac App Store
6. Download → save as `embedded.provisionprofile`
7. Copy to `WinTrim.Avalonia/embedded.provisionprofile`

## Build Process

### Step 1: Find Your Signing Identities

```bash
security find-identity -v -p codesigning
```

Look for:
- `3rd Party Mac Developer Application: Your Name (TEAMID)`
- `3rd Party Mac Developer Installer: Your Name (TEAMID)`

### Step 2: Create App Icon

You need a proper `.icns` file with all required sizes:

```bash
# Create iconset folder
mkdir AppIcon.iconset

# Add PNG files at these sizes:
# icon_16x16.png, icon_16x16@2x.png
# icon_32x32.png, icon_32x32@2x.png
# icon_128x128.png, icon_128x128@2x.png
# icon_256x256.png, icon_256x256@2x.png
# icon_512x512.png, icon_512x512@2x.png

# Convert to icns
iconutil -c icns AppIcon.iconset -o WinTrim.Avalonia/Assets/AppIcon.icns
```

### Step 3: Update Build Script

Edit `build-macos-appstore.sh` and update:
```bash
SIGNING_IDENTITY="3rd Party Mac Developer Application: YOUR_NAME (TEAM_ID)"
INSTALLER_IDENTITY="3rd Party Mac Developer Installer: YOUR_NAME (TEAM_ID)"
```

Or set environment variables:
```bash
export SIGNING_IDENTITY="3rd Party Mac Developer Application: Jeremy Foxx (ABC123XYZ)"
export INSTALLER_IDENTITY="3rd Party Mac Developer Installer: Jeremy Foxx (ABC123XYZ)"
```

### Step 4: Build

```bash
cd WinTrim.Avalonia
./build-macos-appstore.sh
```

Output will be in `publish/macos-appstore/`:
- `WinTrim.app` - The signed app bundle
- `WinTrim-1.0.0.pkg` - Signed installer for App Store

### Step 5: Upload to App Store Connect

1. Install **Transporter** from the Mac App Store
2. Open Transporter
3. Drag `WinTrim-1.0.0.pkg` into the window
4. Click "Deliver"

### Step 6: Submit for Review

1. Go to App Store Connect
2. Select your app
3. Fill in all metadata:
   - Screenshots (at least one 1280x800 or 1440x900)
   - Description
   - Keywords
   - Support URL
   - Privacy Policy URL
4. Select the build you uploaded
5. Click "Submit for Review"

## Sandbox Considerations

⚠️ **Important**: Mac App Store apps run in a sandbox. WinTrim can only access:

- User-selected folders (via file picker)
- Downloads folder (read-only by default)

To scan the full disk, users must grant explicit permission via:
1. System Preferences → Security & Privacy → Privacy
2. Full Disk Access

Consider adding a prompt in the app to guide users to enable this.

## App Review Tips

1. **Privacy Policy**: Required - host at wintrim.io/privacy
2. **No network telemetry**: ✅ Already done
3. **Sandbox compliant**: ✅ With entitlements
4. **Age Rating**: 4+ (utility app)
5. **Screenshots**: Show the treemap and main features

## Files Created

| File | Purpose |
|------|---------|
| `Entitlements.plist` | Sandbox permissions for App Store |
| `Info.plist` | macOS app bundle metadata |
| `build-macos-appstore.sh` | Build automation script |

## Branch Strategy

**Recommended approach:**
- Keep code public in `main`/`avalonia-migration`
- Use `.gitignore` to exclude:
  - `*.provisionprofile`
  - Signing credentials
- Use GitHub Secrets for CI/CD builds

## Troubleshooting

### "No signing identity found"
- Ensure certificates are installed in Keychain Access
- Run `security find-identity -v -p codesigning`

### "Provisioning profile doesn't match"
- Ensure Bundle ID matches exactly: `com.mobius29er.wintrim`
- Re-download provisioning profile from Apple Developer Portal

### App rejected for sandbox violation
- Check Console.app for sandbox denial logs
- May need to add more entitlements or prompt for Full Disk Access

## Cost Summary

| Item | Cost |
|------|------|
| Apple Developer Account | $99/year |
| App Store listing | Free |
| Your time | Priceless 😄 |

## Timeline

- Initial review: 24-48 hours (usually)
- Rejection response: Same day usually
- Total time to first approval: 3-7 days typical
