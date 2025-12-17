# DevFlow

<p align="center">
  <img src="DevFlow/Assets/Icons/icon_foreground.svg" alt="DevFlow Logo" width="128" height="128">
</p>

<p align="center">
  <strong>A cross-platform API client built with Uno Platform</strong>
</p>

<p align="center">
  <a href="https://github.com/prime399/DevFlow/releases">
    <img src="https://img.shields.io/github/v/release/prime399/DevFlow?style=flat-square" alt="Release">
  </a>
  <a href="https://github.com/prime399/DevFlow/blob/master/LICENSE">
    <img src="https://img.shields.io/github/license/prime399/DevFlow?style=flat-square" alt="License">
  </a>
  <a href="https://devflow.primefolio.tech/">
    <img src="https://img.shields.io/badge/demo-live-brightgreen?style=flat-square" alt="Live Demo">
  </a>
</p>

<p align="center">
  <a href="https://devflow.primefolio.tech/">Live Demo (WebAssembly)</a> •
  <a href="https://github.com/prime399/DevFlow/releases">Download</a> •
  <a href="#features">Features</a> •
  <a href="#installation">Installation</a>
</p>

---

## Overview

**DevFlow** is a full-featured, cross-platform API client application similar to Postman or Hoppscotch. Built entirely with [Uno Platform](https://platform.uno/), it runs on Windows, macOS, Linux, and WebAssembly from a single codebase.

Test REST APIs, GraphQL endpoints, and real-time connections (WebSocket/SSE) from a unified, modern interface with a beautiful dark theme.

---

## Live Demo

Try DevFlow instantly in your browser - no installation required:

### [devflow.primefolio.tech](https://devflow.primefolio.tech/)

---

## Downloads

Download the latest release for your platform:

### [GitHub Releases](https://github.com/prime399/DevFlow/releases)

| Platform | Architecture | Download |
|----------|--------------|----------|
| **Windows** | x64, ARM64 | [Latest Release](https://github.com/prime399/DevFlow/releases) |
| **macOS** | ARM64 (Apple Silicon) | [Latest Release](https://github.com/prime399/DevFlow/releases) |
| **Linux** | AMD64 (x64) | `.deb`, `.rpm` packages |
| **WebAssembly** | Browser | [Live Demo](https://devflow.primefolio.tech/) |

---

## Features

### REST Client
- Full HTTP method support: `GET`, `POST`, `PUT`, `DELETE`, `PATCH`, `HEAD`, `OPTIONS`
- Query parameters editor with enable/disable toggles
- Custom headers management
- Request body editor with JSON support
- Response viewer with JSON formatting, raw view, and headers inspection

### GraphQL Testing
- GraphQL query editor
- Variables support
- Response visualization

### Real-time Connections
- **WebSocket** - bidirectional communication testing
- **Server-Sent Events (SSE)** - streaming data testing

### Authorization
- Bearer Token authentication
- Basic Auth (username/password)
- API Key (header or query parameter)

### Cross-Platform
- Single codebase for all platforms
- Pixel-perfect UI consistency via SkiaSharp renderer
- Native performance on desktop platforms
- WebAssembly support for browser-based usage

### Modern UI
- Beautiful dark theme (Catppuccin Mocha)
- Fluent UI icons
- Custom fonts (Inter, JetBrains Mono)
- Responsive layout

---

## Installation

### Option 1: Download Pre-built Binaries

Visit the [Releases Page](https://github.com/prime399/DevFlow/releases) and download the appropriate package for your platform.

### Option 2: Build from Source

#### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Uno Platform workloads](https://platform.uno/docs/articles/get-started.html)

```bash
# Install Uno Platform templates
dotnet new install Uno.Templates

# Install required workloads
dotnet workload install wasm-tools

# Verify your environment
dotnet tool install -g uno.check
uno-check
```

#### Clone and Build

```bash
# Clone the repository
git clone https://github.com/prime399/DevFlow.git
cd DevFlow

# Build the solution
dotnet build DevFlow.sln
```

#### Run

**Desktop (Windows/macOS/Linux):**
```bash
dotnet run --project DevFlow/DevFlow.csproj -f net10.0-desktop
```

**WebAssembly:**
```bash
dotnet run --project DevFlow/DevFlow.csproj -f net10.0-browserwasm
```

#### Run with Hot Reload

**Windows (PowerShell):**
```powershell
$env:DOTNET_MODIFIABLE_ASSEMBLIES = "debug"
dotnet run --project DevFlow/DevFlow.csproj -f net10.0-desktop
```

**macOS/Linux (Bash):**
```bash
export DOTNET_MODIFIABLE_ASSEMBLIES=debug
dotnet run --project DevFlow/DevFlow.csproj -f net10.0-desktop
```

---

## Tech Stack

| Component | Technology |
|-----------|------------|
| **Framework** | [Uno Platform](https://platform.uno/) 6.4.26 |
| **Runtime** | .NET 10.0 |
| **UI** | WinUI 3 / XAML |
| **Renderer** | SkiaSharp |
| **Architecture** | MVUX (Model-View-Update-eXtended) |
| **Theme** | Uno.Material + Catppuccin Mocha |
| **Icons** | Fluent UI System Icons |
| **Fonts** | Inter, JetBrains Mono |

### Uno Features Enabled

```xml
<UnoFeatures>
  Material;
  Toolkit;
  MVUX;
  Navigation;
  HttpKiota;
  ThemeService;
  SkiaRenderer;
  Hosting;
  Logging;
  Configuration;
  Localization;
</UnoFeatures>
```

---

## Project Structure

```
DevFlow/
├── DevFlow.sln
├── DevFlow/                       # Uno Platform client
│   ├── App.xaml.cs               # DI, navigation, HTTP config
│   ├── Controls/                 # Reusable UI controls
│   │   ├── KeyValueEditorControl.xaml
│   │   ├── CodeEditorControl.xaml
│   │   ├── ResponseViewerControl.xaml
│   │   └── AuthorizationEditorControl.xaml
│   ├── Presentation/
│   │   ├── MainPage.xaml
│   │   └── ViewModels/
│   ├── Services/
│   │   └── Realtime/             # WebSocket, SSE services
│   ├── Styles/                   # Theme and styles
│   │   ├── AppTheme.xaml
│   │   ├── Icons.xaml
│   │   └── RequestBuilderStyles.xaml
│   └── Assets/
│       └── Fonts/
├── DevFlow.Api/                  # ASP.NET Core backend (optional)
└── DevFlow.Shared/               # Shared DTOs
```

---

## Screenshots

<!-- Add your screenshots here -->

---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Acknowledgments

- [Uno Platform](https://platform.uno/) - Cross-platform framework
- [Catppuccin](https://github.com/catppuccin/catppuccin) - Color palette inspiration
- [Fluent UI](https://github.com/microsoft/fluentui-system-icons) - Icon library

---

## Author

**Anshu Pathak**

- GitHub: [@prime399](https://github.com/prime399)
- Portfolio: [primefolio.tech](https://primefolio.tech)

---

<p align="center">
  Built with ❤️ using <a href="https://platform.uno/">Uno Platform</a>
</p>
