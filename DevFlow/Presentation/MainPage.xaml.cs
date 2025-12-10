using System.Collections.ObjectModel;
using System.Text.Json;
using DevFlow.Helpers;
using DevFlow.Models;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace DevFlow.Presentation;

public sealed partial class MainPage : Page
{
    private int _currentTabIndex = 0;
    private int _currentResponseTabIndex = 0;
    private bool _wrapLines = false;
    private string _filterText = string.Empty;
    private bool _isResizing = false;
    private double _startY;
    private double _startHeight;
    private readonly Button[] _tabButtons;
    private readonly FrameworkElement[] _tabPanels;
    private Button[] _responseTabButtons = null!;
    private FrameworkElement[] _responseTabPanels = null!;
    private FrameworkElement[] _authPanels = null!;

    public MainPage()
    {
        this.InitializeComponent();
        
        _tabButtons = new Button[] { TabParameters, TabBody, TabHeaders, TabAuth, TabPreScript, TabPostScript };
        _tabPanels = new FrameworkElement[] { ParametersPanel, BodyPanel, HeadersPanel, AuthPanel, PreScriptPanel, PostScriptPanel };
        
        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize response tab arrays
        _responseTabButtons = new Button[] { ResponseTabJson, ResponseTabRaw, ResponseTabHeaders, ResponseTabTestResults };
        _responseTabPanels = new FrameworkElement[] { ResponseJsonPanel, ResponseRawPanel, ResponseHeadersPanel, ResponseTestResultsPanel };
        
        // Initialize auth panels array
        _authPanels = new FrameworkElement[] { AuthNonePanel, AuthBasicPanel, AuthBearerPanel, AuthApiKeyPanel, AuthOAuth2Panel };
        
        UpdateLineNumbers();
        UpdateResponseLineNumbers();
        UpdateRawLineNumbers();
        
        // Initialize first radio button as checked
        InitializeAuthTypeRadioButtons();
        
        // Register for text binding updates
        if (BodyTextBox != null)
        {
            BodyTextBox.RegisterPropertyChangedCallback(TextBox.TextProperty, (s, dp) =>
            {
                UpdateLineNumbers();
            });
        }
        
        if (ResponseBodyText != null)
        {
            ResponseBodyText.RegisterPropertyChangedCallback(TextBlock.TextProperty, (s, dp) =>
            {
                UpdateResponseLineNumbers();
                UpdateRawLineNumbers();
            });
        }

        // Register for GraphQL response body updates
        if (GQLResponseRawText != null)
        {
            GQLResponseRawText.RegisterPropertyChangedCallback(TextBlock.TextProperty, (s, dp) =>
            {
                var body = GQLResponseRawText.Text ?? string.Empty;
                _gqlResponseBody = body;
                
                // Update JSON highlighting
                if (GQLResponseHighlightBlock != null)
                {
                    try
                    {
                        JsonSyntaxHighlighter.ApplyHighlighting(GQLResponseHighlightBlock, body);
                    }
                    catch
                    {
                        GQLResponseHighlightBlock.Text = body;
                    }
                }
                
                UpdateGQLResponseLineNumbers();
            });
        }

        // Register for REST status changes to update badge color
        if (ResponseStatusText != null)
        {
            ResponseStatusText.RegisterPropertyChangedCallback(TextBlock.TextProperty, (s, dp) =>
            {
                UpdateStatusBadgeColor(StatusBadge, ResponseStatusText.Text);
            });
        }

        // Register for GraphQL status changes to update badge color
        if (GQLResponseStatusText != null)
        {
            GQLResponseStatusText.RegisterPropertyChangedCallback(TextBlock.TextProperty, (s, dp) =>
            {
                UpdateStatusBadgeColor(GQLStatusBadge, GQLResponseStatusText.Text);
            });
        }
    }

    private void UpdateStatusBadgeColor(Border? badge, string? statusText)
    {
        if (badge == null || string.IsNullOrEmpty(statusText)) return;

        Brush brush;
        if (statusText.StartsWith("2"))
            brush = (Brush)Application.Current.Resources["SuccessBrush"];
        else if (statusText.StartsWith("3"))
            brush = (Brush)Application.Current.Resources["InfoBrush"];
        else if (statusText.StartsWith("4"))
            brush = (Brush)Application.Current.Resources["WarningBrush"];
        else if (statusText.StartsWith("5") || statusText.Contains("failed", StringComparison.OrdinalIgnoreCase))
            brush = (Brush)Application.Current.Resources["ErrorBrush"];
        else
            brush = (Brush)Application.Current.Resources["TextMutedBrush"];

        badge.Background = brush;
    }

    private ObservableCollection<RequestParameter>? GetParameters()
    {
        var dc = DataContext;
        if (dc == null) return null;
        
        var prop = dc.GetType().GetProperty("Parameters");
        return prop?.GetValue(dc) as ObservableCollection<RequestParameter>;
    }

    private ObservableCollection<RequestHeader>? GetHeaders()
    {
        var dc = DataContext;
        if (dc == null) return null;
        
        var prop = dc.GetType().GetProperty("Headers");
        return prop?.GetValue(dc) as ObservableCollection<RequestHeader>;
    }

