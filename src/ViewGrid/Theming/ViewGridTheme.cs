using System.Drawing;
namespace ViewGrid.Theming;

public sealed class ViewGridTheme
{
    public Color BackColor { get; set; }
    public Color ForeColor { get; set; }
    public Color HeaderBackColor { get; set; }
    public Color HeaderForeColor { get; set; }

    // v28.14.2 compatibility aliases used by newer ViewGrid popup/dialog helpers.
    public Color HeaderBack { get => HeaderBackColor; set => HeaderBackColor = value; }
    public Color HeaderFore { get => HeaderForeColor; set => HeaderForeColor = value; }
    public Color GridColor { get; set; }
    public Color AlternateBackColor { get; set; }
    public Color SelectionBackColor { get; set; }
    public Color SelectionForeColor { get; set; }
    public Color HotBackColor { get; set; }
    public Color AccentColor { get; set; }

    // v25.28 compatibility/shortcut aliases.
    // Bazı yeni yardımcı sınıflar kısa adları (Accent/Surface/Text) kullanabiliyor.
    // Asıl tema alanlarını değiştirmeden bu adları güvenli şekilde eşliyoruz.
    public Color Accent { get => AccentColor; set => AccentColor = value; }
    public Color Surface { get => PanelBackColor; set => PanelBackColor = value; }
    public Color Text { get => ForeColor; set => ForeColor = value; }

    // v26.28 theme compatibility aliases.
    // Bunlar kolon API'deki eski Text property ile ilgili değil;
    // text editor / filter popup gibi yüzeylerde kullanılacak tema renkleridir.
    public Color TextBackColor
    {
        get => ControlBackColor == Color.Empty ? BackColor : ControlBackColor;
        set => ControlBackColor = value;
    }

    public Color TextForeColor
    {
        get => ForeColor;
        set => ForeColor = value;
    }

    public Color BorderColor { get; set; }
    public Color EmptyTextColor { get; set; }
    public Color MutedForeColor { get; set; }
    public bool IsDark { get; set; }
    public bool UseFluentBackdrop { get; set; }
    public bool UseAcrylicEffect { get; set; }
    public bool UseAnimatedSelection { get; set; } = true;
    public int CornerRadius { get; set; } = 8;
    public int AcrylicOpacity { get; set; } = 235;
    public Color PanelBackColor { get; set; }
    public Color ControlBackColor { get; set; }
    public Color SelectionGlowColor { get; set; }

    public static ViewGridTheme LightTheme() => new()
    {
        BackColor = Color.White,
        ForeColor = Color.FromArgb(25,25,25),
        HeaderBackColor = Color.FromArgb(246,247,250),
        HeaderForeColor = Color.FromArgb(20,20,20),
        GridColor = Color.FromArgb(230,233,238),
        AlternateBackColor = Color.FromArgb(250,251,253),
        SelectionBackColor = Color.FromArgb(0, 120, 215),
        SelectionForeColor = Color.White,
        HotBackColor = Color.FromArgb(236,244,255),
        AccentColor = Color.FromArgb(0,120,215),
        BorderColor = Color.FromArgb(210,214,220),
        EmptyTextColor = Color.FromArgb(100,100,100),
        MutedForeColor = Color.FromArgb(115, 115, 115),
        PanelBackColor = Color.FromArgb(250,251,253),
        ControlBackColor = Color.White,
        SelectionGlowColor = Color.FromArgb(60, 0, 120, 215),
        IsDark = false
    };
    public static ViewGridTheme DarkTheme() => new()
    {
        BackColor = Color.FromArgb(30,30,30),
        ForeColor = Color.FromArgb(235,235,235),
        HeaderBackColor = Color.FromArgb(38,38,42),
        HeaderForeColor = Color.White,
        GridColor = Color.FromArgb(58,58,64),
        AlternateBackColor = Color.FromArgb(35,35,39),
        SelectionBackColor = Color.FromArgb(76, 95, 140),
        SelectionForeColor = Color.White,
        HotBackColor = Color.FromArgb(45,52,64),
        AccentColor = Color.FromArgb(128,104,176),
        BorderColor = Color.FromArgb(70,70,76),
        EmptyTextColor = Color.FromArgb(170,170,170),
        MutedForeColor = Color.FromArgb(145, 145, 150),
        PanelBackColor = Color.FromArgb(36,36,40),
        ControlBackColor = Color.FromArgb(42,42,46),
        SelectionGlowColor = Color.FromArgb(90, 128,104,176),
        IsDark = true
    };

