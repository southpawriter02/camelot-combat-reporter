#!/bin/bash
# create-rpm.sh
# Creates an RPM package for Fedora/RHEL/CentOS distributions
#
# Usage: ./create-rpm.sh [version] [publish_dir]
#
# Requires: rpm-build package
# Install: sudo dnf install rpm-build (Fedora) or sudo yum install rpm-build (RHEL/CentOS)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERSION="${1:-1.6.0}"
PUBLISH_DIR="${2:-$SCRIPT_DIR/../../publish/linux-x64}"
OUTPUT_DIR="$SCRIPT_DIR/../../dist"
PACKAGE_NAME="camelot-combat-reporter"

echo "Creating RPM package..."
echo "Version: $VERSION"
echo "Publish directory: $PUBLISH_DIR"
echo "Output directory: $OUTPUT_DIR"

# Create directories
mkdir -p "$OUTPUT_DIR"

# Set up RPM build environment
RPM_BUILD_ROOT="$OUTPUT_DIR/rpmbuild"
rm -rf "$RPM_BUILD_ROOT"
mkdir -p "$RPM_BUILD_ROOT"/{BUILD,RPMS,SOURCES,SPECS,SRPMS}

# Copy spec file
cp "$SCRIPT_DIR/rpm/$PACKAGE_NAME.spec" "$RPM_BUILD_ROOT/SPECS/"

# Update version in spec file
sed -i "s/^Version:.*/Version:        $VERSION/" "$RPM_BUILD_ROOT/SPECS/$PACKAGE_NAME.spec"

# Create source directory and copy files
mkdir -p "$RPM_BUILD_ROOT/SOURCES/publish"
cp -r "$PUBLISH_DIR"/* "$RPM_BUILD_ROOT/SOURCES/publish/"

# Copy icon if available
if [ -f "$SCRIPT_DIR/AppDir/usr/share/icons/hicolor/256x256/apps/camelot-combat-reporter.png" ]; then
    cp "$SCRIPT_DIR/AppDir/usr/share/icons/hicolor/256x256/apps/camelot-combat-reporter.png" \
       "$RPM_BUILD_ROOT/SOURCES/publish/"
fi

# Create LICENSE file if not present
if [ ! -f "$RPM_BUILD_ROOT/SOURCES/publish/LICENSE" ]; then
    cat > "$RPM_BUILD_ROOT/SOURCES/publish/LICENSE" << 'EOF'
MIT License

Copyright (c) 2026 Camelot Combat Reporter Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
EOF
fi

# Build RPM
echo "Building RPM..."
rpmbuild --define "_topdir $RPM_BUILD_ROOT" \
         --define "_sourcedir $RPM_BUILD_ROOT/SOURCES" \
         -bb "$RPM_BUILD_ROOT/SPECS/$PACKAGE_NAME.spec"

# Copy result to output directory
cp "$RPM_BUILD_ROOT/RPMS/x86_64"/*.rpm "$OUTPUT_DIR/"

# Clean up
rm -rf "$RPM_BUILD_ROOT"

echo ""
echo "RPM package created in: $OUTPUT_DIR"
echo ""
echo "To install: sudo dnf install ./${PACKAGE_NAME}-${VERSION}*.rpm"
echo "Or: sudo rpm -i ${PACKAGE_NAME}-${VERSION}*.rpm"
echo ""
echo "File info:"
ls -lh "$OUTPUT_DIR"/*.rpm 2>/dev/null || echo "No RPM files found"
