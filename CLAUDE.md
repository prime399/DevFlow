# CLAUDE.md

This file provides guidance to Claude Code when working with the DevFlow project.

## Project Overview

**DevFlow** is an Uno Platform cross-platform API client application (similar to Postman/Hoppscotch) with ASP.NET Core backend, supporting data sync between desktop and web clients.

Built with:
- **Uno.Sdk 6.4.26** (defined in `global.json`)
- **.NET 10.0** (net10.0-desktop, net10.0-browserwasm)
- **MVUX** pattern for reactive state management
- **Uno.Extensions.Navigation** for region-based navigation
- **ASP.NET Core Minimal API** backend with OpenAPI/Swagger
- **Catppuccin Mocha** dark theme with unified color system

## Icons & Fonts

### Icon Library
The app uses **Fluent UI System Icons** via `Uno.Fonts.Fluent` package. Icons are defined in `Styles/Icons.xaml` with semantic names.

**Usage:**
```xml
<FontIcon Glyph="{StaticResource IconGlyphSave}" Style="{StaticResource ActionIconStyle}" />
```

**Available Icon Categories:**
- Navigation: `IconGlyphMenu`, `IconGlyphBack`, `IconGlyphHome`, `IconGlyphSettings`
- HTTP/API: `IconGlyphGlobe`, `IconGlyphSend`, `IconGlyphCloud`, `IconGlyphSync`
- Actions: `IconGlyphAdd`, `IconGlyphDelete`, `IconGlyphEdit`, `IconGlyphSave`, `IconGlyphCopy`
- Status: `IconGlyphSuccess`, `IconGlyphError`, `IconGlyphWarning`, `IconGlyphInfo`
- Request Builder: `IconGlyphParameters`, `IconGlyphHeaders`, `IconGlyphBody`, `IconGlyphAuth`

### Custom Fonts (Cross-Platform)
- **Inter** - Modern UI font for text (Regular, Medium, SemiBold, Bold)
- **JetBrains Mono** - Monospace font for code/response display

**Font Resources:**
```xml
<TextBlock FontFamily="{StaticResource AppFontFamily}" />           <!-- Inter Regular -->
<TextBlock FontFamily="{StaticResource AppFontFamilyMedium}" />     <!-- Inter Medium -->
<TextBlock FontFamily="{StaticResource AppFontFamilySemiBold}" />   <!-- Inter SemiBold -->
<TextBlock FontFamily="{StaticResource MonospaceFontFamily}" />     <!-- JetBrains Mono -->
```

**Font Sizes:**
- `FontSizeCaption` (11), `FontSizeSmall` (12), `FontSizeBody` (14)
- `FontSizeSubtitle` (16), `FontSizeTitle` (20), `FontSizeHeader` (24)

## Solution Structure