    public static ViewGridTheme FluentLightTheme()
    {
        var t = LightTheme();
        t.UseFluentBackdrop = true;
        t.UseAcrylicEffect = true;
        t.UseAnimatedSelection = true;
        t.CornerRadius = 10;
        t.AcrylicOpacity = 242;
        t.PanelBackColor = Color.FromArgb(248, 249, 252);
        t.ControlBackColor = Color.FromArgb(252, 253, 255);
        t.SelectionGlowColor = Color.FromArgb(70, 0, 120, 215);
        return t;
    }

    public static ViewGridTheme FluentDarkTheme()
    {
        var t = DarkTheme();
        t.UseFluentBackdrop = true;
        t.UseAcrylicEffect = true;
        t.UseAnimatedSelection = true;
        t.CornerRadius = 10;
        t.AcrylicOpacity = 230;
        t.PanelBackColor = Color.FromArgb(34, 34, 38);
        t.ControlBackColor = Color.FromArgb(40, 40, 45);
        t.SelectionGlowColor = Color.FromArgb(100, 128, 104, 176);
        return t;
    }

    public static ViewGridTheme FromParentColor(Color parentBackColor, Color parentForeColor)
    {
        if (parentBackColor == Color.Empty || parentBackColor == Color.Transparent)
            return WindowsThemeService.CurrentTheme();

        double luminance = (0.299 * parentBackColor.R + 0.587 * parentBackColor.G + 0.114 * parentBackColor.B) / 255d;
        bool dark = luminance < 0.45d;
        var t = dark ? DarkTheme() : LightTheme();

        t.BackColor = Blend(parentBackColor, dark ? Color.Black : Color.White, dark ? 0.08 : 0.04);
        t.PanelBackColor = Blend(parentBackColor, dark ? Color.Black : Color.White, dark ? 0.02 : 0.02);
        t.ControlBackColor = t.BackColor;
        t.HeaderBackColor = Blend(parentBackColor, dark ? Color.White : Color.Black, dark ? 0.08 : 0.06);
        t.AlternateBackColor = Blend(t.BackColor, dark ? Color.White : Color.Black, dark ? 0.04 : 0.025);
        t.HotBackColor = Blend(t.BackColor, dark ? Color.White : Color.Black, dark ? 0.10 : 0.06);
        t.GridColor = Blend(parentBackColor, dark ? Color.White : Color.Black, dark ? 0.18 : 0.14);
        t.BorderColor = Blend(parentBackColor, dark ? Color.White : Color.Black, dark ? 0.24 : 0.18);

        var fore = parentForeColor == Color.Empty || parentForeColor == Color.Transparent
            ? (dark ? Color.White : Color.Black)
            : parentForeColor;
        t.ForeColor = fore;
        t.HeaderForeColor = fore;
        t.MutedForeColor = Blend(fore, parentBackColor, 0.45);
        t.EmptyTextColor = Blend(fore, parentBackColor, 0.38);
        t.AccentColor = dark ? Color.FromArgb(120, 150, 230) : Color.FromArgb(0, 120, 215);
        t.SelectionBackColor = dark ? Color.FromArgb(90, 105, 155) : Color.FromArgb(0, 120, 215);
        t.SelectionForeColor = Color.White;
        t.SelectionGlowColor = Color.FromArgb(dark ? 110 : 70, t.SelectionBackColor);
        t.IsDark = dark;
        return ViewGridThemeAccessibility.Normalize(t);
    }

    private static Color Blend(Color first, Color second, double amount)
    {
        amount = Math.Max(0, Math.Min(1, amount));
        int r = (int)Math.Round(first.R + (second.R - first.R) * amount);
        int g = (int)Math.Round(first.G + (second.G - first.G) * amount);
        int b = (int)Math.Round(first.B + (second.B - first.B) * amount);
        return Color.FromArgb(r, g, b);
    }
    public static ViewGridTheme FromPreset(ViewGridThemePreset preset) => ViewGridThemeAccessibility.Normalize(preset switch
    {
        ViewGridThemePreset.System => WindowsThemeService.CurrentTheme(),
        ViewGridThemePreset.Light => LightTheme(),
        ViewGridThemePreset.Dark => DarkTheme(),
        ViewGridThemePreset.FluentLight => FluentLightTheme(),
        ViewGridThemePreset.FluentDark => FluentDarkTheme(),
        ViewGridThemePreset.MidnightPurple => MidnightPurpleTheme(),
        ViewGridThemePreset.SlateBlue => SlateBlueTheme(),
        ViewGridThemePreset.Graphite => GraphiteTheme(),
        ViewGridThemePreset.Emerald => EmeraldTheme(),
        ViewGridThemePreset.WarmSand => WarmSandTheme(),
        ViewGridThemePreset.Ocean => OceanTheme(),
        ViewGridThemePreset.RoseQuartz => RoseQuartzTheme(),
        ViewGridThemePreset.CyberNeon => CyberNeonTheme(),
        ViewGridThemePreset.Nord => NordTheme(),
        ViewGridThemePreset.Olive => OliveTheme(),
        ViewGridThemePreset.Steel => SteelTheme(),
        _ => WindowsThemeService.CurrentTheme()
    });

