#!/bin/bash
# WinTrim Build Script for macOS and Windows distribution
# Run from project root: ./build.sh [platform]

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/WinTrim.Avalonia"
OUTPUT_DIR="$SCRIPT_DIR/dist"
VERSION="1.0.0"
APP_NAME="WinTrim"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}🔧 WinTrim Build Script v${VERSION}${NC}"
echo ""

# Ensure output directory exists
mkdir -p "$OUTPUT_DIR"

build_windows() {
    echo -e "${YELLOW}📦 Building Windows x64...${NC}"
    
    dotnet publish "$PROJECT_DIR/WinTrim.Avalonia.csproj" \
        -c Release \
        -r win-x64 \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:PublishReadyToRun=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:EnableCompressionInSingleFile=true \
        -o "$OUTPUT_DIR/win-x64"
    
    echo -e "${GREEN}✅ Windows build complete: $OUTPUT_DIR/win-x64/WinTrim.exe${NC}"
    
    # Create ZIP for distribution
    cd "$OUTPUT_DIR"
    zip -r "WinTrim-${VERSION}-win-x64.zip" win-x64/
    echo -e "${GREEN}✅ Created: WinTrim-${VERSION}-win-x64.zip${NC}"
}

build_macos_arm64() {
    echo -e "${YELLOW}📦 Building macOS ARM64 (Apple Silicon)...${NC}"
    
    dotnet publish "$PROJECT_DIR/WinTrim.Avalonia.csproj" \
        -c Release \
        -r osx-arm64 \
        --self-contained true \
        -p:PublishReadyToRun=true \
        -o "$OUTPUT_DIR/osx-arm64"
    
    echo -e "${GREEN}✅ macOS ARM64 build complete${NC}"
    
    # Create .app bundle
    create_macos_app "osx-arm64"
}

build_macos_x64() {
    echo -e "${YELLOW}📦 Building macOS x64 (Intel)...${NC}"
    
    dotnet publish "$PROJECT_DIR/WinTrim.Avalonia.csproj" \
        -c Release \
        -r osx-x64 \
        --self-contained true \
        -p:PublishReadyToRun=true \
        -o "$OUTPUT_DIR/osx-x64"
    
    echo -e "${GREEN}✅ macOS x64 build complete${NC}"
    
    # Create .app bundle
    create_macos_app "osx-x64"
}

create_macos_app() {
    local RID=$1
    local APP_BUNDLE="$OUTPUT_DIR/${APP_NAME}-${RID}.app"
    
    echo -e "${YELLOW}📱 Creating macOS app bundle for $RID...${NC}"
    
    # Create app bundle structure
    rm -rf "$APP_BUNDLE"
    mkdir -p "$APP_BUNDLE/Contents/MacOS"
    mkdir -p "$APP_BUNDLE/Contents/Resources"
    
    # Copy executable and dependencies
    cp -R "$OUTPUT_DIR/$RID/"* "$APP_BUNDLE/Contents/MacOS/"
    
    # Create Info.plist
    cat > "$APP_BUNDLE/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleDisplayName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleIdentifier</key>
    <string>com.mobius29er.wintrim</string>
    <key>CFBundleVersion</key>
    <string>${VERSION}</string>
    <key>CFBundleShortVersionString</key>
    <string>${VERSION}</string>
    <key>CFBundleExecutable</key>
    <string>WinTrim</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>LSMinimumSystemVersion</key>
    <string>11.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSHumanReadableCopyright</key>
    <string>Copyright © 2026 Mobius29er</string>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.utilities</string>
</dict>
</plist>
EOF
    
    # Make executable
    chmod +x "$APP_BUNDLE/Contents/MacOS/WinTrim"
    
    echo -e "${GREEN}✅ Created: ${APP_BUNDLE}${NC}"
    
    # Create DMG
    create_dmg "$RID"
}

create_dmg() {
    local RID=$1
    local DMG_NAME="${APP_NAME}-${VERSION}-${RID}.dmg"
    local APP_BUNDLE="$OUTPUT_DIR/${APP_NAME}-${RID}.app"
    
    echo -e "${YELLOW}💿 Creating DMG for $RID...${NC}"
    
    # Check if create-dmg is available, otherwise use hdiutil
    if command -v create-dmg &> /dev/null; then
        create-dmg \
            --volname "$APP_NAME" \
            --window-pos 200 120 \
            --window-size 600 400 \
            --icon-size 100 \
            --icon "$APP_NAME-$RID.app" 150 190 \
            --app-drop-link 450 185 \
            "$OUTPUT_DIR/$DMG_NAME" \
            "$APP_BUNDLE"
    else
        # Fallback to hdiutil
        hdiutil create -volname "$APP_NAME" -srcfolder "$APP_BUNDLE" -ov -format UDZO "$OUTPUT_DIR/$DMG_NAME"
    fi
    
    echo -e "${GREEN}✅ Created: $OUTPUT_DIR/$DMG_NAME${NC}"
}

