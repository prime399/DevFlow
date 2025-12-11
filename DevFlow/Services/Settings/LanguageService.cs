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

    // Fallback languages matching appsettings.json LocalizationConfiguration
    private static readonly LanguageOption[] FallbackLanguages =
    [
        new("en", "English", "English"),
        new("es", "Espanol", "Spanish"),
        new("fr", "Francais", "French"),
        new("pt-BR", "Portugues (Brasil)", "Portuguese (Brazil)")
    ];

    public LanguageService(ILocalizationService localizationService, ISettingsService settingsService)
    {
        _localizationService = localizationService;
        _settingsService = settingsService;

        // Build supported languages from the localization service's cultures
        var cultures = _localizationService.SupportedCultures?.ToList() ?? [];
        
        if (cultures.Count > 0)
        {
            _supportedLanguages = cultures
                .Select(culture => new LanguageOption(
                    Code: culture.Name,
                    NativeName: string.IsNullOrEmpty(culture.NativeName) ? culture.Name : culture.NativeName,
                    EnglishName: string.IsNullOrEmpty(culture.EnglishName) ? culture.Name : culture.EnglishName
                ))
                .ToList();
                
            System.Diagnostics.Debug.WriteLine($"LanguageService: Loaded {_supportedLanguages.Count} languages from ILocalizationService");
        }
        else
        {
            // Use fallback languages when localization service doesn't provide any
            _supportedLanguages = FallbackLanguages.ToList();
            System.Diagnostics.Debug.WriteLine($"LanguageService: Using {_supportedLanguages.Count} fallback languages");
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
        System.Diagnostics.Debug.WriteLine($"LanguageService: ChangeLanguageAsync called for '{languageCode}'");
        
        // Save the preference first (always save, even if culture change fails)
        await _settingsService.SetLanguageAsync(languageCode);
        System.Diagnostics.Debug.WriteLine($"LanguageService: Saved language preference '{languageCode}'");

        // Find the culture in supported cultures
        var supportedCultures = _localizationService.SupportedCultures?.ToList() ?? [];
        var targetCulture = supportedCultures
            .FirstOrDefault(c => c.Name.Equals(languageCode, StringComparison.OrdinalIgnoreCase));

        if (targetCulture == null)
        {
            // Try to create culture from code if not in supported list
            try
            {
                targetCulture = new CultureInfo(languageCode);
                System.Diagnostics.Debug.WriteLine($"LanguageService: Created CultureInfo for '{languageCode}'");
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"LanguageService: Could not create CultureInfo for '{languageCode}'");
                return false;
            }
        }

        // Check if it's already the current culture
        if (_localizationService.CurrentCulture.Name.Equals(targetCulture.Name, StringComparison.OrdinalIgnoreCase))
        {
            System.Diagnostics.Debug.WriteLine($"LanguageService: Already using '{languageCode}'");
            return false;
        }

        try
        {
            // Use ILocalizationService to change the culture (this triggers restart)
            await _localizationService.SetCurrentCultureAsync(targetCulture);
            System.Diagnostics.Debug.WriteLine($"LanguageService: Culture changed to '{targetCulture.Name}'");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LanguageService: Failed to change culture - {ex.Message}");
            return false;
        }
    }
}
