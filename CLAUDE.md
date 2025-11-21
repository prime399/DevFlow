# CLAUDE.md

This file provides guidance to Claude Code when working with the DevFlow project.

## Project Overview

**DevFlow** is an Uno Platform cross-platform application with ASP.NET Core backend, supporting data sync between desktop and web clients.

Built with:
- **Uno.Sdk 6.4.26** (defined in `global.json`)
- **.NET 10.0** (net10.0-desktop, net10.0-browserwasm)
- **MVUX** pattern for reactive state management
- **Uno.Extensions.Navigation** for region-based navigation
- **ASP.NET Core Minimal API** backend with OpenAPI/Swagger

## Solution Structure

```
DevFlow/
├── DevFlow.sln
├── global.json                    # Uno.Sdk version
├── DevFlow/                       # Uno Platform client (desktop + wasm)
│   ├── DevFlow.csproj
│   ├── App.xaml.cs               # DI, navigation, HTTP client config
│   ├── Presentation/             # Pages + MVUX Models
│   ├── Models/                   # Configuration models
│   ├── Services/                 # API client services
│   └── appsettings.json          # API URL config
├── DevFlow.Api/                  # ASP.NET Core backend
│   ├── DevFlow.Api.csproj
│   ├── Program.cs                # Minimal API endpoints
│   └── appsettings.json
└── DevFlow.Shared/               # Shared DTOs
    └── DataItem.cs
```

## Build and Run Commands

### Prerequisites (one-time)

```bash
# Install Uno templates
dotnet new install Uno.Templates

# Install wasm-tools workload (for WebAssembly)
dotnet workload install wasm-tools

# Install Uno DevServer (for Hot Reload from CLI)
dotnet tool install -g Uno.DevServer
```

### Build

```bash
# Build entire solution
dotnet build DevFlow/DevFlow.sln

# Build desktop only
dotnet build DevFlow/DevFlow/DevFlow.csproj -f net10.0-desktop

# Build WASM only
dotnet build DevFlow/DevFlow/DevFlow.csproj -f net10.0-browserwasm
```

### Run the App

**Step 1: Start the API backend**
```bash
dotnet run --project DevFlow/DevFlow.Api/DevFlow.Api.csproj
```
API will be available at https://localhost:7192 (or http://localhost:5167)

**Step 2: Run the client**

Desktop:
```bash
dotnet run --project DevFlow/DevFlow/DevFlow.csproj -f net10.0-desktop
```

WebAssembly:
```bash
dotnet run --project DevFlow/DevFlow/DevFlow.csproj -f net10.0-browserwasm
```
Open browser to https://localhost:5000

### Run with Hot Reload (Desktop)

```powershell
# PowerShell (Windows)
$env:DOTNET_MODIFIABLE_ASSEMBLIES = "debug"; dotnet run --project DevFlow/DevFlow/DevFlow.csproj -f net10.0-desktop
```

```bash
# Bash (macOS/Linux)
export DOTNET_MODIFIABLE_ASSEMBLIES=debug && dotnet run --project DevFlow/DevFlow/DevFlow.csproj -f net10.0-desktop
```

## Architecture

### Data Flow

```
[Desktop Client]  <--HTTP-->  [ASP.NET Core API]  <-->  [In-Memory Store]
[WASM Client]     <--HTTP-->  [ASP.NET Core API]  <-->  [In-Memory Store]
```

### Key Patterns

**MVUX Pattern:** Models use `IListFeed<T>` and `IState<T>` for reactive data binding.

**API Client:** `IDataItemService` interface with `HttpClient` implementation, configured via DI in `App.xaml.cs`.

**CORS:** API configured to allow any origin for development (restrict in production).

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/items | Get all items |
| GET | /api/items/{id} | Get item by ID |
| POST | /api/items | Create new item |
| PUT | /api/items/{id} | Update item |
| DELETE | /api/items/{id} | Delete item |

### UnoFeatures (enabled in csproj)

Material, Toolkit, MVUX, Navigation, Hosting, Logging, Configuration, HttpKiota, Serialization, Localization, ThemeService, SkiaRenderer, Storage, Dsp

## Configuration

API base URL configured in `DevFlow/appsettings.json`:
```json
{
  "AppConfig": {
    "ApiBaseUrl": "https://localhost:7192"
  }
}
```

## Troubleshooting

### WASM build fails with "wasm-tools workload not found"
```bash
dotnet workload install wasm-tools
```

### API connection refused
1. Ensure API is running (`dotnet run --project DevFlow.Api`)
2. Check `appsettings.json` has correct `ApiBaseUrl`
3. For WASM, ensure CORS is enabled in API
