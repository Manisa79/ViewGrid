using System;
using System.Drawing;

namespace ViewGrid.Theming;

/// <summary>
/// v33 Theme Accessibility Engine.
/// ViewGrid içindeki koyu/açık tema geçişlerinde düşük kontrastlı yazı, buton,
/// combo/textbox ve kart yüzeylerini merkezi olarak normalize eder.
/// </summary>
public static class ViewGridThemeAccessibility
{
    public const double NormalTextRatio = 4.5d;
    public const double LargeTextRatio = 3.0d;
    public const double SurfaceRatio = 1.18d;

    public static ViewGridTheme Normalize(ViewGridTheme? source)
    {
        var t = source ?? ViewGridTheme.LightTheme();

        if (t.BackColor == Color.Empty || t.BackColor == Color.Transparent)
            t.BackColor = t.IsDark ? Color.FromArgb(18, 27, 40) : Color.White;

        t.IsDark = GetLuminance(t.BackColor) < 0.48d;

        if (t.PanelBackColor == Color.Empty || t.PanelBackColor == Color.Transparent)
            t.PanelBackColor = t.IsDark ? Blend(t.BackColor, Color.White, 0.04d) : Blend(t.BackColor, Color.Black, 0.02d);

        if (t.ControlBackColor == Color.Empty || t.ControlBackColor == Color.Transparent)
            t.ControlBackColor = t.IsDark ? Blend(t.PanelBackColor, Color.White, 0.07d) : Color.White;

        t.PanelBackColor = EnsureSurfaceContrast(t.PanelBackColor, t.BackColor, t.IsDark);
        t.ControlBackColor = EnsureSurfaceContrast(t.ControlBackColor, t.PanelBackColor, t.IsDark);
        t.HeaderBackColor = EnsureSurfaceContrast(t.HeaderBackColor == Color.Empty ? t.ControlBackColor : t.HeaderBackColor, t.BackColor, t.IsDark);
        t.AlternateBackColor = EnsureSurfaceContrast(t.AlternateBackColor == Color.Empty ? t.PanelBackColor : t.AlternateBackColor, t.BackColor, t.IsDark);
        t.HotBackColor = EnsureSurfaceContrast(t.HotBackColor == Color.Empty ? t.ControlBackColor : t.HotBackColor, t.BackColor, t.IsDark);

        t.ForeColor = EnsureReadableTextColor(t.ForeColor == Color.Empty ? PreferredText(t.BackColor) : t.ForeColor, t.BackColor, NormalTextRatio);
        t.HeaderForeColor = EnsureReadableTextColor(t.HeaderForeColor == Color.Empty ? t.ForeColor : t.HeaderForeColor, t.HeaderBackColor, NormalTextRatio);
        t.SelectionForeColor = EnsureReadableTextColor(t.SelectionForeColor == Color.Empty ? Color.White : t.SelectionForeColor, t.SelectionBackColor, NormalTextRatio);
        t.MutedForeColor = EnsureReadableTextColor(t.MutedForeColor == Color.Empty ? MutedText(t.BackColor, t.ForeColor) : t.MutedForeColor, t.PanelBackColor, 3.8d);
        t.EmptyTextColor = EnsureReadableTextColor(t.EmptyTextColor == Color.Empty ? t.MutedForeColor : t.EmptyTextColor, t.PanelBackColor, 3.8d);
        t.BorderColor = EnsureStrokeColor(t.BorderColor == Color.Empty ? t.MutedForeColor : t.BorderColor, t.BackColor, 1.7d);
        t.GridColor = EnsureStrokeColor(t.GridColor == Color.Empty ? t.BorderColor : t.GridColor, t.BackColor, 1.45d);

        if (t.AccentColor == Color.Empty || t.AccentColor == Color.Transparent)
            t.AccentColor = t.IsDark ? Color.FromArgb(70, 174, 255) : Color.FromArgb(0, 120, 215);
        t.AccentColor = EnsureAccentColor(t.AccentColor, t.BackColor);

        if (t.SelectionBackColor == Color.Empty || t.SelectionBackColor == Color.Transparent)
            t.SelectionBackColor = t.AccentColor;
        t.SelectionBackColor = EnsureSurfaceContrast(t.SelectionBackColor, t.BackColor, t.IsDark);
        t.SelectionForeColor = EnsureReadableTextColor(t.SelectionForeColor, t.SelectionBackColor, NormalTextRatio);

        return t;
    }

