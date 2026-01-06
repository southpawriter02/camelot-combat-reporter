#!/bin/bash
# create-universal.sh
# Creates a universal binary (x64 + ARM64) for macOS
#
# Usage: ./create-universal.sh [publish_dir]
#
# This script combines the x64 and ARM64 builds into a single universal binary
# that runs natively on both Intel and Apple Silicon Macs.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PUBLISH_DIR="${1:-$SCRIPT_DIR/../../publish}"
OUTPUT_DIR="$PUBLISH_DIR/osx-universal"

echo "Creating universal binary..."
echo "Publish directory: $PUBLISH_DIR"
echo "Output directory: $OUTPUT_DIR"

# Check that both architectures exist
if [ ! -d "$PUBLISH_DIR/osx-x64" ]; then
    echo "Error: osx-x64 build not found at $PUBLISH_DIR/osx-x64"
    exit 1
fi

if [ ! -d "$PUBLISH_DIR/osx-arm64" ]; then
    echo "Error: osx-arm64 build not found at $PUBLISH_DIR/osx-arm64"
    exit 1
fi

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Find the main executable
MAIN_EXEC="CamelotCombatReporter.Gui"

# Create universal binary for the main executable
if [ -f "$PUBLISH_DIR/osx-x64/$MAIN_EXEC" ] && [ -f "$PUBLISH_DIR/osx-arm64/$MAIN_EXEC" ]; then
    echo "Creating universal binary for $MAIN_EXEC..."
    lipo -create \
        "$PUBLISH_DIR/osx-x64/$MAIN_EXEC" \
        "$PUBLISH_DIR/osx-arm64/$MAIN_EXEC" \
        -output "$OUTPUT_DIR/$MAIN_EXEC"
    chmod +x "$OUTPUT_DIR/$MAIN_EXEC"
else
    echo "Error: Main executable not found"
    exit 1
fi

# Copy all other files from one of the builds (they should be architecture-independent)
echo "Copying supporting files..."
for file in "$PUBLISH_DIR/osx-x64"/*; do
    filename=$(basename "$file")
    if [ "$filename" != "$MAIN_EXEC" ]; then
        # Check if it's a native library that needs lipo
        if file "$file" | grep -q "Mach-O"; then
            if [ -f "$PUBLISH_DIR/osx-arm64/$filename" ]; then
                echo "Creating universal binary for $filename..."
                lipo -create \
                    "$PUBLISH_DIR/osx-x64/$filename" \
                    "$PUBLISH_DIR/osx-arm64/$filename" \
                    -output "$OUTPUT_DIR/$filename" 2>/dev/null || cp "$file" "$OUTPUT_DIR/"
            else
                cp "$file" "$OUTPUT_DIR/"
            fi
        else
            cp "$file" "$OUTPUT_DIR/"
        fi
    fi
done

echo "Universal binary created at $OUTPUT_DIR"
echo ""
echo "Verification:"
file "$OUTPUT_DIR/$MAIN_EXEC"
lipo -info "$OUTPUT_DIR/$MAIN_EXEC"
