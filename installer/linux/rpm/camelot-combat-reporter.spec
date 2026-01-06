Name:           camelot-combat-reporter
Version:        1.6.0
Release:        1%{?dist}
Summary:        Combat log analyzer for Dark Age of Camelot

License:        MIT
URL:            https://github.com/southpawriter02/camelot-combat-reporter
Source0:        %{name}-%{version}.tar.gz

BuildArch:      x86_64
AutoReqProv:    no

Requires:       libicu >= 70
Requires:       openssl-libs >= 1.1
Requires:       zlib

%description
Camelot Combat Reporter is a cross-platform application for parsing
and analyzing combat logs from Dark Age of Camelot (DAoC).

Features include:
- Real-time log parsing and analysis
- Damage, healing, and combat statistics
- RvR siege tracking and battleground statistics
- Relic raid tracking
- Session comparison and trend analysis
- Plugin support for extensibility

%prep
# No prep needed - we're using pre-built binaries

%install
rm -rf %{buildroot}

# Create directories
mkdir -p %{buildroot}/opt/%{name}
mkdir -p %{buildroot}/usr/bin
mkdir -p %{buildroot}/usr/share/applications
mkdir -p %{buildroot}/usr/share/icons/hicolor/256x256/apps
mkdir -p %{buildroot}/usr/share/mime/packages

# Copy application files
cp -r %{_sourcedir}/publish/* %{buildroot}/opt/%{name}/

# Create symlink
ln -sf /opt/%{name}/CamelotCombatReporter.Gui %{buildroot}/usr/bin/camelot-combat-reporter

# Copy desktop file
cat > %{buildroot}/usr/share/applications/%{name}.desktop << 'EOF'
[Desktop Entry]
Type=Application
Name=Camelot Combat Reporter
GenericName=Combat Log Analyzer
Comment=Parse and analyze combat logs from Dark Age of Camelot
Exec=/opt/camelot-combat-reporter/CamelotCombatReporter.Gui %F
Icon=camelot-combat-reporter
Terminal=false
Categories=Game;Utility;
Keywords=daoc;combat;log;analyzer;mmorpg;
MimeType=application/x-combat-log;text/x-log;
StartupNotify=true
EOF

# Copy MIME type definition
cat > %{buildroot}/usr/share/mime/packages/%{name}.xml << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<mime-info xmlns="http://www.freedesktop.org/standards/shared-mime-info">
    <mime-type type="application/x-combat-log">
        <comment>Combat Log File</comment>
        <glob pattern="*.combat"/>
        <icon name="camelot-combat-reporter"/>
    </mime-type>
</mime-info>
EOF

%post
# Update desktop database
update-desktop-database /usr/share/applications &> /dev/null || :

# Update icon cache
gtk-update-icon-cache /usr/share/icons/hicolor &> /dev/null || :

# Update MIME database
update-mime-database /usr/share/mime &> /dev/null || :

%postun
# Update desktop database
update-desktop-database /usr/share/applications &> /dev/null || :

# Update icon cache
gtk-update-icon-cache /usr/share/icons/hicolor &> /dev/null || :

# Update MIME database
update-mime-database /usr/share/mime &> /dev/null || :

%files
%license /opt/%{name}/LICENSE
/opt/%{name}
/usr/bin/camelot-combat-reporter
/usr/share/applications/%{name}.desktop
/usr/share/icons/hicolor/256x256/apps/camelot-combat-reporter.png
/usr/share/mime/packages/%{name}.xml

%changelog
* Mon Jan 06 2026 Camelot Combat Reporter Contributors <noreply@github.com> - 1.6.0-1
- Distribution Builds Phase 2
- Windows MSI installer with file associations
- macOS DMG with universal binary support
- Linux packages (AppImage, deb, rpm)
- Auto-Update System

* Sun Jan 05 2026 Camelot Combat Reporter Contributors <noreply@github.com> - 1.5.0-1
- RvR Features
- Keep and siege tracking
- Relic tracking and raid sessions
- Battleground statistics
