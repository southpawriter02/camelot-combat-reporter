#!/bin/bash
# create-deb.sh
# Creates a Debian package (.deb) for Ubuntu/Debian distributions
#
# Usage: ./create-deb.sh [version] [publish_dir]

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERSION="${1:-1.6.0}"
PUBLISH_DIR="${2:-$SCRIPT_DIR/../../publish/linux-x64}"
OUTPUT_DIR="$SCRIPT_DIR/../../dist"
PACKAGE_NAME="camelot-combat-reporter"
STAGING_DIR="$OUTPUT_DIR/deb-staging"

echo "Creating Debian package..."
echo "Version: $VERSION"
echo "Publish directory: $PUBLISH_DIR"
echo "Output directory: $OUTPUT_DIR"

# Create directories
mkdir -p "$OUTPUT_DIR"
rm -rf "$STAGING_DIR"
mkdir -p "$STAGING_DIR/DEBIAN"
mkdir -p "$STAGING_DIR/opt/$PACKAGE_NAME"
mkdir -p "$STAGING_DIR/usr/share/applications"
mkdir -p "$STAGING_DIR/usr/share/icons/hicolor/256x256/apps"
mkdir -p "$STAGING_DIR/usr/share/mime/packages"

# Copy DEBIAN control files
echo "Copying control files..."
cp "$SCRIPT_DIR/debian/control" "$STAGING_DIR/DEBIAN/"
cp "$SCRIPT_DIR/debian/postinst" "$STAGING_DIR/DEBIAN/"
cp "$SCRIPT_DIR/debian/postrm" "$STAGING_DIR/DEBIAN/"
cp "$SCRIPT_DIR/debian/copyright" "$STAGING_DIR/DEBIAN/"

# Update version in control file
sed -i "s/^Version:.*/Version: $VERSION/" "$STAGING_DIR/DEBIAN/control"

# Make scripts executable
chmod 755 "$STAGING_DIR/DEBIAN/postinst"
chmod 755 "$STAGING_DIR/DEBIAN/postrm"

# Copy application files
echo "Copying application files..."
cp -r "$PUBLISH_DIR"/* "$STAGING_DIR/opt/$PACKAGE_NAME/"

# Make main executable
chmod +x "$STAGING_DIR/opt/$PACKAGE_NAME/CamelotCombatReporter.Gui"

# Copy desktop file
cp "$SCRIPT_DIR/AppDir/camelot-combat-reporter.desktop" "$STAGING_DIR/usr/share/applications/"
# Update Exec path in desktop file
sed -i "s|Exec=CamelotCombatReporter.Gui|Exec=/opt/$PACKAGE_NAME/CamelotCombatReporter.Gui|" \
    "$STAGING_DIR/usr/share/applications/camelot-combat-reporter.desktop"

# Copy icon if available
if [ -f "$SCRIPT_DIR/AppDir/usr/share/icons/hicolor/256x256/apps/camelot-combat-reporter.png" ]; then
    cp "$SCRIPT_DIR/AppDir/usr/share/icons/hicolor/256x256/apps/camelot-combat-reporter.png" \
       "$STAGING_DIR/usr/share/icons/hicolor/256x256/apps/"
fi

# Create MIME type definition
cat > "$STAGING_DIR/usr/share/mime/packages/camelot-combat-reporter.xml" << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<mime-info xmlns="http://www.freedesktop.org/standards/shared-mime-info">
    <mime-type type="application/x-combat-log">
        <comment>Combat Log File</comment>
        <glob pattern="*.combat"/>
        <icon name="camelot-combat-reporter"/>
    </mime-type>
</mime-info>
EOF

# Calculate installed size
INSTALLED_SIZE=$(du -sk "$STAGING_DIR/opt" | cut -f1)
sed -i "/^Description:/i Installed-Size: $INSTALLED_SIZE" "$STAGING_DIR/DEBIAN/control"

# Build package
echo "Building package..."
DEB_FILE="${PACKAGE_NAME}_${VERSION}_amd64.deb"
dpkg-deb --build --root-owner-group "$STAGING_DIR" "$OUTPUT_DIR/$DEB_FILE"

# Clean up
rm -rf "$STAGING_DIR"

echo ""
echo "Debian package created: $OUTPUT_DIR/$DEB_FILE"
echo ""
echo "To install: sudo dpkg -i $DEB_FILE"
echo "Or: sudo apt install ./$DEB_FILE"
echo ""
echo "File info:"
ls -lh "$OUTPUT_DIR/$DEB_FILE"