    public static Color EnsureReadableTextColor(Color preferred, Color back, double minRatio = NormalTextRatio)
    {
        if (back == Color.Empty || back == Color.Transparent)
            return preferred == Color.Empty ? Color.Black : preferred;
        if (preferred == Color.Empty || preferred == Color.Transparent)
            preferred = PreferredText(back);
        if (ContrastRatio(back, preferred) >= minRatio)
            return preferred;

        Color white = Color.White;
        Color black = Color.Black;
        double cw = ContrastRatio(back, white);
        double cb = ContrastRatio(back, black);
        Color best = cw >= cb ? white : black;
        if (Math.Max(cw, cb) >= minRatio)
            return best;

        return best;
    }

    public static Color EnsureStrokeColor(Color preferred, Color back, double minRatio)
    {
        if (ContrastRatio(back, preferred) >= minRatio)
            return preferred;
        return GetLuminance(back) < 0.5d ? Blend(back, Color.White, 0.42d) : Blend(back, Color.Black, 0.34d);
    }

    public static Color EnsureAccentColor(Color accent, Color back)
    {
        if (ContrastRatio(back, accent) >= 3.0d)
            return accent;
        return GetLuminance(back) < 0.5d ? Color.FromArgb(80, 190, 255) : Color.FromArgb(0, 86, 160);
    }

    public static Color EnsureSurfaceContrast(Color surface, Color back, bool dark)
    {
        if (surface == Color.Empty || surface == Color.Transparent)
            surface = dark ? Blend(back, Color.White, 0.05d) : Blend(back, Color.Black, 0.025d);
        if (ContrastRatio(back, surface) >= SurfaceRatio)
            return surface;
        return dark ? Blend(back, Color.White, 0.09d) : Blend(back, Color.Black, 0.045d);
    }

    public static Color ButtonBack(ViewGridTheme theme, bool primary = false)
    {
        theme = Normalize(theme);
        if (primary) return theme.AccentColor;
        return theme.IsDark ? Blend(theme.ControlBackColor, theme.AccentColor, 0.12d) : Blend(theme.ControlBackColor, theme.AccentColor, 0.06d);
    }

    public static Color ButtonBorder(ViewGridTheme theme, bool primary = false)
    {
        theme = Normalize(theme);
        return primary ? EnsureStrokeColor(Blend(theme.AccentColor, theme.BackColor, 0.25d), theme.AccentColor, 1.35d)
                       : EnsureStrokeColor(Blend(theme.BorderColor, theme.AccentColor, 0.18d), theme.ControlBackColor, 1.45d);
    }

    public static Color MutedText(Color back, Color fore)
    {
        var mixed = Blend(fore == Color.Empty ? PreferredText(back) : fore, back, 0.32d);
        return EnsureReadableTextColor(mixed, back, 3.8d);
    }

    public static Color PreferredText(Color back) => GetLuminance(back) < 0.5d ? Color.White : Color.FromArgb(18, 24, 32);

    public static double ContrastRatio(Color a, Color b)
    {
        double la = GetLuminance(a);
        double lb = GetLuminance(b);
        double lighter = Math.Max(la, lb);
        double darker = Math.Min(la, lb);
        return (lighter + 0.05d) / (darker + 0.05d);
    }

    public static double GetLuminance(Color c)
    {
        static double Channel(double value)
        {
            value /= 255d;
            return value <= 0.03928d ? value / 12.92d : Math.Pow((value + 0.055d) / 1.055d, 2.4d);
        }
        return 0.2126d * Channel(c.R) + 0.7152d * Channel(c.G) + 0.0722d * Channel(c.B);
    }

    public static Color Blend(Color first, Color second, double amount)
    {
        amount = Math.Max(0d, Math.Min(1d, amount));
        int r = (int)Math.Round(first.R + (second.R - first.R) * amount);
        int g = (int)Math.Round(first.G + (second.G - first.G) * amount);
        int b = (int)Math.Round(first.B + (second.B - first.B) * amount);
        return Color.FromArgb(r, g, b);
    }
}