    public static ViewGridTheme MidnightPurpleTheme()
    {
        var t = DarkTheme();
        t.BackColor = Color.FromArgb(22, 20, 28);
        t.HeaderBackColor = Color.FromArgb(35, 30, 48);
        t.AlternateBackColor = Color.FromArgb(27, 24, 34);
        t.HotBackColor = Color.FromArgb(44, 37, 62);
        t.SelectionBackColor = Color.FromArgb(128, 104, 176);
        t.AccentColor = Color.FromArgb(156, 124, 218);
        t.BorderColor = Color.FromArgb(72, 62, 95);
        t.PanelBackColor = Color.FromArgb(28, 24, 36);
        t.ControlBackColor = Color.FromArgb(34, 29, 44);
        t.SelectionGlowColor = Color.FromArgb(115, 156, 124, 218);
        t.UseAnimatedSelection = true;
        return t;
    }

    public static ViewGridTheme SlateBlueTheme()
    {
        var t = DarkTheme();
        t.BackColor = Color.FromArgb(24, 29, 38);
        t.HeaderBackColor = Color.FromArgb(31, 39, 52);
        t.AlternateBackColor = Color.FromArgb(28, 34, 44);
        t.HotBackColor = Color.FromArgb(40, 52, 70);
        t.SelectionBackColor = Color.FromArgb(64, 112, 180);
        t.AccentColor = Color.FromArgb(84, 146, 230);
        t.BorderColor = Color.FromArgb(58, 72, 94);
        t.PanelBackColor = Color.FromArgb(28, 34, 44);
        t.ControlBackColor = Color.FromArgb(34, 42, 55);
        t.SelectionGlowColor = Color.FromArgb(95, 84, 146, 230);
        return t;
    }

    public static ViewGridTheme GraphiteTheme()
    {
        var t = DarkTheme();
        t.BackColor = Color.FromArgb(26, 27, 29);
        t.HeaderBackColor = Color.FromArgb(39, 40, 43);
        t.AlternateBackColor = Color.FromArgb(31, 32, 34);
        t.HotBackColor = Color.FromArgb(48, 50, 54);
        t.SelectionBackColor = Color.FromArgb(86, 92, 102);
        t.AccentColor = Color.FromArgb(170, 174, 184);
        t.BorderColor = Color.FromArgb(70, 72, 78);
        t.PanelBackColor = Color.FromArgb(32, 33, 36);
        t.ControlBackColor = Color.FromArgb(38, 39, 42);
        t.SelectionGlowColor = Color.FromArgb(85, 170, 174, 184);
        return t;
    }

    public static ViewGridTheme EmeraldTheme()
    {
        var t = LightTheme();
        t.BackColor = Color.FromArgb(250, 253, 251);
        t.HeaderBackColor = Color.FromArgb(236, 248, 242);
        t.AlternateBackColor = Color.FromArgb(244, 251, 247);
        t.HotBackColor = Color.FromArgb(225, 244, 235);
        t.SelectionBackColor = Color.FromArgb(28, 132, 88);
        t.AccentColor = Color.FromArgb(18, 148, 96);
        t.BorderColor = Color.FromArgb(188, 220, 204);
        t.PanelBackColor = Color.FromArgb(244, 251, 247);
        t.ControlBackColor = Color.White;
        t.SelectionGlowColor = Color.FromArgb(65, 18, 148, 96);
        return t;
    }

    public static ViewGridTheme WarmSandTheme()
    {
        var t = LightTheme();
        t.BackColor = Color.FromArgb(255, 252, 246);
        t.HeaderBackColor = Color.FromArgb(250, 240, 224);
        t.AlternateBackColor = Color.FromArgb(255, 248, 238);
        t.HotBackColor = Color.FromArgb(250, 233, 208);
        t.SelectionBackColor = Color.FromArgb(190, 118, 45);
        t.AccentColor = Color.FromArgb(212, 132, 54);
        t.BorderColor = Color.FromArgb(226, 204, 174);
        t.PanelBackColor = Color.FromArgb(255, 248, 238);
        t.ControlBackColor = Color.White;
        t.SelectionGlowColor = Color.FromArgb(70, 212, 132, 54);
        return t;
    }

