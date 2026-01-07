#!/bin/bash
# create-app-bundle.sh
# Creates a macOS .app bundle from published files
#
# Usage: ./create-app-bundle.sh [publish_dir] [output_dir]
#
# This script packages the application into a proper macOS .app bundle
# that can be distributed via DMG or directly.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PUBLISH_DIR="${1:-$SCRIPT_DIR/../../publish/osx-universal}"
OUTPUT_DIR="${2:-$SCRIPT_DIR/../../staging}"
APP_NAME="Camelot Combat Reporter"
APP_BUNDLE="$OUTPUT_DIR/$APP_NAME.app"

echo "Creating app bundle..."
echo "Source: $PUBLISH_DIR"
echo "Output: $APP_BUNDLE"

# Clean up previous bundle
rm -rf "$APP_BUNDLE"

# Create bundle structure
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Copy Info.plist
cp "$SCRIPT_DIR/Info.plist" "$APP_BUNDLE/Contents/"

# Copy executable and all dependencies
echo "Copying application files..."
cp -R "$PUBLISH_DIR"/* "$APP_BUNDLE/Contents/MacOS/"

# Rename executable if needed
if [ -f "$APP_BUNDLE/Contents/MacOS/CamelotCombatReporter.Gui" ]; then
    # Keep original name as Info.plist references it
    chmod +x "$APP_BUNDLE/Contents/MacOS/CamelotCombatReporter.Gui"
fi

# Copy icon if it exists
if [ -f "$SCRIPT_DIR/assets/icon.icns" ]; then
    cp "$SCRIPT_DIR/assets/icon.icns" "$APP_BUNDLE/Contents/Resources/"
fi

# Create PkgInfo file
echo "APPL????" > "$APP_BUNDLE/Contents/PkgInfo"

# Set permissions
chmod -R 755 "$APP_BUNDLE"

echo ""
echo "App bundle created at: $APP_BUNDLE"
echo ""
echo "Bundle contents:"
ls -la "$APP_BUNDLE/Contents/"
ls -la "$APP_BUNDLE/Contents/MacOS/" | head -10
echo "..."
