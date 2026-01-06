# Windows Installer Assets

This directory contains assets for the Windows MSI installer.

## Required Files

### icon.ico
- Application icon
- Multiple sizes recommended: 16x16, 32x32, 48x48, 256x256
- Used in Add/Remove Programs and file associations

### banner.bmp
- Installer banner image
- Size: 493x58 pixels
- 24-bit BMP format
- Displayed at the top of installer dialogs

### dialog.bmp
- Installer dialog background
- Size: 493x312 pixels
- 24-bit BMP format
- Displayed on the welcome and completion dialogs

### license.rtf
- License agreement in RTF format
- Already provided (MIT License)

## Creating Placeholder Images

To create placeholder images for testing:

```bash
# Using ImageMagick
convert -size 493x58 xc:#1a1a2e -fill white -gravity center \
    -font Arial -pointsize 20 -annotate 0 "Camelot Combat Reporter" \
    banner.bmp

convert -size 493x312 xc:#1a1a2e -fill white -gravity center \
    -font Arial -pointsize 24 -annotate 0 "Camelot Combat Reporter\nSetup" \
    dialog.bmp

convert -size 256x256 xc:#4a90d9 -fill white -gravity center \
    -font Arial -pointsize 48 -annotate 0 "CCR" \
    icon.png
# Then convert to .ico format
```

## Branding Guidelines

- Primary Color: #1a1a2e (Dark blue)
- Accent Color: #4a90d9 (Light blue)
- Text Color: #ffffff (White)
- Font: Segoe UI (Windows system font)
