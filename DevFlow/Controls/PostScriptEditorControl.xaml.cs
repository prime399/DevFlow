using System.Collections.ObjectModel;
using DevFlow.Models;
using DevFlow.Services.Scripting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevFlow.Controls;

public sealed partial class PostScriptEditorControl : UserControl
{
    public static readonly DependencyProperty ScriptTextProperty =
        DependencyProperty.Register(nameof(ScriptText), typeof(string), typeof(PostScriptEditorControl),
            new PropertyMetadata(string.Empty, OnScriptTextChanged));

    private readonly ObservableCollection<string> _consoleLines = new();
    private readonly PostRequestScriptRunner _scriptRunner;
    private bool _isUpdating;

    public PostScriptEditorControl()
    {
        this.InitializeComponent();
        _scriptRunner = new PostRequestScriptRunner();
        ConsoleOutput.ItemsSource = _consoleLines;
        UpdateLineNumbers();
    }

    public string ScriptText
    {
        get => (string)GetValue(ScriptTextProperty);
        set => SetValue(ScriptTextProperty, value);
    }

    public event EventHandler<ScriptExecutionResult>? ScriptExecuted;

    private static void OnScriptTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PostScriptEditorControl control && !control._isUpdating)
        {
            control._isUpdating = true;
            control.CodeEditor.Text = e.NewValue as string ?? string.Empty;
            control._isUpdating = false;
            control.UpdateLineNumbers();
        }
    }

    private void CodeEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isUpdating)
        {
            _isUpdating = true;
            ScriptText = CodeEditor.Text;
            _isUpdating = false;
        }
        UpdateLineNumbers();
    }

    private void UpdateLineNumbers()
    {
        var text = CodeEditor.Text ?? string.Empty;
        var lineCount = Math.Max(1, text.Split('\n').Length);
        var lines = Enumerable.Range(1, lineCount).Select(i => i.ToString()).ToList();
        LineNumbersControl.ItemsSource = lines;
    }

    private void Editor_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        LineNumbersScroller?.ChangeView(null, EditorScroller.VerticalOffset, null, true);
    }

    private void RunScript_Click(object sender, RoutedEventArgs e)
    {
        // Run with mock response for testing
        ExecuteScript(new ResponseContext
        {
            StatusCode = 200,
            Body = "{\"message\": \"Test response\", \"success\": true}",
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "X-Request-Id", "test-123" }
            }
        });
    }

    public ScriptExecutionResult ExecuteScript(ResponseContext? response = null)
    {
        _consoleLines.Clear();
        _consoleLines.Add($"[{DateTime.Now:HH:mm:ss}] Running post-request script...");

        var result = _scriptRunner.Execute(ScriptText, response);

        foreach (var log in result.Logs)
        {
            _consoleLines.Add(log);
        }

        if (result.IsSuccess)
        {
            _consoleLines.Add($"[{DateTime.Now:HH:mm:ss}] Script executed successfully");
            
            // Show set variables
            var env = ScriptEnvironment.Global.GetAll();
            if (env.Any())
            {
                _consoleLines.Add("Environment variables:");
                foreach (var kvp in env)
                {
                    _consoleLines.Add($"  {kvp.Key} = {kvp.Value}");
                }
            }
        }
        else
        {
            _consoleLines.Add($"[{DateTime.Now:HH:mm:ss}] Error: {result.ErrorMessage}");
            if (result.ErrorLine > 0)
            {
                _consoleLines.Add($"  at line {result.ErrorLine}");
            }
        }

        ScriptExecuted?.Invoke(this, result);
        return result;
    }

    private void ClearScript_Click(object sender, RoutedEventArgs e)
    {
        CodeEditor.Text = string.Empty;
        ScriptText = string.Empty;
    }

    private void ClearConsole_Click(object sender, RoutedEventArgs e)
    {
        _consoleLines.Clear();
    }

    private void InsertSnippet(string snippet)
    {
        var selectionStart = CodeEditor.SelectionStart;
        var currentText = CodeEditor.Text ?? string.Empty;

        if (selectionStart > 0 && currentText.Length > 0 && currentText[selectionStart - 1] != '\n')
        {
            snippet = "\n" + snippet;
        }

        CodeEditor.Text = currentText.Insert(selectionStart, snippet);
        CodeEditor.SelectionStart = selectionStart + snippet.Length;
        CodeEditor.Focus(FocusState.Programmatic);
    }

    private void InsertSnippet_SetEnv(object sender, RoutedEventArgs e)
    {
        InsertSnippet(
@"// Set an environment variable
pw.env.set(""variable"", ""value"");");
    }

    private void InsertSnippet_StatusCode200(object sender, RoutedEventArgs e)
    {
        InsertSnippet(
@"// Check status code is 200
pw.test(""Status code is 200"", () => {
    pw.expect(pw.response.status).toBe(200);
});");
    }

    private void InsertSnippet_AssertBody(object sender, RoutedEventArgs e)
    {
        InsertSnippet(
@"// Check JSON response property
pw.test(""Check JSON response property"", () => {
    pw.expect(pw.response.body.message).toBeTruthy();
});");
    }

    private void InsertSnippet_Status2xx(object sender, RoutedEventArgs e)
    {
        InsertSnippet(
@"// Check status code is 2xx
pw.test(""Status code is 2xx"", () => {
    pw.expect(pw.response.status).toBeInRange(200, 299);
});");
    }

    private void InsertSnippet_Status3xx(object sender, RoutedEventArgs e)
    {
        InsertSnippet(
@"// Check status code is 3xx
pw.test(""Status code is 3xx"", () => {
    pw.expect(pw.response.status).toBeInRange(300, 399);
});");
    }

    private void InsertSnippet_Status4xx(object sender, RoutedEventArgs e)
    {
        InsertSnippet(
@"// Check status code is 4xx
pw.test(""Status code is 4xx"", () => {
    pw.expect(pw.response.status).toBeInRange(400, 499);
});");
    }

    private void InsertSnippet_Status5xx(object sender, RoutedEventArgs e)
    {
        InsertSnippet(
@"// Check status code is 5xx
pw.test(""Status code is 5xx"", () => {
    pw.expect(pw.response.status).toBeInRange(500, 599);
});");
    }

    public void Clear()
    {
        CodeEditor.Text = string.Empty;
        _consoleLines.Clear();
    }
}