    public static ViewGridTheme OceanTheme()
    {
        var t = DarkTheme();
        t.BackColor = Color.FromArgb(12, 30, 42);
        t.HeaderBackColor = Color.FromArgb(18, 46, 64);
        t.AlternateBackColor = Color.FromArgb(15, 36, 50);
        t.HotBackColor = Color.FromArgb(24, 64, 86);
        t.SelectionBackColor = Color.FromArgb(0, 132, 176);
        t.AccentColor = Color.FromArgb(0, 180, 216);
        t.BorderColor = Color.FromArgb(42, 88, 108);
        t.PanelBackColor = Color.FromArgb(14, 36, 50);
        t.ControlBackColor = Color.FromArgb(18, 44, 60);
        t.SelectionGlowColor = Color.FromArgb(100, 0, 180, 216);
        return t;
    }

    public static ViewGridTheme RoseQuartzTheme()
    {
        var t = LightTheme();
        t.BackColor = Color.FromArgb(255, 249, 251);
        t.HeaderBackColor = Color.FromArgb(252, 232, 240);
        t.AlternateBackColor = Color.FromArgb(255, 243, 247);
        t.HotBackColor = Color.FromArgb(249, 221, 233);
        t.SelectionBackColor = Color.FromArgb(188, 80, 122);
        t.AccentColor = Color.FromArgb(214, 90, 138);
        t.BorderColor = Color.FromArgb(230, 190, 205);
        t.PanelBackColor = Color.FromArgb(255, 243, 247);
        t.ControlBackColor = Color.White;
        t.SelectionGlowColor = Color.FromArgb(70, 214, 90, 138);
        return t;
    }

    public static ViewGridTheme CyberNeonTheme()
    {
        var t = DarkTheme();
        t.BackColor = Color.FromArgb(10, 12, 20);
        t.HeaderBackColor = Color.FromArgb(18, 22, 36);
        t.AlternateBackColor = Color.FromArgb(14, 18, 30);
        t.HotBackColor = Color.FromArgb(28, 36, 58);
        t.SelectionBackColor = Color.FromArgb(110, 62, 230);
        t.AccentColor = Color.FromArgb(0, 220, 190);
        t.BorderColor = Color.FromArgb(56, 66, 95);
        t.PanelBackColor = Color.FromArgb(14, 18, 30);
        t.ControlBackColor = Color.FromArgb(18, 23, 38);
        t.SelectionGlowColor = Color.FromArgb(110, 0, 220, 190);
        return t;
    }

    public static ViewGridTheme NordTheme()
    {
        var t = DarkTheme();
        t.BackColor = Color.FromArgb(36, 41, 51);
        t.HeaderBackColor = Color.FromArgb(46, 52, 64);
        t.AlternateBackColor = Color.FromArgb(40, 46, 58);
        t.HotBackColor = Color.FromArgb(59, 66, 82);
        t.SelectionBackColor = Color.FromArgb(94, 129, 172);
        t.AccentColor = Color.FromArgb(136, 192, 208);
        t.BorderColor = Color.FromArgb(76, 86, 106);
        t.PanelBackColor = Color.FromArgb(40, 46, 58);
        t.ControlBackColor = Color.FromArgb(46, 52, 64);
        t.SelectionGlowColor = Color.FromArgb(90, 136, 192, 208);
        return t;
    }

    public static ViewGridTheme OliveTheme()
    {
        var t = LightTheme();
        t.BackColor = Color.FromArgb(250, 252, 244);
        t.HeaderBackColor = Color.FromArgb(235, 242, 218);
        t.AlternateBackColor = Color.FromArgb(245, 249, 235);
        t.HotBackColor = Color.FromArgb(225, 237, 200);
        t.SelectionBackColor = Color.FromArgb(105, 137, 56);
        t.AccentColor = Color.FromArgb(128, 158, 72);
        t.BorderColor = Color.FromArgb(202, 218, 172);
        t.PanelBackColor = Color.FromArgb(245, 249, 235);
        t.ControlBackColor = Color.White;
        t.SelectionGlowColor = Color.FromArgb(70, 128, 158, 72);
        return t;
    }

    public static ViewGridTheme SteelTheme()
    {
        var t = LightTheme();
        t.BackColor = Color.FromArgb(247, 249, 252);
        t.HeaderBackColor = Color.FromArgb(226, 232, 240);
        t.AlternateBackColor = Color.FromArgb(241, 245, 249);
        t.HotBackColor = Color.FromArgb(219, 234, 254);
        t.SelectionBackColor = Color.FromArgb(51, 85, 139);
        t.AccentColor = Color.FromArgb(59, 130, 246);
        t.BorderColor = Color.FromArgb(203, 213, 225);
        t.PanelBackColor = Color.FromArgb(241, 245, 249);
        t.ControlBackColor = Color.White;
        t.SelectionGlowColor = Color.FromArgb(70, 59, 130, 246);
        return t;
    }

}

