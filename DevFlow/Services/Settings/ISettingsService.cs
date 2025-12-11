namespace DevFlow.Services.Settings;

/// <summary>
/// Interface for persisting and retrieving application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the currently saved language code (e.g., "en", "es", "fr", "pt-BR").
    /// </summary>
    Task<string?> GetLanguageAsync();

    /// <summary>
    /// Sets the language preference. This will be applied on next app restart.
    /// </summary>
    /// <param name="languageCode">The language code to save.</param>
    Task SetLanguageAsync(string languageCode);

    /// <summary>
    /// Gets the saved theme preference ("System", "Light", "Dark").
    /// </summary>
    Task<string?> GetThemeAsync();

    /// <summary>
    /// Sets the theme preference.
    /// </summary>
    /// <param name="theme">The theme to save.</param>
    Task SetThemeAsync(string theme);

    /// <summary>
    /// Gets a generic setting value by key.
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Sets a generic setting value by key.
    /// </summary>
    Task SetAsync<T>(string key, T value);
}
