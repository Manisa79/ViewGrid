using System.Drawing;

namespace ViewGrid.Theming;

public enum ViewGridThemeStudioPreset
{
    Light,
    Dark,
    HighContrastDark,
    HighContrastLight,
    AudixDark,
    AudixLight,
    AoiSupportDark,
    AoiSupportLight,
    FactoryOsDark,
    FactoryOsLight,
    SmokeWhite,
    MidnightGlass
}

public sealed class ViewGridThemeStudioPalette
{
    public string Name { get; set; } = string.Empty;
    public ViewGridThemeStudioPreset Preset { get; set; }
    public ViewGridTheme Theme { get; set; } = ViewGridTheme.LightTheme();
    public string Notes { get; set; } = string.Empty;
}

public static class ViewGridThemeStudio
{
    public static ViewGridTheme Create(ViewGridThemeStudioPreset preset)
    {
        ViewGridTheme theme = preset switch
        {
            ViewGridThemeStudioPreset.Dark => ViewGridTheme.DarkTheme(),
            ViewGridThemeStudioPreset.HighContrastDark => HighContrastDark(),
            ViewGridThemeStudioPreset.HighContrastLight => HighContrastLight(),
            ViewGridThemeStudioPreset.AudixDark => AudixDark(),
            ViewGridThemeStudioPreset.AudixLight => AudixLight(),
            ViewGridThemeStudioPreset.AoiSupportDark => AoiSupportDark(),
            ViewGridThemeStudioPreset.AoiSupportLight => AoiSupportLight(),
            ViewGridThemeStudioPreset.FactoryOsDark => FactoryOsDark(),
            ViewGridThemeStudioPreset.FactoryOsLight => FactoryOsLight(),
            ViewGridThemeStudioPreset.SmokeWhite => SmokeWhite(),
            ViewGridThemeStudioPreset.MidnightGlass => MidnightGlass(),
            _ => ViewGridTheme.LightTheme()
        };
        return ViewGridThemeAccessibility.Normalize(theme);
    }

    public static IReadOnlyList<ViewGridThemeStudioPalette> BuiltInPalettes()
    {
        return Enum.GetValues(typeof(ViewGridThemeStudioPreset))
            .Cast<ViewGridThemeStudioPreset>()
            .Select(p => new ViewGridThemeStudioPalette
            {
                Name = p.ToString(),
                Preset = p,
                Theme = Create(p),
                Notes = GetNotes(p)
            })
            .ToArray();
    }

    public static string ExportPaletteText(ViewGridTheme theme, string name = "Custom")
    {
        theme = ViewGridThemeAccessibility.Normalize(theme);
        return string.Join(Environment.NewLine, new[]
        {
            "ViewGrid Theme Studio Palette: " + name,
            "BackColor=" + ToHex(theme.BackColor),
            "ForeColor=" + ToHex(theme.ForeColor),
            "PanelBackColor=" + ToHex(theme.PanelBackColor),
            "ControlBackColor=" + ToHex(theme.ControlBackColor),
            "HeaderBackColor=" + ToHex(theme.HeaderBackColor),
            "HeaderForeColor=" + ToHex(theme.HeaderForeColor),
            "AccentColor=" + ToHex(theme.AccentColor),
            "BorderColor=" + ToHex(theme.BorderColor),
            "SelectionBackColor=" + ToHex(theme.SelectionBackColor),
            "SelectionForeColor=" + ToHex(theme.SelectionForeColor),
            "MutedForeColor=" + ToHex(theme.MutedForeColor),
            "EmptyTextColor=" + ToHex(theme.EmptyTextColor),
            "IsDark=" + theme.IsDark
        });
    }

    private static ViewGridTheme HighContrastDark()
    {
        var t = ViewGridTheme.DarkTheme();
        t.BackColor = Color.FromArgb(6, 10, 16);
        t.PanelBackColor = Color.FromArgb(12, 20, 31);
        t.ControlBackColor = Color.FromArgb(18, 30, 45);
        t.HeaderBackColor = Color.FromArgb(16, 27, 40);
        t.ForeColor = Color.White;
        t.HeaderForeColor = Color.White;
        t.AccentColor = Color.FromArgb(72, 196, 255);
        t.SelectionBackColor = Color.FromArgb(0, 104, 180);
        t.BorderColor = Color.FromArgb(76, 102, 132);
        t.IsDark = true;
        return t;
    }

    private static ViewGridTheme HighContrastLight()
    {
        var t = ViewGridTheme.LightTheme();
        t.BackColor = Color.White;
        t.PanelBackColor = Color.FromArgb(246, 248, 252);
        t.ControlBackColor = Color.White;
        t.HeaderBackColor = Color.FromArgb(235, 240, 248);
        t.ForeColor = Color.FromArgb(10, 18, 28);
        t.HeaderForeColor = t.ForeColor;
        t.AccentColor = Color.FromArgb(0, 86, 160);
        t.SelectionBackColor = Color.FromArgb(0, 104, 180);
        t.SelectionForeColor = Color.White;
        t.BorderColor = Color.FromArgb(114, 132, 154);
        t.IsDark = false;
        return t;
    }

