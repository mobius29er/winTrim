#!/bin/bash
# ============================================================================
# WinTrim - Mac App Store Build Script
# ============================================================================
# This script builds WinTrim for Mac App Store distribution.
# 
# Prerequisites:
#   1. Apple Developer Account ($99/year)
#   2. Xcode installed with command line tools
#   3. Signing certificates installed in Keychain:
#      - "3rd Party Mac Developer Application: Your Name (TEAM_ID)"
#      - "3rd Party Mac Developer Installer: Your Name (TEAM_ID)"
#   4. Mac App Store provisioning profile
#
# Usage:
#   ./build-macos-appstore.sh
#
# Environment Variables (set these or edit below):
#   SIGNING_IDENTITY - Your signing identity
#   INSTALLER_IDENTITY - Your installer signing identity
#   PROVISIONING_PROFILE - Path to .provisionprofile file
# ============================================================================

set -e

# Configuration - UPDATE THESE VALUES
APP_NAME="WinTrim"
BUNDLE_ID="com.mobius29er.wintrim"
VERSION="1.0.0"
BUILD_NUMBER="6"

# Signing identities (from Keychain)
# Run: security find-identity -v -p codesigning
# Using SHA-1 hash to select the specific certificate that matches the provisioning profile
SIGNING_IDENTITY="${SIGNING_IDENTITY:-D3820ECE5140BFB00D16BAD94B74FE23E9956EED}"
INSTALLER_IDENTITY="${INSTALLER_IDENTITY:-3rd Party Mac Developer Installer: Foxxception LLC (BVPSR4AWJD)}"

# Paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
PROJECT_DIR="$SCRIPT_DIR"
OUTPUT_DIR="$PROJECT_ROOT/publish/macos-appstore"
APP_BUNDLE="$OUTPUT_DIR/$APP_NAME.app"

echo "============================================"
echo "Building $APP_NAME for Mac App Store"
echo "Version: $VERSION"
echo "============================================"

# Clean previous builds
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Step 1: Publish the app
echo ""
echo "Step 1: Publishing .NET app..."
dotnet publish "$PROJECT_DIR/WinTrim.Avalonia.csproj" \
    -c Release \
    -r osx-arm64 \
    --self-contained true \
    -p:PublishSingleFile=false \
    -p:UseAppHost=true \
    -p:DebugType=none \
    -p:DebugSymbols=false \
    -o "$OUTPUT_DIR/publish"

# Also build for Intel Macs (universal binary)
echo "Building for Intel Macs..."
dotnet publish "$PROJECT_DIR/WinTrim.Avalonia.csproj" \
    -c Release \
    -r osx-x64 \
    --self-contained true \
    -p:PublishSingleFile=false \
    -p:UseAppHost=true \
    -p:DebugType=none \
    -p:DebugSymbols=false \
    -o "$OUTPUT_DIR/publish-x64"

# Step 2: Create app bundle structure
echo ""
echo "Step 2: Creating app bundle structure..."
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Copy ARM64 build as base
cp -R "$OUTPUT_DIR/publish/"* "$APP_BUNDLE/Contents/MacOS/"

# Step 2b: Create universal binaries using lipo
echo ""
echo "Step 2b: Creating universal binaries..."
for arm_file in "$OUTPUT_DIR/publish/"*; do
    filename=$(basename "$arm_file")
    x64_file="$OUTPUT_DIR/publish-x64/$filename"
    dest_file="$APP_BUNDLE/Contents/MacOS/$filename"
    
    # Skip if x64 version doesn't exist
    if [ ! -f "$x64_file" ]; then
        continue
    fi
    
    # Check if this is a Mach-O binary (executable or dylib)
    if file "$arm_file" | grep -q "Mach-O"; then
        echo "  Creating universal: $filename"
        lipo -create "$arm_file" "$x64_file" -output "$dest_file" 2>/dev/null || cp "$arm_file" "$dest_file"
    fi
done

# Copy Info.plist
cp "$PROJECT_DIR/Info.plist" "$APP_BUNDLE/Contents/"

# Step 3: Create/copy app icon
echo ""
echo "Step 3: Setting up app icon..."
if [ -f "$PROJECT_DIR/Assets/AppIcon.icns" ]; then
    cp "$PROJECT_DIR/Assets/AppIcon.icns" "$APP_BUNDLE/Contents/Resources/"
