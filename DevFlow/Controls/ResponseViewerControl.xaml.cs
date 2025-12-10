using System.Collections;
using System.Collections.ObjectModel;
using DevFlow.Helpers;
using DevFlow.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DevFlow.Controls;

public sealed partial class ResponseViewerControl : UserControl
{
    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(nameof(Status), typeof(string), typeof(ResponseViewerControl),
            new PropertyMetadata("Awaiting request", OnStatusChanged));

    public static readonly DependencyProperty ResponseTimeProperty =
        DependencyProperty.Register(nameof(ResponseTime), typeof(string), typeof(ResponseViewerControl),
            new PropertyMetadata("0 ms", OnResponseTimeChanged));

    public static readonly DependencyProperty ResponseSizeProperty =
        DependencyProperty.Register(nameof(ResponseSize), typeof(string), typeof(ResponseViewerControl),
            new PropertyMetadata("0 KB", OnResponseSizeChanged));

    public static readonly DependencyProperty ResponseBodyProperty =
        DependencyProperty.Register(nameof(ResponseBody), typeof(string), typeof(ResponseViewerControl),
            new PropertyMetadata(string.Empty, OnResponseBodyChanged));

    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(ResponseViewerControl),
            new PropertyMetadata(string.Empty, OnErrorMessageChanged));

    public static readonly DependencyProperty ResponseHeadersProperty =
        DependencyProperty.Register(nameof(ResponseHeaders), typeof(IList), typeof(ResponseViewerControl),
            new PropertyMetadata(null, OnResponseHeadersChanged));

    private int _currentTabIndex = 0;
    private bool _wrapLines = false;
    private string _filterText = string.Empty;
    private string _originalBody = string.Empty;
    private Button[] _tabButtons = null!;
    private FrameworkElement[] _tabPanels = null!;

    public ResponseViewerControl()
    {
        this.InitializeComponent();
        _tabButtons = new Button[] { TabJson, TabRaw, TabHeaders };
        _tabPanels = new FrameworkElement[] { JsonPanel, RawPanel, HeadersPanel };
    }

    public string Status
    {
        get => (string)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public string ResponseTime
    {
        get => (string)GetValue(ResponseTimeProperty);
        set => SetValue(ResponseTimeProperty, value);
    }

    public string ResponseSize
    {
        get => (string)GetValue(ResponseSizeProperty);
        set => SetValue(ResponseSizeProperty, value);
    }

    public string ResponseBody
    {
        get => (string)GetValue(ResponseBodyProperty);
        set => SetValue(ResponseBodyProperty, value);
    }

    public string ErrorMessage
    {
        get => (string)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public IList? ResponseHeaders
    {
        get => (IList?)GetValue(ResponseHeadersProperty);
        set => SetValue(ResponseHeadersProperty, value);
    }

    public event EventHandler? ClearRequested;
    public event EventHandler? DownloadRequested;

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ResponseViewerControl control)
        {
            var status = e.NewValue as string ?? "Awaiting request";
            control.StatusText.Text = status;
            control.UpdateStatusBadgeColor(status);
        }
    }

    private static void OnResponseTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ResponseViewerControl control)
        {
            control.TimeText.Text = e.NewValue as string ?? "0 ms";
        }
    }

    private static void OnResponseSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ResponseViewerControl control)
        {
            control.SizeText.Text = e.NewValue as string ?? "0 KB";
        }
    }

    private static void OnResponseBodyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ResponseViewerControl control)
        {
            var body = e.NewValue as string ?? string.Empty;
            control._originalBody = body;
            control.ApplyFilter();
            control.UpdateLineNumbers();
        }
    }

    private static void OnErrorMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ResponseViewerControl control)
        {
            var error = e.NewValue as string ?? string.Empty;
            control.ErrorText.Text = error;
            control.ErrorText.Visibility = string.IsNullOrEmpty(error) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private static void OnResponseHeadersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ResponseViewerControl control)
        {
            control.HeadersListControl.ItemsSource = e.NewValue as IList;
        }
    }

    private void UpdateStatusBadgeColor(string statusText)
    {
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

        StatusBadge.Background = brush;
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
                ? (Style)Application.Current.Resources["RequestTabActiveStyle"]
                : (Style)Application.Current.Resources["RequestTabStyle"];

            _tabPanels[i].Visibility = i == tabIndex ? Visibility.Visible : Visibility.Collapsed;
        }

        _currentTabIndex = tabIndex;
    }

    private void Filter_TextChanged(object sender, TextChangedEventArgs e)
    {
        _filterText = FilterBox.Text ?? string.Empty;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var text = _originalBody;

        if (string.IsNullOrEmpty(_filterText))
        {
            try
            {
                JsonSyntaxHighlighter.ApplyHighlighting(JsonHighlightBlock, text);
            }
            catch
            {
                JsonHighlightBlock.Text = text;
            }
            RawText.Text = text;
        }
        else
        {
            var lines = text.Split('\n');
            var filteredLines = lines.Where(l => l.Contains(_filterText, StringComparison.OrdinalIgnoreCase));
            var filteredText = string.Join("\n", filteredLines);

            try
            {
                JsonSyntaxHighlighter.ApplyHighlighting(JsonHighlightBlock, filteredText);
            }
            catch
            {
                JsonHighlightBlock.Text = filteredText;
            }
            RawText.Text = filteredText;
        }

        UpdateLineNumbers();
    }

    private void UpdateLineNumbers()
    {
        var text = string.IsNullOrEmpty(_filterText) ? _originalBody : RawText.Text;
        var lineCount = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;

        var lineNumbers = new List<string>();
        for (int i = 1; i <= lineCount; i++)
        {
            lineNumbers.Add(i.ToString());
        }

        JsonLineNumbersControl.ItemsSource = lineNumbers;
        RawLineNumbersControl.ItemsSource = lineNumbers;
    }

    private void JsonContent_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        JsonLineNumbersScroller?.ChangeView(null, JsonContentScroller.VerticalOffset, null, true);
    }

    private void RawContent_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        RawLineNumbersScroller?.ChangeView(null, RawContentScroller.VerticalOffset, null, true);
    }

    private void WrapLines_Click(object sender, RoutedEventArgs e)
    {
        _wrapLines = !_wrapLines;

        var wrapping = _wrapLines ? TextWrapping.Wrap : TextWrapping.NoWrap;
        var scrollMode = _wrapLines ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;

        JsonHighlightBlock.TextWrapping = wrapping;
        JsonContentScroller.HorizontalScrollBarVisibility = scrollMode;
        RawText.TextWrapping = wrapping;
        RawContentScroller.HorizontalScrollBarVisibility = scrollMode;

        WrapIcon.Foreground = _wrapLines
            ? (Brush)Application.Current.Resources["AccentPrimaryBrush"]
            : (Brush)Application.Current.Resources["TextMutedBrush"];
    }

    private async void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_originalBody))
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(_originalBody);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

            var originalGlyph = CopyIcon.Glyph;
            var originalForeground = CopyIcon.Foreground;

            CopyIcon.Glyph = "\uE73E";
            CopyIcon.Foreground = (Brush)Application.Current.Resources["SuccessBrush"];

            await Task.Delay(1500);

            CopyIcon.Glyph = originalGlyph;
            CopyIcon.Foreground = originalForeground;
        }
    }

    private void Download_Click(object sender, RoutedEventArgs e)
    {
        DownloadRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        ClearRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ClearResponse()
    {
        Status = "Awaiting request";
        ResponseTime = "0 ms";
        ResponseSize = "0 KB";
        ResponseBody = string.Empty;
        ErrorMessage = string.Empty;
        HeadersListControl.ItemsSource = null;
        _originalBody = string.Empty;
        ApplyFilter();
    }
}
