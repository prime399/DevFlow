using DevFlow.Models;
using DevFlow.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DevFlow.Presentation.Controls;

public sealed partial class GraphQLRequestView : UserControl
{
    private int _currentTabIndex = 0;
    private bool _isResizing = false;
    private double _startY;
    private double _startHeight;
    private Button[] _tabButtons = null!;
    private FrameworkElement[] _tabPanels = null!;

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(GraphQLViewModel), typeof(GraphQLRequestView),
            new PropertyMetadata(null, OnViewModelChanged));

    public GraphQLRequestView()
    {
        this.InitializeComponent();
        _tabButtons = new Button[] { TabQuery, TabVariables, TabHeaders, TabAuth };
        _tabPanels = new FrameworkElement[] { QueryEditor, VariablesEditor, HeadersEditor, AuthEditor };
    }

    public GraphQLViewModel? ViewModel
    {
        get => (GraphQLViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GraphQLRequestView view && e.NewValue is GraphQLViewModel vm)
        {
            view.BindViewModel(vm);
        }
    }

    private void BindViewModel(GraphQLViewModel vm)
    {
        GraphQLTabsControl.ItemsSource = vm.TabManager.Tabs;
        AuthEditor.AuthTypes = vm.AuthTypes;
        AuthEditor.ApiKeyLocations = vm.ApiKeyLocations;
        SyncActiveTabToUI();
    }

    private void ContentTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tagStr && int.TryParse(tagStr, out int tabIndex))
        {
            SwitchToContentTab(tabIndex);
        }
    }

    private void SwitchToContentTab(int tabIndex)
    {
        if (tabIndex == _currentTabIndex) return;

        for (int i = 0; i < _tabButtons.Length; i++)
        {
            _tabButtons[i].Style = i == tabIndex
                ? (Style)Application.Current.Resources["RequestTabActiveStyle"]
                : (Style)Application.Current.Resources["RequestTabStyle"];

            _tabPanels[i].Visibility = i == tabIndex ? Visibility.Visible : Visibility.Collapsed;
        }

        _currentTabIndex = tabIndex;
    }

    private void AddGraphQLTab_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.TabManager.AddNewTab();
        SyncActiveTabToUI();
    }

    private void GraphQLTab_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border && border.DataContext is GraphQLTab tab && ViewModel != null)
        {
            ViewModel.TabManager.SetActiveTab(tab);
            SyncActiveTabToUI();
        }
    }

    private void CloseGraphQLTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is GraphQLTab tab && ViewModel != null)
        {
            ViewModel.TabManager.CloseTab(tab);
            SyncActiveTabToUI();
        }
    }

    private async void GraphQLTabName_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is GraphQLTab tab && ViewModel != null)
        {
            var dialog = new ContentDialog
            {
                Title = "Edit Request",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var nameTextBox = new TextBox { Text = tab.Name, PlaceholderText = "Untitled" };
            dialog.Content = nameTextBox;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                ViewModel.TabManager.RenameTab(tab, nameTextBox.Text.Trim());
            }
        }
    }

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        SendingProgress.IsActive = true;
        RunButton.IsEnabled = false;

        try
        {
            SyncUIToActiveTab();
            
            await ViewModel.SendRequest(CancellationToken.None);
            
            ResponseViewer.Status = await ViewModel.ResponseStatus;
            ResponseViewer.ResponseTime = await ViewModel.ResponseTime;
            ResponseViewer.ResponseSize = await ViewModel.ResponseSize;
            ResponseViewer.ResponseBody = await ViewModel.ResponseBody;
            ResponseViewer.ErrorMessage = await ViewModel.ErrorMessage;
            ResponseViewer.ResponseHeaders = ViewModel.ResponseHeaders;
        }
        finally
        {
            SendingProgress.IsActive = false;
            RunButton.IsEnabled = true;
        }
    }

    private void FormatQuery_Click(object sender, RoutedEventArgs e)
    {
        // GraphQL query formatting would go here
        // For now, just format if it's JSON (variables)
        if (_currentTabIndex == 1)
        {
            VariablesEditor.FormatJson();
        }
    }

    private void SyncActiveTabToUI()
    {
        var activeTab = ViewModel?.TabManager.ActiveTab;
        if (activeTab == null) return;

        EndpointTextBox.Text = activeTab.Endpoint;
        QueryEditor.Text = activeTab.Query;
        VariablesEditor.Text = activeTab.Variables;
        HeadersEditor.ItemsSource = activeTab.Headers;
        AuthEditor.Authorization = activeTab.Authorization;
    }

    private void SyncUIToActiveTab()
    {
        var activeTab = ViewModel?.TabManager.ActiveTab;
        if (activeTab == null) return;

        activeTab.Endpoint = EndpointTextBox.Text;
        activeTab.Query = QueryEditor.Text;
        activeTab.Variables = VariablesEditor.Text;
    }

    #region Splitter

    private void Splitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border splitter)
        {
            _isResizing = true;
            _startY = e.GetCurrentPoint(this).Position.Y;
            _startHeight = ResponseSection.Height;
            splitter.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void Splitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isResizing)
        {
            var currentY = e.GetCurrentPoint(this).Position.Y;
            var delta = _startY - currentY;
            var newHeight = _startHeight + delta;

            newHeight = Math.Max(ResponseSection.MinHeight, Math.Min(ResponseSection.MaxHeight, newHeight));
            ResponseSection.Height = newHeight;
            e.Handled = true;
        }
    }

    private void Splitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border splitter)
        {
            _isResizing = false;
            splitter.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
    }

    #endregion

    #region Response

    private async void ResponseViewer_ClearRequested(object? sender, EventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.ResetResponse(CancellationToken.None);
            ResponseViewer.ClearResponse();
        }
    }

    private async void ResponseViewer_DownloadRequested(object? sender, EventArgs e)
    {
        if (ViewModel == null) return;

        var body = await ViewModel.ResponseBody;
        if (string.IsNullOrEmpty(body)) return;

        try
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            savePicker.FileTypeChoices.Add("JSON", new List<string> { ".json" });
            savePicker.SuggestedFileName = $"graphql_response_{DateTime.Now:yyyyMMdd_HHmmss}";

            var window = (Application.Current as App)?.MainWindow;
            if (window != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            }

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await Windows.Storage.FileIO.WriteTextAsync(file, body);
            }
        }
        catch
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(body);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

    #endregion
}
