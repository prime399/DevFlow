using System.Collections.ObjectModel;
using DevFlow.Helpers;
using DevFlow.Services.Scripting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevFlow.Controls;

public sealed partial class ScriptEditorControl : UserControl
{
    public static readonly DependencyProperty ScriptTextProperty =
        DependencyProperty.Register(nameof(ScriptText), typeof(string), typeof(ScriptEditorControl),
            new PropertyMetadata(string.Empty, OnScriptTextChanged));

    private readonly ObservableCollection<string> _consoleLines = new();
    private readonly PreRequestScriptRunner _scriptRunner;
    private bool _isUpdating;

    public ScriptEditorControl()
    {
        this.InitializeComponent();
        _scriptRunner = new PreRequestScriptRunner();
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
        if (d is ScriptEditorControl control && !control._isUpdating)
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
        ExecuteScript();
    }

    public ScriptExecutionResult ExecuteScript()
    {
        _consoleLines.Clear();
        _consoleLines.Add($"[{DateTime.Now:HH:mm:ss}] Running script...");

        var result = _scriptRunner.Execute(ScriptText);

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

        // Add newlines if needed
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

    private void InsertSnippet_Timestamp(object sender, RoutedEventArgs e)
    {
        InsertSnippet(
@"// Set timestamp variable
const currentTime = Date.now();
pw.env.set(""timestamp"", currentTime.toString());");
    }

    private void InsertSnippet_Random(object sender, RoutedEventArgs e)
    {
        InsertSnippet(
@"// Set random number variable
const min = 1;
const max = 1000;
const randomArbitrary = Math.random() * (max - min) + min;
pw.env.set(""randomNumber"", randomArbitrary.toString());");
    }

    private void InsertSnippet_GetEnv(object sender, RoutedEventArgs e)
    {
        InsertSnippet(
@"// Get environment variable
const value = pw.env.get(""variable"");
console.log(""Value:"", value);");
    }

    private void InsertSnippet_Test(object sender, RoutedEventArgs e)
    {
        InsertSnippet(
@"// Write a test case
pw.test(""Test description"", () => {
    const value = pw.env.get(""variable"");
    pw.expect(value).toBeTruthy();
});");
    }

    private void InsertSnippet_Expect(object sender, RoutedEventArgs e)
    {
        InsertSnippet(
@"// Assertion examples
pw.expect(value).toBe(expected);
pw.expect(value).not.toBeNull();
pw.expect(value).toContain(""text"");
pw.expect(number).toBeGreaterThan(0);");
    }

    public void Clear()
    {
        CodeEditor.Text = string.Empty;
        _consoleLines.Clear();
    }
}