```
DevFlow/
├── DevFlow.sln
├── global.json                    # Uno.Sdk version
├── DevFlow/                       # Uno Platform client (desktop + wasm)
│   ├── DevFlow.csproj
│   ├── App.xaml                  # Resource dictionaries merged here
│   ├── App.xaml.cs               # DI, navigation, HTTP client config
│   ├── Controls/                 # Reusable UI controls
│   │   ├── KeyValueEditorControl.xaml/.cs    # Parameters/Headers table editor
│   │   ├── CodeEditorControl.xaml/.cs        # Code editor with line numbers
│   │   ├── ResponseViewerControl.xaml/.cs    # Response display with tabs
│   │   └── AuthorizationEditorControl.xaml/.cs # Auth configuration panel
│   ├── Presentation/
│   │   ├── MainPage.xaml/.cs     # Main API client UI (monolithic, legacy)
│   │   ├── MainPageModular.xaml/.cs # Modular shell using section controls
│   │   ├── Controls/             # Feature-specific section controls
│   │   │   ├── RestRequestView.xaml/.cs      # Complete REST UI section
│   │   │   ├── GraphQLRequestView.xaml/.cs   # Complete GraphQL UI section
│   │   │   └── RealtimeView.xaml/.cs         # Complete Realtime UI section
│   │   └── ViewModels/           # Feature-specific ViewModels
│   │       ├── RestRequestViewModel.cs       # REST request logic + state
│   │       ├── GraphQLViewModel.cs           # GraphQL request logic + state
│   │       └── RealtimeViewModel.cs          # Realtime connection logic
│   ├── Models/                   # Data models and interfaces
│   │   ├── IKeyValueItem.cs      # Unified interface for key-value items
│   │   ├── RequestParameter.cs   # URL parameters (implements IKeyValueItem)
│   │   ├── RequestHeader.cs      # HTTP headers (implements IKeyValueItem)
│   │   └── ...
│   ├── Services/
│   │   ├── Realtime/             # Realtime connection services
│   │   │   ├── IRealtimeConnectionService.cs # Connection abstraction
│   │   │   ├── WebSocketConnectionService.cs # WebSocket implementation
│   │   │   └── SSEConnectionService.cs       # Server-Sent Events implementation
│   │   └── ...
│   ├── Assets/
│   │   └── Fonts/                # Custom fonts (Inter, JetBrains Mono)
│   ├── Styles/                   # XAML styles and themes
│   │   ├── AppTheme.xaml         # Unified color definitions + fonts
│   │   ├── Icons.xaml            # Fluent icon glyph definitions
│   │   ├── NavigationStyles.xaml # NavigationView styles
│   │   ├── RequestBuilderStyles.xaml # API request UI styles
│   │   └── ColorPaletteOverride.xaml/.json
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

### Modular Architecture

The app uses a **layered modular architecture** for maintainability and testability:

```
┌─────────────────────────────────────────────────────────────┐
│                    MainPageModular (Shell)                  │
│  ┌─────────────────┬─────────────────┬─────────────────┐   │
│  │  RestRequestView│ GraphQLRequest  │   RealtimeView  │   │
│  │  (Section)      │ View (Section)  │   (Section)     │   │
│  └────────┬────────┴────────┬────────┴────────┬────────┘   │
│           │                 │                 │             │
│  ┌────────┴────────┬────────┴────────┬────────┴────────┐   │
│  │ Reusable Controls: KeyValueEditor, CodeEditor,      │   │
│  │ ResponseViewer, AuthorizationEditor                 │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
           │                 │                 │
  ┌────────┴────────┬────────┴────────┬────────┴────────┐
  │RestRequestVM    │ GraphQLVM       │ RealtimeVM      │
  │(ViewModel)      │ (ViewModel)     │ (ViewModel)     │
  └────────┬────────┴────────┬────────┴────────┬────────┘
           │                 │                 │
  ┌────────┴─────────────────┴─────────────────┴────────┐
  │              Services Layer                         │
  │  IHttpClientFactory, IRealtimeConnectionService     │
  └─────────────────────────────────────────────────────┘
```

**Layer Responsibilities:**

| Layer | Purpose | Examples |
|-------|---------|----------|
| **Shell** | Navigation, layout, section switching | `MainPageModular` |
| **Section Controls** | Feature-complete UI for a domain | `RestRequestView`, `GraphQLRequestView`, `RealtimeView` |
| **Reusable Controls** | Generic UI components used across sections | `KeyValueEditorControl`, `CodeEditorControl` |
| **ViewModels** | Business logic, state management, HTTP calls | `RestRequestViewModel`, `GraphQLViewModel` |
| **Services** | Infrastructure concerns, external connections | `WebSocketConnectionService`, `SSEConnectionService` |

### Reusable Controls

Located in `Controls/`, these are generic UI components:

| Control | Purpose | Key Properties |
|---------|---------|----------------|
| `KeyValueEditorControl` | Table editor for key-value pairs | `ItemsSource`, `ItemType`, `Title` |
| `CodeEditorControl` | Text editor with line numbers | `Text`, `PlaceholderText`, `IsReadOnly` |
| `ResponseViewerControl` | Response display with JSON/Raw/Headers tabs | `ResponseBody`, `Status`, `ResponseHeaders` |
| `AuthorizationEditorControl` | Auth type selector and config panels | `Authorization` |

**Usage:**
```xml
<controls:KeyValueEditorControl 
    Title="PARAMETERS"
    ItemType="Parameter"
    ItemsSource="{x:Bind ViewModel.Parameters, Mode=OneWay}" />
