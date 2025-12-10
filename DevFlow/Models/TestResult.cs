using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DevFlow.Models;

public class TestResult : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private TestStatus _status = TestStatus.Pending;
    private string _message = string.Empty;
    private string _expected = string.Empty;
    private string _actual = string.Empty;
    private long _duration;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public TestStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusIcon)); OnPropertyChanged(nameof(StatusColor)); }
    }

    public string Message
    {
        get => _message;
        set { _message = value; OnPropertyChanged(); }
    }

    public string Expected
    {
        get => _expected;
        set { _expected = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasExpectedActual)); }
    }

    public string Actual
    {
        get => _actual;
        set { _actual = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasExpectedActual)); }
    }

    public long Duration
    {
        get => _duration;
        set { _duration = value; OnPropertyChanged(); OnPropertyChanged(nameof(DurationText)); }
    }

    public bool HasExpectedActual => !string.IsNullOrEmpty(Expected) || !string.IsNullOrEmpty(Actual);
    
    public string DurationText => $"{_duration}ms";

    public string StatusIcon => Status switch
    {
        TestStatus.Passed => "\uE73E",  // Checkmark
        TestStatus.Failed => "\uE711",  // X
        TestStatus.Skipped => "\uE72B", // Forward
        _ => "\uE916"                   // Clock
    };

    public string StatusColor => Status switch
    {
        TestStatus.Passed => "SuccessBrush",
        TestStatus.Failed => "ErrorBrush",
        TestStatus.Skipped => "WarningBrush",
        _ => "TextMutedBrush"
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public enum TestStatus
{
    Pending,
    Passed,
    Failed,
    Skipped
}

public class TestSuite : INotifyPropertyChanged
{
    private string _name = "Pre-request Script";
    private int _passed;
    private int _failed;
    private int _skipped;
    private long _totalDuration;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public ObservableCollection<TestResult> Tests { get; } = new();

    public int Passed
    {
        get => _passed;
        set { _passed = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }

    public int Failed
    {
        get => _failed;
        set { _failed = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); OnPropertyChanged(nameof(HasFailures)); }
    }

    public int Skipped
    {
        get => _skipped;
        set { _skipped = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }

    public long TotalDuration
    {
        get => _totalDuration;
        set { _totalDuration = value; OnPropertyChanged(); OnPropertyChanged(nameof(DurationText)); }
    }

    public int Total => Tests.Count;
    public bool HasFailures => Failed > 0;
    public string Summary => $"{Passed} passed, {Failed} failed, {Skipped} skipped";
    public string DurationText => $"{TotalDuration}ms";

    public void Clear()
    {
        Tests.Clear();
        Passed = 0;
        Failed = 0;
        Skipped = 0;
        TotalDuration = 0;
    }

    public void AddResult(TestResult result)
    {
        Tests.Add(result);
        switch (result.Status)
        {
            case TestStatus.Passed: Passed++; break;
            case TestStatus.Failed: Failed++; break;
            case TestStatus.Skipped: Skipped++; break;
        }
        TotalDuration += result.Duration;
        OnPropertyChanged(nameof(Total));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
