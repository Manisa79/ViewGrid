using System.Globalization;

namespace Taylan.Pano.Localization;

/// <summary>
/// Application-wide localization facade for Pano. Set this once at application startup
/// and every PanoControl / built-in dialog / built-in menu will use the same language.
/// </summary>
public static class PanoLocalization
{
    public static event EventHandler? LanguageChanged;

    public static PanoLanguage Language
    {
        get => PanoText.Language;
        set
        {
            if (PanoText.Language == value) return;
            PanoText.Language = value;
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static PanoLanguage EffectiveLanguage => PanoText.EffectiveLanguage;

    public static IReadOnlyList<PanoLanguage> SupportedLanguages { get; } = new[]
    {
        PanoLanguage.Auto,
        PanoLanguage.Turkish,
        PanoLanguage.English,
        PanoLanguage.German,
        PanoLanguage.French,
        PanoLanguage.Spanish,
        PanoLanguage.Italian,
        PanoLanguage.Russian,
        PanoLanguage.Arabic,
        PanoLanguage.Chinese,
        PanoLanguage.Japanese
    };

    public static void Use(PanoLanguage language) => Language = language;

    public static string T(string key) => PanoText.T(key);

    public static string DisplayName(PanoLanguage language)
    {
        return language switch
        {
            PanoLanguage.Auto => "Auto / System",
            PanoLanguage.Turkish => "Türkçe",
            PanoLanguage.English => "English",
            PanoLanguage.German => "Deutsch",
            PanoLanguage.French => "Français",
            PanoLanguage.Spanish => "Español",
            PanoLanguage.Italian => "Italiano",
            PanoLanguage.Russian => "Русский",
            PanoLanguage.Arabic => "العربية",
            PanoLanguage.Chinese => "中文",
            PanoLanguage.Japanese => "日本語",
            _ => language.ToString()
        };
    }

    public static PanoLanguage FromName(string? value, PanoLanguage fallback = PanoLanguage.Auto)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        if (Enum.TryParse<PanoLanguage>(value, ignoreCase: true, out var parsed)) return parsed;

        string normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "tr" or "turkish" or "türkçe" => PanoLanguage.Turkish,
            "en" or "english" => PanoLanguage.English,
            "de" or "german" or "deutsch" => PanoLanguage.German,
            "fr" or "french" or "français" => PanoLanguage.French,
            "es" or "spanish" or "español" => PanoLanguage.Spanish,
            "it" or "italian" or "italiano" => PanoLanguage.Italian,
            "ru" or "russian" => PanoLanguage.Russian,
            "ar" or "arabic" => PanoLanguage.Arabic,
            "zh" or "chinese" => PanoLanguage.Chinese,
            "ja" or "japanese" => PanoLanguage.Japanese,
            _ => fallback
        };
    }

    public static void UseCurrentUICulture()
    {
        Language = PanoLanguage.Auto;
        _ = CultureInfo.CurrentUICulture;
    }
}
