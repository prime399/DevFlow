using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevFlow.Controls;

public sealed partial class CodeEditorControl : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(CodeEditorControl),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(CodeEditorControl),
            new PropertyMetadata(string.Empty, OnPlaceholderTextChanged));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(CodeEditorControl),
            new PropertyMetadata(false, OnIsReadOnlyChanged));

    public static readonly DependencyProperty MinHeightOverrideProperty =
        DependencyProperty.Register(nameof(MinHeightOverride), typeof(double), typeof(CodeEditorControl),
            new PropertyMetadata(200.0, OnMinHeightOverrideChanged));

    private bool _isUpdatingText;

    public CodeEditorControl()
    {
        this.InitializeComponent();
        UpdateLineNumbers();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public double MinHeightOverride
    {
        get => (double)GetValue(MinHeightOverrideProperty);
        set => SetValue(MinHeightOverrideProperty, value);
    }

    public event EventHandler<string>? TextChanged;

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CodeEditorControl control && !control._isUpdatingText)
        {
            control._isUpdatingText = true;
            control.EditorTextBox.Text = e.NewValue as string ?? string.Empty;
            control._isUpdatingText = false;
            control.UpdateLineNumbers();
        }
    }

    private static void OnPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CodeEditorControl control)
        {
            control.EditorTextBox.PlaceholderText = e.NewValue as string ?? string.Empty;
        }
    }

    private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CodeEditorControl control)
        {
            control.EditorTextBox.IsReadOnly = (bool)e.NewValue;
        }
    }

    private static void OnMinHeightOverrideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CodeEditorControl control)
        {
            control.EditorTextBox.MinHeight = (double)e.NewValue;
        }
    }

    private void EditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isUpdatingText)
        {
            _isUpdatingText = true;
            Text = EditorTextBox.Text;
            _isUpdatingText = false;
        }
        UpdateLineNumbers();
        TextChanged?.Invoke(this, EditorTextBox.Text);
    }

    private void ContentScroller_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        LineNumbersScroller?.ChangeView(null, ContentScroller.VerticalOffset, null, true);
    }

    private void UpdateLineNumbers()
    {
        var text = EditorTextBox?.Text ?? string.Empty;
        var lineCount = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;

        var lineNumbers = new List<string>();
        for (int i = 1; i <= lineCount; i++)
        {
            lineNumbers.Add(i.ToString());
        }
        LineNumbersControl.ItemsSource = lineNumbers;
    }

    public void FormatJson()
    {
        if (EditorTextBox == null) return;

        try
        {
            var text = EditorTextBox.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            var formatted = DevFlow.Serialization.JsonHelper.FormatJson(text, relaxedEscaping: false);
            EditorTextBox.Text = formatted;
        }
        catch
        {
            // Invalid JSON, don't format
        }
    }

    public void Clear()
    {
        EditorTextBox.Text = string.Empty;
    }

    public void SetText(string text)
    {
        EditorTextBox.Text = text;
    }

    public string GetText() => EditorTextBox.Text;
}
