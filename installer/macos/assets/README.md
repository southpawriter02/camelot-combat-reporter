# macOS Installer Assets

This directory contains assets for the macOS DMG installer.

## Required Files

### icon.icns
- Application icon in Apple Icon Image format
- Multiple sizes embedded: 16x16, 32x32, 64x64, 128x128, 256x256, 512x512, 1024x1024
- Used for the app and file associations

### VolumeIcon.icns
- DMG volume icon
- Same format as icon.icns
- Displayed when the DMG is mounted in Finder

### dmg-background.png
- DMG window background image
- Size: 600x400 pixels
- Standard resolution (1x)

### dmg-background@2x.png
- DMG window background image for Retina displays
- Size: 1200x800 pixels
- High resolution (2x)

## Creating Icons

### Using iconutil (macOS)

1. Create an iconset folder with all required sizes:
```bash
mkdir icon.iconset
sips -z 16 16     icon-1024.png --out icon.iconset/icon_16x16.png
sips -z 32 32     icon-1024.png --out icon.iconset/icon_16x16@2x.png
sips -z 32 32     icon-1024.png --out icon.iconset/icon_32x32.png
sips -z 64 64     icon-1024.png --out icon.iconset/icon_32x32@2x.png
sips -z 128 128   icon-1024.png --out icon.iconset/icon_128x128.png
sips -z 256 256   icon-1024.png --out icon.iconset/icon_128x128@2x.png
sips -z 256 256   icon-1024.png --out icon.iconset/icon_256x256.png
sips -z 512 512   icon-1024.png --out icon.iconset/icon_256x256@2x.png
sips -z 512 512   icon-1024.png --out icon.iconset/icon_512x512.png
sips -z 1024 1024 icon-1024.png --out icon.iconset/icon_512x512@2x.png
iconutil -c icns icon.iconset
```

2. Convert to icns:
```bash
iconutil -c icns icon.iconset -o icon.icns
```

### Creating DMG Background

The background should show:
- Application icon on the left
- Arrow pointing to Applications folder
- Application name and brief tagline

Example using ImageMagick:
```bash
# Create base background
convert -size 600x400 xc:'#1a1a2e' \
    -fill '#4a90d9' -draw "roundrectangle 20,20 580,380 10,10" \
    -fill white -gravity center -font Arial -pointsize 24 \
    -annotate +0-50 "Drag to Applications" \
    dmg-background.png

# Create @2x version
convert -size 1200x800 xc:'#1a1a2e' \
    -fill '#4a90d9' -draw "roundrectangle 40,40 1160,760 20,20" \
    -fill white -gravity center -font Arial -pointsize 48 \
    -annotate +0-100 "Drag to Applications" \
    dmg-background@2x.png
```

## Branding Guidelines

- Primary Color: #1a1a2e (Dark blue)
- Accent Color: #4a90d9 (Light blue)
- Text Color: #ffffff (White)
- The icon should represent combat/gaming analysis
- Keep the DMG background clean and professional
