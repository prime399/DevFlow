using DevFlow.Presentation.ViewModels;
using DevFlow.Services.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevFlow.Presentation.Controls;

public sealed partial class SettingsView : UserControl
{
    private SettingsViewModel? _viewModel;
    private string _originalLanguageCode = string.Empty;
    private string _selectedLanguageCode = string.Empty;
    private string _selectedTheme = "Dark";
    private bool _hasUnsavedChanges;

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(SettingsViewModel), typeof(SettingsView),
            new PropertyMetadata(null, OnViewModelChanged));

    public SettingsView()
    {
        this.InitializeComponent();
        Loaded += SettingsView_Loaded;
    }

    private void SettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        // Re-bind if ViewModel was set before control loaded
        if (_viewModel != null)
        {
            BindViewModel(_viewModel);
        }
        else
        {
            // Show placeholder if no ViewModel
            CurrentLanguageText.Text = "Language settings not available";
            System.Diagnostics.Debug.WriteLine("SettingsView: No ViewModel available on Loaded");
        }
    }

    public SettingsViewModel? ViewModel
    {
        get => (SettingsViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SettingsView view && e.NewValue is SettingsViewModel vm)
        {
            view._viewModel = vm;
            view.BindViewModel(vm);
        }
    }

    private void BindViewModel(SettingsViewModel vm)
    {
        System.Diagnostics.Debug.WriteLine($"SettingsView: BindViewModel called with {vm.AvailableLanguages.Count} languages");
        
        // Populate languages
        LanguageComboBox.ItemsSource = vm.AvailableLanguages;
        
        if (vm.AvailableLanguages.Count == 0)
        {
            CurrentLanguageText.Text = "No languages available";
            System.Diagnostics.Debug.WriteLine("SettingsView: No languages available in ViewModel");
            return;
        }
        
        // Set current language selection
        var currentLangCode = vm.LanguageService.CurrentLanguageCode;
        System.Diagnostics.Debug.WriteLine($"SettingsView: Current language code is '{currentLangCode}'");
        
        bool found = false;
        foreach (var lang in vm.AvailableLanguages)
        {
            if (lang.Code.Equals(currentLangCode, StringComparison.OrdinalIgnoreCase))
            {
                LanguageComboBox.SelectedItem = lang;
                _originalLanguageCode = lang.Code;
                _selectedLanguageCode = lang.Code;
                UpdateCurrentLanguageDisplay(lang);
                found = true;
                System.Diagnostics.Debug.WriteLine($"SettingsView: Selected language '{lang.NativeName}' ({lang.Code})");
                break;
            }
        }

        // If nothing selected, select first
        if (!found && vm.AvailableLanguages.Count > 0)
        {
            LanguageComboBox.SelectedIndex = 0;
            if (vm.AvailableLanguages[0] is LanguageOption firstLang)
            {
                _originalLanguageCode = firstLang.Code;
                _selectedLanguageCode = firstLang.Code;
                UpdateCurrentLanguageDisplay(firstLang);
                System.Diagnostics.Debug.WriteLine($"SettingsView: Defaulted to first language '{firstLang.NativeName}' ({firstLang.Code})");
            }
        }
    }

    private void UpdateCurrentLanguageDisplay(LanguageOption lang)
    {
        CurrentLanguageText.Text = $"Current: {lang.NativeName} ({lang.Code})";
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is LanguageOption selectedLang)
        {
            _selectedLanguageCode = selectedLang.Code;
            
            // Show restart notice if language changed from original
            var needsRestart = !_selectedLanguageCode.Equals(_originalLanguageCode, StringComparison.OrdinalIgnoreCase);
            RestartNotice.Visibility = needsRestart ? Visibility.Visible : Visibility.Collapsed;
            
            if (_viewModel != null)
            {
                _viewModel.SelectedLanguage = selectedLang;
            }
            
            UpdateUnsavedState();
        }
    }

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string theme)
        {
            _selectedTheme = theme;
            ThemeDescription.Text = theme;
            
            if (_viewModel != null)
            {
                _viewModel.SelectedTheme = theme;
            }
            
            UpdateUnsavedState();
        }
    }

    private void UpdateUnsavedState()
    {
        _hasUnsavedChanges = !_selectedLanguageCode.Equals(_originalLanguageCode, StringComparison.OrdinalIgnoreCase);
        // For now, just track language changes as we're focused on that feature
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        SaveButton.IsEnabled = false;
        StatusText.Text = "Saving settings...";
        StatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextMutedBrush"] 
            ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);

        try
        {
            // Check if language changed
            if (!_selectedLanguageCode.Equals(_originalLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                StatusText.Text = "Changing language... App will restart.";
                
                // This will trigger app restart
                await _viewModel.SaveSettingsAsync();
            }
            else
            {
                // Just save other settings
                await _viewModel.SaveSettingsAsync();
                
                StatusText.Text = "Settings saved successfully!";
                StatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SuccessBrush"]
                    ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                
                _hasUnsavedChanges = false;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
            StatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ErrorBrush"]
                ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private async void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        // Reset to original values
        foreach (var lang in _viewModel.AvailableLanguages)
        {
            if (lang.Code.Equals(_originalLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                LanguageComboBox.SelectedItem = lang;
                break;
            }
        }

        _selectedLanguageCode = _originalLanguageCode;
        RestartNotice.Visibility = Visibility.Collapsed;
        _hasUnsavedChanges = false;
        
        StatusText.Text = "Settings reset to saved values.";
        StatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextMutedBrush"]
            ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);

        await _viewModel.ResetSettingsAsync();
    }
}
