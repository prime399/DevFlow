# Uno Platform Experience Report

A retrospective on building **DevFlow** - a cross-platform API client using Uno Platform.

## Project Stats

| Metric | Value |
|--------|-------|
| Target Frameworks | `net10.0-desktop`, `net10.0-browserwasm` |
| Uno SDK Version | 6.4.26 |
| Lines of XAML | ~5,000+ |
| Reusable Controls | 4 |
| ViewModels | 3 |

---

## Uno Platform Components Used

### Core Framework
| Component | Usage | Verdict |
|-----------|-------|---------|
| **Uno.Sdk** | Single project for Desktop + WASM | Excellent - simplified multi-targeting |
| **SkiaSharp Renderer** | Consistent rendering across platforms | Great visual fidelity |
| **XAML/WinUI 3** | UI markup and controls | Familiar, productive |

### Uno.Extensions
| Extension | Usage | Verdict |
|-----------|-------|---------|
| **MVUX** | Reactive state with `IState<T>`, `IListFeed<T>` | Good for simple state, learning curve |
| **Navigation** | Region-based navigation | Powerful but complex setup |
| **Hosting** | DI container, configuration | Seamless .NET integration |
| **HttpKiota** | HTTP client factory | Works as expected |
| **ThemeService** | Runtime theme switching | Easy to use |
| **Logging** | Structured logging | Standard .NET experience |

### UI Libraries
| Library | Usage | Verdict |
|---------|-------|---------|
| **Uno.Material** | Material Design 3 theme | Beautiful defaults |
| **Uno.Toolkit** | Extended controls | Useful additions |
| **Uno.Fonts.Fluent** | Fluent UI icons | Comprehensive icon set |

---

## What Went Well

### 1. Single Codebase, Multiple Platforms
```xml
<TargetFrameworks>net10.0-desktop;net10.0-browserwasm</TargetFrameworks>
```
- One `.csproj`, one codebase for Desktop and WebAssembly
- No platform-specific code needed for this project
- Build once, run everywhere actually worked

### 2. WinUI 3 XAML Compatibility
- Existing WinUI/UWP knowledge transferred directly
- Standard controls (`NavigationView`, `ItemsControl`, `Grid`) worked as expected
- `x:Bind` compiled bindings worked flawlessly
- Styles and resource dictionaries behaved consistently

### 3. SkiaSharp Rendering Consistency
- Pixel-perfect rendering across Desktop and WASM
- Custom fonts (Inter, JetBrains Mono) rendered identically
- No platform-specific visual quirks

### 4. Hot Reload (Desktop)
```powershell
$env:DOTNET_MODIFIABLE_ASSEMBLIES = "debug"
dotnet run -f net10.0-desktop
```
- XAML Hot Reload worked reliably for style changes
- Significantly sped up UI iteration

### 5. .NET Ecosystem Integration
- Standard `HttpClient` and `IHttpClientFactory` worked out of the box
- `System.Net.WebSockets.ClientWebSocket` worked on both platforms
- JSON serialization with `System.Text.Json` - no issues
- Full `async/await` support

### 6. Material Design Theme
- `Uno.Material` provided a polished look immediately
- Easy to override with custom `Catppuccin Mocha` colors
- `ColorPaletteOverride` allowed deep customization

---

## What Was Challenging

### 1. MVUX Learning Curve
```csharp
// This pattern took time to understand
public IState<string> ResponseBody => State<string>.Value(this, () => string.Empty);

// Updating state is async and requires CancellationToken
await ResponseBody.UpdateAsync(_ => newValue, ct);
```
- **Issue**: Documentation is sparse for advanced scenarios
- **Workaround**: Used standard `INotifyPropertyChanged` for complex ViewModels
- **Lesson**: MVUX is great for simple state, but mixing patterns may be needed

### 2. Dispatcher Access in ViewModels
```csharp
// Getting the dispatcher was non-obvious
var dispatcher = DispatcherQueue.GetForCurrentThread();

// Had to dispatch UI updates from async operations
dispatcher?.TryEnqueue(() => {
    ResponseHeaders.Clear();
    foreach (var h in headersList)
        ResponseHeaders.Add(h);
});
```
- **Issue**: `ObservableCollection` modifications from background threads
- **Workaround**: Captured `DispatcherQueue` in constructor, used `TryEnqueue`

### 3. DI Container Access in Pages
```csharp
// This didn't work - Host is protected
var service = (App.Current as App)?.Host?.Services?.GetService<IMyService>();

// Workaround: Created simple wrapper classes
internal class SimpleHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new HttpClient();
}
```
- **Issue**: Accessing DI services outside of constructor injection
- **Workaround**: Constructor injection or simple factory wrappers

### 4. WebAssembly Build Times
```
Build time (Desktop): ~2 seconds
Build time (WASM):    ~45 seconds
```
- **Issue**: WASM builds are significantly slower
- **Workaround**: Develop primarily on Desktop, test WASM periodically

### 5. Complex Control Styling
```xml
<!-- Some controls required deep template overrides -->
<Style TargetType="NavigationViewItem">
    <Setter Property="Template">
        <!-- 100+ lines of template XAML -->
    </Setter>
</Style>
```
- **Issue**: Customizing certain controls required full template copies
- **Workaround**: Accepted some default styles, focused customization on key areas

### 6. Debugging XAML Binding Errors
- **Issue**: Silent failures when bindings don't resolve
- **Workaround**: Used `x:Bind` with `FallbackValue`, checked Output window carefully
- **Lesson**: Always test with actual data, not just design-time

---

## Performance Observations

| Scenario | Desktop | WebAssembly |
|----------|---------|-------------|
| Cold start | ~1.5s | ~3-4s |
| Hot Reload | Instant | N/A |
| HTTP requests | Native speed | Slightly slower |
| UI responsiveness | Excellent | Good |
| Memory usage | ~150MB | Browser-dependent |

---

## Tips for Future Uno Projects

1. **Start with Desktop target** - Faster builds, better debugging
2. **Use `x:Bind` over `{Binding}`** - Compile-time checking, better performance
3. **Keep MVUX simple** - Use for leaf-level state, not complex orchestration
4. **Create reusable controls early** - Pays off in consistency and maintenance
5. **Test WASM regularly** - Don't wait until the end
6. **Use SkiaSharp renderer** - Most consistent cross-platform experience

---

## Conclusion

Uno Platform delivered on its promise of cross-platform .NET development. The ability to use familiar WinUI XAML while targeting Desktop and WebAssembly from a single codebase was the biggest win. Challenges were mostly around documentation gaps and some architectural decisions (DI access, dispatcher patterns) rather than fundamental platform issues.

**Would use again**: Yes, especially for projects needing Desktop + Web from shared code.

**Best suited for**: Line-of-business apps, developer tools, dashboards - apps where WinUI's control library covers the UI needs.
