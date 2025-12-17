using DevFlow.Presentation;
using DevFlow.Services.Auth;
using DevFlow.Services.Settings;
using Uno.Resizetizer;
using Microsoft.Extensions.DependencyInjection;

namespace DevFlow;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    public Window? MainWindow { get; private set; }
    public IHost? Host { get; private set; }

    /// <summary>
    /// Pending OAuth callback URL to be processed after the app is fully initialized.
    /// </summary>
    private static string? _pendingAuthCallback;

    /// <summary>
    /// Set the pending auth callback from external activation.
    /// </summary>
    public static void SetPendingAuthCallback(string callbackUrl)
    {
        Console.WriteLine($"[App] Setting pending auth callback: {callbackUrl}");
        _pendingAuthCallback = callbackUrl;
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Console.WriteLine("[App] OnLaunched called");
        
        var builder = this.CreateBuilder(args)
            // Add navigation support for toolkit controls such as TabBar and NavigationView
            .UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                    // Uno Platform namespace filter groups
                    // Uncomment individual methods to see more detailed logging
                    //// Generic Xaml events
                    //logBuilder.XamlLogLevel(LogLevel.Debug);
                    //// Layout specific messages
                    //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                    //// Storage messages
                    //logBuilder.StorageLogLevel(LogLevel.Debug);
                    //// Binding related messages
                    //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                    //// Binder memory references tracking
                    //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                    //// DevServer and HotReload related
                    //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                    //// Debug JS interop
                    //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                // Enable localization (see appsettings.json for supported languages)
                .UseLocalization()
                .UseHttp((context, services) => {
#if DEBUG
                // DelegatingHandler will be automatically injected
                services.AddTransient<DelegatingHandler, DebugHttpHandler>();
#endif
                    // Configure HttpClient for API
                    var apiBaseUrl = context.Configuration["AppConfig:ApiBaseUrl"] ?? "https://localhost:7192";
                    services.AddHttpClient<Services.IDataItemService, Services.DataItemService>(client =>
                    {
                        client.BaseAddress = new Uri(apiBaseUrl);
                    });
                    services.AddHttpClient("ApiTester");
                })
                .ConfigureServices((context, services) =>
                {
                    // Register Settings Services for language switching
                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<ILanguageService, LanguageService>();
                    
                    // Register Supabase Auth Service
                    services.AddSingleton<ISupabaseAuthService, SupabaseAuthService>();
                })
                .UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes)
            );
        MainWindow = builder.Window;

        #if DEBUG
        MainWindow.UseStudio();
#endif
                MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>();
        
        // Process pending OAuth callback if any
        await ProcessPendingAuthCallbackAsync();
    }

    /// <summary>
    /// Process any pending OAuth callback that was received during app launch or from external activation.
    /// </summary>
    private async Task ProcessPendingAuthCallbackAsync()
    {
        if (!string.IsNullOrEmpty(_pendingAuthCallback))
        {
            Console.WriteLine($"[App] Processing pending auth callback");
            await HandleAuthCallbackAsync(_pendingAuthCallback);
            _pendingAuthCallback = null;
        }
    }

    /// <summary>
    /// Handle the OAuth callback by extracting the authorization code and exchanging it for a session.
    /// </summary>
    public async Task HandleAuthCallbackAsync(string callbackUri)
    {
        Console.WriteLine($"[App] HandleAuthCallbackAsync called with: {callbackUri.Substring(0, Math.Min(50, callbackUri.Length))}...");
        
        try
        {
            if (Host?.Services == null)
            {
                Console.WriteLine("[App] ERROR: Host services not available");
                return;
            }

            var authService = Host.Services.GetService<ISupabaseAuthService>();
            if (authService == null)
            {
                Console.WriteLine("[App] ERROR: Auth service not found");
                return;
            }

            // Ensure auth service is initialized
            await authService.InitializeAsync();

            // Parse the callback URI to extract the authorization code
            var uri = new Uri(callbackUri);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var code = query["code"];

            if (!string.IsNullOrEmpty(code))
            {
                Console.WriteLine($"[App] Authorization code found, exchanging for session...");
                var success = await authService.HandleOAuthCallbackAsync(code);
                Console.WriteLine($"[App] OAuth callback result: {success}");
            }
            else
            {
                // Try fragment-based tokens (implicit flow fallback)
                Console.WriteLine("[App] No code found, trying SetSessionFromUrl...");
                var success = await authService.SetSessionFromUrlAsync(callbackUri);
                Console.WriteLine($"[App] SetSessionFromUrl result: {success}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[App] ERROR handling auth callback: {ex.Message}");
        }
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellModel)),
            new ViewMap<MainPage, MainModel>(),
            new DataViewMap<SecondPage, SecondModel, Entity>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellModel>(),
                Nested:
                [
                    new ("Main", View: views.FindByViewModel<MainModel>(), IsDefault:true),
                    new ("Second", View: views.FindByViewModel<SecondModel>()),
                ]
            )
        );
    }
}