```

### IKeyValueItem Interface

Unified interface for parameter/header items enabling control reuse:

```csharp
public interface IKeyValueItem : INotifyPropertyChanged
{
    Guid Id { get; }
    bool IsEnabled { get; set; }
    string Key { get; set; }      // Unified binding property
    string Value { get; set; }    // Unified binding property
    string Description { get; set; }
}
```

Both `RequestParameter` and `RequestHeader` implement this interface, exposing `Key`/`Value` properties that delegate to their specific properties (`ParamKey`/`ParamValue` or `HeaderKey`/`HeaderValue`).

### Service Abstraction Pattern

Realtime connections use interface abstraction for testability:

```csharp
public interface IRealtimeConnectionService
{
    bool IsConnected { get; }
    event EventHandler<string>? MessageReceived;
    event EventHandler<string>? ErrorOccurred;
    event EventHandler? Connected;
    event EventHandler? Disconnected;
    
    Task ConnectAsync(string url, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task SendMessageAsync(string message, CancellationToken ct = default);
}
```

Implementations: `WebSocketConnectionService`, `SSEConnectionService`

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

## Theming & Styling

### Unified Theme System

All colors are centralized in `Styles/AppTheme.xaml` using **Catppuccin Mocha** dark theme. This ensures color consistency across the entire app.

**Color Categories:**
- **Surface Colors:** `AppBackgroundBrush`, `SurfaceBrush`, `SurfaceVariantBrush`, `OverlayBrush`
- **Text Colors:** `TextPrimaryBrush`, `TextSecondaryBrush`, `TextMutedBrush`, `TextSubtleBrush`
- **Accent Colors:** `AccentPrimaryBrush`, `AccentSecondaryBrush`
- **Semantic Colors:** `SuccessBrush`, `WarningBrush`, `ErrorBrush`, `InfoBrush`
- **HTTP Method Colors:** `HttpGetBrush`, `HttpPostBrush`, `HttpPutBrush`, `HttpDeleteBrush`, etc.
- **Interactive States:** `HoverBrush`, `PressedBrush`, `SelectedBrush`

**Usage Pattern:**
```xml
<!-- Use StaticResource for theme colors -->
<TextBlock Foreground="{StaticResource TextPrimaryBrush}" />
<Border Background="{StaticResource SurfaceBrush}" />

<!-- DO NOT use hardcoded colors like #RRGGBB -->
```

### Style Files

| File | Purpose |
|------|---------|
| `AppTheme.xaml` | Color definitions, font families, font sizes |
| `Icons.xaml` | Fluent icon glyphs and icon styles (`SmallIconStyle`, `ActionIconStyle`, etc.) |
| `NavigationStyles.xaml` | `AnimatedPaneToggleButtonStyle`, `AnimatedNavigationViewItemStyle` |
| `RequestBuilderStyles.xaml` | `SendButtonStyle`, `SaveButtonStyle`, `UrlTextBoxStyle`, `MethodComboBoxStyle`, `RequestTabStyle`, `RequestTabActiveStyle`, `ParameterTextBoxStyle`, `IconButtonStyle` |

### Resource Dictionary Load Order (App.xaml)

```xml
<ResourceDictionary.MergedDictionaries>
  <XamlControlsResources />
  <utum:MaterialToolkitTheme ... />
  <ResourceDictionary Source="ms-appx:///Styles/AppTheme.xaml" />        <!-- 1. Colors + Fonts first -->
  <ResourceDictionary Source="ms-appx:///Styles/Icons.xaml" />           <!-- 2. Icons -->
  <ResourceDictionary Source="ms-appx:///Styles/NavigationStyles.xaml" />
  <ResourceDictionary Source="ms-appx:///Styles/RequestBuilderStyles.xaml" />
</ResourceDictionary.MergedDictionaries>
```

### UI Design Pattern

The app follows a **Hoppscotch-style** API client design:
- Dark theme with colored HTTP method indicators
- Request bar: Method dropdown + URL input + Send/Save buttons
- Tab navigation: Parameters, Body, Headers, Authorization, Scripts
- Table-style parameter input with Key/Value/Description columns
- Response viewer with status badge and code formatting

## Configuration

API base URL configured in `DevFlow/appsettings.json`:
```json
{
  "AppConfig": {
    "ApiBaseUrl": "https://localhost:7192"
  }
}
```

## Coding Conventions

### XAML Styling Rules

1. **Always use theme resources** - Never use hardcoded hex colors (`#RRGGBB`)
2. **Use `StaticResource`** for theme colors, not `ThemeResource`
3. **Extract reusable styles** to appropriate style files in `Styles/` folder
4. **Reference styles by key** in XAML pages:
   ```xml
   <Button Style="{StaticResource SendButtonStyle}" />
   <NavigationView.PaneToggleButtonStyle>
     <StaticResource ResourceKey="AnimatedPaneToggleButtonStyle" />
   </NavigationView.PaneToggleButtonStyle>
   ```

### Animation Guidelines

- Use `DoubleAnimation` with `CubicEase` for smooth transitions
- Hover effects: 0.12s duration with `EaseOut`
- Press effects: 0.08s duration with `EaseIn`
- Selection transitions: 0.15-0.2s duration
- Scale transforms for hover: 1.02x, for press: 0.98x

### File Organization

- **Styles go in `Styles/`** - Never inline complex styles in page XAML
- **Colors go in `AppTheme.xaml`** - Single source of truth
- **Control styles go in specific files** - Navigation, RequestBuilder, etc.
- **Reusable controls go in `Controls/`** - Generic UI components
- **Section controls go in `Presentation/Controls/`** - Feature-specific composites
- **ViewModels go in `Presentation/ViewModels/`** - One per feature/section
- **Services go in `Services/`** - Infrastructure and external connections

### Adding New Features

When adding a new feature section (e.g., gRPC support):

1. **Create ViewModel** in `Presentation/ViewModels/`:
   ```csharp
   public partial record GrpcViewModel
   {
       public IState<string> ServiceUrl => State<string>.Value(this, () => "");
       // ... state and methods
   }
   ```

2. **Create Section Control** in `Presentation/Controls/`:
   - XAML: Compose reusable controls (`KeyValueEditorControl`, etc.)
   - Code-behind: Wire up ViewModel via DependencyProperty

3. **Reuse existing controls** where possible:
   - `KeyValueEditorControl` for metadata/headers
   - `CodeEditorControl` for request body
   - `ResponseViewerControl` for response display
   - `AuthorizationEditorControl` for auth config

4. **Add to shell** in `MainPageModular.xaml`:
   ```xml
   <local:GrpcRequestView x:Name="GrpcView" Visibility="Collapsed" />
   ```

5. **Create service abstraction** if needed (in `Services/`).

## Troubleshooting

### WASM build fails with "wasm-tools workload not found"
```bash
dotnet workload install wasm-tools
```

### API connection refused
1. Ensure API is running (`dotnet run --project DevFlow.Api`)
2. Check `appsettings.json` has correct `ApiBaseUrl`
3. For WASM, ensure CORS is enabled in API

### Build fails with file locked error
The app is currently running. Close the desktop app before rebuilding, or use Hot Reload for XAML changes.
