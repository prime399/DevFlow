using System.Globalization;
using Uno.Extensions.Localization;

namespace DevFlow.Services.Settings;

/// <summary>
/// Model representing a language option for display in UI.
/// </summary>
public record LanguageOption(string Code, string NativeName, string EnglishName);

/// <summary>
/// Interface for language management operations.
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Gets the list of supported languages.
    /// </summary>
    IReadOnlyList<LanguageOption> SupportedLanguages { get; }

    /// <summary>
    /// Gets the current language code.
    /// </summary>
    string CurrentLanguageCode { get; }

    /// <summary>
    /// Gets a language option by its code.
    /// </summary>
    LanguageOption? GetLanguage(string code);

    /// <summary>
    /// Changes the application language. Requires app restart to take effect.
    /// </summary>
    /// <param name="languageCode">The language code to switch to.</param>
    /// <returns>True if the language was changed and restart is needed.</returns>
    Task<bool> ChangeLanguageAsync(string languageCode);
}

/// <summary>
/// Service for managing application language using Uno.Extensions.Localization.
/// Uses ILocalizationService for runtime culture switching.
/// </summary>
public class LanguageService : ILanguageService
{
    private readonly ILocalizationService _localizationService;
    private readonly ISettingsService _settingsService;
    private readonly List<LanguageOption> _supportedLanguages;

    public LanguageService(ILocalizationService localizationService, ISettingsService settingsService)
    {
        _localizationService = localizationService;
        _settingsService = settingsService;

        // Build supported languages from the localization service's cultures
        _supportedLanguages = _localizationService.SupportedCultures
            .Select(culture => new LanguageOption(
                Code: culture.Name,
                NativeName: culture.NativeName,
                EnglishName: culture.EnglishName
            ))
            .ToList();

        // Ensure we have at least English if nothing is configured
        if (_supportedLanguages.Count == 0)
        {
            _supportedLanguages.Add(new LanguageOption("en", "English", "English"));
        }
    }

    public IReadOnlyList<LanguageOption> SupportedLanguages => _supportedLanguages;

    public string CurrentLanguageCode => _localizationService.CurrentCulture.Name;

    public LanguageOption? GetLanguage(string code)
    {
        return _supportedLanguages.FirstOrDefault(l => 
            l.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> ChangeLanguageAsync(string languageCode)
    {
        // Find the culture in supported cultures
        var targetCulture = _localizationService.SupportedCultures
            .FirstOrDefault(c => c.Name.Equals(languageCode, StringComparison.OrdinalIgnoreCase));

        if (targetCulture == null)
        {
            return false;
        }

        // Check if it's already the current culture
        if (_localizationService.CurrentCulture.Name.Equals(targetCulture.Name, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Save the preference first
        await _settingsService.SetLanguageAsync(languageCode);

        // Use ILocalizationService to change the culture (this triggers restart)
        await _localizationService.SetCurrentCultureAsync(targetCulture);

        return true;
    }
}
