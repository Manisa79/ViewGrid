using ViewGrid.Theming;

namespace ViewGrid.Core;

public partial class ViewGridControl
{
    [Category("ViewGrid - V35 Theme Studio")]
    [DefaultValue(false)]
    [Description("Theme Studio preset ve canlı önizleme akışını aktif eder.")]
    public bool EnableThemeStudio { get; set; }

    [Category("ViewGrid - V35 Theme Studio")]
    [DefaultValue(ViewGridThemeStudioPreset.FactoryOsDark)]
    [Description("ViewGrid Theme Studio içinde seçili olan hazır tema paleti.")]
    public ViewGridThemeStudioPreset ThemeStudioPreset { get; set; } = ViewGridThemeStudioPreset.FactoryOsDark;

    [Category("ViewGrid - V35 Theme Studio")]
    [DefaultValue(true)]
    [Description("Theme Studio preset uygulanırken v33 erişilebilirlik normalizasyonunu zorunlu tutar.")]
    public bool ThemeStudioEnforceAccessibility { get; set; } = true;

    public void ApplyThemeStudioPreset(ViewGridThemeStudioPreset preset)
    {
        EnableThemeStudio = true;
        ThemeStudioPreset = preset;
        var theme = ViewGridThemeStudio.Create(preset);
        if (ThemeStudioEnforceAccessibility)
            theme = ViewGridThemeAccessibility.Normalize(theme);
        ApplyTheme(theme);
    }

    public IReadOnlyList<ViewGridThemeStudioPalette> GetThemeStudioPalettes()
    {
        return ViewGridThemeStudio.BuiltInPalettes();
    }

    public string ExportCurrentThemeStudioPalette(string name = "Current")
    {
        return ViewGridThemeStudio.ExportPaletteText(_theme, name);
    }

    public void ApplyV35ThemeStudioPack(ViewGridThemeStudioPreset preset = ViewGridThemeStudioPreset.FactoryOsDark)
    {
        EnableThemeStudio = true;
        ThemeStudioEnforceAccessibility = true;
        EnforceThemeAccessibility = true;
        AutoEnsureReadableTextColors = true;
        ApplyThemeStudioPreset(preset);
    }
}
