using System.Collections.ObjectModel;
using System.Text.Json;
using DevFlow.Models;

namespace DevFlow.Presentation;

public sealed partial class MainPage : Page
{
    private int _currentTabIndex = 0;
    private readonly Button[] _tabButtons;
    private readonly FrameworkElement[] _tabPanels;

    public MainPage()
    {
        this.InitializeComponent();
        
        _tabButtons = new Button[] { TabParameters, TabBody, TabHeaders, TabAuth, TabPreScript, TabPostScript };
        _tabPanels = new FrameworkElement[] { ParametersPanel, BodyPanel, HeadersPanel, AuthPanel, PreScriptPanel, PostScriptPanel };
        
        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateLineNumbers();
        UpdateResponseLineNumbers();
        
        // Subscribe to DataContext changes for response updates
        DataContextChanged += (s, args) =>
        {
            if (args.NewValue != null)
            {
                // Update response line numbers when response body changes
                var bodyProp = args.NewValue.GetType().GetProperty("ResponseBody");
                if (bodyProp != null)
                {
                    UpdateResponseLineNumbers();
                }
            }
        };
    }

    private ObservableCollection<RequestParameter>? GetParameters()
    {
        var dc = DataContext;
        if (dc == null) return null;
        
        var prop = dc.GetType().GetProperty("Parameters");
        return prop?.GetValue(dc) as ObservableCollection<RequestParameter>;
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
        var lineNumbers = new List<string>();
        
        for (int i = 1; i <= lineCount; i++)
        {
            lineNumbers.Add(i.ToString());
        }
        
        if (LineNumbersControl != null)
        {
            LineNumbersControl.ItemsSource = lineNumbers;
        }
    }

    private void UpdateResponseLineNumbers()
    {
        var text = ResponseBodyText?.Text ?? string.Empty;
        var lineCount = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;
        var lineNumbers = new List<string>();
        
        for (int i = 1; i <= lineCount; i++)
        {
            lineNumbers.Add(i.ToString());
        }
        
        if (ResponseLineNumbersControl != null)
        {
            ResponseLineNumbersControl.ItemsSource = lineNumbers;
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
}
