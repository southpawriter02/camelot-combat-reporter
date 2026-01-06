#!/bin/bash
# notarize.sh
# Signs and notarizes the macOS application for distribution
#
# Usage: ./notarize.sh [app_path] [dmg_path]
#
# Environment variables required:
#   APPLE_DEVELOPER_ID    - Developer ID Application certificate name
#   APPLE_TEAM_ID         - Apple Developer Team ID
#   APPLE_ID              - Apple ID email
#   APPLE_ID_PASSWORD     - App-specific password for notarization
#   KEYCHAIN_PROFILE      - (optional) Stored keychain profile name
#
# To set up a keychain profile:
#   xcrun notarytool store-credentials "notary-profile" \
#       --apple-id "your@email.com" \
#       --team-id "TEAMID" \
#       --password "app-specific-password"

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STAGING_DIR="$SCRIPT_DIR/../../staging"
DIST_DIR="$SCRIPT_DIR/../../dist"
APP_NAME="Camelot Combat Reporter"
APP_PATH="${1:-$STAGING_DIR/$APP_NAME.app}"
DMG_PATH="${2:-}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
echo_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
echo_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Check if signing identity is available
if [ -z "$APPLE_DEVELOPER_ID" ]; then
    echo_warn "APPLE_DEVELOPER_ID not set. Skipping code signing."
    echo_warn "Set APPLE_DEVELOPER_ID to enable signing (e.g., 'Developer ID Application: Your Name (TEAMID)')"
    exit 0
fi

echo_info "Starting code signing and notarization process..."
echo_info "App path: $APP_PATH"

# Verify app exists
if [ ! -d "$APP_PATH" ]; then
    echo_error "App bundle not found at $APP_PATH"
    exit 1
fi

# Step 1: Sign all executables and libraries
echo_info "Signing app bundle..."

# Sign embedded frameworks and libraries first
find "$APP_PATH" -type f \( -name "*.dylib" -o -name "*.so" \) -exec \
    codesign --force --verify --verbose \
        --sign "$APPLE_DEVELOPER_ID" \
        --options runtime \
        --timestamp \
        {} \;

# Sign the main executable
codesign --deep --force --verify --verbose \
    --sign "$APPLE_DEVELOPER_ID" \
    --options runtime \
    --entitlements "$SCRIPT_DIR/entitlements.plist" \
    --timestamp \
    "$APP_PATH"

# Step 2: Verify signature
echo_info "Verifying signature..."
codesign --verify --deep --strict --verbose=2 "$APP_PATH"

# Check Gatekeeper assessment
echo_info "Checking Gatekeeper assessment..."
spctl --assess --type execute --verbose "$APP_PATH" || {
    echo_warn "Gatekeeper assessment failed. This is expected before notarization."
}

# Step 3: Create DMG if path not provided
if [ -z "$DMG_PATH" ]; then
    echo_info "Creating DMG for notarization..."
    VERSION=$(defaults read "$APP_PATH/Contents/Info.plist" CFBundleShortVersionString 2>/dev/null || echo "1.0.0")
    DMG_PATH="$DIST_DIR/CamelotCombatReporter-$VERSION.dmg"

    # Create DMG
    "$SCRIPT_DIR/create-dmg.sh" "$VERSION"
fi

# Step 4: Notarize
echo_info "Submitting for notarization..."

if [ -n "$KEYCHAIN_PROFILE" ]; then
    # Use stored keychain profile
    xcrun notarytool submit "$DMG_PATH" \
        --keychain-profile "$KEYCHAIN_PROFILE" \
        --wait
elif [ -n "$APPLE_ID" ] && [ -n "$APPLE_ID_PASSWORD" ] && [ -n "$APPLE_TEAM_ID" ]; then
    # Use environment variables
    xcrun notarytool submit "$DMG_PATH" \
        --apple-id "$APPLE_ID" \
        --password "$APPLE_ID_PASSWORD" \
        --team-id "$APPLE_TEAM_ID" \
        --wait
else
    echo_error "Notarization credentials not configured."
    echo_error "Set KEYCHAIN_PROFILE or (APPLE_ID, APPLE_ID_PASSWORD, APPLE_TEAM_ID)"
    exit 1
fi

# Step 5: Staple the notarization ticket
echo_info "Stapling notarization ticket..."
xcrun stapler staple "$DMG_PATH"

# Step 6: Verify stapling
echo_info "Verifying notarization..."
xcrun stapler validate "$DMG_PATH"
spctl --assess --type open --context context:primary-signature --verbose "$DMG_PATH"

echo_info "Notarization complete!"
echo_info "DMG is ready for distribution: $DMG_PATH"
