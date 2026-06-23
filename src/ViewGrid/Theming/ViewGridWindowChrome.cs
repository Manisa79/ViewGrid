using System.Runtime.InteropServices;

namespace ViewGrid.Theming;

/// <summary>
/// Applies native Windows title-bar theming for WinForms windows.
/// It is safe on older Windows versions; unsupported DWM attributes are ignored.
/// </summary>
public static class ViewGridWindowChrome
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    private const int DWMWCP_DEFAULT = 0;
    private const int DWMSBT_AUTO = 0;
    private const int DWMSBT_MAINWINDOW = 2;       // Mica on Windows 11
    private const int DWMSBT_TRANSIENTWINDOW = 3;  // Acrylic-like transient backdrop on Windows 11

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

    public static bool IsSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763);
    public static bool IsBackdropSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

    public static void Apply(Form form, ViewGridTheme theme, bool preferGlassWhenAvailable = true)
    {
        if (form is null || form.IsDisposed || !form.IsHandleCreated || !OperatingSystem.IsWindows()) return;

        try
        {
            ApplyDarkMode(form.Handle, theme.IsDark);
            ApplyCaptionColors(form.Handle, theme);
            ApplyRoundedCorners(form.Handle);
            ApplyBackdrop(form.Handle, theme, preferGlassWhenAvailable);
        }
        catch
        {
            // Native chrome is a visual enhancement only. Never fail the app because of OS/DWM support.
        }
    }

    public static void ApplyOnHandleCreated(Form form, Func<ViewGridTheme> themeProvider, bool preferGlassWhenAvailable = true)
    {
        if (form is null) return;

        void ApplyNow()
        {
            try { Apply(form, themeProvider(), preferGlassWhenAvailable); }
            catch { }
        }

        if (form.IsHandleCreated) ApplyNow();
        form.HandleCreated += (_, _) => ApplyNow();
        form.Shown += (_, _) => ApplyNow();
        WindowsThemeService.ThemeChanged += (_, _) => ApplyNow();
    }

    private static void ApplyDarkMode(IntPtr hwnd, bool dark)
    {
        if (!IsSupported) return;
        int value = dark ? 1 : 0;
        _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
    }

    private static void ApplyCaptionColors(IntPtr hwnd, ViewGridTheme theme)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;

        int caption = ToColorRef(theme.IsDark ? Blend(theme.HeaderBackColor, theme.AccentColor, 0.12) : theme.HeaderBackColor);
        int text = ToColorRef(theme.HeaderForeColor);
        int border = ToColorRef(theme.BorderColor);

        _ = DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));
        _ = DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref text, sizeof(int));
        _ = DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref border, sizeof(int));
    }

    private static void ApplyRoundedCorners(IntPtr hwnd)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
        int pref = DWMWCP_DEFAULT;
        _ = DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
    }

    private static void ApplyBackdrop(IntPtr hwnd, ViewGridTheme theme, bool preferGlassWhenAvailable)
    {
        if (!preferGlassWhenAvailable || !IsBackdropSupported) return;

        int backdrop = theme.UseAcrylicEffect ? DWMSBT_TRANSIENTWINDOW : theme.UseFluentBackdrop ? DWMSBT_MAINWINDOW : DWMSBT_AUTO;
        _ = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));

        // Small top extension helps native glass/Mica visually connect with custom themed content.
        var margins = new Margins { Left = 0, Right = 0, Top = theme.UseFluentBackdrop || theme.UseAcrylicEffect ? 1 : 0, Bottom = 0 };
        _ = DwmExtendFrameIntoClientArea(hwnd, ref margins);
    }

    private static int ToColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);

    private static Color Blend(Color a, Color b, double amount)
    {
        amount = Math.Max(0, Math.Min(1, amount));
        int r = (int)(a.R + (b.R - a.R) * amount);
        int g = (int)(a.G + (b.G - a.G) * amount);
        int bl = (int)(a.B + (b.B - a.B) * amount);
        return Color.FromArgb(r, g, bl);
    }
}
