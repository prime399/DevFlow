using Windows.Storage;

namespace DevFlow.Services.Settings;

/// <summary>
/// Implementation of ISettingsService using Windows.Storage.ApplicationData.
/// Persists settings to local storage that survives app restarts.
/// </summary>
public class SettingsService : ISettingsService
{
    private const string LanguageKey = "AppLanguage";
    private const string ThemeKey = "AppTheme";

    private readonly ApplicationDataContainer _localSettings;

    public SettingsService()
    {
        _localSettings = ApplicationData.Current.LocalSettings;
    }

    public Task<string?> GetLanguageAsync()
    {
        return Task.FromResult(GetValue<string>(LanguageKey));
    }

    public Task SetLanguageAsync(string languageCode)
    {
        SetValue(LanguageKey, languageCode);
        return Task.CompletedTask;
    }

    public Task<string?> GetThemeAsync()
    {
        return Task.FromResult(GetValue<string>(ThemeKey));
    }

    public Task SetThemeAsync(string theme)
    {
        SetValue(ThemeKey, theme);
        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        return Task.FromResult(GetValue<T>(key));
    }

    public Task SetAsync<T>(string key, T value)
    {
        SetValue(key, value);
        return Task.CompletedTask;
    }

    private T? GetValue<T>(string key)
    {
        if (_localSettings.Values.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
            {
                return typedValue;
            }
        }
        return default;
    }

    private void SetValue<T>(string key, T value)
    {
        if (value is null)
        {
            _localSettings.Values.Remove(key);
        }
        else
        {
            _localSettings.Values[key] = value;
        }
    }
}
