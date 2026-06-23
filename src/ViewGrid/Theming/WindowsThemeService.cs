using Microsoft.Win32;
namespace ViewGrid.Theming;
public static class WindowsThemeService
{
    public static event EventHandler? ThemeChanged;
    static WindowsThemeService()
    {
        SystemEvents.UserPreferenceChanged += (_,__) => ThemeChanged?.Invoke(null, EventArgs.Empty);
        SystemEvents.DisplaySettingsChanged += (_,__) => ThemeChanged?.Invoke(null, EventArgs.Empty);
    }
    public static bool IsDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 0;
        }
        catch { return false; }
    }
    public static ViewGridTheme CurrentTheme() => IsDarkMode() ? ViewGridTheme.DarkTheme() : ViewGridTheme.LightTheme();
}
