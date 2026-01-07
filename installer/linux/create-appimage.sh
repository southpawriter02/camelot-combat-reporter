#!/bin/bash
# create-appimage.sh
# Creates an AppImage for Linux distribution
#
# Usage: ./create-appimage.sh [version] [publish_dir]
#
# Requires: appimagetool (https://github.com/AppImage/AppImageKit)
# Download: wget https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERSION="${1:-1.6.0}"
PUBLISH_DIR="${2:-$SCRIPT_DIR/../../publish/linux-x64}"
OUTPUT_DIR="$SCRIPT_DIR/../../dist"
APPDIR="$SCRIPT_DIR/AppDir"
STAGING_DIR="$OUTPUT_DIR/AppImage-staging"

echo "Creating AppImage..."
echo "Version: $VERSION"
echo "Publish directory: $PUBLISH_DIR"
echo "Output directory: $OUTPUT_DIR"

# Create directories
mkdir -p "$OUTPUT_DIR"
rm -rf "$STAGING_DIR"
mkdir -p "$STAGING_DIR"

# Copy AppDir template
cp -r "$APPDIR"/* "$STAGING_DIR/"

# Create usr/bin directory
mkdir -p "$STAGING_DIR/usr/bin"

# Copy application files
echo "Copying application files..."
cp -r "$PUBLISH_DIR"/* "$STAGING_DIR/usr/bin/"

# Make executable
chmod +x "$STAGING_DIR/usr/bin/CamelotCombatReporter.Gui"
chmod +x "$STAGING_DIR/AppRun"

# Ensure required directories exist BEFORE copying (empty dirs may not be tracked by git)
mkdir -p "$STAGING_DIR/usr/share/applications"
mkdir -p "$STAGING_DIR/usr/share/icons/hicolor/256x256/apps"

# Copy desktop file to share/applications
cp "$STAGING_DIR/camelot-combat-reporter.desktop" "$STAGING_DIR/usr/share/applications/"

# Create symlink for icon at root (required by AppImage)
if [ -f "$STAGING_DIR/usr/share/icons/hicolor/256x256/apps/camelot-combat-reporter.png" ]; then
    cp "$STAGING_DIR/usr/share/icons/hicolor/256x256/apps/camelot-combat-reporter.png" \
       "$STAGING_DIR/camelot-combat-reporter.png"
else
    echo "Warning: Icon not found. Creating placeholder..."
    # Create a simple placeholder icon (1x1 transparent PNG)
    echo -n -e '\x89PNG\r\n\x1a\n\x00\x00\x00\rIHDR\x00\x00\x01\x00\x00\x00\x01\x00\x08\x06\x00\x00\x00' > "$STAGING_DIR/camelot-combat-reporter.png"
fi

# Download appimagetool if not available
APPIMAGETOOL=""
if command -v appimagetool &> /dev/null; then
    APPIMAGETOOL="appimagetool"
elif [ -f "$SCRIPT_DIR/appimagetool-x86_64.AppImage" ]; then
    chmod +x "$SCRIPT_DIR/appimagetool-x86_64.AppImage"
    APPIMAGETOOL="$SCRIPT_DIR/appimagetool-x86_64.AppImage"
else
    echo "Downloading appimagetool..."
    wget -q "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage" \
         -O "$SCRIPT_DIR/appimagetool-x86_64.AppImage"
    chmod +x "$SCRIPT_DIR/appimagetool-x86_64.AppImage"
    APPIMAGETOOL="$SCRIPT_DIR/appimagetool-x86_64.AppImage"
fi

# Build AppImage
echo "Building AppImage..."
ARCH=x86_64 "$APPIMAGETOOL" "$STAGING_DIR" "$OUTPUT_DIR/CamelotCombatReporter-$VERSION-x86_64.AppImage"

# Clean up staging directory
rm -rf "$STAGING_DIR"

echo ""
echo "AppImage created: $OUTPUT_DIR/CamelotCombatReporter-$VERSION-x86_64.AppImage"
echo ""
echo "File info:"
ls -lh "$OUTPUT_DIR/CamelotCombatReporter-$VERSION-x86_64.AppImage"