sign_macos_app() {
    local RID=$1
    local APP_BUNDLE="$OUTPUT_DIR/${APP_NAME}-${RID}.app"
    
    echo -e "${YELLOW}🔐 Signing macOS app for $RID...${NC}"
    
    # Check for signing identity
    if [ -z "$APPLE_SIGNING_IDENTITY" ]; then
        echo -e "${RED}⚠️  APPLE_SIGNING_IDENTITY not set. Skipping code signing.${NC}"
        echo "   Set it with: export APPLE_SIGNING_IDENTITY='Developer ID Application: Your Name (TEAMID)'"
        return 1
    fi
    
    # Sign all .dylib files first
    find "$APP_BUNDLE" -name "*.dylib" -exec codesign --force --sign "$APPLE_SIGNING_IDENTITY" --options runtime {} \;
    
    # Sign the main executable
    codesign --force --sign "$APPLE_SIGNING_IDENTITY" --options runtime --entitlements "$SCRIPT_DIR/entitlements.plist" "$APP_BUNDLE"
    
    echo -e "${GREEN}✅ App signed successfully${NC}"
}

notarize_macos_app() {
    local RID=$1
    local DMG_NAME="${APP_NAME}-${VERSION}-${RID}.dmg"
    
    echo -e "${YELLOW}📤 Notarizing macOS app...${NC}"
    
    if [ -z "$APPLE_ID" ] || [ -z "$APPLE_APP_PASSWORD" ] || [ -z "$APPLE_TEAM_ID" ]; then
        echo -e "${RED}⚠️  Apple credentials not set. Skipping notarization.${NC}"
        echo "   Set: APPLE_ID, APPLE_APP_PASSWORD (app-specific password), APPLE_TEAM_ID"
        return 1
    fi
    
    xcrun notarytool submit "$OUTPUT_DIR/$DMG_NAME" \
        --apple-id "$APPLE_ID" \
        --password "$APPLE_APP_PASSWORD" \
        --team-id "$APPLE_TEAM_ID" \
        --wait
    
    # Staple the notarization ticket
    xcrun stapler staple "$OUTPUT_DIR/$DMG_NAME"
    
    echo -e "${GREEN}✅ Notarization complete${NC}"
}

show_help() {
    echo "Usage: ./build.sh [command]"
    echo ""
    echo "Commands:"
    echo "  windows     Build Windows x64 executable"
    echo "  macos       Build macOS (both ARM64 and x64)"
    echo "  macos-arm64 Build macOS ARM64 only (Apple Silicon)"
    echo "  macos-x64   Build macOS x64 only (Intel)"
    echo "  all         Build all platforms"
    echo "  sign        Sign macOS app (requires APPLE_SIGNING_IDENTITY)"
    echo "  notarize    Notarize macOS app (requires Apple credentials)"
    echo "  clean       Clean build outputs"
    echo ""
    echo "Environment variables for signing:"
    echo "  APPLE_SIGNING_IDENTITY  - Your signing identity"
    echo "  APPLE_ID                - Your Apple ID email"
    echo "  APPLE_APP_PASSWORD      - App-specific password"
    echo "  APPLE_TEAM_ID           - Your team ID"
}

clean() {
    echo -e "${YELLOW}🧹 Cleaning build outputs...${NC}"
    rm -rf "$OUTPUT_DIR"
    rm -rf "$PROJECT_DIR/bin"
    rm -rf "$PROJECT_DIR/obj"
    echo -e "${GREEN}✅ Clean complete${NC}"
}

# Main
case "${1:-all}" in
    windows)
        build_windows
        ;;
    macos)
        build_macos_arm64
        build_macos_x64
        ;;
    macos-arm64)
        build_macos_arm64
        ;;
    macos-x64)
        build_macos_x64
        ;;
    all)
        build_windows
        build_macos_arm64
        build_macos_x64
        ;;
    sign)
        sign_macos_app "osx-arm64"
        sign_macos_app "osx-x64"
        ;;
    notarize)
        notarize_macos_app "osx-arm64"
        notarize_macos_app "osx-x64"
        ;;
    clean)
        clean
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        echo -e "${RED}Unknown command: $1${NC}"
        show_help
        exit 1
        ;;
esac

echo ""
echo -e "${GREEN}🎉 Build complete!${NC}"
echo "Output: $OUTPUT_DIR"
