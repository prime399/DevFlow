# DevFlow Downloads

Download DevFlow for your platform. All builds are self-contained and don't require .NET to be installed.

## 📥 Latest Release

[![GitHub Release](https://img.shields.io/github/v/release/prime399/DevFlow?style=for-the-badge)](https://github.com/prime399/DevFlow/releases/latest)

---

## 🪟 Windows

| Download | Architecture | Requirements |
|----------|-------------|--------------|
| [DevFlow-Windows-x64.zip](https://github.com/prime399/DevFlow/releases/latest/download/DevFlow-Windows-x64.zip) | x64 (Intel/AMD) | Windows 10/11 |
| [DevFlow-Windows-ARM64.zip](https://github.com/prime399/DevFlow/releases/latest/download/DevFlow-Windows-ARM64.zip) | ARM64 | Windows 11 ARM (Surface Pro X, etc.) |

### Installation
1. Download the ZIP file for your architecture
2. Extract to a folder of your choice
3. Run `DevFlow.exe`

---

## 🍎 macOS

| Download | Architecture | Requirements |
|----------|-------------|--------------|
| [DevFlow-macOS-ARM64.zip](https://github.com/prime399/DevFlow/releases/latest/download/DevFlow-macOS-ARM64.zip) | Apple Silicon | macOS 11+ (M1/M2/M3/M4 Macs) |

> **Note**: Only Apple Silicon (M1/M2/M3/M4) Macs are supported. Intel Mac users can use the [Web Version](#-web-version-webassembly).

### Installation
1. Download the ZIP file
2. Extract the `DevFlow.app` bundle
3. Move `DevFlow.app` to your `/Applications` folder
4. **First launch**: Right-click → Open (to bypass Gatekeeper)

> **Note**: If you see "DevFlow is damaged and can't be opened", run:
> ```bash
> xattr -cr /Applications/DevFlow.app
> ```

---

## 🐧 Linux

### Universal Downloads

| Download | Format | Compatible With |
|----------|--------|-----------------|
| [DevFlow-Linux-x64.tar.gz](https://github.com/prime399/DevFlow/releases/latest/download/DevFlow-Linux-x64.tar.gz) | Tarball | Any x64 Linux distro |
| [DevFlow-Linux-ARM64.tar.gz](https://github.com/prime399/DevFlow/releases/latest/download/DevFlow-Linux-ARM64.tar.gz) | Tarball | ARM64 (Raspberry Pi, etc.) |
| [DevFlow-Linux-x64.AppImage](https://github.com/prime399/DevFlow/releases/latest/download/DevFlow-Linux-x64.AppImage) | AppImage | Any x64 Linux distro |

### Distribution-Specific Packages

#### Debian / Ubuntu / Linux Mint / Pop!_OS

| Download | Architecture |
|----------|-------------|
| [DevFlow-Linux-x64.deb](https://github.com/prime399/DevFlow/releases/latest/download/DevFlow-Linux-x64.deb) | x64 (amd64) |
| [DevFlow-Linux-ARM64.deb](https://github.com/prime399/DevFlow/releases/latest/download/DevFlow-Linux-ARM64.deb) | ARM64 |

```bash
# Install with dpkg
sudo dpkg -i DevFlow-Linux-x64.deb

# Or with apt (handles dependencies)
sudo apt install ./DevFlow-Linux-x64.deb
```

#### Fedora / RHEL / CentOS / openSUSE

| Download | Architecture |
|----------|-------------|
| [DevFlow-Linux-x64.rpm](https://github.com/prime399/DevFlow/releases/latest/download/DevFlow-Linux-x64.rpm) | x64 |

```bash
# Fedora / RHEL 8+
sudo dnf install DevFlow-Linux-x64.rpm

# CentOS 7 / older RHEL
sudo yum install DevFlow-Linux-x64.rpm

# openSUSE
sudo zypper install DevFlow-Linux-x64.rpm
```

### Installation Instructions

#### Tarball (.tar.gz)
```bash
# Extract
tar -xzf DevFlow-Linux-x64.tar.gz -C ~/Applications/DevFlow

# Run
~/Applications/DevFlow/DevFlow
```

#### AppImage
```bash
# Make executable
chmod +x DevFlow-Linux-x64.AppImage

# Run
./DevFlow-Linux-x64.AppImage
```

---

## 🌐 Web Version (WebAssembly)

DevFlow is also available as a web application:

🔗 **[Launch DevFlow Web](https://devflow.vercel.app)** *(or your deployment URL)*

No installation required - runs directly in your browser!

---

## 🔧 System Requirements

### Minimum Requirements

| Platform | Requirement |
|----------|-------------|
| **Windows** | Windows 10 (1809+) or Windows 11 |
| **macOS** | macOS 10.15 Catalina or later |
| **Linux** | glibc 2.17+ (most modern distros) |

### Recommended

- 4GB RAM
- 200MB disk space
- Display: 1280x720 or higher

---

## 🏗️ Build from Source

If you prefer to build from source:

```bash
# Clone the repository
git clone https://github.com/prime399/DevFlow.git
cd DevFlow

# Build for your platform
dotnet publish DevFlow/DevFlow.csproj -c Release -f net10.0-desktop

# The output will be in DevFlow/bin/Release/net10.0-desktop/publish/
```

---

## ❓ Troubleshooting

### Windows
- **"Windows protected your PC"**: Click "More info" → "Run anyway"
- **Missing DLLs**: Install [Visual C++ Redistributable](https://aka.ms/vs/17/release/vc_redist.x64.exe)

### macOS
- **"App is damaged"**: Run `xattr -cr /Applications/DevFlow.app`
- **"Unidentified developer"**: Right-click → Open → Open

### Linux
- **Missing libraries**: Install GTK3 and dependencies:
  ```bash
  # Ubuntu/Debian
  sudo apt install libgtk-3-0 libwebkit2gtk-4.0-37
  
  # Fedora
  sudo dnf install gtk3 webkit2gtk3
  ```

---

## 📝 Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history and release notes.

---

## 📄 License

DevFlow is released under the [MIT License](LICENSE).