    private static ViewGridTheme AudixDark()
    {
        var t = HighContrastDark();
        t.BackColor = Color.FromArgb(10, 14, 24);
        t.PanelBackColor = Color.FromArgb(16, 23, 38);
        t.ControlBackColor = Color.FromArgb(23, 33, 53);
        t.AccentColor = Color.FromArgb(116, 92, 255);
        t.SelectionBackColor = Color.FromArgb(98, 76, 210);
        return t;
    }

    private static ViewGridTheme AudixLight()
    {
        var t = HighContrastLight();
        t.BackColor = Color.FromArgb(250, 251, 255);
        t.PanelBackColor = Color.FromArgb(242, 245, 252);
        t.ControlBackColor = Color.White;
        t.AccentColor = Color.FromArgb(96, 70, 210);
        t.SelectionBackColor = Color.FromArgb(96, 70, 210);
        return t;
    }

    private static ViewGridTheme AoiSupportDark()
    {
        var t = HighContrastDark();
        t.BackColor = Color.FromArgb(8, 18, 28);
        t.PanelBackColor = Color.FromArgb(12, 31, 45);
        t.ControlBackColor = Color.FromArgb(18, 43, 61);
        t.AccentColor = Color.FromArgb(46, 204, 144);
        t.SelectionBackColor = Color.FromArgb(0, 132, 92);
        return t;
    }

    private static ViewGridTheme AoiSupportLight()
    {
        var t = HighContrastLight();
        t.BackColor = Color.FromArgb(247, 251, 249);
        t.PanelBackColor = Color.FromArgb(236, 247, 242);
        t.AccentColor = Color.FromArgb(0, 126, 84);
        t.SelectionBackColor = Color.FromArgb(0, 126, 84);
        return t;
    }

    private static ViewGridTheme FactoryOsDark()
    {
        var t = HighContrastDark();
        t.BackColor = Color.FromArgb(7, 18, 31);
        t.PanelBackColor = Color.FromArgb(11, 28, 46);
        t.ControlBackColor = Color.FromArgb(17, 40, 64);
        t.AccentColor = Color.FromArgb(255, 183, 77);
        t.SelectionBackColor = Color.FromArgb(174, 105, 24);
        return t;
    }

    private static ViewGridTheme FactoryOsLight()
    {
        var t = HighContrastLight();
        t.BackColor = Color.FromArgb(249, 250, 252);
        t.PanelBackColor = Color.FromArgb(240, 244, 248);
        t.AccentColor = Color.FromArgb(196, 104, 24);
        t.SelectionBackColor = Color.FromArgb(196, 104, 24);
        return t;
    }

    private static ViewGridTheme SmokeWhite()
    {
        var t = HighContrastLight();
        t.BackColor = Color.FromArgb(245, 247, 250);
        t.PanelBackColor = Color.FromArgb(238, 242, 246);
        t.ControlBackColor = Color.FromArgb(252, 253, 255);
        t.HeaderBackColor = Color.FromArgb(232, 237, 244);
        t.AccentColor = Color.FromArgb(0, 120, 215);
        return t;
    }

    private static ViewGridTheme MidnightGlass()
    {
        var t = HighContrastDark();
        t.BackColor = Color.FromArgb(9, 12, 22);
        t.PanelBackColor = Color.FromArgb(15, 20, 34);
        t.ControlBackColor = Color.FromArgb(24, 31, 50);
        t.HeaderBackColor = Color.FromArgb(18, 25, 42);
        t.AccentColor = Color.FromArgb(120, 150, 255);
        return t;
    }

    private static string GetNotes(ViewGridThemeStudioPreset preset) => preset switch
    {
        ViewGridThemeStudioPreset.AudixDark or ViewGridThemeStudioPreset.AudixLight => "Albüm kapağı, poster, media tile ve filmstrip için yüksek okunurluk.",
        ViewGridThemeStudioPreset.AoiSupportDark or ViewGridThemeStudioPreset.AoiSupportLight => "Ticket kartları, durum rozeti ve operatör/teknisyen mesajları için.",
        ViewGridThemeStudioPreset.FactoryOsDark or ViewGridThemeStudioPreset.FactoryOsLight => "Line Workspace, Factory Navigator ve üretim dashboardları için.",
        ViewGridThemeStudioPreset.SmokeWhite => "NavixBar ve açık panel yüzeyleri ile yumuşak uyum.",
        _ => "Genel ViewGrid tema paleti."
    };

    private static string ToHex(Color color) => "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
}
