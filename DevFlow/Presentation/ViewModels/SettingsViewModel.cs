using DevFlow.Services.Settings;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DevFlow.Presentation.ViewModels;

/// <summary>
/// ViewModel for the Settings page.
/// Manages language selection, theme preferences, and other app settings.
/// </summary>
public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ILanguageService _languageService;
    private readonly ISettingsService _settingsService;
    
    private LanguageOption? _selectedLanguage;
    private string _selectedTheme = "Dark";
    private bool _hasUnsavedChanges;
    private bool _isSaving;
    private string _statusMessage = string.Empty;
    private bool _requiresRestart;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsViewModel(ILanguageService languageService, ISettingsService settingsService)
    {
        _languageService = languageService;
        _settingsService = settingsService;
        
        // Initialize with current values
        _selectedLanguage = _languageService.GetLanguage(_languageService.CurrentLanguageCode) 
            ?? _languageService.SupportedLanguages.FirstOrDefault();
        
        // Load saved theme preference
        _ = LoadSettingsAsync();
    }

    /// <summary>
    /// List of available languages.
    /// </summary>
    public IReadOnlyList<LanguageOption> AvailableLanguages => _languageService.SupportedLanguages;

    /// <summary>
    /// Gets the language service for direct access.
    /// </summary>
    public ILanguageService LanguageService => _languageService;

    /// <summary>
    /// Currently selected language.
    /// </summary>
    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (_selectedLanguage != value)
            {
                _selectedLanguage = value;
                OnPropertyChanged();
                _ = OnLanguageChangedAsync();
            }
        }
    }

    /// <summary>
    /// Theme options available.
    /// </summary>
    public IReadOnlyList<string> ThemeOptions { get; } = new[] { "System", "Light", "Dark" };

    /// <summary>
    /// Currently selected theme.
    /// </summary>
    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme != value)
            {
                _selectedTheme = value;
                OnPropertyChanged();
                HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Indicates if there are unsaved changes.
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set
        {
            if (_hasUnsavedChanges != value)
            {
                _hasUnsavedChanges = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Indicates if save operation is in progress.
    /// </summary>
    public bool IsSaving
    {
        get => _isSaving;
        set
        {
            if (_isSaving != value)
            {
                _isSaving = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Status message to display to user.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Indicates if language change requires restart.
    /// </summary>
    public bool RequiresRestart
    {
        get => _requiresRestart;
        set
        {
            if (_requiresRestart != value)
            {
                _requiresRestart = value;
                OnPropertyChanged();
            }
        }
    }

    private async Task LoadSettingsAsync()
    {
        var savedTheme = await _settingsService.GetThemeAsync();
        if (!string.IsNullOrEmpty(savedTheme))
        {
            _selectedTheme = savedTheme;
            OnPropertyChanged(nameof(SelectedTheme));
        }
    }

    /// <summary>
    /// Called when language selection changes.
    /// </summary>
    private async Task OnLanguageChangedAsync()
    {
        if (_selectedLanguage != null && _selectedLanguage.Code != _languageService.CurrentLanguageCode)
        {
            HasUnsavedChanges = true;
            RequiresRestart = true;
        }
        else
        {
            RequiresRestart = false;
        }
    }

    /// <summary>
    /// Saves all settings and restarts app if language changed.
    /// </summary>
    public async Task SaveSettingsAsync()
    {
        IsSaving = true;
        StatusMessage = "Saving settings...";

        try
        {
            // Save theme
            if (!string.IsNullOrEmpty(_selectedTheme))
            {
                await _settingsService.SetThemeAsync(_selectedTheme);
            }

            // Check if language changed
            if (_selectedLanguage != null && _selectedLanguage.Code != _languageService.CurrentLanguageCode)
            {
                StatusMessage = "Changing language... App will restart.";
                
                // This will trigger app restart via ILocalizationService
                await _languageService.ChangeLanguageAsync(_selectedLanguage.Code);
            }
            else
            {
                StatusMessage = "Settings saved successfully!";
                HasUnsavedChanges = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving settings: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Resets settings to their original values.
    /// </summary>
    public async Task ResetSettingsAsync()
    {
        _selectedLanguage = _languageService.GetLanguage(_languageService.CurrentLanguageCode);
        OnPropertyChanged(nameof(SelectedLanguage));
        
        await LoadSettingsAsync();
        
        HasUnsavedChanges = false;
        RequiresRestart = false;
        StatusMessage = "Settings reset to saved values.";
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
