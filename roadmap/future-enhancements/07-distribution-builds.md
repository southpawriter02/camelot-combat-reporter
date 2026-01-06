# 7. Distribution Builds

## Status: ✅ Complete (v1.6.0)

**Prerequisites:**
- ✅ Cross-platform GUI (Avalonia)
- ✅ .NET 9.0 project structure
- ✅ Build pipeline configuration
- ⬚ Code signing certificates (infrastructure ready, certificates optional)

---

## Description

Create distributable executables for all major platforms, including installers, portable packages, and app store distributions. This enables end users to easily install and run Camelot Combat Reporter without requiring .NET SDK installation.

## Functionality

### Target Platforms

| Platform | Format | Distribution Method |
|----------|--------|---------------------|
| **Windows** | `.exe`, `.msi`, `.msix` | Direct download, Microsoft Store |
| **macOS** | `.dmg`, `.app`, `.pkg` | Direct download, Homebrew |
| **Linux** | `.deb`, `.rpm`, `.AppImage`, `.snap`, `.flatpak` | Direct download, package managers |

### Build Types

* **Self-Contained:**
  * Includes .NET runtime
  * No dependencies required
  * Larger file size (~60-80MB)
  * Ideal for most users

* **Framework-Dependent:**
  * Requires .NET 9.0 runtime
  * Smaller file size (~5-10MB)
  * For users with .NET installed

* **Single-File:**
  * Everything in one executable
  * Extracts to temp on first run
  * Clean distribution, slower startup

* **Native AOT (Future):**
  * Ahead-of-time compilation
  * Fastest startup
  * Smallest runtime footprint
  * Limited reflection support

### Windows Distribution

* **Installer (.msi):**
  * WiX Toolset-based installer
  * Start menu shortcuts
  * File associations (.combat, .log)
  * Uninstaller support
  * Optional desktop shortcut

* **MSIX Package:**
  * Modern Windows packaging
  * Microsoft Store compatible
  * Automatic updates support
  * Sandboxed installation
  * AppInstaller for sideloading

* **Portable (.zip):**
  * No installation required
  * Run from any location
  * Settings in app folder

### macOS Distribution

* **DMG Installer:**
  * Drag-to-Applications install
  * Background image with instructions
  * Code signed and notarized
  * Universal binary (x64 + ARM64)

* **PKG Installer:**
  * Standard macOS installer
  * Pre/post install scripts
  * System-wide installation option

* **Homebrew Cask:**
  * `brew install --cask camelot-combat-reporter`
  * Automatic updates
  * Easy installation/removal

### Linux Distribution

* **AppImage:**
  * Universal Linux format
  * No installation required
  * Automatic updates (AppImageUpdate)
  * Desktop integration optional

* **Debian Package (.deb):**
  * Ubuntu, Debian, Mint
  * `sudo apt install ./package.deb`
  * Dependency management

* **RPM Package:**
  * Fedora, RHEL, CentOS
  * `sudo dnf install ./package.rpm`
  * Spec file maintenance

* **Snap Package:**
  * Ubuntu Snap Store
  * Sandboxed execution
  * Automatic updates

* **Flatpak:**
  * Flathub distribution
  * Sandboxed execution
  * Broad distro support

## Requirements

* **Build Infrastructure:**
  * GitHub Actions for CI/CD
  * Build agents for each platform
  * Artifact storage and hosting

* **Code Signing:**
  * Windows: Authenticode certificate
  * macOS: Apple Developer ID + Notarization
  * Linux: GPG signing for packages

* **Release Management:**
  * Semantic versioning
  * Changelog generation
  * Release notes automation
  * Update notification system

## Limitations

* macOS requires Apple Developer membership ($99/year)
* Windows Store requires Microsoft Partner account
* Native AOT may not support all Avalonia features
* Each platform requires specific build tooling

## Dependencies

* **Avalonia:** Cross-platform UI framework
* **.NET 9.0:** Runtime and build tools
* **GitHub Actions:** CI/CD pipeline

## Implementation Phases

### Phase 1: Basic Builds
- [ ] Configure `dotnet publish` for self-contained builds
- [ ] Create GitHub Actions workflow for Windows x64
- [ ] Create GitHub Actions workflow for macOS (x64 + ARM64)
- [ ] Create GitHub Actions workflow for Linux x64
- [ ] Generate portable .zip archives

### Phase 2: Windows Installers
- [ ] Set up WiX Toolset project
- [ ] Create MSI installer template
- [ ] Add file associations
- [ ] Obtain code signing certificate
- [ ] Sign installer and executables

### Phase 3: macOS Distribution
- [ ] Create DMG with background and layout
- [ ] Set up notarization workflow
- [ ] Build universal binary (x64 + ARM64)
- [ ] Submit to Homebrew Cask

### Phase 4: Linux Packages
- [ ] Create AppImage build script
- [ ] Set up .deb packaging
- [ ] Set up .rpm packaging
- [ ] Create Snap package manifest
- [ ] Create Flatpak manifest

### Phase 5: Auto-Updates
- [ ] Implement update check mechanism
- [ ] Create update download/install flow
- [ ] Set up release feed (JSON/XML)
- [ ] Add update notification UI

## Technical Notes

### Publish Profiles

```xml
<!-- Windows x64 Self-Contained -->
<PropertyGroup>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <SelfContained>true</SelfContained>
  <PublishSingleFile>true</PublishSingleFile>
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
</PropertyGroup>

<!-- macOS Universal Binary -->
<PropertyGroup>
  <RuntimeIdentifiers>osx-x64;osx-arm64</RuntimeIdentifiers>
  <SelfContained>true</SelfContained>
</PropertyGroup>
```

### GitHub Actions Structure

```yaml
name: Build and Release

on:
  push:
    tags: ['v*']

jobs:
  build-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
      - run: dotnet publish -c Release -r win-x64
      - uses: actions/upload-artifact@v4

  build-macos:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
      - run: dotnet publish -c Release -r osx-x64
      - run: dotnet publish -c Release -r osx-arm64
      # Create universal binary, DMG, notarize

  build-linux:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
      - run: dotnet publish -c Release -r linux-x64
      # Create AppImage, deb, rpm

  release:
    needs: [build-windows, build-macos, build-linux]
    runs-on: ubuntu-latest
    steps:
      - uses: actions/download-artifact@v4
      - uses: softprops/action-gh-release@v1
```

### File Associations

```xml
<!-- Windows file associations -->
<FileType Name="Combat Log" Extension=".combat" ContentType="application/x-combat-log">
  <Icon Source="app.ico" Index="1"/>
</FileType>
```

### Update Feed Format

```json
{
  "version": "1.3.0",
  "releaseDate": "2025-01-02",
  "releaseNotes": "https://github.com/.../releases/v1.3.0",
  "downloads": {
    "win-x64": "https://.../CamelotCombatReporter-1.3.0-win-x64.msi",
    "osx-universal": "https://.../CamelotCombatReporter-1.3.0.dmg",
    "linux-x64": "https://.../CamelotCombatReporter-1.3.0-x86_64.AppImage"
  },
  "checksums": {
    "win-x64": "sha256:...",
    "osx-universal": "sha256:...",
    "linux-x64": "sha256:..."
  }
}
```
