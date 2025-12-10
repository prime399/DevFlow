using System.Collections.ObjectModel;
using DevFlow.Models;
using DevFlow.Services.Scripting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DevFlow.Controls;

public sealed partial class TestResultsControl : UserControl
{
    private readonly ObservableCollection<TestResult> _results = new();

    public TestResultsControl()
    {
        this.InitializeComponent();
        ResultsListControl.ItemsSource = _results;
    }

    public void UpdateResults(ScriptExecutionResult? result)
    {
        _results.Clear();

        if (result == null || !result.HasTests)
        {
            ShowEmptyState();
            return;
        }

        foreach (var test in result.TestResults)
        {
            _results.Add(test);
        }

        ShowResults(result);
    }

    public void Clear()
    {
        _results.Clear();
        ShowEmptyState();
    }

    private void ShowEmptyState()
    {
        EmptyState.Visibility = Visibility.Visible;
        ResultsListControl.Visibility = Visibility.Collapsed;
        PassedBadge.Visibility = Visibility.Collapsed;
        FailedBadge.Visibility = Visibility.Collapsed;
        SkippedBadge.Visibility = Visibility.Collapsed;
        
        TitleText.Text = "Test Results";
        SummaryText.Text = "No tests run";
        DurationText.Text = "0ms";
        
        StatusBadge.Background = (Brush)Application.Current.Resources["TextMutedBrush"];
        StatusIcon.Glyph = "\uE9D9"; // Clock
    }

    private void ShowResults(ScriptExecutionResult result)
    {
        EmptyState.Visibility = Visibility.Collapsed;
        ResultsListControl.Visibility = Visibility.Visible;

        var passed = result.PassedCount;
        var failed = result.FailedCount;
        var skipped = result.SkippedCount;
        var total = result.TestResults.Count;

        // Update summary
        TitleText.Text = result.AllPassed ? "All Tests Passed!" : "Test Results";
        SummaryText.Text = $"{total} test{(total != 1 ? "s" : "")} completed";
        DurationText.Text = $"{result.TotalDuration}ms";

        // Update status badge
        if (failed > 0)
        {
            StatusBadge.Background = (Brush)Application.Current.Resources["ErrorBrush"];
            StatusIcon.Glyph = "\uE711"; // X
        }
        else if (passed > 0)
        {
            StatusBadge.Background = (Brush)Application.Current.Resources["SuccessBrush"];
            StatusIcon.Glyph = "\uE73E"; // Checkmark
        }
        else
        {
            StatusBadge.Background = (Brush)Application.Current.Resources["WarningBrush"];
            StatusIcon.Glyph = "\uE72B"; // Forward/Skip
        }

        // Update count badges
        if (passed > 0)
        {
            PassedBadge.Visibility = Visibility.Visible;
            PassedCount.Text = $"{passed} Passed";
        }
        else
        {
            PassedBadge.Visibility = Visibility.Collapsed;
        }

        if (failed > 0)
        {
            FailedBadge.Visibility = Visibility.Visible;
            FailedCount.Text = $"{failed} Failed";
        }
        else
        {
            FailedBadge.Visibility = Visibility.Collapsed;
        }

        if (skipped > 0)
        {
            SkippedBadge.Visibility = Visibility.Visible;
            SkippedCount.Text = $"{skipped} Skipped";
        }
        else
        {
            SkippedBadge.Visibility = Visibility.Collapsed;
        }
    }
}
