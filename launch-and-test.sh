#!/bin/bash
# Launch WinTrim and monitor for errors

echo "======================================"
echo "Launching WinTrim for Testing"
echo "======================================"
echo ""
echo "Opening Console.app to monitor for errors..."
echo "Filter for 'WinTrim' in Console.app"
echo ""

# Open Console.app with filter (if possible)
open -a Console &

echo "Launching WinTrim..."
echo ""

cd WinTrim.Avalonia/bin/Release/net8.0

# Launch the app and capture output
./WinTrim 2>&1 | tee ~/Desktop/wintrim-test-log.txt &

APP_PID=$!

echo "✅ WinTrim launched (PID: $APP_PID)"
echo ""
echo "📋 Test checklist:"
echo "  1. Does the app window appear?"
echo "  2. Click 'Choose Folder' and select ~/Documents"
echo "  3. Wait for scan to complete"
echo "  4. Check DeleteMe folder: ls ~/Desktop/DeleteMe"
echo "  5. Add items to Collector"
echo "  6. Click 'Move to Cleanup Folder'"
echo "  7. Try QuickClean if available"
echo "  8. Switch themes in Settings"
echo ""
echo "📄 Output is being logged to: ~/Desktop/wintrim-test-log.txt"
echo ""
echo "Press Ctrl+C to stop monitoring when done testing"
echo ""

# Wait for app to exit
wait $APP_PID

echo ""
echo "App closed."
echo "Check ~/Desktop/wintrim-test-log.txt for any errors"
