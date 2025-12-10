using System.Net.Http;
using DevFlow.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevFlow.Presentation;

public sealed partial class MainPageModular : Page
{
    private RestRequestViewModel? _restViewModel;
    private GraphQLViewModel? _graphQLViewModel;
    private RealtimeViewModel? _realtimeViewModel;

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
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var content = item.Content?.ToString();
            
            RestView.Visibility = Visibility.Collapsed;
            GraphQLView.Visibility = Visibility.Collapsed;
            RealtimeView.Visibility = Visibility.Collapsed;
            SettingsContent.Visibility = Visibility.Collapsed;

            switch (content)
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
                    SettingsContent.Visibility = Visibility.Visible;
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
