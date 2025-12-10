using System.Collections.ObjectModel;
using DevFlow.Models;
using DevFlow.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DevFlow.Presentation.Controls;

public sealed partial class RestRequestView : UserControl
{
    private int _currentTabIndex = 0;
    private bool _isResizing = false;
    private double _startY;
    private double _startHeight;
    private Button[] _tabButtons = null!;
    private FrameworkElement[] _tabPanels = null!;

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(RestRequestViewModel), typeof(RestRequestView),
            new PropertyMetadata(null, OnViewModelChanged));

    public RestRequestView()
    {
        this.InitializeComponent();
        _tabButtons = new Button[] { TabParameters, TabBody, TabHeaders, TabAuth };
        _tabPanels = new FrameworkElement[] { ParametersEditor, BodyPanel, HeadersEditor, AuthEditor };
    }

    public RestRequestViewModel? ViewModel
    {
        get => (RestRequestViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public event EventHandler? SendRequested;
    public event EventHandler? ResetRequested;

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RestRequestView view && e.NewValue is RestRequestViewModel vm)
        {
            view.BindViewModel(vm);
        }
    }

    private void BindViewModel(RestRequestViewModel vm)
    {
        RequestTabsControl.ItemsSource = vm.TabManager.Tabs;
        MethodComboBox.ItemsSource = vm.Methods;
        MethodComboBox.SelectedItem = "GET";
        ContentTypeComboBox.ItemsSource = vm.ContentTypes;
        ContentTypeComboBox.SelectedIndex = 0;
        
        ParametersEditor.ItemsSource = vm.Parameters;
        HeadersEditor.ItemsSource = vm.Headers;
        AuthEditor.Authorization = vm.Authorization;
        AuthEditor.AuthTypes = vm.AuthTypes;
        AuthEditor.ApiKeyLocations = vm.ApiKeyLocations;
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

    private void AddNewTab_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.TabManager.AddNewTab();
        SyncActiveTabToUI();
    }

    private void RequestTab_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border && border.DataContext is RequestTab tab && ViewModel != null)
        {
            ViewModel.TabManager.SetActiveTab(tab);
            SyncActiveTabToUI();
        }
    }

    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is RequestTab tab && ViewModel != null)
        {
            ViewModel.TabManager.CloseTab(tab);
            SyncActiveTabToUI();
        }
    }

    private async void TabName_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is RequestTab tab && ViewModel != null)
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

    private void HttpMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MethodComboBox.SelectedItem is string method && ViewModel?.TabManager.ActiveTab != null)
        {
            ViewModel.TabManager.ActiveTab.HttpMethod = method;
        }
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        SendingProgress.IsActive = true;
        SendButton.IsEnabled = false;

        try
        {
            // Sync UI to active tab before sending
            SyncUIToActiveTab();
            
            await ViewModel.SendRequest(CancellationToken.None);
            
            // Update response viewer
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
            SendButton.IsEnabled = true;
        }
    }

    private void FormatJson_Click(object sender, RoutedEventArgs e)
    {
        BodyEditor.FormatJson();
    }

    private void SyncActiveTabToUI()
    {
        var activeTab = ViewModel?.TabManager.ActiveTab;
        if (activeTab == null) return;

        MethodComboBox.SelectedItem = activeTab.HttpMethod;
        UrlTextBox.Text = activeTab.Url;
        BodyEditor.Text = activeTab.BodyText;
        ParametersEditor.ItemsSource = activeTab.Parameters;
        HeadersEditor.ItemsSource = activeTab.Headers;
        AuthEditor.Authorization = activeTab.Authorization;
        
        if (ContentTypeComboBox.ItemsSource is IList<ContentType> contentTypes)
        {
            var index = contentTypes.ToList().FindIndex(ct => ct.Value == activeTab.ContentType?.Value);
            if (index >= 0)
                ContentTypeComboBox.SelectedIndex = index;
        }
    }

    private void SyncUIToActiveTab()
    {
        var activeTab = ViewModel?.TabManager.ActiveTab;
        if (activeTab == null) return;

        activeTab.HttpMethod = MethodComboBox.SelectedItem as string ?? "GET";
        activeTab.Url = UrlTextBox.Text;
        activeTab.BodyText = BodyEditor.Text;
        activeTab.ContentType = ContentTypeComboBox.SelectedItem as ContentType;
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
            savePicker.FileTypeChoices.Add("Text", new List<string> { ".txt" });
            savePicker.SuggestedFileName = $"response_{DateTime.Now:yyyyMMdd_HHmmss}";

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
            // Fallback: copy to clipboard
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(body);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

    #endregion
}
