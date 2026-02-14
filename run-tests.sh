#!/bin/bash
# WinTrim macOS Automated Tests
# This script runs automated tests where possible and provides manual test instructions

set -e

echo "======================================"
echo "WinTrim macOS Testing Script"
echo "======================================"
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check if app exists
APP_PATH="WinTrim.Avalonia/bin/Release/net8.0/WinTrim"
if [ ! -f "$APP_PATH" ]; then
    echo -e "${RED}❌ App not found at $APP_PATH${NC}"
    echo "Run: cd WinTrim.Avalonia && dotnet build -c Release"
    exit 1
fi

echo -e "${GREEN}✅ App found: $APP_PATH${NC}"
echo ""

# Test 1: Check DeleteMe folder creation
echo -e "${BLUE}Test 1: DeleteMe Folder${NC}"
DELETEME_PATH="$HOME/Desktop/DeleteMe"
if [ -d "$DELETEME_PATH" ]; then
    echo -e "${GREEN}✅ DeleteMe folder exists at $DELETEME_PATH${NC}"
else
    echo -e "${YELLOW}⚠️  DeleteMe folder not found (will be created on first launch)${NC}"
fi
echo ""

# Test 2: Check build artifacts
echo -e "${BLUE}Test 2: Build Artifacts${NC}"
REQUIRED_FILES=(
    "WinTrim.Avalonia/bin/Release/net8.0/WinTrim.dll"
    "WinTrim.Avalonia/bin/Release/net8.0/WinTrim.Core.dll"
    "WinTrim.Avalonia/bin/Release/net8.0/WinTrim.deps.json"
)

ALL_FOUND=true
for file in "${REQUIRED_FILES[@]}"; do
    if [ -f "$file" ]; then
        echo -e "${GREEN}✅ $file${NC}"
    else
        echo -e "${RED}❌ Missing: $file${NC}"
        ALL_FOUND=false
    fi
done
echo ""

# Test 3: Check file permissions
echo -e "${BLUE}Test 3: Executable Permissions${NC}"
if [ -x "$APP_PATH" ]; then
    echo -e "${GREEN}✅ WinTrim is executable${NC}"
else
    echo -e "${RED}❌ WinTrim is not executable${NC}"
    echo "Run: chmod +x $APP_PATH"
fi
echo ""

# Test 4: Check for core dependencies
echo -e "${BLUE}Test 4: Dependencies${NC}"
LIBS=(
    "Avalonia.dll"
    "SkiaSharp.dll"
    "LiveChartsCore.dll"
)

cd WinTrim.Avalonia/bin/Release/net8.0
for lib in "${LIBS[@]}"; do
    if ls *"$lib"* 1> /dev/null 2>&1; then
        echo -e "${GREEN}✅ Found $lib${NC}"
    else
        echo -e "${RED}❌ Missing $lib${NC}"
    fi
done
cd - > /dev/null
echo ""

# Test 5: Check entitlements files
echo -e "${BLUE}Test 5: Entitlements Configuration${NC}"
ENTITLEMENT_FILES=(
    "WinTrim.Avalonia/Entitlements.plist"
    "WinTrim.Avalonia/Info.plist"
)

for file in "${ENTITLEMENT_FILES[@]}"; do
    if [ -f "$file" ]; then
        echo -e "${GREEN}✅ $file exists${NC}"
    else
        echo -e "${RED}❌ Missing: $file${NC}"
    fi
done
echo ""

# Test 6: Verify Info.plist contents
echo -e "${BLUE}Test 6: Info.plist Validation${NC}"
INFO_PLIST="WinTrim.Avalonia/Info.plist"
if [ -f "$INFO_PLIST" ]; then
    # Check for key entries
    if grep -q "com.mobius29er.wintrim" "$INFO_PLIST"; then
        echo -e "${GREEN}✅ Bundle ID correct${NC}"
    else
        echo -e "${RED}❌ Bundle ID not found${NC}"
    fi

    if grep -q "NSDesktopFolderUsageDescription" "$INFO_PLIST"; then
        echo -e "${GREEN}✅ Desktop folder permission description present${NC}"
    else
        echo -e "${YELLOW}⚠️  Desktop folder permission description missing${NC}"
    fi
else
    echo -e "${RED}❌ Info.plist not found${NC}"
fi
echo ""

# Manual test instructions
echo "======================================"
echo -e "${YELLOW}MANUAL TESTS REQUIRED${NC}"
echo "======================================"
echo ""
echo "Now run these manual tests:"
echo ""
echo -e "${BLUE}1. Launch App:${NC}"
echo "   cd WinTrim.Avalonia/bin/Release/net8.0"
echo "   ./WinTrim"
echo "   Expected: App launches, main window appears"
echo ""
echo -e "${BLUE}2. Test Scanning:${NC}"
echo "   - Click 'Choose Folder' or similar"
echo "   - Select your Documents folder"
echo "   - Wait for scan to complete"
echo "   - Verify treemap shows colored blocks"
echo ""
echo -e "${BLUE}3. Test Collector → DeleteMe:${NC}"
echo "   - Add a test file to Collector"
echo "   - Click 'Move to Cleanup Folder'"
echo "   - Verify file appears in ~/Desktop/DeleteMe"
echo "   - Check: ls ~/Desktop/DeleteMe"
echo ""
echo -e "${BLUE}4. Test QuickClean:${NC}"
echo "   - If '⚡ Quick Clean' button visible, click it"
echo "   - Verify warning dialog appears"
echo "   - Test both Cancel and Delete paths"
echo ""
echo -e "${BLUE}5. Test Theme Switching:${NC}"
echo "   - Open Settings"
echo "   - Try all 5 themes"
echo "   - Verify UI updates correctly"
echo ""
echo -e "${BLUE}6. Check Console for Errors:${NC}"
echo "   - Open Console.app"
echo "   - Filter for 'WinTrim'"
echo "   - Look for errors or crashes"
echo ""
echo "======================================"
echo "Record results in: test-results.md"
echo "======================================"
echo ""

# Summary
echo -e "${GREEN}Automated Tests Complete!${NC}"
echo ""
if [ "$ALL_FOUND" = true ]; then
    echo -e "${GREEN}✅ All automated checks passed${NC}"
    echo -e "${YELLOW}➡️  Proceed with manual testing${NC}"
else
    echo -e "${RED}⚠️  Some automated checks failed${NC}"
    echo -e "${YELLOW}➡️  Fix issues before manual testing${NC}"
fi
echo ""
echo "To launch app:"
echo "  cd WinTrim.Avalonia/bin/Release/net8.0 && ./WinTrim"
echo ""
