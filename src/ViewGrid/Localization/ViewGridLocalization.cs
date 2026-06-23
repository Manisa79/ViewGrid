using System.Globalization;

namespace ViewGrid.Localization;

/// <summary>
/// Application-wide localization facade for ViewGrid. Set this once at application startup
/// and every ViewGridControl / built-in dialog / built-in menu will use the same language.
/// </summary>
public static class ViewGridLocalization
{
    public static event EventHandler? LanguageChanged;

    public static ViewGridLanguage Language
    {
        get => ViewGridText.Language;
        set
        {
            if (ViewGridText.Language == value) return;
            ViewGridText.Language = value;
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static ViewGridLanguage EffectiveLanguage => ViewGridText.EffectiveLanguage;

    public static IReadOnlyList<ViewGridLanguage> SupportedLanguages { get; } = new[]
    {
        ViewGridLanguage.Auto,
        ViewGridLanguage.Turkish,
        ViewGridLanguage.English,
        ViewGridLanguage.German,
        ViewGridLanguage.French,
        ViewGridLanguage.Spanish,
        ViewGridLanguage.Italian,
        ViewGridLanguage.Russian,
        ViewGridLanguage.Arabic,
        ViewGridLanguage.Chinese,
        ViewGridLanguage.Japanese
    };

    public static void Use(ViewGridLanguage language) => Language = language;

    public static string T(string key) => ViewGridText.T(key);

    public static string DisplayName(ViewGridLanguage language)
    {
        return language switch
        {
            ViewGridLanguage.Auto => "Auto / System",
            ViewGridLanguage.Turkish => "Türkçe",
            ViewGridLanguage.English => "English",
            ViewGridLanguage.German => "Deutsch",
            ViewGridLanguage.French => "Français",
            ViewGridLanguage.Spanish => "Español",
            ViewGridLanguage.Italian => "Italiano",
            ViewGridLanguage.Russian => "Русский",
            ViewGridLanguage.Arabic => "العربية",
            ViewGridLanguage.Chinese => "中文",
            ViewGridLanguage.Japanese => "日本語",
            _ => language.ToString()
        };
    }

    public static ViewGridLanguage FromName(string? value, ViewGridLanguage fallback = ViewGridLanguage.Auto)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        if (Enum.TryParse<ViewGridLanguage>(value, ignoreCase: true, out var parsed)) return parsed;

        string normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "tr" or "turkish" or "türkçe" => ViewGridLanguage.Turkish,
            "en" or "english" => ViewGridLanguage.English,
            "de" or "german" or "deutsch" => ViewGridLanguage.German,
            "fr" or "french" or "français" => ViewGridLanguage.French,
            "es" or "spanish" or "español" => ViewGridLanguage.Spanish,
            "it" or "italian" or "italiano" => ViewGridLanguage.Italian,
            "ru" or "russian" => ViewGridLanguage.Russian,
            "ar" or "arabic" => ViewGridLanguage.Arabic,
            "zh" or "chinese" => ViewGridLanguage.Chinese,
            "ja" or "japanese" => ViewGridLanguage.Japanese,
            _ => fallback
        };
    }

    public static void UseCurrentUICulture()
    {
        Language = ViewGridLanguage.Auto;
        _ = CultureInfo.CurrentUICulture;
    }
}