    private void Tab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tagStr && int.TryParse(tagStr, out int tabIndex))
        {
            SwitchToTab(tabIndex);
        }
    }

    private void SwitchToTab(int tabIndex)
    {
        if (tabIndex == _currentTabIndex) return;
        
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            _tabButtons[i].Style = i == tabIndex 
                ? (Style)Resources["RequestTabActiveStyle"] ?? (Style)Application.Current.Resources["RequestTabActiveStyle"]
                : (Style)Resources["RequestTabStyle"] ?? (Style)Application.Current.Resources["RequestTabStyle"];
            
            _tabPanels[i].Visibility = i == tabIndex ? Visibility.Visible : Visibility.Collapsed;
        }
        
        _currentTabIndex = tabIndex;
        
        if (tabIndex == 1)
        {
            UpdateLineNumbers();
        }
    }

    private void UpdateLineNumbers()
    {
        var text = BodyTextBox?.Text ?? string.Empty;
        var lineCount = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;
        
        if (LineNumbersControl != null)
        {
            var lineNumbers = new List<string>();
            for (int i = 1; i <= lineCount; i++)
            {
                lineNumbers.Add(i.ToString());
            }
            LineNumbersControl.ItemsSource = lineNumbers;
        }
    }

    private void UpdateResponseLineNumbers()
    {
        var text = ResponseBodyText?.Text ?? string.Empty;
        var lineCount = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;
        
        if (ResponseLineNumbersControl != null)
        {
            var lineNumbers = new List<string>();
            for (int i = 1; i <= lineCount; i++)
            {
                lineNumbers.Add(i.ToString());
            }
            ResponseLineNumbersControl.ItemsSource = lineNumbers;
        }
        
        // Apply syntax highlighting to response
        if (ResponseHighlightBlock != null)
        {
            try
            {
                JsonSyntaxHighlighter.ApplyHighlighting(ResponseHighlightBlock, text);
            }
            catch
            {
                // Fallback to plain text
                ResponseHighlightBlock.Text = text;
            }
        }
    }

    private void BodyTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateLineNumbers();
    }

    private void FormatJson_Click(object sender, RoutedEventArgs e)
    {
        if (BodyTextBox == null) return;
        
        try
        {
            var text = BodyTextBox.Text;
            if (string.IsNullOrWhiteSpace(text)) return;
            
            using var doc = JsonDocument.Parse(text);
            var formatted = JsonSerializer.Serialize(doc, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            BodyTextBox.Text = formatted;
        }
        catch (JsonException)
        {
            // Invalid JSON, don't format
        }
    }

    private void DeleteParameter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is RequestParameter parameter)
        {
            var parameters = GetParameters();
            if (parameters != null)
            {
                if (parameters.Count > 1)
                {
                    parameters.Remove(parameter);
                }
                else
                {
                    parameter.ParamKey = string.Empty;
                    parameter.ParamValue = string.Empty;
                    parameter.Description = string.Empty;
                    parameter.IsEnabled = true;
                }
            }
        }
    }

    private void AddParameter_Click(object sender, RoutedEventArgs e)
    {
        var parameters = GetParameters();
        parameters?.Add(new RequestParameter());
    }

    private void ClearAllParameters_Click(object sender, RoutedEventArgs e)
    {
        var parameters = GetParameters();
        if (parameters != null)
        {
            parameters.Clear();
            parameters.Add(new RequestParameter());
        }
    }

    private void DeleteHeader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is RequestHeader header)
        {
            var headers = GetHeaders();
            if (headers != null)
            {
                if (headers.Count > 1)
                {
                    headers.Remove(header);
                }
                else
                {
                    header.HeaderKey = string.Empty;
                    header.HeaderValue = string.Empty;
                    header.Description = string.Empty;
                    header.IsEnabled = true;
                }
            }
        }
    }

    private void AddHeader_Click(object sender, RoutedEventArgs e)
    {
        var headers = GetHeaders();
        headers?.Add(new RequestHeader());
    }

    private void ClearAllHeaders_Click(object sender, RoutedEventArgs e)
    {
        var headers = GetHeaders();
        if (headers != null)
        {
            headers.Clear();
            headers.Add(new RequestHeader());
        }
    }

    private void ToggleHeaderActive_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is RequestHeader header)
        {
            header.IsEnabled = !header.IsEnabled;
        }
    }

    private void HeaderKey_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var query = sender.Text?.ToLowerInvariant() ?? string.Empty;
            var suggestions = CommonHeaderKeys.All
                .Where(h => string.IsNullOrEmpty(query) || h.ToLowerInvariant().Contains(query))
                .Take(15)
                .ToList();
            sender.ItemsSource = suggestions;
        }
    }

    private void HeaderKey_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string selectedHeader)
        {
            sender.Text = selectedHeader;
        }
    }

    #region Response Section Handlers

    private void UpdateRawLineNumbers()
    {
        var text = ResponseBodyText?.Text ?? string.Empty;
        var lineCount = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;
        
        if (RawLineNumbersControl != null)
        {
            var lineNumbers = new List<string>();
            for (int i = 1; i <= lineCount; i++)
            {
                lineNumbers.Add(i.ToString());
            }
            RawLineNumbersControl.ItemsSource = lineNumbers;
        }
    }

    private void ResponseTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tagStr && int.TryParse(tagStr, out int tabIndex))
        {
            SwitchToResponseTab(tabIndex);
        }
    }

    private void SwitchToResponseTab(int tabIndex)
    {
        System.Diagnostics.Debug.WriteLine($"SwitchToResponseTab called with index: {tabIndex}, current: {_currentResponseTabIndex}");
        
        if (tabIndex == _currentResponseTabIndex || _responseTabButtons == null) return;
        
        for (int i = 0; i < _responseTabButtons.Length; i++)
        {
            _responseTabButtons[i].Style = i == tabIndex 
                ? (Style)Resources["RequestTabActiveStyle"] ?? (Style)Application.Current.Resources["RequestTabActiveStyle"]
                : (Style)Resources["RequestTabStyle"] ?? (Style)Application.Current.Resources["RequestTabStyle"];
            
            _responseTabPanels[i].Visibility = i == tabIndex ? Visibility.Visible : Visibility.Collapsed;
            System.Diagnostics.Debug.WriteLine($"  Panel {i}: Visibility = {_responseTabPanels[i].Visibility}");
        }
        
        _currentResponseTabIndex = tabIndex;
        
        // Debug: Check ResponseHeaders binding
        if (tabIndex == 2 && ResponseHeadersControl != null)
        {
            System.Diagnostics.Debug.WriteLine($"  ResponseHeadersControl.ItemsSource has {(ResponseHeadersControl.ItemsSource as System.Collections.ICollection)?.Count ?? 0} items");
        }
    }

    private void ResponseContent_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        // Sync line numbers scroll with content scroll
        if (sender is ScrollViewer contentScroller && LineNumbersScroller != null)
        {
            LineNumbersScroller.ChangeView(null, contentScroller.VerticalOffset, null, true);
        }
    }

    private void RawContent_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        // Sync line numbers scroll with content scroll
        if (sender is ScrollViewer contentScroller && RawLineNumbersScroller != null)
        {
            RawLineNumbersScroller.ChangeView(null, contentScroller.VerticalOffset, null, true);
        }
    }

    private void ResponseFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox filterBox)
        {
            _filterText = filterBox.Text ?? string.Empty;
            ApplyResponseFilter();
        }
    }

    private void ApplyResponseFilter()
    {
        var text = ResponseBodyText?.Text ?? string.Empty;
        
        if (string.IsNullOrEmpty(_filterText))
        {
            // Show all content
            if (ResponseHighlightBlock != null)
            {
                JsonSyntaxHighlighter.ApplyHighlighting(ResponseHighlightBlock, text);
            }
            if (ResponseRawText != null)
            {
                ResponseRawText.Text = text;
            }
        }
        else
        {
            // Filter lines containing the search text
            var lines = text.Split('\n');
            var filteredLines = lines.Where(l => l.Contains(_filterText, StringComparison.OrdinalIgnoreCase));
            var filteredText = string.Join("\n", filteredLines);
            
            if (ResponseHighlightBlock != null)
            {
                JsonSyntaxHighlighter.ApplyHighlighting(ResponseHighlightBlock, filteredText);
            }
            if (ResponseRawText != null)
            {
                ResponseRawText.Text = filteredText;
            }
            
            // Update line numbers for filtered content
            var lineCount = string.IsNullOrEmpty(filteredText) ? 1 : filteredText.Split('\n').Length;
            var lineNumbers = new List<string>();
            for (int i = 1; i <= lineCount; i++)
            {
                lineNumbers.Add(i.ToString());
            }
            if (ResponseLineNumbersControl != null)
            {
                ResponseLineNumbersControl.ItemsSource = lineNumbers;
            }
            if (RawLineNumbersControl != null)
            {
                RawLineNumbersControl.ItemsSource = lineNumbers;
            }
        }
    }

    private void WrapLines_Click(object sender, RoutedEventArgs e)
    {
        _wrapLines = !_wrapLines;
        
        var wrapping = _wrapLines ? TextWrapping.Wrap : TextWrapping.NoWrap;
        var scrollMode = _wrapLines ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        
        // Update JSON view
        if (ResponseHighlightBlock != null)
        {
            ResponseHighlightBlock.TextWrapping = wrapping;
        }
        if (ResponseContentScroller != null)
        {
            ResponseContentScroller.HorizontalScrollBarVisibility = scrollMode;
        }
        
        // Update Raw view
        if (ResponseRawText != null)
        {
            ResponseRawText.TextWrapping = wrapping;
        }
        if (RawContentScroller != null)
        {
            RawContentScroller.HorizontalScrollBarVisibility = scrollMode;
        }
        
        // Update icon color to indicate active state
        if (WrapIcon != null)
        {
            WrapIcon.Foreground = _wrapLines 
                ? (Brush)Application.Current.Resources["AccentPrimaryBrush"] 
                : (Brush)Application.Current.Resources["TextMutedBrush"];
        }
    }

    private async void CopyResponse_Click(object sender, RoutedEventArgs e)
    {
        var text = ResponseBodyText?.Text ?? string.Empty;
        if (!string.IsNullOrEmpty(text))
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(text);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            
            // Show feedback - change icon to checkmark temporarily
            if (CopyResponseIcon != null)
            {
                var originalGlyph = CopyResponseIcon.Glyph;
                var originalForeground = CopyResponseIcon.Foreground;
                
                CopyResponseIcon.Glyph = "\uE73E"; // Checkmark
                CopyResponseIcon.Foreground = (Brush)Application.Current.Resources["SuccessBrush"];
                
                await Task.Delay(1500);
                
                CopyResponseIcon.Glyph = originalGlyph;
                CopyResponseIcon.Foreground = originalForeground;
            }
        }
    }

    private async void DownloadResponse_Click(object sender, RoutedEventArgs e)
    {
        var text = ResponseBodyText?.Text ?? string.Empty;
        if (string.IsNullOrEmpty(text)) return;

        try
        {
            // Use file picker for saving
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            savePicker.FileTypeChoices.Add("JSON", new List<string> { ".json" });
            savePicker.FileTypeChoices.Add("Text", new List<string> { ".txt" });
            savePicker.SuggestedFileName = $"response_{DateTime.Now:yyyyMMdd_HHmmss}";

            // Initialize with window handle
            var window = (Application.Current as App)?.MainWindow;
            if (window != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            }

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await Windows.Storage.FileIO.WriteTextAsync(file, text);
            }
        }
        catch
        {
            // Fallback: copy to clipboard
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(text);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

    #endregion

    #region Response Splitter Handlers

    private void ResponseSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border splitter)
        {
            _isResizing = true;
            _startY = e.GetCurrentPoint(this).Position.Y;
            _startHeight = ResponseSection?.Height ?? 280;
            splitter.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void ResponseSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isResizing && ResponseSection != null)
        {
            var currentY = e.GetCurrentPoint(this).Position.Y;
            var delta = _startY - currentY; // Negative when dragging down, positive when dragging up
            var newHeight = _startHeight + delta;
            
            // Clamp to min/max bounds
            newHeight = Math.Max(ResponseSection.MinHeight, Math.Min(ResponseSection.MaxHeight, newHeight));
            ResponseSection.Height = newHeight;
            e.Handled = true;
        }
    }

    private void ResponseSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border splitter)
        {
            _isResizing = false;
            splitter.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
    }

    private void ResponseSplitter_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _isResizing = false;
    }

    #endregion

    #region Navigation

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var content = item.Content?.ToString();
            if (content == "REST")
            {
                RestContent.Visibility = Visibility.Visible;
                GraphQLContent.Visibility = Visibility.Collapsed;
            }
            else if (content == "GraphQL")
            {
                RestContent.Visibility = Visibility.Collapsed;
                GraphQLContent.Visibility = Visibility.Visible;
                UpdateGQLQueryLineNumbers();
            }
        }
    }

    #endregion

    #region GraphQL Handlers

    private int _gqlCurrentTabIndex = 0;
    private int _gqlCurrentResponseTabIndex = 0;
    private bool _gqlWrapLines = false;
    private bool _gqlIsResizing = false;
    private double _gqlStartY;
    private double _gqlStartHeight;
    private string _gqlResponseBody = string.Empty;

    private GraphQLTabManager? GetGraphQLTabManager()
    {
        var dc = DataContext;
        if (dc == null) return null;
        var prop = dc.GetType().GetProperty("GraphQLTabManager");
        return prop?.GetValue(dc) as GraphQLTabManager;
    }

    private void AddGraphQLTab_Click(object sender, RoutedEventArgs e)
    {
        GetGraphQLTabManager()?.AddNewTab();
    }

    private void GraphQLTab_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border && border.DataContext is GraphQLTab tab)
        {
            GetGraphQLTabManager()?.SetActiveTab(tab);
            UpdateGQLQueryLineNumbers();
            UpdateGQLVariablesLineNumbers();
        }
    }

    private void CloseGraphQLTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is GraphQLTab tab)
        {
            GetGraphQLTabManager()?.CloseTab(tab);
        }
    }

    private async void GraphQLTabName_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is GraphQLTab tab)
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
                GetGraphQLTabManager()?.RenameTab(tab, nameTextBox.Text.Trim());
            }
        }
    }

    private void GQLTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tagStr && int.TryParse(tagStr, out int tabIndex))
        {
            SwitchToGQLTab(tabIndex);
        }
    }

    private void SwitchToGQLTab(int tabIndex)
    {
        if (tabIndex == _gqlCurrentTabIndex) return;

        var tabButtons = new Button[] { GQLTabQuery, GQLTabVariables, GQLTabHeaders, GQLTabAuth };
        var tabPanels = new FrameworkElement[] { GQLQueryPanel, GQLVariablesPanel, GQLHeadersPanel, GQLAuthPanel };

        for (int i = 0; i < tabButtons.Length; i++)
        {
            tabButtons[i].Style = i == tabIndex
                ? (Style)Resources["RequestTabActiveStyle"] ?? (Style)Application.Current.Resources["RequestTabActiveStyle"]
                : (Style)Resources["RequestTabStyle"] ?? (Style)Application.Current.Resources["RequestTabStyle"];

            tabPanels[i].Visibility = i == tabIndex ? Visibility.Visible : Visibility.Collapsed;
        }

        _gqlCurrentTabIndex = tabIndex;

        if (tabIndex == 0)
            UpdateGQLQueryLineNumbers();
        else if (tabIndex == 1)
            UpdateGQLVariablesLineNumbers();
        else if (tabIndex == 2)
            UpdateGQLHeadersCount();
    }

    private void GQLQueryTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateGQLQueryLineNumbers();
    }

    private void UpdateGQLQueryLineNumbers()
    {
        var text = GQLQueryTextBox?.Text ?? string.Empty;
        var lineCount = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;
        var lineNumbers = Enumerable.Range(1, lineCount).Select(i => i.ToString()).ToList();
        if (GQLQueryLineNumbers != null)
            GQLQueryLineNumbers.ItemsSource = lineNumbers;
    }

    private void GQLVariablesTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateGQLVariablesLineNumbers();
    }

    private void UpdateGQLVariablesLineNumbers()
    {
        var text = GQLVariablesTextBox?.Text ?? string.Empty;
        var lineCount = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;
        var lineNumbers = Enumerable.Range(1, lineCount).Select(i => i.ToString()).ToList();
        if (GQLVariablesLineNumbers != null)
            GQLVariablesLineNumbers.ItemsSource = lineNumbers;
    }

    private void UpdateGQLHeadersCount()
    {
        var tabManager = GetGraphQLTabManager();
        if (tabManager?.ActiveTab != null && GQLHeadersCount != null)
        {
            GQLHeadersCount.Text = tabManager.ActiveTab.Headers.Count.ToString();
        }
    }

    private void GQLQuery_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (sender is ScrollViewer contentScroller && GQLQueryLineNumbersScroller != null)
        {
            GQLQueryLineNumbersScroller.ChangeView(null, contentScroller.VerticalOffset, null, true);
        }
    }

    private void UpdateGQLResponseLineNumbers()
    {
        var lineCount = string.IsNullOrEmpty(_gqlResponseBody) ? 1 : _gqlResponseBody.Split('\n').Length;
        var lineNumbers = Enumerable.Range(1, lineCount).Select(i => i.ToString()).ToList();
        if (GQLResponseLineNumbersControl != null)
            GQLResponseLineNumbersControl.ItemsSource = lineNumbers;
    }

    private void GQLHistory_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Show history panel
    }

    private async void GQLCopyQuery_Click(object sender, RoutedEventArgs e)
    {
        var text = GQLQueryTextBox?.Text ?? string.Empty;
        if (!string.IsNullOrEmpty(text))
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(text);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

    private void GQLFormatQuery_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement GraphQL query formatting
    }

    private void GQLFormatVariables_Click(object sender, RoutedEventArgs e)
    {
        if (GQLVariablesTextBox == null) return;

        try
        {
            var text = GQLVariablesTextBox.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            using var doc = JsonDocument.Parse(text);
            var formatted = JsonSerializer.Serialize(doc, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            GQLVariablesTextBox.Text = formatted;
        }
        catch (JsonException)
        {
            // Invalid JSON
        }
    }

    private void GQLClearVariables_Click(object sender, RoutedEventArgs e)
    {
        if (GQLVariablesTextBox != null)
            GQLVariablesTextBox.Text = "{}";
    }

    private void GQLAddHeader_Click(object sender, RoutedEventArgs e)
    {
        var tabManager = GetGraphQLTabManager();
        tabManager?.ActiveTab?.Headers.Add(new RequestHeader());
        UpdateGQLHeadersCount();
    }

    private void GQLDeleteHeader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is RequestHeader header)
        {
            var tabManager = GetGraphQLTabManager();
            var headers = tabManager?.ActiveTab?.Headers;
            if (headers != null)
            {
                if (headers.Count > 1)
                {
                    headers.Remove(header);
                }
                else
                {
                    header.HeaderKey = string.Empty;
                    header.HeaderValue = string.Empty;
                    header.IsEnabled = true;
                }
                UpdateGQLHeadersCount();
            }
        }
    }

    private void GQLClearAllHeaders_Click(object sender, RoutedEventArgs e)
    {
        var tabManager = GetGraphQLTabManager();
        var headers = tabManager?.ActiveTab?.Headers;
        if (headers != null)
        {
            headers.Clear();
            headers.Add(new RequestHeader());
            UpdateGQLHeadersCount();
        }
    }

    private void GQLAuthType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is AuthTypeOption selectedType)
        {
            var tabManager = GetGraphQLTabManager();
            if (tabManager?.ActiveTab?.Authorization != null)
            {
                tabManager.ActiveTab.Authorization.AuthType = selectedType.Type;
                tabManager.ActiveTab.Authorization.IsEnabled = selectedType.Type != AuthorizationType.None;
            }
            SwitchToGQLAuthPanel(selectedType.Type);
        }
    }

    private void SwitchToGQLAuthPanel(AuthorizationType authType)
    {
        if (GQLAuthNonePanel == null) return;

        GQLAuthNonePanel.Visibility = authType == AuthorizationType.None ? Visibility.Visible : Visibility.Collapsed;
        if (GQLAuthBasicPanel != null)
            GQLAuthBasicPanel.Visibility = authType == AuthorizationType.BasicAuth ? Visibility.Visible : Visibility.Collapsed;
        if (GQLAuthBearerPanel != null)
            GQLAuthBearerPanel.Visibility = authType == AuthorizationType.BearerToken ? Visibility.Visible : Visibility.Collapsed;
        if (GQLAuthApiKeyPanel != null)
            GQLAuthApiKeyPanel.Visibility = authType == AuthorizationType.ApiKey ? Visibility.Visible : Visibility.Collapsed;
    }

    private void GQLBasicAuth_TextChanged(object sender, TextChangedEventArgs e)
    {
        var tabManager = GetGraphQLTabManager();
        if (tabManager?.ActiveTab?.Authorization != null && GQLBasicUsernameInput != null)
        {
            tabManager.ActiveTab.Authorization.Username = GQLBasicUsernameInput.Text;
        }
    }

    private void GQLBasicAuth_PasswordChanged(object sender, RoutedEventArgs e)
    {
        var tabManager = GetGraphQLTabManager();
        if (tabManager?.ActiveTab?.Authorization != null && GQLBasicPasswordInput != null)
        {
            tabManager.ActiveTab.Authorization.Password = GQLBasicPasswordInput.Password;
        }
    }

    private void GQLBearerToken_TextChanged(object sender, TextChangedEventArgs e)
    {
        var tabManager = GetGraphQLTabManager();
        if (tabManager?.ActiveTab?.Authorization != null && GQLBearerTokenInput != null)
        {
            tabManager.ActiveTab.Authorization.BearerToken = GQLBearerTokenInput.Text;
        }
    }

    private void GQLApiKey_TextChanged(object sender, TextChangedEventArgs e)
    {
        var tabManager = GetGraphQLTabManager();
        if (tabManager?.ActiveTab?.Authorization != null)
        {
            if (GQLApiKeyNameInput != null)
                tabManager.ActiveTab.Authorization.ApiKeyName = GQLApiKeyNameInput.Text;
            if (GQLApiKeyValueInput != null)
                tabManager.ActiveTab.Authorization.ApiKeyValue = GQLApiKeyValueInput.Text;
        }
    }

    private void GQLApiKeyLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ApiKeyLocationOption option)
        {
            var tabManager = GetGraphQLTabManager();
            if (tabManager?.ActiveTab?.Authorization != null)
            {
                tabManager.ActiveTab.Authorization.ApiKeyLocation = option.Location;
            }
        }
    }

    private void GQLClearAuth_Click(object sender, RoutedEventArgs e)
    {
        var tabManager = GetGraphQLTabManager();
        if (tabManager?.ActiveTab?.Authorization != null)
        {
            var auth = tabManager.ActiveTab.Authorization;
            auth.AuthType = AuthorizationType.None;
            auth.IsEnabled = false;
            auth.Username = string.Empty;
            auth.Password = string.Empty;
            auth.BearerToken = string.Empty;
            auth.ApiKeyName = string.Empty;
            auth.ApiKeyValue = string.Empty;
        }
        
        if (GQLBasicUsernameInput != null)
            GQLBasicUsernameInput.Text = string.Empty;
        if (GQLBasicPasswordInput != null)
            GQLBasicPasswordInput.Password = string.Empty;
        if (GQLBearerTokenInput != null)
            GQLBearerTokenInput.Text = string.Empty;
        if (GQLApiKeyNameInput != null)
            GQLApiKeyNameInput.Text = string.Empty;
        if (GQLApiKeyValueInput != null)
            GQLApiKeyValueInput.Text = string.Empty;
        if (GQLAuthTypeComboBox != null)
            GQLAuthTypeComboBox.SelectedIndex = 0;
        if (GQLApiKeyLocationComboBox != null)
            GQLApiKeyLocationComboBox.SelectedIndex = 0;
    }

    private void GQLResponseTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tagStr && int.TryParse(tagStr, out int tabIndex))
        {
            SwitchToGQLResponseTab(tabIndex);
        }
    }

    private void SwitchToGQLResponseTab(int tabIndex)
    {
        if (tabIndex == _gqlCurrentResponseTabIndex) return;

        var tabButtons = new Button[] { GQLResponseTabJson, GQLResponseTabRaw, GQLResponseTabHeaders, GQLResponseTabErrors };
        var tabPanels = new FrameworkElement[] { GQLResponseJsonPanel, GQLResponseRawPanel, GQLResponseHeadersPanel, GQLResponseErrorsPanel };

        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null)
            {
                tabButtons[i].Style = i == tabIndex
                    ? (Style)Resources["RequestTabActiveStyle"] ?? (Style)Application.Current.Resources["RequestTabActiveStyle"]
                    : (Style)Resources["RequestTabStyle"] ?? (Style)Application.Current.Resources["RequestTabStyle"];
            }

            if (tabPanels[i] != null)
                tabPanels[i].Visibility = i == tabIndex ? Visibility.Visible : Visibility.Collapsed;
        }

        _gqlCurrentResponseTabIndex = tabIndex;
    }

    private void GQLResponseContent_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (sender is ScrollViewer contentScroller && GQLResponseLineNumbersScroller != null)
        {
            GQLResponseLineNumbersScroller.ChangeView(null, contentScroller.VerticalOffset, null, true);
        }
    }

    private void GQLWrapLines_Click(object sender, RoutedEventArgs e)
    {
        _gqlWrapLines = !_gqlWrapLines;

        var wrapping = _gqlWrapLines ? TextWrapping.Wrap : TextWrapping.NoWrap;
        var scrollMode = _gqlWrapLines ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;

        if (GQLResponseHighlightBlock != null)
            GQLResponseHighlightBlock.TextWrapping = wrapping;
        if (GQLResponseContentScroller != null)
            GQLResponseContentScroller.HorizontalScrollBarVisibility = scrollMode;
        if (GQLResponseRawText != null)
            GQLResponseRawText.TextWrapping = wrapping;
        if (GQLRawContentScroller != null)
            GQLRawContentScroller.HorizontalScrollBarVisibility = scrollMode;

        if (GQLWrapIcon != null)
        {
            GQLWrapIcon.Foreground = _gqlWrapLines
                ? (Brush)Application.Current.Resources["AccentPrimaryBrush"]
                : (Brush)Application.Current.Resources["TextMutedBrush"];
        }
    }

    private async void GQLCopyResponse_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_gqlResponseBody))
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(_gqlResponseBody);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

            if (GQLCopyResponseIcon != null)
            {
                var originalGlyph = GQLCopyResponseIcon.Glyph;
                var originalForeground = GQLCopyResponseIcon.Foreground;

                GQLCopyResponseIcon.Glyph = "\uE73E";
                GQLCopyResponseIcon.Foreground = (Brush)Application.Current.Resources["SuccessBrush"];

                await Task.Delay(1500);

                GQLCopyResponseIcon.Glyph = originalGlyph;
                GQLCopyResponseIcon.Foreground = originalForeground;
            }
        }
    }

    private async void GQLDownloadResponse_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_gqlResponseBody)) return;

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
                await Windows.Storage.FileIO.WriteTextAsync(file, _gqlResponseBody);
            }
        }
        catch
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(_gqlResponseBody);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

    private void GQLClearResponse_Click(object sender, RoutedEventArgs e)
    {
        _gqlResponseBody = string.Empty;
        
        if (GQLResponseHighlightBlock != null)
            GQLResponseHighlightBlock.Text = "// Response will appear here after running a request";
        if (GQLResponseRawText != null)
            GQLResponseRawText.Text = string.Empty;
        if (GQLResponseStatusText != null)
            GQLResponseStatusText.Text = "Awaiting request";
        if (GQLStatusBadge != null)
            GQLStatusBadge.Background = (Brush)Application.Current.Resources["TextMutedBrush"];
        if (GQLResponseTimeText != null)
            GQLResponseTimeText.Text = "0 ms";
        if (GQLResponseSizeText != null)
            GQLResponseSizeText.Text = "0 KB";
        if (GQLResponseLineNumbersControl != null)
            GQLResponseLineNumbersControl.ItemsSource = null;
    }

    #region GraphQL Response Splitter

    private void GQLResponseSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border splitter)
        {
            _gqlIsResizing = true;
            _gqlStartY = e.GetCurrentPoint(this).Position.Y;
            _gqlStartHeight = GQLResponseSection?.Height ?? 280;
            splitter.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void GQLResponseSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_gqlIsResizing && GQLResponseSection != null)
        {
            var currentY = e.GetCurrentPoint(this).Position.Y;
            var delta = _gqlStartY - currentY;
            var newHeight = _gqlStartHeight + delta;

            newHeight = Math.Max(GQLResponseSection.MinHeight, Math.Min(GQLResponseSection.MaxHeight, newHeight));
            GQLResponseSection.Height = newHeight;
            e.Handled = true;
        }
    }

    private void GQLResponseSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border splitter)
        {
            _gqlIsResizing = false;
            splitter.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
    }

    private void GQLResponseSplitter_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _gqlIsResizing = false;
    }

    #endregion

    #endregion

    #region Request Tab Handlers

    private RequestTabManager? GetTabManager()
    {
        var dc = DataContext;
        if (dc == null) return null;
        
        var prop = dc.GetType().GetProperty("TabManager");
        return prop?.GetValue(dc) as RequestTabManager;
    }

    private void AddNewTab_Click(object sender, RoutedEventArgs e)
    {
        var tabManager = GetTabManager();
        tabManager?.AddNewTab();
        SyncActiveTabToUI();
    }

    private void HttpMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is string method)
        {
            var tabManager = GetTabManager();
            if (tabManager?.ActiveTab != null)
            {
                tabManager.ActiveTab.HttpMethod = method;
            }
        }
    }

    private void RequestTab_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border && border.DataContext is RequestTab tab)
        {
            var tabManager = GetTabManager();
            if (tabManager != null)
            {
                tabManager.SetActiveTab(tab);
                SyncActiveTabToUI();
            }
        }
    }

    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is RequestTab tab)
        {
            var tabManager = GetTabManager();
            tabManager?.CloseTab(tab);
            SyncActiveTabToUI();
        }
    }

    private async void TabName_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is RequestTab tab)
        {
            await ShowRenameDialog(tab);
        }
    }

    private async Task ShowRenameDialog(RequestTab tab)
    {
        var dialog = new ContentDialog
        {
            Title = "Edit Request",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var inputPanel = new StackPanel { Spacing = 12 };
        
        var labelText = new TextBlock
        {
            Text = "Label",
            Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
            FontFamily = (FontFamily)Application.Current.Resources["AppFontFamilyMedium"],
            FontSize = 12
        };
        
        var nameTextBox = new TextBox
        {
            Text = tab.Name,
            PlaceholderText = "Untitled",
            SelectionStart = 0,
            SelectionLength = tab.Name.Length
        };

        inputPanel.Children.Add(labelText);
        inputPanel.Children.Add(nameTextBox);
        
        dialog.Content = inputPanel;

        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            var newName = nameTextBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(newName))
            {
                var tabManager = GetTabManager();
                tabManager?.RenameTab(tab, newName);
            }
        }
    }

    private void SyncActiveTabToUI()
    {
        var tabManager = GetTabManager();
        var activeTab = tabManager?.ActiveTab;
        
        if (activeTab == null) return;
        
        // Sync method, URL, body to IState properties
        // This bridges the tab data to the existing binding infrastructure
        var dc = DataContext;
        if (dc == null) return;
        
        // Update SelectedMethod state
        var methodProp = dc.GetType().GetProperty("SelectedMethod");
        if (methodProp?.GetValue(dc) is IState<string> methodState)
        {
            _ = methodState.UpdateAsync(_ => activeTab.HttpMethod, CancellationToken.None);
        }
        
        // Update RequestUrl state
        var urlProp = dc.GetType().GetProperty("RequestUrl");
        if (urlProp?.GetValue(dc) is IState<string> urlState)
        {
            _ = urlState.UpdateAsync(_ => activeTab.Url, CancellationToken.None);
        }
        
        // Update BodyText state
        var bodyProp = dc.GetType().GetProperty("BodyText");
        if (bodyProp?.GetValue(dc) is IState<string> bodyState)
        {
            _ = bodyState.UpdateAsync(_ => activeTab.BodyText, CancellationToken.None);
        }
        
        // Update SelectedContentType state
        var contentTypeProp = dc.GetType().GetProperty("SelectedContentType");
        if (contentTypeProp?.GetValue(dc) is IState<ContentType> contentTypeState)
        {
            _ = contentTypeState.UpdateAsync(_ => activeTab.ContentType, CancellationToken.None);
        }
        
        // Refresh bindings for Parameters and Headers (they now point to active tab's collections)
        ParametersItemsControl?.GetBindingExpression(ItemsControl.ItemsSourceProperty)?.UpdateSource();
        HeadersItemsControl?.GetBindingExpression(ItemsControl.ItemsSourceProperty)?.UpdateSource();
    }

    private void SyncUIToActiveTab()
    {
        var tabManager = GetTabManager();
        var activeTab = tabManager?.ActiveTab;
        
        if (activeTab == null) return;
        
        var dc = DataContext;
        if (dc == null) return;
        
        // Get current values from UI states and update active tab
        var methodProp = dc.GetType().GetProperty("SelectedMethod");
        if (methodProp?.GetValue(dc) is IState<string> methodState)
        {
            var method = methodState.GetType().GetMethod("GetValue")?.Invoke(methodState, null) as string;
            if (!string.IsNullOrEmpty(method))
            {
                activeTab.HttpMethod = method;
            }
        }
        
        var urlProp = dc.GetType().GetProperty("RequestUrl");
        if (urlProp?.GetValue(dc) is IState<string> urlState)
        {
            var url = urlState.GetType().GetMethod("GetValue")?.Invoke(urlState, null) as string;
            if (url != null)
            {
                activeTab.Url = url;
            }
        }
        
        var bodyProp = dc.GetType().GetProperty("BodyText");
        if (bodyProp?.GetValue(dc) is IState<string> bodyState)
        {
            var body = bodyState.GetType().GetMethod("GetValue")?.Invoke(bodyState, null) as string;
            if (body != null)
            {
                activeTab.BodyText = body;
            }
        }
    }

    #endregion

    #region Authorization Handlers

    private AuthorizationConfig? GetAuthorizationConfig()
    {
        var dc = DataContext;
        if (dc == null) return null;
        
        var prop = dc.GetType().GetProperty("Authorization");
        return prop?.GetValue(dc) as AuthorizationConfig;
    }

    private void InitializeAuthTypeRadioButtons()
    {
        // Find and check the first radio button
        if (AuthPanel?.Content is Grid authGrid)
        {
            var itemsControl = FindDescendant<ItemsControl>(authGrid);
            if (itemsControl?.ItemsSource is System.Collections.IEnumerable items)
            {
                var firstContainer = itemsControl.ContainerFromIndex(0);
                if (firstContainer != null)
                {
                    var radio = FindDescendant<RadioButton>(firstContainer as DependencyObject);
                    if (radio != null)
                    {
                        radio.IsChecked = true;
                    }
                }
            }
        }
    }

    private T? FindDescendant<T>(DependencyObject? parent) where T : class
    {
        if (parent == null) return default;
        
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;
            
            var descendant = FindDescendant<T>(child);
            if (descendant != null)
                return descendant;
        }
        return default;
    }

    private void AuthType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is AuthTypeOption selectedType)
        {
            var auth = GetAuthorizationConfig();
            if (auth != null)
            {
                auth.AuthType = selectedType.Type;
            }
            SwitchToAuthPanel(selectedType.Type);
            UpdateAuthTypeRadioButtons(selectedType.Type);
        }
    }

    private void AuthTypeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radio && radio.Tag is AuthorizationType authType)
        {
            var auth = GetAuthorizationConfig();
            if (auth != null)
            {
                auth.AuthType = authType;
            }
            SwitchToAuthPanel(authType);
            UpdateAuthTypeComboBox(authType);
        }
    }

    private void AuthTypeItem_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            var radio = FindDescendant<RadioButton>(border);
            if (radio != null)
            {
                radio.IsChecked = true;
            }
        }
    }

    private void SwitchToAuthPanel(AuthorizationType authType)
    {
        if (_authPanels == null) return;
        
        var panelIndex = authType switch
        {
            AuthorizationType.None => 0,
            AuthorizationType.BasicAuth => 1,
            AuthorizationType.BearerToken => 2,
            AuthorizationType.ApiKey => 3,
            AuthorizationType.OAuth2 => 4,
            _ => 0
        };
        
        for (int i = 0; i < _authPanels.Length; i++)
        {
            _authPanels[i].Visibility = i == panelIndex ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void UpdateAuthTypeComboBox(AuthorizationType authType)
    {
        if (AuthTypeComboBox?.ItemsSource is System.Collections.IList items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is AuthTypeOption option && option.Type == authType)
                {
                    AuthTypeComboBox.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void UpdateAuthTypeRadioButtons(AuthorizationType authType)
    {
        // This is handled by the ItemsControl template - radio buttons will update via Tag binding
    }

    private void ApiKeyLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ApiKeyLocationOption selectedLocation)
        {
            var auth = GetAuthorizationConfig();
            if (auth != null)
            {
                auth.ApiKeyLocation = selectedLocation.Location;
            }
        }
    }

    private void ClearAuth_Click(object sender, RoutedEventArgs e)
    {
        var auth = GetAuthorizationConfig();
        if (auth != null)
        {
            auth.Username = string.Empty;
            auth.Password = string.Empty;
            auth.BearerToken = string.Empty;
            auth.ApiKeyName = string.Empty;
            auth.ApiKeyValue = string.Empty;
            auth.AccessToken = string.Empty;
            auth.TokenType = "Bearer";
            
            // Clear UI fields
            if (BasicAuthUsername != null) BasicAuthUsername.Text = string.Empty;
            if (BasicAuthPassword != null) BasicAuthPassword.Password = string.Empty;
            if (BearerTokenInput != null) BearerTokenInput.Text = string.Empty;
            if (ApiKeyName != null) ApiKeyName.Text = string.Empty;
            if (ApiKeyValue != null) ApiKeyValue.Password = string.Empty;
            if (OAuth2AccessToken != null) OAuth2AccessToken.Text = string.Empty;
            if (OAuth2TokenType != null) OAuth2TokenType.Text = "Bearer";
        }
    }

    #endregion
}
