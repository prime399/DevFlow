using System.Net.Http;
using DevFlow.Presentation.ViewModels;
using DevFlow.Services.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Localization;

namespace DevFlow.Presentation;

public sealed partial class MainPageModular : Page
{
    private RestRequestViewModel? _restViewModel;
    private GraphQLViewModel? _graphQLViewModel;
    private RealtimeViewModel? _realtimeViewModel;
    private SettingsViewModel? _settingsViewModel;

    public MainPageModular()
    {
        this.InitializeComponent();
        Loaded += MainPageModular_Loaded;
    }

    private void MainPageModular_Loaded(object sender, RoutedEventArgs e)
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

        // Initialize Settings ViewModel from DI if available, otherwise create manually
        try
        {
            var serviceProvider = ((App)Application.Current).Host?.Services;
            if (serviceProvider != null)
            {
                var localizationService = serviceProvider.GetService(typeof(ILocalizationService)) as ILocalizationService;
                var settingsService = serviceProvider.GetService(typeof(ISettingsService)) as ISettingsService;

                if (localizationService != null && settingsService != null)
                {
                    var languageService = new LanguageService(localizationService, settingsService);
                    _settingsViewModel = new SettingsViewModel(languageService, settingsService);
                    SettingsView.ViewModel = _settingsViewModel;
                }
            }
        }
        catch
        {
            // Settings will work without ViewModel, just won't have language switching
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
            }
        }
    }
}

// Simple HttpClientFactory for the modular page
internal class SimpleHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new HttpClient();
}