else
    echo "WARNING: AppIcon.icns not found. Create one at Assets/AppIcon.icns"
    echo "You can convert PNG to ICNS using: iconutil -c icns AppIcon.iconset"
fi

# Step 4: Copy provisioning profile (if exists)
echo ""
echo "Step 4: Embedding provisioning profile..."
if [ -f "$PROVISIONING_PROFILE" ]; then
    cp "$PROVISIONING_PROFILE" "$APP_BUNDLE/Contents/embedded.provisionprofile"
elif [ -f "$PROJECT_DIR/embedded.provisionprofile" ]; then
    cp "$PROJECT_DIR/embedded.provisionprofile" "$APP_BUNDLE/Contents/"
else
    echo "WARNING: No provisioning profile found."
    echo "Download from Apple Developer Portal and save as embedded.provisionprofile"
fi

# Step 5: Sign the app bundle
echo ""
echo "Step 5: Signing app bundle..."
echo "Using identity: $SIGNING_IDENTITY"

# Remove any extended attributes that might cause issues
xattr -cr "$APP_BUNDLE"

# For .NET apps with App Store distribution:
# We must sign EVERY file in Contents/MacOS (including JSON, DLLs, etc.)
# The key is to sign files first, then the main executable, then the bundle
# IMPORTANT: Nested executables must NOT have application-identifier entitlement

MAIN_EXECUTABLE="$APP_BUNDLE/Contents/MacOS/WinTrim"

echo "Signing ALL files in Contents/MacOS (except main executable)..."
find "$APP_BUNDLE/Contents/MacOS/" -type f | while read -r fname; do
    if [ "$fname" = "$MAIN_EXECUTABLE" ]; then
        echo "  Skipping main executable (will sign separately): $(basename "$fname")"
        continue
    fi
    echo "  Signing: $(basename "$fname")"
    codesign --force --timestamp --options=runtime \
        --entitlements "$PROJECT_DIR/Entitlements-nested.plist" \
        --sign "$SIGNING_IDENTITY" "$fname" 2>&1 || echo "    (non-code file, continuing)"
done

# Sign any frameworks if they exist
if [ -d "$APP_BUNDLE/Contents/Frameworks" ]; then
    echo "Signing frameworks..."
    find "$APP_BUNDLE/Contents/Frameworks/" -type f | while read -r fname; do
        echo "  Signing: $(basename "$fname")"
        codesign --force --timestamp --options=runtime \
            --entitlements "$PROJECT_DIR/Entitlements-nested.plist" \
            --sign "$SIGNING_IDENTITY" "$fname" 2>&1 || true
    done
fi

# Sign the main executable with full entitlements (including application-identifier)
echo "Signing main executable with application-identifier..."
codesign --force --timestamp --options=runtime \
    --entitlements "$PROJECT_DIR/Entitlements.plist" \
    --sign "$SIGNING_IDENTITY" \
    "$MAIN_EXECUTABLE"

# Finally sign the entire app bundle
echo "Signing app bundle..."
codesign --force --timestamp --options=runtime \
    --entitlements "$PROJECT_DIR/Entitlements.plist" \
    --sign "$SIGNING_IDENTITY" \
    "$APP_BUNDLE"

# Step 6: Verify signature
echo ""
echo "Step 6: Verifying signature..."
codesign --verify --deep --strict --verbose=2 "$APP_BUNDLE"
spctl --assess --type exec --verbose "$APP_BUNDLE" || echo "Note: spctl check may fail until app is notarized"

# Step 7: Create installer package for App Store
echo ""
echo "Step 7: Creating installer package..."
productbuild --component "$APP_BUNDLE" /Applications \
    --sign "$INSTALLER_IDENTITY" \
    "$OUTPUT_DIR/$APP_NAME-$VERSION.pkg"

echo ""
echo "============================================"
echo "Build complete!"
echo "============================================"
echo ""
echo "Output files:"
echo "  App Bundle: $APP_BUNDLE"
echo "  Installer:  $OUTPUT_DIR/$APP_NAME-$VERSION.pkg"
echo ""
echo "Next steps:"
echo "  1. Open Transporter app (from App Store)"
echo "  2. Drag the .pkg file into Transporter"
echo "  3. Upload to App Store Connect"
echo "  4. Submit for review in App Store Connect"
echo ""
