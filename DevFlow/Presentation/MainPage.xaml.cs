using System.Net.Http;
using DevFlow.Presentation.ViewModels;
using DevFlow.Services.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Localization;

namespace DevFlow.Presentation;

public sealed partial class MainPage : Page
{
    private RestRequestViewModel? _restViewModel;
    private GraphQLViewModel? _graphQLViewModel;
    private RealtimeViewModel? _realtimeViewModel;
    private SettingsViewModel? _settingsViewModel;

    public MainPage()
    {
        this.InitializeComponent();
        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeViewModels();
    }

    private void InitializeViewModels()
    {
        // Create a simple HttpClientFactory wrapper
        var httpClientFactory = new SimpleHttpClientFactory();
        
        _restViewModel = new RestRequestViewModel(httpClientFactory);
        _graphQLViewModel = new GraphQLViewModel(httpClientFactory);
        _realtimeViewModel = new RealtimeViewModel();

        RestView.ViewModel = _restViewModel;
        GraphQLView.ViewModel = _graphQLViewModel;
        RealtimeView.ViewModel = _realtimeViewModel;

        // Initialize Settings ViewModel from DI
        InitializeSettingsViewModel();
    }

    private void InitializeSettingsViewModel()
    {
        try
        {
            var serviceProvider = ((App)Application.Current).Host?.Services;
            if (serviceProvider == null)
            {
                System.Diagnostics.Debug.WriteLine("Settings: Host services not available");
                return;
            }

            // Get services from DI container
            var localizationService = serviceProvider.GetService(typeof(ILocalizationService)) as ILocalizationService;
            var settingsService = serviceProvider.GetService(typeof(ISettingsService)) as ISettingsService;

            if (localizationService == null)
            {
                System.Diagnostics.Debug.WriteLine("Settings: ILocalizationService not registered");
                return;
            }

            if (settingsService == null)
            {
                System.Diagnostics.Debug.WriteLine("Settings: ISettingsService not registered");
                return;
            }

            // Get or create language service
            var languageService = serviceProvider.GetService(typeof(ILanguageService)) as ILanguageService;
            if (languageService == null)
            {
                languageService = new LanguageService(localizationService, settingsService);
            }

            _settingsViewModel = new SettingsViewModel(languageService, settingsService);
            SettingsView.ViewModel = _settingsViewModel;
            
            System.Diagnostics.Debug.WriteLine($"Settings: Initialized with {languageService.SupportedLanguages.Count} languages, current: {languageService.CurrentLanguageCode}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Settings: Failed to initialize - {ex.Message}");
        }
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            
            RestView.Visibility = Visibility.Collapsed;
            GraphQLView.Visibility = Visibility.Collapsed;
            RealtimeView.Visibility = Visibility.Collapsed;
            SettingsView.Visibility = Visibility.Collapsed;
            ProfileView.Visibility = Visibility.Collapsed;

            switch (tag)
            {
                case "REST":
                    RestView.Visibility = Visibility.Visible;
                    break;
                case "GraphQL":
                    GraphQLView.Visibility = Visibility.Visible;
                    break;
                case "Realtime":
                    RealtimeView.Visibility = Visibility.Visible;
                    break;
                case "Settings":
                    SettingsView.Visibility = Visibility.Visible;
                    break;
                case "Profile":
                    ProfileView.Visibility = Visibility.Visible;
                    break;
            }
        }
    }

    private void TopBar_ProfileRequested(object? sender, EventArgs e)
    {
        // Navigate to Profile section
        RestView.Visibility = Visibility.Collapsed;
        GraphQLView.Visibility = Visibility.Collapsed;
        RealtimeView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Collapsed;
        ProfileView.Visibility = Visibility.Visible;
    }
}

// Simple HttpClientFactory for the modular page
internal class SimpleHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new HttpClient();
}
