#!/bin/bash
# create-dmg.sh
# Creates a macOS DMG installer with drag-to-Applications functionality
#
# Usage: ./create-dmg.sh [version]
#
# Requires: create-dmg (brew install create-dmg)
# The .app bundle must already exist in the staging directory.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERSION="${1:-1.6.0}"
STAGING_DIR="$SCRIPT_DIR/../../staging"
OUTPUT_DIR="$SCRIPT_DIR/../../dist"
APP_NAME="Camelot Combat Reporter"
DMG_NAME="CamelotCombatReporter-$VERSION.dmg"

echo "Creating DMG installer..."
echo "Version: $VERSION"
echo "Staging directory: $STAGING_DIR"
echo "Output: $OUTPUT_DIR/$DMG_NAME"

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Verify app bundle exists
if [ ! -d "$STAGING_DIR/$APP_NAME.app" ]; then
    echo "Error: App bundle not found at $STAGING_DIR/$APP_NAME.app"
    echo "Please run create-app-bundle.sh first."
    exit 1
fi

# Remove old DMG if it exists
rm -f "$OUTPUT_DIR/$DMG_NAME"

# Check if create-dmg is available
if command -v create-dmg &> /dev/null; then
    echo "Using create-dmg tool..."

    # Build create-dmg command
    CREATE_DMG_ARGS=(
        --volname "$APP_NAME"
        --window-pos 200 120
        --window-size 600 400
        --icon-size 100
        --icon "$APP_NAME.app" 150 190
        --app-drop-link 450 190
        --hide-extension "$APP_NAME.app"
    )

    # Add background if it exists
    if [ -f "$SCRIPT_DIR/assets/dmg-background@2x.png" ]; then
        CREATE_DMG_ARGS+=(--background "$SCRIPT_DIR/assets/dmg-background@2x.png")
    elif [ -f "$SCRIPT_DIR/assets/dmg-background.png" ]; then
        CREATE_DMG_ARGS+=(--background "$SCRIPT_DIR/assets/dmg-background.png")
    fi

    # Add volume icon if it exists
    if [ -f "$SCRIPT_DIR/assets/VolumeIcon.icns" ]; then
        CREATE_DMG_ARGS+=(--volicon "$SCRIPT_DIR/assets/VolumeIcon.icns")
    fi

    create-dmg "${CREATE_DMG_ARGS[@]}" "$OUTPUT_DIR/$DMG_NAME" "$STAGING_DIR/$APP_NAME.app"
else
    echo "create-dmg not found, using hdiutil..."

    # Create temporary DMG directory
    DMG_TEMP="$OUTPUT_DIR/dmg_temp"
    rm -rf "$DMG_TEMP"
    mkdir -p "$DMG_TEMP"

    # Copy app to temp directory
    cp -R "$STAGING_DIR/$APP_NAME.app" "$DMG_TEMP/"

    # Create symbolic link to Applications
    ln -s /Applications "$DMG_TEMP/Applications"

    # Create DMG
    hdiutil create -volname "$APP_NAME" \
        -srcfolder "$DMG_TEMP" \
        -ov -format UDZO \
        "$OUTPUT_DIR/$DMG_NAME"

    # Clean up
    rm -rf "$DMG_TEMP"
fi

echo ""
echo "DMG created successfully: $OUTPUT_DIR/$DMG_NAME"
echo ""
echo "File info:"
ls -lh "$OUTPUT_DIR/$DMG_NAME"
